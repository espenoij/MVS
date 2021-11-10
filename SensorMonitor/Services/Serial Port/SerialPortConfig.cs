using System.Configuration;
using System.IO.Ports;

namespace SensorMonitor
{
    public class SerialPortConfig : ConfigurationElement
    {
        public SerialPortConfig() { }

        public SerialPortConfig(SensorData sensorData)
        {
            this.portName = sensorData.serialPort.portName;
            this.baudRate = sensorData.serialPort.baudRate.ToString();
            this.dataBits = sensorData.serialPort.dataBits.ToString();
            this.stopBits = sensorData.serialPort.stopBits.ToString();
            this.parity = sensorData.serialPort.parity.ToString();
            this.handshake = sensorData.serialPort.handshake.ToString();
            this.packetHeader = sensorData.serialPort.packetHeader;
            this.packetEnd = sensorData.serialPort.packetEnd;
            this.packetDelimiter = sensorData.serialPort.packetDelimiter;
            this.packetCombineFields = sensorData.serialPort.packetCombineFields.ToString();
            this.fixedPosData = sensorData.serialPort.fixedPosData.ToString();
            this.fixedPosStart = sensorData.serialPort.fixedPosStart.ToString();
            this.fixedPosTotal = sensorData.serialPort.fixedPosTotal.ToString();
            this.dataField = sensorData.serialPort.dataField;
            this.decimalSeparator = sensorData.serialPort.decimalSeparator.ToString();
            this.autoExtractValue = sensorData.serialPort.autoExtractValue.ToString();

            this.calculationType1 = sensorData.serialPort.calculationSetup[0].type.ToString();
            this.calculationParameter1 = sensorData.serialPort.calculationSetup[0].parameter.ToString();

            this.calculationType2 = sensorData.serialPort.calculationSetup[1].type.ToString();
            this.calculationParameter2 = sensorData.serialPort.calculationSetup[1].parameter.ToString();

            this.calculationType3 = sensorData.serialPort.calculationSetup[2].type.ToString();
            this.calculationParameter3 = sensorData.serialPort.calculationSetup[2].parameter.ToString();

            this.calculationType4 = sensorData.serialPort.calculationSetup[3].type.ToString();
            this.calculationParameter4 = sensorData.serialPort.calculationSetup[3].parameter.ToString();
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

        [ConfigurationProperty("packetHeader", DefaultValue = "", IsRequired = true, IsKey = false)]
        public string packetHeader
        {
            get { return (string)this["packetHeader"]; }
            set { this["packetHeader"] = value; }
        }

        [ConfigurationProperty("packetEnd", DefaultValue = "", IsRequired = true, IsKey = false)]
        public string packetEnd
        {
            get { return (string)this["packetEnd"]; }
            set { this["packetEnd"] = value; }
        }

        [ConfigurationProperty("packetDelimiter", DefaultValue = "", IsRequired = true, IsKey = false)]
        public string packetDelimiter
        {
            get { return (string)this["packetDelimiter"]; }
            set { this["packetDelimiter"] = value; }
        }

        [ConfigurationProperty("packetCombineFields", DefaultValue = "", IsRequired = true, IsKey = false)]
        public string packetCombineFields
        {
            get { return (string)this["packetCombineFields"]; }
            set { this["packetCombineFields"] = value; }
        }

        [ConfigurationProperty("fixedPosData", DefaultValue = "", IsRequired = true, IsKey = false)]
        public string fixedPosData
        {
            get { return (string)this["fixedPosData"]; }
            set { this["fixedPosData"] = value; }
        }

        [ConfigurationProperty("fixedPosStart", DefaultValue = "", IsRequired = true, IsKey = false)]
        public string fixedPosStart
        {
            get { return (string)this["fixedPosStart"]; }
            set { this["fixedPosStart"] = value; }
        }

        [ConfigurationProperty("fixedPosTotal", DefaultValue = "", IsRequired = true, IsKey = false)]
        public string fixedPosTotal
        {
            get { return (string)this["fixedPosTotal"]; }
            set { this["fixedPosTotal"] = value; }
        }

        [ConfigurationProperty("dataField", DefaultValue = "", IsRequired = true, IsKey = false)]
        public string dataField
        {
            get { return (string)this["dataField"]; }
            set { this["dataField"] = value; }
        }

        [ConfigurationProperty("decimalSeparator", DefaultValue = "", IsRequired = true, IsKey = false)]
        public string decimalSeparator
        {
            get { return (string)this["decimalSeparator"]; }
            set { this["decimalSeparator"] = value; }
        }

        [ConfigurationProperty("autoExtractValue", DefaultValue = "", IsRequired = true, IsKey = false)]
        public string autoExtractValue
        {
            get { return (string)this["autoExtractValue"]; }
            set { this["autoExtractValue"] = value; }
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
