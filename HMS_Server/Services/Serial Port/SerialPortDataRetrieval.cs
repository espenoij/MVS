using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Text.RegularExpressions;
using Telerik.Windows.Data;

namespace HMS_Server
{
    class SerialPortDataRetrieval
    {
        // Configuration
        Config config;

        // Database
        DatabaseHandler database;

        RadObservableCollection<SensorData> sensorDataList;

        // Serial Port: List
        private List<SerialPort> serialPortList = new List<SerialPort>();

        // Serial Port: Liste hvor data fra serie portene lagres
        private List<SerialPortData> serialPortDataReceivedList = new List<SerialPortData>();

        // Error Handler
        private ErrorHandler errorHandler;

        // Admin Settings
        private AdminSettingsVM adminSettingsVM;

        public SerialPortDataRetrieval(Config config, RadObservableCollection<SensorData> sensorDataList, DatabaseHandler database, ErrorHandler errorHandler, AdminSettingsVM adminSettingsVM)
        {
            this.config = config;
            this.database = database;
            this.sensorDataList = sensorDataList;

            this.errorHandler = errorHandler;
            this.adminSettingsVM = adminSettingsVM;
        }

        public void Load(SensorData sensorData)
        {
            // Sjekke om serie port er lagt inn/åpnet fra før
            SerialPort sp = serialPortList.Find(x => x.PortName == sensorData.serialPort.portName);

            // Dersom den ikke eksisterer -> så legger vi den inn i serie port listen...
            if (sp == null)
            {
                // Sette serie port settings på SerialPort object
                SerialPort serialPort = new SerialPort();

                serialPort.PortName = sensorData.serialPort.portName;
                serialPort.BaudRate = sensorData.serialPort.baudRate;
                serialPort.DataBits = sensorData.serialPort.dataBits;
                serialPort.StopBits = sensorData.serialPort.stopBits;
                serialPort.Parity = sensorData.serialPort.parity;
                serialPort.Handshake = sensorData.serialPort.handshake;

                // Koble opp metode for å motta data
                serialPort.DataReceived += new SerialDataReceivedEventHandler(DataReceived);

                // Legge inn i serie port listen
                serialPortList.Add(serialPort);

                // Opprette enhet i data mottaks listen også
                SerialPortData dataReceived = new SerialPortData();
                dataReceived.portName = serialPort.PortName;
                dataReceived.firstRead = true;
                serialPortDataReceivedList.Add(dataReceived);
            }
        }

        private void DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            SerialPort serialPort = sender as SerialPort;

            // Lese fra port
            string inputData = serialPort?.ReadExisting();

            // Finne frem hvor vi skal lagre lest data fra serie port
            SerialPortData serialPortData = serialPortDataReceivedList.Find(x => x.portName == serialPort.PortName);
            if (serialPortData != null)
            {
                if (serialPortData.firstRead)
                {
                    // Dump inn buffer ved første lesing - gjør ingenting
                    serialPortData.firstRead = false;
                }
                else
                {
                    // Lagre data
                    serialPortData.data = TextHelper.EscapeControlChars(inputData);
                    serialPortData.timestamp = DateTime.UtcNow;

                    // Oppdatere status
                    if (!string.IsNullOrEmpty(inputData))
                    {
                        serialPortData.portStatus = PortStatus.Reading;
                    }

                    // Prosessere data
                    DataProcessing(serialPortData);
                }
            }
        }

        public void Start()
        {
            foreach (var item in serialPortDataReceivedList)
            {
                // Resette firstRead variablene
                item.firstRead = true;

                // Resette time stamp
                item.timestamp = DateTime.UtcNow;
            }

            // Starte serial ports
            foreach (var item in serialPortList)
            {
                if (!item.IsOpen)
                {
                    try
                    {
                        item.Open();
                    }
                    catch (Exception ex)
                    {
                        // Sette feilmelding
                        errorHandler.Insert(
                            new ErrorMessage(
                                DateTime.UtcNow,
                                ErrorMessageType.SerialPort,
                                ErrorMessageCategory.AdminUser,
                                string.Format("Error opening serial port: {0} (Start), System Message: {1}", item.PortName, ex.Message)));

                        // Endre status
                        SerialPortData serialPortData = serialPortDataReceivedList.Find(x => x.portName == item.PortName);
                        if (serialPortData != null)
                            serialPortData.portStatus = PortStatus.OpenError;
                    }
                }
            }
        }

        public void Stop()
        {
            // Stoppe serial ports
            foreach (var serialPort in serialPortList)
                if (serialPort != null)
                    if (serialPort.IsOpen)
                        serialPort.Close();
        }

