using System.Configuration;

namespace SensorMonitorClient
{

    public class HelicopterOperatorConfigCollection : ConfigurationElementCollection
    {
        public HelicopterOperatorConfigCollection()
        {
        }

        public HelicopterOperatorConfig this[int index]
        {
            get { return (HelicopterOperatorConfig)BaseGet(index); }
            set
            {
                if (BaseGet(index) != null)
                {
                    BaseRemoveAt(index);
                }
                BaseAdd(index, value);
            }
        }

        public void Add(HelicopterOperatorConfig clientDataConfig)
        {
            BaseAdd(clientDataConfig);
        }

        public void Clear()
        {
            BaseClear();
        }

        protected override ConfigurationElement CreateNewElement()
        {
            return new HelicopterOperatorConfig();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((HelicopterOperatorConfig)element).id;
        }

        public void Remove(HelicopterOperatorConfig clientDataConfig)
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
