using System.Configuration;

namespace MVS
{
    public class SensorConfig : ConfigurationElement
    {
        public SensorConfig() { }

        public SensorConfig(SensorData sensorData)
        {
            this.id = sensorData.id;
            this.type = sensorData.type.ToString();
            this.name = sensorData.name;
            this.description = sensorData.description;

            this.mruType = sensorData.mruType.ToString();

            this.serialPort = null;
            this.modbus = null;
            this.fileReader = null;

            switch (sensorData.type)
            {
                case SensorType.SerialPort:
                    this.serialPort = new SerialPortConfig(sensorData);
                    break;

                case SensorType.ModbusRTU:
                case SensorType.ModbusASCII:
                case SensorType.ModbusTCP:
                    this.modbus = new ModbusConfig(sensorData);
                    break;

                case SensorType.FileReader:
                    this.fileReader = new FileReaderConfig(sensorData);
                    break;

                case SensorType.FixedValue:
                    this.fixedValue = new FixedValueConfig(sensorData);
                    break;
            }
        }

        [ConfigurationProperty("id", DefaultValue = 0, IsRequired = true, IsKey = true)]
        [IntegerValidator(MinValue = 0, MaxValue = int.MaxValue)]
        public int id
        {
            get { return (int)this["id"]; }
            set { this["id"] = value; }
        }

        [ConfigurationProperty("type", DefaultValue = "", IsRequired = true, IsKey = false)]
        public string type
        {
            get { return (string)this["type"]; }
            set { this["type"] = value; }
        }

        [ConfigurationProperty("name", DefaultValue = "", IsRequired = true, IsKey = false)]
        public string name
        {
            get { return (string)this["name"]; }
            set { this["name"] = value; }
        }

        [ConfigurationProperty("description", DefaultValue = "", IsRequired = true, IsKey = false)]
        public string description
        {
            get { return (string)this["description"]; }
            set { this["description"] = value; }
        }

        [ConfigurationProperty("mruType", DefaultValue = "", IsRequired = true, IsKey = false)]
        public string mruType
        {
            get { return (string)this["mruType"]; }
            set { this["mruType"] = value; }
        }

        [ConfigurationProperty("serialPort", IsRequired = false)]
        public SerialPortConfig serialPort
        {
            get { return (SerialPortConfig)this["serialPort"]; }
            set { this["serialPort"] = value; }
        }

        [ConfigurationProperty("modbus", IsRequired = false)]
        public ModbusConfig modbus
        {
            get { return (ModbusConfig)this["modbus"]; }
            set { this["modbus"] = value; }
        }

        [ConfigurationProperty("fileReader", IsRequired = false)]
        public FileReaderConfig fileReader
        {
            get { return (FileReaderConfig)this["fileReader"]; }
            set { this["fileReader"] = value; }
        }

        [ConfigurationProperty("fixedValue", IsRequired = false)]
        public FixedValueConfig fixedValue
        {
            get { return (FixedValueConfig)this["fixedValue"]; }
            set { this["fixedValue"] = value; }
        }
    }
}