        public void Restart(string portName)
        {
            // Restarter en port
            SerialPort serialPort = serialPortList.Find(x => x.PortName == portName);
            if (serialPort != null)
            {
                try
                {
                    // Lukke...
                    serialPort.Close();

                    // ...og åpne igjen
                    serialPort.Open();
                }
                catch (Exception ex)
                {
                    // Sette feilmelding
                    errorHandler.Insert(
                        new ErrorMessage(
                            DateTime.UtcNow,
                            ErrorMessageType.SerialPort,
                            ErrorMessageCategory.AdminUser,
                            string.Format("Error restarting serial port: {0} (Restart), System Message: {1}", serialPort.PortName, ex.Message)));
                }
            }
        }

        private void DataProcessing(SerialPortData serialPortData)
        {
            // Har vi data?
            if (!string.IsNullOrEmpty(serialPortData.data))
            {
                //  Trinn 1: Finne alle sensorer som er satt opp på den aktuelle serie porten og prosessere
                foreach (var sensorData in sensorDataList)
                {
                    if (sensorData.type == SensorType.SerialPort)
                    {
                        if (sensorData.serialPort.portName == serialPortData.portName)
                        {
                            // Data Processing
                            SerialPortProcessing process = new SerialPortProcessing();

                            try
                            {
                                // Init processing
                                process.inputType = sensorData.serialPort.inputType;
                                process.binaryType = sensorData.serialPort.binaryType;
                                process.packetHeader = sensorData.serialPort.packetHeader;
                                process.packetEnd = sensorData.serialPort.packetEnd;
                                process.packetDelimiter = Regex.Unescape(sensorData.serialPort.packetDelimiter);
                                process.packetCombineFields = sensorData.serialPort.packetCombineFields;
                                process.fixedPosData = sensorData.serialPort.fixedPosData;
                                process.fixedPosStart = sensorData.serialPort.fixedPosStart;
                                process.fixedPosTotal = sensorData.serialPort.fixedPosTotal;
                                process.dataField = Convert.ToInt32(sensorData.serialPort.dataField);
                                process.decimalSeparator = sensorData.serialPort.decimalSeparator;
                                process.autoExtractValue = sensorData.serialPort.autoExtractValue;

                                // Trinn 2: Prosessere raw data, finne pakker
                                List<string> incomingPackets = process.FindSelectedPackets(serialPortData.data);

                                // Prosessere pakkene som ble funnet
                                foreach (var packet in incomingPackets)
                                {
                                    // Trinn 3: Finne datafeltene i pakke
                                    PacketDataFields packetDataFields = process.FindPacketDataFields(packet);

                                    // Trinn 4: Prosessere valgt datafelt
                                    SelectedDataField selectedData = process.FindSelectedDataInPacket(packetDataFields);

                                    // Trinn 5: Utføre kalkulasjoner på utvalgt datafelt
                                    CalculatedData calculatedData = process.ApplyCalculationsToSelectedData(selectedData, sensorData.dataCalculations, serialPortData.timestamp, errorHandler, ErrorMessageCategory.None, adminSettingsVM);

                                    // Lagre resultat
                                    sensorData.timestamp = serialPortData.timestamp;
                                    sensorData.data = calculatedData.data;

                                    // Lagre til databasen
                                    if (sensorData.saveToDatabase && sensorData.saveFreq == DatabaseSaveFrequency.Sensor)
                                    {
                                        // Legger ikke inn data dersom data ikke er satt
                                        if (!double.IsNaN(sensorData.data))
                                        {
                                            try
                                            {
                                                database.Insert(sensorData);

                                                errorHandler.ResetDatabaseError(ErrorHandler.DatabaseErrorType.Insert5);
                                            }
                                            catch (Exception ex)
                                            {
                                                errorHandler.Insert(
                                                    new ErrorMessage(
                                                        DateTime.UtcNow,
                                                        ErrorMessageType.Database,
                                                        ErrorMessageCategory.None,
                                                        string.Format("Database Error (Insert 3)\n\nSystem Message:\n{0}", ex.Message),
                                                        sensorData.id));

                                                errorHandler.SetDatabaseError(ErrorHandler.DatabaseErrorType.Insert5);
                                            }
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                // Stte feilmelding
                                errorHandler.Insert(
                                    new ErrorMessage(
                                        DateTime.UtcNow,
                                        ErrorMessageType.SerialPort,
                                        ErrorMessageCategory.AdminUser,
                                        string.Format("DataProcessing error, System Message: {0}", ex.Message),
                                        sensorData.id));
                            }
                        }
                    }
                }
            }
        }

        public List<SerialPortData> GetSerialPortDataReceivedList()
        {
            return serialPortDataReceivedList;
        }

        public List<SerialPort> GetSerialPortList()
        {
            return serialPortList;
        }

        public SerialPort GetSerialPort(string portName)
        {
            return serialPortList.Find(x => x.PortName == portName);
        }

        public void Clear()
        {
            serialPortList.Clear();
            serialPortDataReceivedList.Clear();
        }
    }
}
