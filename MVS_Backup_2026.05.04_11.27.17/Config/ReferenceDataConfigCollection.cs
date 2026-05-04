using System.Configuration;

namespace MVS
{

    public class ReferenceDataConfigCollection : ConfigurationElementCollection
    {
        public ReferenceDataConfigCollection()
        {
        }

        public ReferenceDataConfig this[int index]
        {
            get { return (ReferenceDataConfig)BaseGet(index); }
            set
            {
                if (BaseGet(index) != null)
                {
                    BaseRemoveAt(index);
                }
                BaseAdd(index, value);
            }
        }

        public void Add(ReferenceDataConfig clientDataConfig)
        {
            BaseAdd(clientDataConfig);
        }

        public void Clear()
        {
            BaseClear();
        }

        protected override ConfigurationElement CreateNewElement()
        {
            return new ReferenceDataConfig();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((ReferenceDataConfig)element).id;
        }

        public void Remove(ReferenceDataConfig clientDataConfig)
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
