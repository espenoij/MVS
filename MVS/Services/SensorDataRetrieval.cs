﻿using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Windows.Data;
using System.Windows.Threading;
using Telerik.Windows.Data;

namespace MVS
{
    public class SensorDataRetrieval
    {
        // Configuration
        private Config config;

        // Error Handler
        private ErrorHandler errorHandler;

        // Sensor Data List
        private RadObservableCollection<SensorData> sensorDataList;
        private object sensorDataListLock = new object();

        // Serie port: Data innhenting
        private SerialPortDataRetrieval serialPortDataRetrieval;

        // MODBUS: Data innhenting
        private ModbusDataRetrieval modbusDataRetrieval;

        // File Reader: Data innhenting
        private FileReaderDataRetrieval fileReaderDataRetrieval;

        // File Reader: Data innhenting
        private FixedValueDataRetrieval fixedValueDataRetrieval;

        // Retrieval Status
        private bool dataRetrievalStarted = false;

        public SensorDataRetrieval(Config config, DatabaseHandler database, ErrorHandler errorHandler, AdminSettingsVM adminSettingsVM)
        {
            this.config = config;
            this.errorHandler = errorHandler;

            sensorDataList = new RadObservableCollection<SensorData>();
            BindingOperations.EnableCollectionSynchronization(sensorDataList, sensorDataListLock);

            // Serial Port data innhenting
            serialPortDataRetrieval = new SerialPortDataRetrieval(config, sensorDataList, database, errorHandler, adminSettingsVM);

            // Modbus data innhenting
            modbusDataRetrieval = new ModbusDataRetrieval(config, database, errorHandler, adminSettingsVM);

            // File Reader data innhenting
            fileReaderDataRetrieval = new FileReaderDataRetrieval(config, database, errorHandler, adminSettingsVM);

            // Fixed value data innhenting
            fixedValueDataRetrieval = new FixedValueDataRetrieval(database, errorHandler);
        }

        public void LoadSensors(MainWindowVM mainWindowVM)
        {
            // Slette data listene
            sensorDataList.Clear();
            serialPortDataRetrieval.Clear();
            modbusDataRetrieval.Clear();
            fileReaderDataRetrieval.Clear();
            fixedValueDataRetrieval.Clear();

            // Hente liste med data fra fil
            SensorConfigCollection sensorConfigCollection = config.GetAllSensorData();

            if (sensorConfigCollection != null)
            {
                // Prosessere alle sensor data setups
                foreach (SensorConfig item in sensorConfigCollection)
                {
                    // Konvertere til SensorData
                    SensorData sensorData = new SensorData(item);

                    // Legge til i listen
                    sensorDataList.Add(sensorData);

                    // Behandle de forskjellige sensor typene
                    switch (sensorData.type)
                    {
                        case SensorType.SerialPort:
                            serialPortDataRetrieval.Load(sensorData, mainWindowVM);
                            break;

                        case SensorType.ModbusRTU:
                        case SensorType.ModbusASCII:
                        case SensorType.ModbusTCP:
                            modbusDataRetrieval.Load(sensorData, mainWindowVM);
                            break;

                        case SensorType.FileReader:
                            fileReaderDataRetrieval.Load(sensorData, mainWindowVM);
                            break;

                        case SensorType.FixedValue:
                            fixedValueDataRetrieval.Load(sensorData, mainWindowVM);
                            break;
                    }
                }
            }

            // MODBUS Reader init
            modbusDataRetrieval.InitReader();
        }

        public List<SerialPortData> GetSerialPortDataReceivedList()
        {
            return serialPortDataRetrieval.GetSerialPortDataReceivedList();
        }

        public RadObservableCollection<SensorData> GetSensorDataList()
        {
            return sensorDataList;
        }

        public SerialPort GetSerialPort(string portName)
        {
            return serialPortDataRetrieval.GetSerialPort(portName);
        }

        public List<FileReaderSetup> GetFileReaderList()
        {
            return fileReaderDataRetrieval.GetFileReaderList();
        }

        public List<FixedValueSetup> GetFixedValueList()
        {
            return fixedValueDataRetrieval.GetFixedValueList();
        }

        public void SensorDataRetrieval_Start()
        {
            dataRetrievalStarted = true;

            // Start Serial Port
            serialPortDataRetrieval.Start();

            // Start MODBUS
            modbusDataRetrieval.Start();

            // Start file reader
            fileReaderDataRetrieval.Start();

            // Start fixed value
            fixedValueDataRetrieval.Start();
        }

