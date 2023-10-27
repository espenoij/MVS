using System.Configuration;

namespace MVS
{
    public class MVSDataConfigSection : ConfigurationSection
    {
        [ConfigurationProperty(ConfigKey.HMSDataItems, IsDefaultCollection = false)]
        [ConfigurationCollection(typeof(MVSDataConfigCollection),
            AddItemName = "data",
            ClearItemsName = "clear",
            RemoveItemName = "remove")]
        public MVSDataConfigCollection ClientDataItems
        {
            get
            {
                return (MVSDataConfigCollection)base[ConfigKey.HMSDataItems];
            }
            set
            {
                base[ConfigKey.HMSDataItems] = value;
            }
        }
    }
}
