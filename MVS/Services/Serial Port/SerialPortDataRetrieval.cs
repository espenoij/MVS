using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Threading;
using Telerik.Windows.Data;

namespace MVS
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

        // Inn-data buffer
        //private string inputDataBuffer;

        private DispatcherTimer sensorProcessingTimer = new DispatcherTimer();

        public SerialPortDataRetrieval(Config config, RadObservableCollection<SensorData> sensorDataList, DatabaseHandler database, ErrorHandler errorHandler, AdminSettingsVM adminSettingsVM)
        {
            this.config = config;
            this.database = database;
            this.sensorDataList = sensorDataList;

            this.errorHandler = errorHandler;
            this.adminSettingsVM = adminSettingsVM;

            sensorProcessingTimer.Interval = TimeSpan.FromMilliseconds(config.ReadWithDefault(ConfigKey.HMSProcessingFrequency, Constants.HMSProcessingFrequencyDefault));
            sensorProcessingTimer.Tick += runSensorProcessing;
            sensorProcessingTimer.Start();

            void runSensorProcessing(object sender, EventArgs e)
            {
                DataProcessing();
            }
        }

        public void Load(SensorData sensorData, MainWindowVM mainWindowVM)
        {
            // Sjekke om denne sensoren skal brukes først
            if (sensorData.UseThisSensor(mainWindowVM))
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

                    // Timeout
                    // Default timeout er: public const int InfiniteTimeout = -1;
                    //serialPort.ReadTimeout = Constants.SerialPortTimeout;
                    //serialPort.WriteTimeout = Constants.SerialPortTimeout;

                    serialPort.DtrEnable = true;    // Data-terminal-ready
                    serialPort.RtsEnable = true;    // Request-to-send

                    // Koble opp metode for å motta data
                    serialPort.DataReceived += new SerialDataReceivedEventHandler(DataReceived);

                    // Legge inn i serie port listen
                    serialPortList.Add(serialPort);

                    // Opprette enhet i data mottakslisten også
                    SerialPortData dataReceived = new SerialPortData();
                    dataReceived.portName = serialPort.PortName;
                    dataReceived.firstRead = true;
                    serialPortDataReceivedList.Add(dataReceived);
                }
            }
        }

        private void DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                SerialPort serialPort = sender as SerialPort;

                // Finne frem hvor vi skal lagre lest data fra serie port
                SerialPortData serialPortData = serialPortDataReceivedList.Find(x => x.portName == serialPort.PortName);
                if (serialPortData != null)
                {
                    if (serialPortData.firstRead)
                    {
                        // Dump inn buffer ved første lesing - gjør ingenting
                        // 2023.02.21: Vurder å fjerne denne delen. Har en funksjon i setup delen, men er unødvendig her.
                        serialPort?.ReadExisting();
                        serialPortData.firstRead = false;
                    }
                    else
                    {
                        // Lese fra port
                        byte[] array = new byte[serialPort.ReadBufferSize];
                        serialPort.Read(array, 0, serialPort.ReadBufferSize);

                        // Lagre data
                        serialPortData.buffer_text += Encoding.UTF8.GetString(array, 0, array.Length);
                        serialPortData.buffer_binary += BitConverter.ToString(array);
                        serialPortData.timestamp = DateTime.UtcNow;

                        // Oppdatere status
                        if (!string.IsNullOrEmpty(serialPortData.buffer_text) ||
                            !string.IsNullOrEmpty(serialPortData.buffer_binary))
                        {
                            serialPortData.portStatus = PortStatus.Reading;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                errorHandler.Insert(
                            new ErrorMessage(
                            DateTime.UtcNow,
                            ErrorMessageType.SerialPort,
                            ErrorMessageCategory.AdminUser,
                            string.Format("DataReceived Error: {0} (Start), System Message: {1}", (sender as SerialPort)?.PortName, ex.Message)));
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

            Thread thread = new Thread(() => runStart());
            thread.IsBackground = true;
            thread.Start();

            void runStart()
            {
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

                    // Større sjanse for suksess dersom alle serie porter ikke åpnes samtidig? 03.01.2024
                    Thread.Sleep(100);
                }

                sensorProcessingTimer.Start();
            }
        }

        public void Stop()
        {
            // Stoppe serial ports
            foreach (var serialPort in serialPortList)
            {
                try
                {
                    if (serialPort != null)
                        if (serialPort.IsOpen)
                            serialPort.Close();

                    sensorProcessingTimer.Stop();
                }
                catch (Exception ex)
                {
                    // Sette feilmelding
                    errorHandler.Insert(
                        new ErrorMessage(
                            DateTime.UtcNow,
                            ErrorMessageType.SerialPort,
                            ErrorMessageCategory.AdminUser,
                            string.Format("Error stopping serial port: {0} (Stop), System Message: {1}", serialPort.PortName, ex.Message)));
                }
            }
        }

        public void Restart(SerialPortData serialPortData)
        {
            // NB! Pass på å ikke kalle denne metoden for ofte.
            // Når CPU bruk overstiger 80% så vil nye tråder bli satt som pending av operativsystemet.

            Thread thread = new Thread(() => runRestart());
            thread.IsBackground = true;
            thread.Start();

            void runRestart()
            {
                SerialPort serialPort = serialPortList.Find(x => x.PortName == serialPortData.portName);

                // Restarter en port
                if (serialPort != null)
                {
                    try
                    {
                        // Lukke...
                        if (serialPort.IsOpen)
                            serialPort.Close();

                        // Bug i SerialPort klassen som gjør at Dispose og Close trenger tid på å lukkes ordentlig
                        Thread.Sleep(1000);

                        // ...og åpne igjen
                        if (!serialPort.IsOpen)
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

                serialPortData.restartTime = DateTime.UtcNow;
            }
        }

        private void DataProcessing()
        {
            foreach (var serialPortData in serialPortDataReceivedList)
            {
                // Har vi data?
                if (!string.IsNullOrEmpty(serialPortData.buffer_text) ||
                    !string.IsNullOrEmpty(serialPortData.buffer_binary))
                {
                    // Data Processing
                    SerialPortProcessing process = new SerialPortProcessing();

                    //  Trinn 1: Finne alle sensorer som er satt opp på den aktuelle serie porten og prosessere
                    foreach (var sensorData in sensorDataList)
                    {
                        if (sensorData.type == SensorType.SerialPort)
                        {
                            if (sensorData.serialPort.portName == serialPortData.portName)
                            {
                                try
                                {
                                    // Init processing
                                    process.inputType = sensorData.serialPort.inputType;
                                    process.binaryType = sensorData.serialPort.binaryType;
                                    process.packetHeader = Regex.Unescape(sensorData.serialPort.packetHeader);
                                    process.packetEnd = Regex.Unescape(sensorData.serialPort.packetEnd);
                                    process.packetDelimiter = Regex.Unescape(sensorData.serialPort.packetDelimiter);
                                    process.packetCombineFields = sensorData.serialPort.packetCombineFields;
                                    process.fixedPosData = sensorData.serialPort.fixedPosData;
                                    process.fixedPosStart = sensorData.serialPort.fixedPosStart;
                                    process.fixedPosTotal = sensorData.serialPort.fixedPosTotal;
                                    process.dataField = Convert.ToInt32(sensorData.serialPort.dataField);
                                    process.decimalSeparator = sensorData.serialPort.decimalSeparator;
                                    process.autoExtractValue = sensorData.serialPort.autoExtractValue;

                                    // Hente data string fra serialPort data
                                    string inputData = string.Empty;
                                    switch (process.inputType)
                                    {
                                        // Tekst input
                                        case InputDataType.Text:
                                            inputData = serialPortData.buffer_text;
                                            break;

                                        // Dersom binary input: Konvertere til binary
                                        case InputDataType.Binary:
                                            inputData = serialPortData.buffer_binary;
                                            break;
                                    }

                                    // Trinn 2: Prosessere raw data, finne pakker
                                    List<string> incomingPackets = process.FindSelectedPackets(inputData);

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

                    // Ferdig med prosessering - tømme buffer
                    serialPortData.buffer_text = string.Empty;
                    serialPortData.buffer_binary = string.Empty;
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
