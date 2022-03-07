using System.Configuration;

namespace HMS_Server
{
    public class ReferenceDataConfigSection : ConfigurationSection
    {
        [ConfigurationProperty(ConfigKey.ReferenceDataItems, IsDefaultCollection = false)]
        [ConfigurationCollection(typeof(ReferenceDataConfigCollection),
            AddItemName = "data",
            ClearItemsName = "clear",
            RemoveItemName = "remove")]
        public ReferenceDataConfigCollection ClientDataItems
        {
            get
            {
                return (ReferenceDataConfigCollection)base[ConfigKey.ReferenceDataItems];
            }
            set
            {
                base[ConfigKey.ReferenceDataItems] = value;
            }
        }
    }
}
