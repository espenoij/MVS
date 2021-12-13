using NModbus;
using NModbus.Serial;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Net.Sockets;
using System.Threading;
using System.Timers;

namespace HMS_Server
{
    class ModbusDataRetrieval
    {
        // Configuration
        private Config config;

        // Database
        private DatabaseHandler database;

        // Error Handler
        private ErrorHandler errorHandler;

        // Read timer
        private System.Timers.Timer modbusReader;

        // MODBUS Helper
        private ModbusHelper modbusHelper = new ModbusHelper();

        // Serie port liste
        private List<SerialPort> modbusSerialPortList = new List<SerialPort>();

        // MODBUS Liste
        private List<SensorData> modbusSensorList = new List<SensorData>();

        public ModbusDataRetrieval(Config config, DatabaseHandler database, ErrorHandler errorHandler)
        {
            this.config = config;
            this.database = database;
            this.errorHandler = errorHandler;
        }

        public void Load(SensorData sensorData)
        {
            // Legge til sensor i MODBUS sensor listen
            modbusSensorList.Add(sensorData);

            // Sjekke om serie port er lagt inn/åpnet fra før
            SerialPort sp = modbusSerialPortList.Find(x => x.PortName == sensorData.modbus.portName);

            // Dersom den ikke eksisterer -> så legger vi den inn i serie port listen...
            if (sp == null)
            {
                // Sette serie port settings på SerialPort object
                SerialPort serialPort = new SerialPort();

                serialPort.PortName = sensorData.modbus.portName;
                serialPort.BaudRate = sensorData.modbus.baudRate;
                serialPort.DataBits = sensorData.modbus.dataBits;
                serialPort.StopBits = sensorData.modbus.stopBits;
                serialPort.Parity = sensorData.modbus.parity;
                serialPort.Handshake = sensorData.modbus.handshake;

                // Timeout
                serialPort.ReadTimeout = Constants.ModbusTimeout;       // NB! Brukes av ReadHoldingRegisters etc. Uten disse vil programmet fryse dersom MODBUS read funksjonene ikke får svar.
                serialPort.WriteTimeout = Constants.ModbusTimeout;

                // Legge inn i MODBUS serie port listen
                modbusSerialPortList.Add(serialPort);
            }
        }

        public void Clear()
        {
            modbusSensorList.Clear();
        }

