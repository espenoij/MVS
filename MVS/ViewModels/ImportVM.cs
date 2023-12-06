using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MVS
{
    public class ImportVM : INotifyPropertyChanged
    {
        // Change notification
        public event PropertyChangedEventHandler PropertyChanged;

        // Configuration settings
        private Config config;

        public ImportVM()
        {
        }

        public void Init(Config config)
        {
            this.config = config;

            // Database Address
            databaseAddress = config.ReadWithDefault(ConfigKey.HMSDatabaseAddress, Constants.DefaultHMSDatabaseAddress);

            // Database Port
            databasePort = config.ReadWithDefault(ConfigKey.HMSDatabasePort, Constants.DefaultHMSDatabasePort);

            // Database Name
            databaseName = config.ReadWithDefault(ConfigKey.HMSDatabaseName, Constants.DefaultHMSDatabaseName);
        }

        /////////////////////////////////////////////////////////////////////////////
        // Database Address
        /////////////////////////////////////////////////////////////////////////////
        private string _databaseAddress { get; set; }
        public string databaseAddress
        {
            get
            {
                return _databaseAddress;
            }
            set
            {
                if (value != null)
                {
                    _databaseAddress = value;
                    config.Write(ConfigKey.HMSDatabaseAddress, value);
                    OnPropertyChanged();
                }
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Server Port
        /////////////////////////////////////////////////////////////////////////////
        private double _databasePort { get; set; }
        public double databasePort
        {
            get
            {
                return _databasePort;
            }
            set
            {
                _databasePort = value;
                config.Write(ConfigKey.HMSDatabasePort, value.ToString());
                OnPropertyChanged();
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Database Name
        /////////////////////////////////////////////////////////////////////////////
        private string _databaseName { get; set; }
        public string databaseName
        {
            get
            {
                return _databaseName;
            }
            set
            {
                if (value != null)
                {
                    _databaseName = value;
                    config.Write(ConfigKey.HMSDatabaseName, value);
                    OnPropertyChanged();
                }
            }
        }

        // Variabel oppdatert
        // Dersom navn ikke er satt brukes kallende medlem sitt navn
        protected virtual void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}

