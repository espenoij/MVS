using System.Configuration;

namespace MVS
{
    public class MVSDataConfigSection : ConfigurationSection
    {
        [ConfigurationProperty(ConfigKey.MVSDataItems, IsDefaultCollection = false)]
        [ConfigurationCollection(typeof(MVSDataConfigCollection),
            AddItemName = "data",
            ClearItemsName = "clear",
            RemoveItemName = "remove")]
        public MVSDataConfigCollection ClientDataItems
        {
            get
            {
                return (MVSDataConfigCollection)base[ConfigKey.MVSDataItems];
            }
            set
            {
                base[ConfigKey.MVSDataItems] = value;
            }
        }
    }
}
