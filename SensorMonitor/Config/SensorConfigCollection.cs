using System.Configuration;

namespace HMS_Server
{

    public class SensorConfigCollection : ConfigurationElementCollection
    {
        public SensorConfigCollection()
        {
        }

        public SensorConfig this[int index]
        {
            get { return (SensorConfig)BaseGet(index); }
            set
            {
                if (BaseGet(index) != null)
                {
                    BaseRemoveAt(index);
                }
                BaseAdd(index, value);
            }
        }

        public void Add(SensorConfig sensorDataConfig)
        {
            BaseAdd(sensorDataConfig);
        }

        public void Clear()
        {
            BaseClear();
        }

        protected override ConfigurationElement CreateNewElement()
        {
            return new SensorConfig();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((SensorConfig)element).id;
        }

        public void Remove(SensorConfig sensorDataConfig)
        {
            BaseRemove(sensorDataConfig.id);
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
