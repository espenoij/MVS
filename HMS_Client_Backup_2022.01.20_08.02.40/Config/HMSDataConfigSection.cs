﻿using System.Configuration;

namespace HMS_Client
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