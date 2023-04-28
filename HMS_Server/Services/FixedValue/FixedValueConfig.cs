using System.Configuration;

namespace HMS_Server
{
    public class FixedValueConfig : ConfigurationElement
    {
        public FixedValueConfig() { }

        public FixedValueConfig(SensorData sensorData)
        {
            this.value = sensorData.fixedValue.value;
            this.frequency = sensorData.fixedValue.frequency.ToString();
        }

        [ConfigurationProperty("value", DefaultValue = "", IsRequired = true, IsKey = false)]
        public string value
        {
            get { return (string)this["value"]; }
            set { this["value"] = value; }
        }

        [ConfigurationProperty("frequency", DefaultValue = "", IsRequired = true, IsKey = false)]
        public string frequency
        {
            get { return (string)this["frequency"]; }
            set { this["frequency"] = value; }
        }
    }
}
