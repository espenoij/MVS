using System.Configuration;

namespace MVS
{
    public class FileReaderConfig : ConfigurationElement
    {
        public FileReaderConfig() { }

        public FileReaderConfig(SensorData sensorData)
        {
            this.fileFolder = sensorData.fileReader.fileFolder;
            this.fileName = sensorData.fileReader.fileName;
            this.readFrequency = sensorData.fileReader.readFrequency.ToString();
            this.delimiter = sensorData.fileReader.delimiter;
            this.fixedPosData = sensorData.fileReader.fixedPosData.ToString();
            this.fixedPosStart = sensorData.fileReader.fixedPosStart.ToString();
            this.fixedPosTotal = sensorData.fileReader.fixedPosTotal.ToString();
            this.dataField = sensorData.fileReader.dataField;
            this.decimalSeparator = sensorData.fileReader.decimalSeparator.ToString();
            this.autoExtractValue = sensorData.fileReader.autoExtractValue.ToString();
            this.calculationType1 = sensorData.fileReader.calculationSetup[0].type.ToString();
            this.calculationParameter1 = sensorData.fileReader.calculationSetup[0].parameter.ToString();
            this.calculationType2 = sensorData.fileReader.calculationSetup[1].type.ToString();
            this.calculationParameter2 = sensorData.fileReader.calculationSetup[1].parameter.ToString();
            this.calculationType3 = sensorData.fileReader.calculationSetup[2].type.ToString();
            this.calculationParameter3 = sensorData.fileReader.calculationSetup[2].parameter.ToString();
            this.calculationType4 = sensorData.fileReader.calculationSetup[3].type.ToString();
            this.calculationParameter4 = sensorData.fileReader.calculationSetup[3].parameter.ToString();
        }

        [ConfigurationProperty("fileFolder", DefaultValue = "", IsRequired = true, IsKey = false)]
        public string fileFolder
        {
            get { return (string)this["fileFolder"]; }
            set { this["fileFolder"] = value; }
        }

        [ConfigurationProperty("fileName", DefaultValue = "", IsRequired = true, IsKey = false)]
        public string fileName
        {
            get { return (string)this["fileName"]; }
            set { this["fileName"] = value; }
        }

        [ConfigurationProperty("readFrequency", DefaultValue = "", IsRequired = true, IsKey = false)]
        public string readFrequency
        {
            get { return (string)this["readFrequency"]; }
            set { this["readFrequency"] = value; }
        }

        [ConfigurationProperty("delimiter", DefaultValue = "", IsRequired = true, IsKey = false)]
        public string delimiter
        {
            get { return (string)this["delimiter"]; }
            set { this["delimiter"] = value; }
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
