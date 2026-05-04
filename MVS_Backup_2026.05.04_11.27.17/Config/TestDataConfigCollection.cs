using System.Configuration;

namespace MVS
{

    public class TestDataConfigCollection : ConfigurationElementCollection
    {
        public TestDataConfigCollection()
        {
        }

        public TestDataConfig this[int index]
        {
            get { return (TestDataConfig)BaseGet(index); }
            set
            {
                if (BaseGet(index) != null)
                {
                    BaseRemoveAt(index);
                }
                BaseAdd(index, value);
            }
        }

        public void Add(TestDataConfig clientDataConfig)
        {
            BaseAdd(clientDataConfig);
        }

        public void Clear()
        {
            BaseClear();
        }

        protected override ConfigurationElement CreateNewElement()
        {
            return new TestDataConfig();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((TestDataConfig)element).id;
        }

        public void Remove(TestDataConfig clientDataConfig)
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
