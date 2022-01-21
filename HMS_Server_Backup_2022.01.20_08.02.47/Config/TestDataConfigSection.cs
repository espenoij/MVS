using System.Configuration;

namespace HMS_Server
{
    public class TestDataConfigSection : ConfigurationSection
    {
        [ConfigurationProperty(ConfigKey.TestDataItems, IsDefaultCollection = false)]
        [ConfigurationCollection(typeof(TestDataConfigCollection),
            AddItemName = "data",
            ClearItemsName = "clear",
            RemoveItemName = "remove")]
        public TestDataConfigCollection ClientDataItems
        {
            get
            {
                return (TestDataConfigCollection)base[ConfigKey.TestDataItems];
            }
            set
            {
                base[ConfigKey.TestDataItems] = value;
            }
        }
    }
}
