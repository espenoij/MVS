using System.Configuration;

namespace HMS_Client
{
    public class HelicopterOperatorConfigSection : ConfigurationSection
    {
        [ConfigurationProperty("HelicopterOperatorHeaders", IsDefaultCollection = false)]
        [ConfigurationCollection(typeof(HelicopterOperatorConfigCollection),
            AddItemName = "helicopterOperator",
            ClearItemsName = "clear",
            RemoveItemName = "remove")]
        public HelicopterOperatorConfigCollection HelicopterOperatorDataItems
        {
            get
            {
                return (HelicopterOperatorConfigCollection)base["HelicopterOperatorHeaders"];
            }
            set
            {
                base["HelicopterOperatorHeaders"] = value;
            }
        }
    }
}
