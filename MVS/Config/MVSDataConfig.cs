﻿using System.Configuration;

namespace MVS
{
    public class MVSDataConfig : ConfigurationElement
    {
        public MVSDataConfig() { }

        public MVSDataConfig(HMSData clientData)
        {
            this.id = clientData.id;
            this.name = clientData.name;
            this.dataId = clientData.dataId.ToString();
            this.dbColumn = clientData.dbColumn.ToString();
        }

        [ConfigurationProperty("id", DefaultValue = 0, IsRequired = true, IsKey = true)]
        [IntegerValidator(MinValue = 0, MaxValue = int.MaxValue)]
        public int id
        {
            get { return (int)this["id"]; }
            set { this["id"] = value; }
        }

        [ConfigurationProperty("name", DefaultValue = "", IsRequired = true, IsKey = false)]
        public string name
        {
            get { return (string)this["name"]; }
            set { this["name"] = value; }
        }

        [ConfigurationProperty("dataId", DefaultValue = "", IsRequired = true, IsKey = false)]
        public string dataId
        {
            get { return (string)this["dataId"]; }
            set { this["dataId"] = value; }
        }

        [ConfigurationProperty("dbColumn", DefaultValue = "", IsRequired = true, IsKey = false)]
        public string dbColumn
        {
            get { return (string)this["dbColumn"]; }
            set { this["dbColumn"] = value; }
        }
    }
}
