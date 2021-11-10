using System.Configuration;

namespace SensorMonitor
{
    public class SensorGroupIDConfigSection : ConfigurationSection
    {
        [ConfigurationProperty("SensorGroupIDHeaders", IsDefaultCollection = false)]
        [ConfigurationCollection(typeof(SensorGroupIDConfigCollection),
            AddItemName = "sensorGroup",
            ClearItemsName = "clear",
            RemoveItemName = "remove")]
        public SensorGroupIDConfigCollection SensorIDDataItems
        {
            get
            {
                return (SensorGroupIDConfigCollection)base["SensorGroupIDHeaders"];
            }
            set
            {
                base["SensorGroupIDHeaders"] = value;
            }
        }
    }
}
