using System.Configuration;

namespace SensorMonitor
{

    public class SensorGroupIDConfigCollection : ConfigurationElementCollection
    {
        public SensorGroupIDConfigCollection()
        {
        }

        public SensorGroupIDConfig this[int index]
        {
            get { return (SensorGroupIDConfig)BaseGet(index); }
            set
            {
                if (BaseGet(index) != null)
                {
                    BaseRemoveAt(index);
                }
                BaseAdd(index, value);
            }
        }

        public void Add(SensorGroupIDConfig clientDataConfig)
        {
            BaseAdd(clientDataConfig);
        }

        public void Clear()
        {
            BaseClear();
        }

        protected override ConfigurationElement CreateNewElement()
        {
            return new SensorGroupIDConfig();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((SensorGroupIDConfig)element).id;
        }

        public void Remove(SensorGroupIDConfig clientDataConfig)
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
