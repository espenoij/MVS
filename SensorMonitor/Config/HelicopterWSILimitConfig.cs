using System.Configuration;

namespace SensorMonitor
{
    public class HelicopterWSILimitConfig : ConfigurationElement
    {
        public HelicopterWSILimitConfig() { }

        public HelicopterWSILimitConfig(HelicopterType type, double limit)
        {
            id = (int)type;
            this.limit = limit.ToString();
        }

        [ConfigurationProperty("id", DefaultValue = 0, IsRequired = true, IsKey = true)]
        [IntegerValidator(MinValue = 0, MaxValue = int.MaxValue)]
        public int id
        {
            get { return (int)this["id"]; }
            set { this["id"] = value; }
        }

        [ConfigurationProperty("limit", DefaultValue = "", IsRequired = true, IsKey = false)]
        public string limit
        {
            get { return (string)this["limit"]; }
            set { this["limit"] = value; }
        }
    }
}
