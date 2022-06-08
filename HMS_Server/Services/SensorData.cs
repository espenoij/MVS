using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO.Ports;
using System.Runtime.CompilerServices;

namespace HMS_Server
{
    public class SensorData : INotifyPropertyChanged
    {
        // Change notification
        public event PropertyChangedEventHandler PropertyChanged;

        public SensorData(SensorData sensorData)
        {
            Set(sensorData);
        }

        public SensorData(SensorType type = SensorType.None)
        {
            switch (type)
            {
                case SensorType.None:
                    break;

                case SensorType.SerialPort:
                    serialPort = new SerialPortSetup();
                    break;

                case SensorType.ModbusRTU:
                case SensorType.ModbusASCII:
                case SensorType.ModbusTCP:
                    modbus = new ModbusSetup();
                    break;

                case SensorType.FileReader:
                    fileReader = new FileReaderSetup();
                    break;
            }

            for (int i = 0; i < Constants.DataCalculationSteps; i++)
                dataCalculations.Add(new DataCalculations());
        }

        public void Set(SensorData sensorData)
        {
            id = sensorData.id;
            type = sensorData.type;
            name = sensorData.name;
            description = sensorData.description;

            // Sjekke om datacalculation objektene er opprettet
            if (dataCalculations.Count != Constants.DataCalculationSteps)
                for (int i = 0; i < Constants.DataCalculationSteps; i++)
                    dataCalculations.Add(new DataCalculations());

            for (int i = 0; i < Constants.DataCalculationSteps && i < sensorData.dataCalculations.Count; i++)
                dataCalculations[i] = sensorData.dataCalculations[i];

            saveToDatabase = sensorData.saveToDatabase;
            saveFreq = sensorData.saveFreq;
            data = sensorData.data;
            timestamp = sensorData.timestamp;
            portStatus = sensorData.portStatus;
            message = sensorData.message;

            serialPort = sensorData.serialPort;
            modbus = sensorData.modbus;
            fileReader = sensorData.fileReader;
        }

