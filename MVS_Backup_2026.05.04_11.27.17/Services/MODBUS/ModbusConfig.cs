using System.Configuration;

namespace MVS
{
    public class ModbusConfig : ConfigurationElement
    {
        public ModbusConfig() { }

        public ModbusConfig(SensorData sensorData)
        {
            this.portName = sensorData.modbus.portName;
            this.baudRate = sensorData.modbus.baudRate.ToString();
            this.dataBits = sensorData.modbus.dataBits.ToString();
            this.stopBits = sensorData.modbus.stopBits.ToString();
            this.parity = sensorData.modbus.parity.ToString();
            this.handshake = sensorData.modbus.handshake.ToString();

            this.tcpAddress = sensorData.modbus.tcpAddress;
            this.tcpPort = sensorData.modbus.tcpPort.ToString();

            this.slaveID = sensorData.modbus.slaveID.ToString();
            this.startAddress = sensorData.modbus.startAddress.ToString();
            this.totalAddresses = sensorData.modbus.totalAddresses.ToString();
            this.dataAddress = sensorData.modbus.dataAddress.ToString();

            this.calculationType1 = sensorData.modbus.calculationSetup[0].type.ToString();
            this.calculationParameter1 = sensorData.modbus.calculationSetup[0].parameter.ToString();

            this.calculationType2 = sensorData.modbus.calculationSetup[1].type.ToString();
            this.calculationParameter2 = sensorData.modbus.calculationSetup[1].parameter.ToString();

            this.calculationType3 = sensorData.modbus.calculationSetup[2].type.ToString();
            this.calculationParameter3 = sensorData.modbus.calculationSetup[2].parameter.ToString();

            this.calculationType4 = sensorData.modbus.calculationSetup[3].type.ToString();
            this.calculationParameter4 = sensorData.modbus.calculationSetup[3].parameter.ToString();
        }

        [ConfigurationProperty("portName", DefaultValue = "", IsRequired = true, IsKey = false)]
        public string portName
        {
            get { return (string)this["portName"]; }
            set { this["portName"] = value; }
        }

        [ConfigurationProperty("baudRate", DefaultValue = "", IsRequired = true, IsKey = false)]
        public string baudRate
        {
            get { return (string)this["baudRate"]; }
            set { this["baudRate"] = value; }
        }

        [ConfigurationProperty("dataBits", DefaultValue = "", IsRequired = true, IsKey = false)]
        public string dataBits
        {
            get { return (string)this["dataBits"]; }
            set { this["dataBits"] = value; }
        }

        [ConfigurationProperty("stopBits", DefaultValue = "", IsRequired = true, IsKey = false)]
        public string stopBits
        {
            get { return (string)this["stopBits"]; }
            set { this["stopBits"] = value; }
        }

        [ConfigurationProperty("parity", DefaultValue = "", IsRequired = true, IsKey = false)]
        public string parity
        {
            get { return (string)this["parity"]; }
            set { this["parity"] = value; }
        }

        [ConfigurationProperty("handshake", DefaultValue = "", IsRequired = true, IsKey = false)]
        public string handshake
        {
            get { return (string)this["handshake"]; }
            set { this["handshake"] = value; }
        }

        [ConfigurationProperty("tcpAddress", DefaultValue = "", IsRequired = true, IsKey = false)]
        public string tcpAddress
        {
            get { return (string)this["tcpAddress"]; }
            set { this["tcpAddress"] = value; }
        }

        [ConfigurationProperty("tcpPort", DefaultValue = "", IsRequired = true, IsKey = false)]
        public string tcpPort
        {
            get { return (string)this["tcpPort"]; }
            set { this["tcpPort"] = value; }
        }

        [ConfigurationProperty("slaveID", DefaultValue = "", IsRequired = true, IsKey = false)]
        public string slaveID
        {
            get { return (string)this["slaveID"]; }
            set { this["slaveID"] = value; }
        }

        [ConfigurationProperty("startAddress", DefaultValue = "", IsRequired = true, IsKey = false)]
        public string startAddress
        {
            get { return (string)this["startAddress"]; }
            set { this["startAddress"] = value; }
        }

        [ConfigurationProperty("totalAddresses", DefaultValue = "", IsRequired = true, IsKey = false)]
        public string totalAddresses
        {
            get { return (string)this["totalAddresses"]; }
            set { this["totalAddresses"] = value; }
        }

        [ConfigurationProperty("dataAddress", DefaultValue = "", IsRequired = true, IsKey = false)]
        public string dataAddress
        {
            get { return (string)this["dataAddress"]; }
            set { this["dataAddress"] = value; }
        }

        [ConfigurationProperty("calculationType1", DefaultValue = "", IsRequired = true, IsKey = false)]
        public string calculationType1
        {
            get { return (string)this["calculationType1"]; }
            set { this["calculationType1"] = value; }
        }

        [ConfigurationProperty("calculationParameter1", DefaultValue = "", IsRequired = true, IsKey = false)]
        public string calculationParameter1
        {
            get { return (string)this["calculationParameter1"]; }
            set { this["calculationParameter1"] = value; }
        }

        [ConfigurationProperty("calculationType2", DefaultValue = "", IsRequired = true, IsKey = false)]
        public string calculationType2
        {
            get { return (string)this["calculationType2"]; }
            set { this["calculationType2"] = value; }
        }

        [ConfigurationProperty("calculationParameter2", DefaultValue = "", IsRequired = true, IsKey = false)]
        public string calculationParameter2
        {
            get { return (string)this["calculationParameter2"]; }
            set { this["calculationParameter2"] = value; }
        }

        [ConfigurationProperty("calculationType3", DefaultValue = "", IsRequired = true, IsKey = false)]
        public string calculationType3
        {
            get { return (string)this["calculationType3"]; }
            set { this["calculationType3"] = value; }
        }

        [ConfigurationProperty("calculationParameter3", DefaultValue = "", IsRequired = true, IsKey = false)]
        public string calculationParameter3
        {
            get { return (string)this["calculationParameter3"]; }
            set { this["calculationParameter3"] = value; }
        }

        [ConfigurationProperty("calculationType4", DefaultValue = "", IsRequired = true, IsKey = false)]
        public string calculationType4
        {
            get { return (string)this["calculationType4"]; }
            set { this["calculationType4"] = value; }
        }

        [ConfigurationProperty("calculationParameter4", DefaultValue = "", IsRequired = true, IsKey = false)]
        public string calculationParameter4
        {
            get { return (string)this["calculationParameter4"]; }
            set { this["calculationParameter4"] = value; }
        }
    }
}