        public void SensorDataRetrieval_Stop()
        {
            dataRetrievalStarted = false;

            serialPortDataRetrieval.Stop();
            modbusDataRetrieval.Stop();
            fileReaderDataRetrieval.Stop();
            fixedValueDataRetrieval.Stop();
        }

        public void UpdateSensorStatus(MainWindowVM mainWindowVM)
        {
            // Dispatcher som oppdatere UI
            DispatcherTimer statusTimer = new DispatcherTimer();
            statusTimer.Interval = TimeSpan.FromMilliseconds(config.ReadWithDefault(ConfigKey.ServerUIUpdateFrequency, Constants.ServerUIUpdateFrequencyDefault));
            statusTimer.Tick += runUpdateSensorStatus;
            statusTimer.Start();

            void runUpdateSensorStatus(object sender, EventArgs e)
            {
                // Lese data timeout fra config
                double dataTimeout = config.ReadWithDefault(ConfigKey.DataTimeout, Constants.DataTimeoutDefault);

                // Gå gjennom listen
                lock (sensorDataListLock)
                {
                    foreach (SensorData sensorData in sensorDataList.ToList())
                    {
                        switch (sensorData.type)
                        {
                            // Serial Port
                            //////////////////////////////////////////////////////////////////////////////////////
                            case SensorType.SerialPort:

                                // Finne korrekt serie port data
                                SerialPortData serialPortData = serialPortDataRetrieval.GetSerialPortDataReceivedList().Find(x => x.portName == sensorData.serialPort.portName);
                                if (serialPortData != null)
                                {
                                    try
                                    {
                                        // Oppdatere serie port status
                                        SerialPort serialPort = serialPortDataRetrieval.GetSerialPortList().Find(x => x.PortName == serialPortData.portName);
                                        if (serialPort != null)
                                        {
                                            // Porten er ikke åpen
                                            if (!serialPort.IsOpen)
                                            {
                                                // Data innhenting er startet -> port skal være åpen
                                                if (dataRetrievalStarted)
                                                {
                                                    // Prøv å restarte porten
                                                    serialPortDataRetrieval.Restart(serialPortData);
                                                }
                                                else
                                                {
                                                    serialPortData.portStatus = PortStatus.Closed;
                                                }
                                            }
                                            // Porten er åpen
                                            else
                                            {
                                                // Dersom det ikke er satt data på porten innen timeout
                                                if (serialPortData.timestamp.AddMilliseconds(dataTimeout) < DateTime.UtcNow)
                                                {
                                                    // Sette status
                                                    serialPortData.portStatus = PortStatus.NoData;

                                                    // Fjerne data fra data feltet
                                                    serialPortData.buffer_text = string.Empty;

                                                    // Prøv å restarte porten
                                                    if (serialPortData.restartTime.AddMilliseconds(dataTimeout) < DateTime.UtcNow)
                                                    {
                                                        serialPortDataRetrieval.Restart(serialPortData);
                                                        serialPortData.restartTime = DateTime.UtcNow;
                                                    }

                                                    // Sette feilmelding
                                                    errorHandler.Insert(
                                                        new ErrorMessage(
                                                            DateTime.UtcNow,
                                                            ErrorMessageType.SerialPort,
                                                            ErrorMessageCategory.AdminUser,
                                                            string.Format("Serial port data timeout: {0}", serialPortData.portName),
                                                            sensorData.id));
                                                }
                                                // Det er data på porten
                                                else
                                                {
                                                    if (sensorData.UseThisSensor(mainWindowVM))
                                                    {
                                                        // Sette status
                                                        if (serialPortData.portStatus == PortStatus.Closed)
                                                        {
                                                            serialPortData.portStatus = PortStatus.Open;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        serialPortData.portStatus = PortStatus.Closed;
                                                    }
                                                }
                                            }
                                        }

                                        // Overføre status til sensor data item
                                        sensorData.portStatus = serialPortData.portStatus;
                                    }
                                    catch (Exception ex)
                                    {
                                        errorHandler.Insert(
                                            new ErrorMessage(
                                                DateTime.UtcNow,
                                                ErrorMessageType.SerialPort,
                                                ErrorMessageCategory.AdminUser,
                                                string.Format("runUpdateSensorStatus: {0}, System message: {1}", serialPortData.portName, ex.Message),
                                                sensorData.id));
                                    }
                                }
                                break;

                            // MODBUS
                            //////////////////////////////////////////////////////////////////////////////////////
                            case SensorType.ModbusRTU:
                            case SensorType.ModbusASCII:
                            case SensorType.ModbusTCP:

                                if (dataRetrievalStarted)
                                {
                                    if (sensorData.UseThisSensor(mainWindowVM))
                                    {
                                        if (sensorData.portStatus == PortStatus.Closed)
                                        {
                                            sensorData.portStatus = PortStatus.Open;
                                        }
                                    }
                                    else
                                    {
                                        sensorData.portStatus = PortStatus.Closed;
                                    }
                                }
                                else
                                {
                                    sensorData.portStatus = PortStatus.Closed;
                                }
                                break;

                            // File Reader
                            //////////////////////////////////////////////////////////////////////////////////////
                            case SensorType.FileReader:

                                // Finne korrekt serie port data
                                FileReaderSetup fileReaderData = fileReaderDataRetrieval.GetFileReaderList().Find(x =>
                                    x.fileFolder == sensorData.fileReader.fileFolder &&
                                    x.fileName == sensorData.fileReader.fileName);

                                if (dataRetrievalStarted)
                                {
                                    if (sensorData.UseThisSensor(mainWindowVM))
                                    {
                                        if (sensorData.portStatus == PortStatus.Closed)
                                        {
                                            sensorData.portStatus = PortStatus.Open;

                                            // Oppdatere status
                                            if (fileReaderData != null)
                                                fileReaderData.portStatus = sensorData.portStatus;
                                        }
                                    }
                                    else
                                    {
                                        sensorData.portStatus = PortStatus.Closed;
                                    }
                                }
                                else
                                {
                                    sensorData.portStatus = PortStatus.Closed;

                                    // Oppdatere status
                                    if (fileReaderData != null)
                                        fileReaderData.portStatus = sensorData.portStatus;
                                }
                                break;

                            // Fixed Value
                            //////////////////////////////////////////////////////////////////////////////////////
                            case SensorType.FixedValue:

                                FixedValueSetup fixedValueData = fixedValueDataRetrieval.GetFixedValueList().Find(x => x.id == sensorData.id);

                                if (dataRetrievalStarted)
                                {
                                    if (sensorData.UseThisSensor(mainWindowVM))
                                    {
                                        if (sensorData.portStatus == PortStatus.Closed)
                                        {
                                            sensorData.portStatus = PortStatus.Open;

                                            // Oppdatere status
                                            if (fixedValueData != null)
                                                fixedValueData.portStatus = sensorData.portStatus;
                                        }
                                    }
                                    else
                                    {
                                        sensorData.portStatus = PortStatus.Closed;
                                    }
                                }
                                else
                                {
                                    sensorData.portStatus = PortStatus.Closed;

                                    // Oppdatere status
                                    if (fixedValueData != null)
                                        fixedValueData.portStatus = sensorData.portStatus;
                                }
                                break;
                        }

                        // Sjekke om det ligger inne feilmeldinger på denne sensor verdien
                        ErrorMessage errorMessage = errorHandler.GetErrorMessage(sensorData.id);
                        if (errorMessage != null)
                        {
                            // ...og at feilmeldingen er nyere enn evt.data(feilmeldingene kommer alltid etter forsøk på data henting / behandling i tid)
                            if (errorMessage.timestamp.AddMilliseconds(config.ReadWithDefault(ConfigKey.ServerUIUpdateFrequency, Constants.ServerUIUpdateFrequencyDefault)) > sensorData.timestamp)
                            {
                                // Legge ut feilmeldingen på skjerm
                                sensorData.message = errorMessage.message;
                            }
                            else
                            {
                                // Fjerne melding fra skjerm
                                sensorData.message = string.Empty;
                            }
                        }
                        else
                        {
                            // Fjerne melding fra skjerm
                            sensorData.message = string.Empty;
                        }

                        // Sette Warning status dersom vi leser data, men har en feilmelding
                        if (sensorData.portStatus == PortStatus.Reading &&
                            sensorData.HasMessage)
                        {
                            sensorData.portStatus = PortStatus.Warning;
                        }
                    }
                }
            }
        }
    }
}
