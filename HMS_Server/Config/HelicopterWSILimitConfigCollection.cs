using System.Configuration;

namespace HMS_Server
{

    public class HelicopterWSILimitConfigCollection : ConfigurationElementCollection
    {
        public HelicopterWSILimitConfigCollection()
        {
        }

        public HelicopterWSILimitConfig this[int index]
        {
            get { return (HelicopterWSILimitConfig)BaseGet(index); }
            set
            {
                if (BaseGet(index) != null)
                {
                    BaseRemoveAt(index);
                }
                BaseAdd(index, value);
            }
        }

        public void Add(HelicopterWSILimitConfig clientDataConfig)
        {
            BaseAdd(clientDataConfig);
        }

        public void Clear()
        {
            BaseClear();
        }

        protected override ConfigurationElement CreateNewElement()
        {
            return new HelicopterWSILimitConfig();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((HelicopterWSILimitConfig)element).id;
        }

        public void Remove(HelicopterWSILimitConfig clientDataConfig)
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
