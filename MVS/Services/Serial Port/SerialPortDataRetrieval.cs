﻿using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Text;
using System.Text.RegularExpressions;
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
                        int bytes = serialPort.BytesToRead;
                        byte[] array = new byte[bytes];
                        for (int i = 0; serialPort.BytesToRead > 0 && i < bytes; i++)
                        {
                            array[i] = Convert.ToByte(serialPort.ReadByte());
                        }

                        // Data er prosessert -> slette buffer
                        if (serialPortData.processingDone)
                        {
                            serialPortData.buffer_text = string.Empty;
                            serialPortData.buffer_binary = string.Empty;

                            serialPortData.processingDone = false;
                        }

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

            sensorProcessingTimer.Start();
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

                    //int lastHeaderPos;
                    //int lastEndPos;

                    //switch (process.inputType)
                    //{
                    //    case InputDataType.Text:
                    //        // Sjekk om vi skal ta vare på noe data eller om buffer skal slettes
                    //        lastHeaderPos = serialPortData.buffer_text.LastIndexOf(process.packetHeader);
                    //        lastEndPos = serialPortData.buffer_text.LastIndexOf(process.packetEnd);

                    //        // Dersom vi har en HEADER etter en END
                    //        if (lastHeaderPos > lastEndPos &&
                    //            lastHeaderPos != -1 &&
                    //            lastEndPos != -1 &&
                    //            !string.IsNullOrEmpty(process.packetHeader) &&
                    //            !string.IsNullOrEmpty(process.packetEnd))
                    //            // Lagre fra header (i tilfelle resten av packet kommer i neste sending fra serieport)
                    //            serialPortData.buffer_text = serialPortData.buffer_text.Substring(lastHeaderPos);
                    //        else
                    //            // Ingen HEADER etter END -> Slette buffer
                    //            serialPortData.buffer_text = String.Empty;

                    //        // Må også begrense hvor my data som skal leses i input buffer når vi ikke finner packets
                    //        // slik at den ikke fylles i det uendelige.
                    //        if (serialPortData.buffer_text.Count() > 4096) // 4KB limit per packet
                    //            serialPortData.buffer_text = String.Empty;

                    //        // Sletter binary buffer
                    //        serialPortData.buffer_binary = String.Empty;
                    //        break;

                    //    case InputDataType.Binary:
                    //        // Sjekk om vi skal ta vare på noe data eller om buffer skal slettes
                    //        lastHeaderPos = serialPortData.buffer_binary.LastIndexOf(process.packetHeader);
                    //        lastEndPos = serialPortData.buffer_binary.LastIndexOf(process.packetEnd);

                    //        // Dersom vi har en HEADER etter en END
                    //        if (lastHeaderPos > lastEndPos &&
                    //            lastHeaderPos != -1 &&
                    //            lastEndPos != -1 &&
                    //            !string.IsNullOrEmpty(process.packetHeader) &&
                    //            !string.IsNullOrEmpty(process.packetEnd))
                    //            // Lagre fra header (i tilfelle resten av packet kommer i neste sending fra serieport)
                    //            serialPortData.buffer_binary = serialPortData.buffer_binary.Substring(lastHeaderPos);
                    //        else
                    //            // Ingen HEADER etter END -> Slette buffer
                    //            serialPortData.buffer_binary = String.Empty;

                    //        // Må også begrense hvor my data som skal leses i input buffer når vi ikke finner packets
                    //        // slik at den ikke fylles i det uendelige.
                    //        if (serialPortData.buffer_binary.Count() > 4096) // 4KB limit per packet
                    //            serialPortData.buffer_binary = String.Empty;

                    //        // Sletter text buffer
                    //        serialPortData.buffer_text = String.Empty;
                    //        break;

                    //    default:
                    //        break;
                    //}

                    // Ferdig med prosessering
                    serialPortData.processingDone = true;
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