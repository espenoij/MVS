using System.Configuration;

namespace HMS_Server
{
    public class FileReaderConfig : ConfigurationElement
    {
        public FileReaderConfig() { }

        public FileReaderConfig(SensorData sensorData)
        {
            this.filePath = sensorData.fileReader.filePath;
            this.fileName = sensorData.fileReader.fileName;
        }

        [ConfigurationProperty("filePath", DefaultValue = "", IsRequired = true, IsKey = false)]
        public string filePath
        {
            get { return (string)this["filePath"]; }
            set { this["filePath"] = value; }
        }

        [ConfigurationProperty("fileName", DefaultValue = "", IsRequired = true, IsKey = false)]
        public string fileName
        {
            get { return (string)this["fileName"]; }
            set { this["fileName"] = value; }
        }
    }
}
