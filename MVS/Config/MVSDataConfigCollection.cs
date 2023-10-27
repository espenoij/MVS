using System.Configuration;

namespace MVS
{

    public class MVSDataConfigCollection : ConfigurationElementCollection
    {
        public MVSDataConfigCollection()
        {
        }

        public MVSDataConfig this[int index]
        {
            get { return (MVSDataConfig)BaseGet(index); }
            set
            {
                if (BaseGet(index) != null)
                {
                    BaseRemoveAt(index);
                }
                BaseAdd(index, value);
            }
        }

        public void Add(MVSDataConfig clientDataConfig)
        {
            BaseAdd(clientDataConfig);
        }

        public void Clear()
        {
            BaseClear();
        }

        protected override ConfigurationElement CreateNewElement()
        {
            return new MVSDataConfig();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((MVSDataConfig)element).id;
        }

        public void Remove(MVSDataConfig clientDataConfig)
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
