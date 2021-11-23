using System.Configuration;

namespace HMS_Server
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
    }
}