        public SensorData(SensorConfig sensorConfig)
        {
            if (sensorConfig != null)
            {
                try
                {
                    id = sensorConfig.id;
                    type = (SensorType)Enum.Parse(typeof(SensorType), sensorConfig.type);
                    name = sensorConfig.name;
                    description = sensorConfig.description;
                    saveToDatabase = bool.Parse(sensorConfig.saveToDatabase);
                    saveFreq = (DatabaseSaveFrequency)Enum.Parse(typeof(DatabaseSaveFrequency), sensorConfig.saveFreq);

                    switch (type)
                    {
                        case SensorType.None:
                            break;

                        case SensorType.SerialPort:
                            serialPort = new SerialPortSetup();

                            // Serial Port setup
                            serialPort.portName = sensorConfig.serialPort.portName;
                            serialPort.baudRate = Convert.ToInt32(sensorConfig.serialPort.baudRate);
                            serialPort.dataBits = Convert.ToInt16(sensorConfig.serialPort.dataBits);
                            serialPort.stopBits = (StopBits)Enum.Parse(typeof(StopBits), sensorConfig.serialPort.stopBits);
                            serialPort.parity = (Parity)Enum.Parse(typeof(Parity), sensorConfig.serialPort.parity);
                            serialPort.handshake = (Handshake)Enum.Parse(typeof(Handshake), sensorConfig.serialPort.handshake);

                            // Serial Port Data Extraction Parameters
                            serialPort.inputType = (InputDataType)Enum.Parse(typeof(InputDataType), sensorConfig.serialPort.inputType);
                            serialPort.totalBytes = Convert.ToInt32(sensorConfig.serialPort.totalBytes);
                            serialPort.binarySigned = bool.Parse(sensorConfig.serialPort.binarySigned);

                            serialPort.packetHeader = TextHelper.UnescapeSpace(sensorConfig.serialPort.packetHeader);
                            serialPort.packetEnd = sensorConfig.serialPort.packetEnd;
                            serialPort.packetDelimiter = sensorConfig.serialPort.packetDelimiter;
                            serialPort.packetCombineFields = (SerialPortSetup.CombineFields)Enum.Parse(typeof(SerialPortSetup.CombineFields), sensorConfig.serialPort.packetCombineFields);

                            serialPort.fixedPosData = bool.Parse(sensorConfig.serialPort.fixedPosData);
                            serialPort.fixedPosStart = Convert.ToInt32(sensorConfig.serialPort.fixedPosStart);
                            serialPort.fixedPosTotal = Convert.ToInt32(sensorConfig.serialPort.fixedPosTotal);

                            serialPort.dataField = sensorConfig.serialPort.dataField;
                            serialPort.decimalSeparator = (DecimalSeparator)Enum.Parse(typeof(DecimalSeparator), sensorConfig.serialPort.decimalSeparator); ;
                            serialPort.autoExtractValue = bool.Parse(sensorConfig.serialPort.autoExtractValue);

                            // Data Calculations
                            double param;

                            serialPort.calculationSetup[0].type = (CalculationType)Enum.Parse(typeof(CalculationType), sensorConfig.serialPort.calculationType1);
                            if (double.TryParse(sensorConfig.serialPort.calculationParameter1, Constants.numberStyle, Constants.cultureInfo, out param))
                                serialPort.calculationSetup[0].parameter = param;
                            else
                                serialPort.calculationSetup[0].parameter = 0;

                            serialPort.calculationSetup[1].type = (CalculationType)Enum.Parse(typeof(CalculationType), sensorConfig.serialPort.calculationType2);
                            if (double.TryParse(sensorConfig.serialPort.calculationParameter2, Constants.numberStyle, Constants.cultureInfo, out param))
                                serialPort.calculationSetup[1].parameter = param;
                            else
                                serialPort.calculationSetup[1].parameter = 0;

                            serialPort.calculationSetup[2].type = (CalculationType)Enum.Parse(typeof(CalculationType), sensorConfig.serialPort.calculationType3);
                            if (double.TryParse(sensorConfig.serialPort.calculationParameter3, Constants.numberStyle, Constants.cultureInfo, out param))
                                serialPort.calculationSetup[2].parameter = param;
                            else
                                serialPort.calculationSetup[2].parameter = 0;

                            serialPort.calculationSetup[3].type = (CalculationType)Enum.Parse(typeof(CalculationType), sensorConfig.serialPort.calculationType4);
                            if (double.TryParse(sensorConfig.serialPort.calculationParameter4, Constants.numberStyle, Constants.cultureInfo, out param))
                                serialPort.calculationSetup[3].parameter = param;
                            else
                                serialPort.calculationSetup[3].parameter = 0;

                            dataCalculations.Clear();
                            for (int i = 0; i < Constants.DataCalculationSteps; i++)
                                dataCalculations.Add(new DataCalculations(serialPort.calculationSetup[i].type, serialPort.calculationSetup[i].parameter));

                            break;

                        case SensorType.ModbusRTU:
                        case SensorType.ModbusASCII:
                        case SensorType.ModbusTCP:
                            modbus = new ModbusSetup();

                            // MODBUS setup
                            modbus.portName = sensorConfig.modbus.portName;
                            modbus.baudRate = Convert.ToInt32(sensorConfig.modbus.baudRate);
                            modbus.dataBits = Convert.ToInt16(sensorConfig.modbus.dataBits);
                            modbus.stopBits = (StopBits)Enum.Parse(typeof(StopBits), sensorConfig.modbus.stopBits);
                            modbus.parity = (Parity)Enum.Parse(typeof(Parity), sensorConfig.modbus.parity);
                            modbus.handshake = (Handshake)Enum.Parse(typeof(Handshake), sensorConfig.modbus.handshake);

                            modbus.tcpAddress = sensorConfig.modbus.tcpAddress;
                            modbus.tcpPort = Convert.ToInt32(sensorConfig.modbus.tcpPort);

                            // Modbus Data Extraction Parameters
                            modbus.slaveID = Convert.ToByte(sensorConfig.modbus.slaveID);
                            modbus.startAddress = Convert.ToInt32(sensorConfig.modbus.startAddress);
                            modbus.totalAddresses = Convert.ToInt32(sensorConfig.modbus.totalAddresses);
                            modbus.dataAddress = Convert.ToInt32(sensorConfig.modbus.dataAddress);

                            // Data Calculations
                            modbus.calculationSetup[0].type = (CalculationType)Enum.Parse(typeof(CalculationType), sensorConfig.modbus.calculationType1);
                            if (double.TryParse(sensorConfig.modbus.calculationParameter1, Constants.numberStyle, Constants.cultureInfo, out param))
                                modbus.calculationSetup[0].parameter = param;
                            else
                                modbus.calculationSetup[0].parameter = 0;

                            modbus.calculationSetup[1].type = (CalculationType)Enum.Parse(typeof(CalculationType), sensorConfig.modbus.calculationType2);
                            if (double.TryParse(sensorConfig.modbus.calculationParameter2, Constants.numberStyle, Constants.cultureInfo, out param))
                                modbus.calculationSetup[1].parameter = param;
                            else
                                modbus.calculationSetup[1].parameter = 0;

                            modbus.calculationSetup[2].type = (CalculationType)Enum.Parse(typeof(CalculationType), sensorConfig.modbus.calculationType3);
                            if (double.TryParse(sensorConfig.modbus.calculationParameter3, Constants.numberStyle, Constants.cultureInfo, out param))
                                modbus.calculationSetup[2].parameter = param;
                            else
                                modbus.calculationSetup[2].parameter = 0;

                            modbus.calculationSetup[3].type = (CalculationType)Enum.Parse(typeof(CalculationType), sensorConfig.modbus.calculationType4);
                            if (double.TryParse(sensorConfig.modbus.calculationParameter4, Constants.numberStyle, Constants.cultureInfo, out param))
                                modbus.calculationSetup[3].parameter = param;
                            else
                                modbus.calculationSetup[3].parameter = 0;

                            dataCalculations.Clear();
                            for (int i = 0; i < Constants.DataCalculationSteps; i++)
                                dataCalculations.Add(new DataCalculations(modbus.calculationSetup[i].type, modbus.calculationSetup[i].parameter));

                            break;

                        case SensorType.FileReader:
                            fileReader = new FileReaderSetup();

                            fileReader.fileFolder = sensorConfig.fileReader.fileFolder;
                            fileReader.fileName = sensorConfig.fileReader.fileName;
                            fileReader.readFrequency = Convert.ToDouble(sensorConfig.fileReader.readFrequency);

                            fileReader.delimiter = sensorConfig.fileReader.delimiter;
                            fileReader.fixedPosData = bool.Parse(sensorConfig.fileReader.fixedPosData);
                            fileReader.fixedPosStart = Convert.ToInt32(sensorConfig.fileReader.fixedPosStart);
                            fileReader.fixedPosTotal = Convert.ToInt32(sensorConfig.fileReader.fixedPosTotal);

                            fileReader.dataField = sensorConfig.fileReader.dataField;
                            fileReader.decimalSeparator = (DecimalSeparator)Enum.Parse(typeof(DecimalSeparator), sensorConfig.fileReader.decimalSeparator); ;
                            fileReader.autoExtractValue = bool.Parse(sensorConfig.fileReader.autoExtractValue);

                            // Data Calculations
                            fileReader.calculationSetup[0].type = (CalculationType)Enum.Parse(typeof(CalculationType), sensorConfig.fileReader.calculationType1);
                            if (double.TryParse(sensorConfig.fileReader.calculationParameter1, Constants.numberStyle, Constants.cultureInfo, out param))
                                fileReader.calculationSetup[0].parameter = param;
                            else
                                fileReader.calculationSetup[0].parameter = 0;

                            fileReader.calculationSetup[1].type = (CalculationType)Enum.Parse(typeof(CalculationType), sensorConfig.fileReader.calculationType2);
                            if (double.TryParse(sensorConfig.fileReader.calculationParameter2, Constants.numberStyle, Constants.cultureInfo, out param))
                                fileReader.calculationSetup[1].parameter = param;
                            else
                                fileReader.calculationSetup[1].parameter = 0;

                            fileReader.calculationSetup[2].type = (CalculationType)Enum.Parse(typeof(CalculationType), sensorConfig.fileReader.calculationType3);
                            if (double.TryParse(sensorConfig.fileReader.calculationParameter3, Constants.numberStyle, Constants.cultureInfo, out param))
                                fileReader.calculationSetup[2].parameter = param;
                            else
                                fileReader.calculationSetup[2].parameter = 0;

                            fileReader.calculationSetup[3].type = (CalculationType)Enum.Parse(typeof(CalculationType), sensorConfig.fileReader.calculationType4);
                            if (double.TryParse(sensorConfig.fileReader.calculationParameter4, Constants.numberStyle, Constants.cultureInfo, out param))
                                fileReader.calculationSetup[3].parameter = param;
                            else
                                fileReader.calculationSetup[3].parameter = 0;

                            dataCalculations.Clear();
                            for (int i = 0; i < Constants.DataCalculationSteps; i++)
                                dataCalculations.Add(new DataCalculations(fileReader.calculationSetup[i].type, fileReader.calculationSetup[i].parameter));

                            break;
                    }
                }
                catch (Exception ex)
                {
                    // Sette feilmelding
                    new ErrorHandler(new DatabaseHandler(true)).Insert(
                        new ErrorMessage(
                            DateTime.UtcNow,
                            ErrorMessageType.All,
                            ErrorMessageCategory.None,
                            string.Format("SensorData (constructor)\n\nSystem Message:\n{0}", ex.Message),
                            id));

                    // NB! Fordi jeg opprette en ny instans av ErrorHandler og database her så vil ikke
                    // denne feilen legges inn i grensesnittet for utskrift av feil meldinger,
                    // men den vil legges i databasen.
                }
            }
        }

