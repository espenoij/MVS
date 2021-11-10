using System.Configuration;

namespace SensorMonitor
{
    public class HelicopterWSILimitConfigSection : ConfigurationSection
    {
        [ConfigurationProperty("HelicopterWSILimitHeaders", IsDefaultCollection = false)]
        [ConfigurationCollection(typeof(HelicopterWSILimitConfigCollection),
            AddItemName = "helicopter",
            ClearItemsName = "clear",
            RemoveItemName = "remove")]
        public HelicopterWSILimitConfigCollection HelicopterWSILimitDataItems
        {
            get
            {
                return (HelicopterWSILimitConfigCollection)base["HelicopterWSILimitHeaders"];
            }
            set
            {
                base["HelicopterWSILimitHeaders"] = value;
            }
        }
    }
}
