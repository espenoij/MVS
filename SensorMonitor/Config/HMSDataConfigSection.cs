using System.Configuration;

namespace HMS_Server
{
    public class HMSDataConfigSection : ConfigurationSection
    {
        [ConfigurationProperty(ConfigKey.HMSDataItems, IsDefaultCollection = false)]
        [ConfigurationCollection(typeof(HMSDataConfigCollection),
            AddItemName = "data",
            ClearItemsName = "clear",
            RemoveItemName = "remove")]
        public HMSDataConfigCollection ClientDataItems
        {
            get
            {
                return (HMSDataConfigCollection)base[ConfigKey.HMSDataItems];
            }
            set
            {
                base[ConfigKey.HMSDataItems] = value;
            }
        }
    }
}