        // Setup
        private int _id { get; set; }
        public int id
        {
            get
            {
                return _id;
            }
            set
            {
                _id = value;
                OnPropertyChanged();
            }
        }

        private SensorType _type;
        public SensorType type
        {
            get
            {
                return _type;
            }
            set
            {
                _type = value;

                switch (_type)
                {
                    case SensorType.None:
                        serialPort = null;
                        modbus = null;
                        fileReader = null;
                        break;

                    case SensorType.SerialPort:
                        serialPort = new SerialPortSetup();
                        modbus = null;
                        fileReader = null;
                        break;

                    case SensorType.ModbusRTU:
                    case SensorType.ModbusASCII:
                    case SensorType.ModbusTCP:
                        serialPort = null;
                        modbus = new ModbusSetup();
                        fileReader = null;
                        break;

                    case SensorType.FileReader:
                        serialPort = null;
                        modbus = null;
                        fileReader = new FileReaderSetup();
                        break;
                }

                OnPropertyChanged();
                OnPropertyChanged(nameof(source));
            }
        }

        public string _name { get; set; }
        public string name
        {
            get
            {
                return _name;
            }
            set
            {
                _name = value;
                OnPropertyChanged();
            }
        }

        private string _description { get; set; }
        public string description
        {
            get
            {
                return _description;
            }
            set
            {
                _description = value;
                OnPropertyChanged();
            }
        }