        public void InitReader()
        {
            // Finne høyeste frekvens
            double highFreq = (double)DatabaseSaveFrequency.Freq_5sec;
            foreach (var sensor in modbusSensorList)
                if (sensor.GetSaveFrequency(config) < highFreq)
                    highFreq = sensor.GetSaveFrequency(config);

            modbusReader = new System.Timers.Timer((int)highFreq);
            modbusReader.AutoReset = true;
            modbusReader.Elapsed += runModbusReader;

            void runModbusReader(Object source, ElapsedEventArgs e)
            {
                // Starter egen thread for hvert MODBUS port
                foreach (var serialPort in modbusSerialPortList)
                {
                    Thread thread = new Thread(() => runModbusReader_Thread(serialPort));
                    thread.Start();
                }
            }

            void runModbusReader_Thread(SerialPort serialPort)
            {
                if (serialPort != null)
                {
                    // Midlertidig register for leste data
                    ModbusRegister modbusRegister = new ModbusRegister();

                    // Data lagres her
                    ModbusData modbusData = new ModbusData();

                    // Løper gjennom alle MODBUS sensorene
                    foreach (var sensorData in modbusSensorList)
                    {
                        // Plukker ut de som er på denne porten
                        if (sensorData.modbus.portName == serialPort.PortName)
                        {
                            // Finne ut hvor det skal leses
                            ModbusObjectType modbusObjectType = modbusHelper.AddressToObjectType(sensorData.modbus.dataAddress);

                            try
                            {
                                // Read registers (basert på type)
                                switch (modbusObjectType)
                                {
                                    case ModbusObjectType.Coil:

                                        // Sjekke om vi må hente data fra MODBUS (eller om data vi er ute etter har blitt lest inn fra før)
                                        if (!modbusRegister.coil[modbusHelper.AddressToOffset(sensorData.modbus.dataAddress)].IsSet)
                                        {
                                            bool[] registers = new bool[0];

                                            // Åpne MODBUS protokoll
                                            ModbusFactory modbusFactory = new ModbusFactory();

                                            // Sjekk MODBUS type
                                            switch (sensorData.type)
                                            {
                                                // RTU eller ASCII
                                                case SensorType.ModbusRTU:
                                                case SensorType.ModbusASCII:
                                                    {
                                                        // Åpne port
                                                        if (!serialPort.IsOpen)
                                                            serialPort.Open();

                                                        if (serialPort.IsOpen)
                                                        {
                                                            // Starte MODBUS
                                                            IModbusSerialMaster modbusMaster;
                                                            if (sensorData.type == SensorType.ModbusRTU)
                                                                modbusMaster = modbusFactory.CreateRtuMaster(new SerialPortAdapter(serialPort));
                                                            else
                                                                modbusMaster = modbusFactory.CreateAsciiMaster(new SerialPortAdapter(serialPort));

                                                            // Lese data
                                                            registers = modbusMaster.ReadCoils(sensorData.modbus.slaveID, modbusHelper.AddressToOffset(sensorData.modbus.startAddress), (ushort)sensorData.modbus.totalAddresses);

                                                            // Lukke port
                                                            serialPort.Close();
                                                        }
                                                    }
                                                    break;

                                                case SensorType.ModbusTCP:
                                                    {
                                                        // TCP client
                                                        TcpClient modbusTCPClient = new TcpClient(sensorData.modbus.tcpAddress, sensorData.modbus.tcpPort);

                                                        modbusTCPClient.ReceiveTimeout = Constants.ModbusTimeout;
                                                        modbusTCPClient.SendTimeout = Constants.ModbusTimeout;

                                                        // Starte MODBUS
                                                        IModbusMaster modbusMaster = modbusFactory.CreateMaster(modbusTCPClient);

                                                        // Lese data
                                                        registers = modbusMaster.ReadCoils(sensorData.modbus.slaveID, modbusHelper.AddressToOffset(sensorData.modbus.startAddress), (ushort)sensorData.modbus.totalAddresses);

                                                        // Lukke forbindelse
                                                        modbusTCPClient.Close();
                                                    }
                                                    break;
                                            }

                                            // Overføre leste data til midlertidig register
                                            if (registers.Length > 0)
                                            {
                                                for (int i = modbusHelper.AddressToOffset(sensorData.modbus.startAddress); i >= 0 && i < ModbusRegister.RegisterSize && i < registers.Length; i++)
                                                {
                                                    modbusRegister.coil[i].data = registers[i];
                                                    modbusRegister.coil[i].IsSet = true;
                                                }
                                            }
                                            // Sette status dersom vi ikke fikk data
                                            else
                                            {
                                                // Status
                                                sensorData.portStatus = PortStatus.NoData;
                                            }
                                        }

                                        // Hente data
                                        modbusData.data = Convert.ToInt32(modbusRegister.coil[modbusHelper.AddressToOffset(sensorData.modbus.dataAddress)]?.data);

                                        break;

                                    case ModbusObjectType.DiscreteInput:

                                        // Sjekke om vi må hente data fra MODBUS (eller om data vi er ute etter har blitt lest inn fra før)
                                        if (!modbusRegister.discrete[modbusHelper.AddressToOffset(sensorData.modbus.dataAddress)].IsSet)
                                        {
                                            bool[] registers = new bool[0];

                                            // Åpne MODBUS protokoll
                                            ModbusFactory modbusFactory = new ModbusFactory();

                                            // Sjekk MODBUS type
                                            switch (sensorData.type)
                                            {
                                                // RTU eller ASCII
                                                case SensorType.ModbusRTU:
                                                case SensorType.ModbusASCII:
                                                    {
                                                        // Åpne port
                                                        if (!serialPort.IsOpen)
                                                            serialPort.Open();

                                                        if (serialPort.IsOpen)
                                                        {
                                                            // Starte MODBUS
                                                            IModbusSerialMaster modbusMaster;
                                                            if (sensorData.type == SensorType.ModbusRTU)
                                                                modbusMaster = modbusFactory.CreateRtuMaster(new SerialPortAdapter(serialPort));
                                                            else
                                                                modbusMaster = modbusFactory.CreateAsciiMaster(new SerialPortAdapter(serialPort));

                                                            // Lese data
                                                            registers = modbusMaster.ReadInputs(sensorData.modbus.slaveID, modbusHelper.AddressToOffset(sensorData.modbus.startAddress), (ushort)sensorData.modbus.totalAddresses);

                                                            // Lukke port
                                                            serialPort.Close();
                                                        }
                                                    }
                                                    break;

                                                case SensorType.ModbusTCP:
                                                    {
                                                        // TCP client
                                                        TcpClient modbusTCPClient = new TcpClient(sensorData.modbus.tcpAddress, sensorData.modbus.tcpPort);

                                                        modbusTCPClient.ReceiveTimeout = Constants.ModbusTimeout;
                                                        modbusTCPClient.SendTimeout = Constants.ModbusTimeout;

                                                        // Starte MODBUS
                                                        IModbusMaster modbusMaster = modbusFactory.CreateMaster(modbusTCPClient);

                                                        // Lese data
                                                        registers = modbusMaster.ReadInputs(sensorData.modbus.slaveID, modbusHelper.AddressToOffset(sensorData.modbus.startAddress), (ushort)sensorData.modbus.totalAddresses);

                                                        // Lukke forbindelse
                                                        modbusTCPClient.Close();
                                                    }
                                                    break;
                                            }

                                            // Overføre leste data til midlertidig register
                                            if (registers.Length > 0)
                                            {
                                                for (int i = modbusHelper.AddressToOffset(sensorData.modbus.startAddress); i >= 0 && i < ModbusRegister.RegisterSize && i < registers.Length; i++)
                                                {
                                                    modbusRegister.discrete[i].data = registers[i];
                                                    modbusRegister.discrete[i].IsSet = true;
                                                }
                                            }
                                            // Sette status dersom vi ikke fikk data
                                            else
                                            {
                                                // Status
                                                sensorData.portStatus = PortStatus.NoData;
                                            }
                                        }

                                        // Hente data
                                        modbusData.data = Convert.ToInt32(modbusRegister.discrete[modbusHelper.AddressToOffset(sensorData.modbus.dataAddress)]?.data);

                                        break;

                                    case ModbusObjectType.InputRegister:

                                        // Sjekke om vi må hente data fra MODBUS (eller om data vi er ute etter har blitt lest inn fra før)
                                        if (!modbusRegister.input[modbusHelper.AddressToOffset(sensorData.modbus.dataAddress)].IsSet)
                                        {
                                            ushort[] registers = new ushort[0];

                                            // Åpne MODBUS protokoll
                                            ModbusFactory modbusFactory = new ModbusFactory();

                                            // Sjekk MODBUS type
                                            switch (sensorData.type)
                                            {
                                                // RTU eller ASCII
                                                case SensorType.ModbusRTU:
                                                case SensorType.ModbusASCII:
                                                    {
                                                        // Åpne port
                                                        if (!serialPort.IsOpen)
                                                            serialPort.Open();

                                                        if (serialPort.IsOpen)
                                                        {
                                                            // Starte MODBUS
                                                            IModbusSerialMaster modbusMaster;
                                                            if (sensorData.type == SensorType.ModbusRTU)
                                                                modbusMaster = modbusFactory.CreateRtuMaster(new SerialPortAdapter(serialPort));
                                                            else
                                                                modbusMaster = modbusFactory.CreateAsciiMaster(new SerialPortAdapter(serialPort));

                                                            // Lese data
                                                            registers = modbusMaster.ReadInputRegisters(sensorData.modbus.slaveID, modbusHelper.AddressToOffset(sensorData.modbus.startAddress), (ushort)sensorData.modbus.totalAddresses);

                                                            // Lukke port
                                                            serialPort.Close();
                                                        }
                                                    }
                                                    break;

                                                case SensorType.ModbusTCP:
                                                    {
                                                        // TCP client
                                                        TcpClient modbusTCPClient = new TcpClient(sensorData.modbus.tcpAddress, sensorData.modbus.tcpPort);

                                                        modbusTCPClient.ReceiveTimeout = Constants.ModbusTimeout;
                                                        modbusTCPClient.SendTimeout = Constants.ModbusTimeout;

                                                        // Starte MODBUS
                                                        IModbusMaster modbusMaster = modbusFactory.CreateMaster(modbusTCPClient);

                                                        // Lese data
                                                        registers = modbusMaster.ReadInputRegisters(sensorData.modbus.slaveID, modbusHelper.AddressToOffset(sensorData.modbus.startAddress), (ushort)sensorData.modbus.totalAddresses);

                                                        // Lukke forbindelse
                                                        modbusTCPClient.Close();
                                                    }
                                                    break;
                                            }

                                            // Overføre leste data til midlertidig register
                                            if (registers.Length > 0)
                                            {
                                                for (int i = modbusHelper.AddressToOffset(sensorData.modbus.startAddress); i >= 0 && i < ModbusRegister.RegisterSize && i < registers.Length; i++)
                                                {
                                                    modbusRegister.input[i].data = registers[i];
                                                    modbusRegister.input[i].IsSet = true;
                                                }
                                            }
                                            // Sette status dersom vi ikke fikk data
                                            else
                                            {
                                                // Status
                                                sensorData.portStatus = PortStatus.NoData;
                                            }
                                        }

                                        // Hente data
                                        modbusData.data = Convert.ToInt32(modbusRegister.input[modbusHelper.AddressToOffset(sensorData.modbus.dataAddress)]?.data);

                                        break;

                                    case ModbusObjectType.HoldingRegister:

                                        // Sjekke om vi må hente data fra MODBUS (eller om data vi er ute etter har blitt lest inn fra før)
                                        if (!modbusRegister.holding[modbusHelper.AddressToOffset(sensorData.modbus.dataAddress)].IsSet)
                                        {
                                            ushort[] registers = new ushort[0];

                                            // Åpne MODBUS protokoll
                                            ModbusFactory modbusFactory = new ModbusFactory();

                                            // Sjekk MODBUS type
                                            switch (sensorData.type)
                                            {
                                                // RTU eller ASCII
                                                case SensorType.ModbusRTU:
                                                case SensorType.ModbusASCII:
                                                    {
                                                        // Åpne port
                                                        if (!serialPort.IsOpen)
                                                            serialPort.Open();

                                                        if (serialPort.IsOpen)
                                                        {
                                                            // Starte MODBUS
                                                            IModbusSerialMaster modbusMaster;
                                                            if (sensorData.type == SensorType.ModbusRTU)
                                                                modbusMaster = modbusFactory.CreateRtuMaster(new SerialPortAdapter(serialPort));
                                                            else
                                                                modbusMaster = modbusFactory.CreateAsciiMaster(new SerialPortAdapter(serialPort));

                                                            // Lese data
                                                            registers = modbusMaster.ReadHoldingRegisters(sensorData.modbus.slaveID, modbusHelper.AddressToOffset(sensorData.modbus.startAddress), (ushort)sensorData.modbus.totalAddresses);

                                                            // Lukke port
                                                            serialPort.Close();
                                                        }
                                                    }
                                                    break;

                                                case SensorType.ModbusTCP:
                                                    {
                                                        // TCP client
                                                        TcpClient modbusTCPClient = new TcpClient(sensorData.modbus.tcpAddress, sensorData.modbus.tcpPort);

                                                        modbusTCPClient.ReceiveTimeout = Constants.ModbusTimeout;
                                                        modbusTCPClient.SendTimeout = Constants.ModbusTimeout;

                                                        // Starte MODBUS
                                                        IModbusMaster modbusMaster = modbusFactory.CreateMaster(modbusTCPClient);

                                                        // Lese data
                                                        registers = modbusMaster.ReadHoldingRegisters(sensorData.modbus.slaveID, modbusHelper.AddressToOffset(sensorData.modbus.startAddress), (ushort)sensorData.modbus.totalAddresses);

                                                        // Lukke forbindelse
                                                        modbusTCPClient.Close();
                                                    }
                                                    break;
                                            }

                                            // Overføre leste data til midlertidig register
                                            if (registers.Length > 0)
                                            {
                                                for (int i = modbusHelper.AddressToOffset(sensorData.modbus.startAddress); i >= 0 && i < ModbusRegister.RegisterSize && i < registers.Length; i++)
                                                {
                                                    modbusRegister.holding[i].data = registers[i];
                                                    modbusRegister.holding[i].IsSet = true;
                                                }
                                            }
                                            // Sette status dersom vi ikke fikk data
                                            else
                                            {
                                                // Status
                                                sensorData.portStatus = PortStatus.NoData;
                                            }
                                        }

                                        // Hente data
                                        modbusData.data = Convert.ToInt32(modbusRegister.holding[modbusHelper.AddressToOffset(sensorData.modbus.dataAddress)]?.data);

                                        break;
                                }

                                // Data Processing
                                ModbusCalculations process = new ModbusCalculations();
                                DateTime dateTime = DateTime.UtcNow;

                                // Utføre kalkulasjoner på data
                                process.ApplyCalculationsToSelectedData(sensorData, dateTime, modbusData, errorHandler, ErrorMessageCategory.AdminUser);

                                // Lagre resultat
                                sensorData.timestamp = dateTime;
                                sensorData.data = modbusData.calculatedData;

                                // Lagre til databasen
                                if (sensorData.saveToDatabase && sensorData.saveFreq == DatabaseSaveFrequency.Freq_2hz) // All annen lagring gjøre av databaseSaveTimer
                                {
                                    // Legger ikke inn data dersom data ikke er satt
                                    if (!double.IsNaN(sensorData.data))
                                    {
                                        try
                                        {
                                            database.Insert(sensorData);

                                            errorHandler.ResetDatabaseError(ErrorHandler.DatabaseErrorType.Insert4);
                                        }
                                        catch (Exception ex)
                                        {
                                            errorHandler.Insert(
                                                new ErrorMessage(
                                                    DateTime.UtcNow,
                                                    ErrorMessageType.Database,
                                                    ErrorMessageCategory.None,
                                                    string.Format("Database Error (Insert 2)\n\nSystem Message:\n{0}", ex.Message),
                                                    sensorData.id));

                                            errorHandler.SetDatabaseError(ErrorHandler.DatabaseErrorType.Insert4);
                                        }
                                    }
                                }

                                // Sette Warning status dersom vi leser data, men har en feilmelding
                                if (sensorData.HasMessage)
                                    sensorData.portStatus = PortStatus.Warning;
                                else
                                    // Status: Leser ok
                                    sensorData.portStatus = PortStatus.Reading;
                            }
                            // Serie Port feil
                            catch (UnauthorizedAccessException ex)
                            {
                                // Sette alle verdier på denne porten til NoData
                                foreach (var item in modbusSensorList)
                                {
                                    // Plukker ut de som er på denne porten
                                    if (item.modbus.portName == serialPort.PortName)
                                    {
                                        // Status
                                        item.portStatus = PortStatus.NoData;
                                    }
                                }

                                // Sette feilmelding
                                errorHandler.Insert(
                                    new ErrorMessage(
                                        DateTime.UtcNow,
                                        ErrorMessageType.MODBUS,
                                        ErrorMessageCategory.AdminUser,
                                        string.Format("Failed to read MODBUS sensor on serial port: {0}, System Message: {1}", sensorData.modbus.portName, ex.Message),
                                        sensorData.id));
                            }
                            // MODBUS feil
                            catch (Exception ex)
                            {
                                // Status
                                sensorData.portStatus = PortStatus.NoData;

                                // Sette feilmelding
                                switch (sensorData.type)
                                {
                                    case SensorType.ModbusRTU:
                                    case SensorType.ModbusASCII:
                                        errorHandler.Insert(
                                            new ErrorMessage(
                                                DateTime.UtcNow,
                                                ErrorMessageType.MODBUS,
                                                ErrorMessageCategory.AdminUser,
                                                string.Format("Failed to read MODBUS data from sensor on serial port: {0}, System Message: {1}", sensorData.modbus.portName, ex.Message),
                                                sensorData.id));
                                        break;

                                    case SensorType.ModbusTCP:
                                        errorHandler.Insert(
                                            new ErrorMessage(
                                                DateTime.UtcNow,
                                                ErrorMessageType.MODBUS,
                                                ErrorMessageCategory.AdminUser,
                                                string.Format("Failed to read MODBUS data from TCP connection {0}:{1}, System Message: {2}",
                                                    sensorData.modbus.tcpAddress,
                                                    sensorData.modbus.tcpPort,
                                                    ex.Message),
                                                sensorData.id));
                                        break;
                                }
                            }
                        }
                    }
                }
            }
        }

        public void Start()
        {
            if (modbusReader != null)
                modbusReader.Start();
        }

        public void Stop()
        {
            if (modbusReader != null)
                modbusReader.Stop();
        }
    }
}
