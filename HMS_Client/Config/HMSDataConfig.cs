using System.Configuration;

namespace HMS_Client
{
    public class HMSDataConfig : ConfigurationElement
    {
        public HMSDataConfig() { }

        public HMSDataConfig(HMSData clientData)
        {
            this.id = clientData.id;
            this.name = clientData.name;
            this.dataId = clientData.dataId.ToString();
            this.sensorId = clientData.sensorGroupId.ToString();
        }

        [ConfigurationProperty("id", DefaultValue = 0, IsRequired = true, IsKey = true)]
        [IntegerValidator(MinValue = 0, MaxValue = int.MaxValue)]
        public int id
        {
            get { return (int)this["id"]; }
            set { this["id"] = value; }
        }

        [ConfigurationProperty("name", DefaultValue = "", IsRequired = true, IsKey = false)]
        public string name
        {
            get { return (string)this["name"]; }
            set { this["name"] = value; }
        }

        [ConfigurationProperty("dataId", DefaultValue = "", IsRequired = true, IsKey = false)]
        public string dataId
        {
            get { return (string)this["dataId"]; }
            set { this["dataId"] = value; }
        }

        [ConfigurationProperty("sensorId", DefaultValue = "", IsRequired = true, IsKey = false)]
        public string sensorId
        {
            get { return (string)this["sensorId"]; }
            set { this["sensorId"] = value; }
        }
    }
}