        public List<DataCalculations> dataCalculations = new List<DataCalculations>();

        // Lagre data til databasen
        public bool saveToDatabase { get; set; }

        // Database lagringsfrekvens
        public DatabaseSaveFrequency saveFreq { get; set; }

        // Resultat data
        public double data { get; set; }

        private DateTime _timestamp { get; set; }
        public DateTime timestamp
        {
            get
            {
                return _timestamp;
            }
            set
            {
                _timestamp = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(timestampString));
            }
        }
        public string timestampString
        {
            get
            {
                if (timestamp.Ticks != 0)
                    return timestamp.ToString(Constants.TimestampFormat, Constants.cultureInfo);
                else
                    return Constants.TimestampNotSet;
            }
        }

        private PortStatus _portStatus { get; set; }
        public PortStatus portStatus
        {
            get
            {
                return _portStatus;
            }
            set
            {
                _portStatus = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(portStatusString));
                OnPropertyChanged(nameof(statusString));
            }
        }
        public string portStatusString
        {
            get
            {
                return portStatus.ToString();
            }
        }

        public string statusString
        {
            get
            {
                switch (portStatus)
                {
                    case PortStatus.Closed:
                    case PortStatus.Open:
                        return DataStatus.NONE.ToString();

                    case PortStatus.Reading:
                        return DataStatus.OK.ToString();

                    case PortStatus.NoData:
                    case PortStatus.Warning:
                    case PortStatus.OpenError:
                    case PortStatus.EndOfFile:
                        return DataStatus.TIMEOUT_ERROR.ToString();

                    default:
                        return DataStatus.NONE.ToString();
                }
            }
        }

        // Error Message
        private string _message { get; set; }
        public string message
        {
            get
            {
                return _message;
            }
            set
            {
                _message = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasMessage));
                OnPropertyChanged(nameof(messageSingleLine));
            }
        }
        public bool HasMessage
        {
            get
            {
                return !string.IsNullOrEmpty(message);
            }
        }
        public string messageSingleLine
        {
            get
            {
                return TextHelper.RemoveNewLine(message);
            }
        }

        // Sensor type-spesifikke data
        public SerialPortSetup serialPort { get; set; }
        public ModbusSetup modbus { get; set; }
        public FileReaderSetup fileReader { get; set; }


        // Brukes i data bidning mot UI
        public string source
        {
            get
            {
                switch (type)
                {
                    case SensorType.SerialPort:
                        return serialPort.portName;

                    case SensorType.ModbusRTU:
                    case SensorType.ModbusASCII:
                        return modbus.portName;

                    case SensorType.ModbusTCP:
                        return modbus.tcpAddress;

                    case SensorType.FileReader:
                        return fileReader.fileName;

                    default:
                        return "-";
                }
            }
        }

        public double GetSaveFrequency(Config config)
        {
            // Returnerer frekvens i millisekund

            switch (saveFreq)
            {
                case DatabaseSaveFrequency.Program:
                    return config.ReadWithDefault(ConfigKey.DatabaseSaveFrequency, Constants.DatabaseSaveFreqDefault);

                case DatabaseSaveFrequency.Freq_2hz:
                    return (int)DatabaseSaveFrequency.Freq_2hz;

                case DatabaseSaveFrequency.Freq_1hz:
                    return (int)DatabaseSaveFrequency.Freq_1hz;

                case DatabaseSaveFrequency.Freq_2sec:
                    return (int)DatabaseSaveFrequency.Freq_2sec;

                case DatabaseSaveFrequency.Freq_3sec:
                    return (int)DatabaseSaveFrequency.Freq_3sec;

                case DatabaseSaveFrequency.Freq_4sec:
                    return (int)DatabaseSaveFrequency.Freq_4sec;

                case DatabaseSaveFrequency.Freq_5sec:
                    return (int)DatabaseSaveFrequency.Freq_5sec;

                default:
                    return 1000;
            }
        }

        // Variabel oppdatert
        // Dersom navn ikke settes brukes kallende medlem sitt navn
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    public enum SensorType
    {
        None,
        SerialPort,
        ModbusRTU,      // Remote Terminal Unit (serial port)
        ModbusASCII,
        ModbusTCP,
        FileReader
    }

    public enum DatabaseSaveFrequency
    {
        Sensor,
        Program,
        Freq_2hz = 500,
        Freq_1hz = 1000,
        Freq_2sec = 2000,
        Freq_3sec = 3000,
        Freq_4sec = 4000,
        Freq_5sec = 5000
    }

    public enum ModbusObjectType
    {
        None,
        Coil,
        DiscreteInput,
        InputRegister,
        HoldingRegister
    }
}
