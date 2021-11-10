using System.Configuration;

namespace SensorMonitorClient
{

    public class HMSDataConfigCollection : ConfigurationElementCollection
    {
        public HMSDataConfigCollection()
        {
        }

        public HMSDataConfig this[int index]
        {
            get { return (HMSDataConfig)BaseGet(index); }
            set
            {
                if (BaseGet(index) != null)
                {
                    BaseRemoveAt(index);
                }
                BaseAdd(index, value);
            }
        }

        public void Add(HMSDataConfig clientDataConfig)
        {
            BaseAdd(clientDataConfig);
        }

        public void Clear()
        {
            BaseClear();
        }

        protected override ConfigurationElement CreateNewElement()
        {
            return new HMSDataConfig();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((HMSDataConfig)element).id;
        }

        public void Remove(HMSDataConfig clientDataConfig)
        {
            BaseRemove(clientDataConfig.id);
        }

        public void RemoveAt(int index)
        {
            BaseRemoveAt(index);
        }

        public void Remove(string name)
        {
            BaseRemove(name);
        }
    }
}
