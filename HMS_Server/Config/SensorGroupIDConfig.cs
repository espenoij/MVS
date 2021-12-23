using System.Configuration;

namespace HMS_Server
{
    public class SensorGroupIDConfig : ConfigurationElement
    {
        public SensorGroupIDConfig() { }

        public SensorGroupIDConfig(SensorGroup sensor)
        {
            if (sensor != null)
            {
                id = sensor.id;
                name = sensor.name;
                dbColumn = sensor.dbColumn;
                active = sensor.active.ToString();
            }
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

        [ConfigurationProperty("dbColumn", DefaultValue = "", IsRequired = true, IsKey = false)]
        public string dbColumn
        {
            get { return (string)this["dbColumn"]; }
            set { this["dbColumn"] = value; }
        }

        [ConfigurationProperty("active", DefaultValue = "", IsRequired = true, IsKey = false)]
        public string active
        {
            get { return (string)this["active"]; }
            set { this["active"] = value; }
        }
    }
}
