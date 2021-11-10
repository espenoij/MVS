using System.Configuration;

namespace SensorMonitor
{
    public class SensorConfigSection : ConfigurationSection
    {
        [ConfigurationProperty("SensorHeaders", IsDefaultCollection = false)]
        [ConfigurationCollection(typeof(SensorConfigCollection),
            AddItemName = "sensor",
            ClearItemsName = "clear",
            RemoveItemName = "remove")]
        public SensorConfigCollection SensorDataItems
        {
            get
            {
                return (SensorConfigCollection)base["SensorHeaders"];
            }
            set
            {
                base["SensorHeaders"] = value;
            }
        }
    }
}
