using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MVS
{
    public class AdminSettingsVM : INotifyPropertyChanged
    {
        // Change notification
        public event PropertyChangedEventHandler PropertyChanged;

        // Configuration settings
        private Config config;

        private MainWindow.RestartRequiredCallback restartRequired;

        public void Init(Config config, MainWindow.RestartRequiredCallback restartRequired)
        {
            this.config = config;
            this.restartRequired = restartRequired;

            // Server Port
            serverPort = config.ReadWithDefault(ConfigKey.ServerPort, Constants.ServerPortDefault);

            // Database Address
            databaseAddress = config.ReadWithDefault(ConfigKey.DatabaseAddress, Constants.DefaultDatabaseAddress);

            // Database Port
            databasePort = config.ReadWithDefault(ConfigKey.DatabasePort, Constants.DefaultDatabasePort);

            // Database Name
            databaseName = config.ReadWithDefault(ConfigKey.DatabaseName, Constants.DefaultDatabaseName);

            // Database Data Storage Time
            databaseDataStorageTime = config.ReadWithDefault(ConfigKey.DataStorageTime, Constants.DatabaseStorageTimeDefault);

            // Database Data Storage Time
            databaseErrorMessageStorageTime = config.ReadWithDefault(ConfigKey.ErrorMessageStorageTime, Constants.DatabaseStorageTimeDefault);

            // Timing
            setupGUIDataLimit = config.ReadWithDefault(ConfigKey.SetupGUIDataLimit, Constants.GUIDataLimitDefault);
            serverUIUpdateFrequency = config.ReadWithDefault(ConfigKey.ServerUIUpdateFrequency, Constants.ServerUIUpdateFrequencyDefault);
            databaseSaveFrequency = config.ReadWithDefault(ConfigKey.DatabaseSaveFrequency, Constants.DatabaseSaveFreqDefault);
            dataTimeout = config.ReadWithDefault(ConfigKey.DataTimeout, Constants.DataTimeoutDefault);
            hmsProcessingFrequency = config.ReadWithDefault(ConfigKey.HMSProcessingFrequency, Constants.HMSProcessingFrequencyDefault);

            // Sensor
            waveHeightCutoff = config.ReadWithDefault(ConfigKey.WaveHeightCutoff, Constants.WaveHeightCutoffDefault);
        }

        public void ApplicationRestartRequired(bool showMessage = true)
        {
            // Callback funksjon for å vise restart app melding
            restartRequired(showMessage);
        }

        /////////////////////////////////////////////////////////////////////////////
        // Server Port
        /////////////////////////////////////////////////////////////////////////////
        private double _serverPort { get; set; }
        public double serverPort
        {
            get
            {
                return _serverPort;
            }
            set
            {
                _serverPort = value;
                config.Write(ConfigKey.ServerPort, value.ToString());
                OnPropertyChanged();
            }
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
                    config.Write(ConfigKey.DatabaseAddress, value);
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
                config.Write(ConfigKey.DatabasePort, value.ToString());
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
                    config.Write(ConfigKey.DatabaseName, value);
                    OnPropertyChanged();
                }
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Database Data Storage Time
        /////////////////////////////////////////////////////////////////////////////
        private double _databaseDataStorageTime { get; set; }
        public double databaseDataStorageTime
        {
            get
            {
                return _databaseDataStorageTime;
            }
            set
            {
                _databaseDataStorageTime = value;
                config.Write(ConfigKey.DataStorageTime, value.ToString());
                OnPropertyChanged();
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Database Error Message Storage Time
        /////////////////////////////////////////////////////////////////////////////
        private double _databaseErrorMessageStorageTime { get; set; }
        public double databaseErrorMessageStorageTime
        {
            get
            {
                return _databaseErrorMessageStorageTime;
            }
            set
            {
                _databaseErrorMessageStorageTime = value;
                config.Write(ConfigKey.ErrorMessageStorageTime, value.ToString());
                OnPropertyChanged();
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Timing: GUI Data Limit
        /////////////////////////////////////////////////////////////////////////////
        private double _setupGUIDataLimit { get; set; }
        public double setupGUIDataLimit
        {
            get
            {
                return _setupGUIDataLimit;
            }
            set
            {
                _setupGUIDataLimit = value;
                config.Write(ConfigKey.SetupGUIDataLimit, value.ToString());
                OnPropertyChanged();
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Timing: GUI Data Limit
        /////////////////////////////////////////////////////////////////////////////
        private double _serverUIUpdateFrequency { get; set; }
        public double serverUIUpdateFrequency
        {
            get
            {
                return _serverUIUpdateFrequency;
            }
            set
            {
                _serverUIUpdateFrequency = value;
                config.Write(ConfigKey.ServerUIUpdateFrequency, value.ToString());
                OnPropertyChanged();
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Timing: Save to Database
        /////////////////////////////////////////////////////////////////////////////
        private double _databaseSaveFrequency { get; set; }
        public double databaseSaveFrequency
        {
            get
            {
                return _databaseSaveFrequency;
            }
            set
            {
                _databaseSaveFrequency = value;
                config.Write(ConfigKey.DatabaseSaveFrequency, value.ToString());
                OnPropertyChanged();
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Timing: Data Timeout (ms)
        /////////////////////////////////////////////////////////////////////////////
        private double _dataTimeout { get; set; }
        public double dataTimeout
        {
            get
            {
                return _dataTimeout;
            }
            set
            {
                _dataTimeout = value;
                config.Write(ConfigKey.DataTimeout, value.ToString());
                OnPropertyChanged();
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Timing: HMS Processing
        /////////////////////////////////////////////////////////////////////////////
        private double _hmsProcessingFrequency { get; set; }
        public double hmsProcessingFrequency
        {
            get
            {
                return _hmsProcessingFrequency;
            }
            set
            {
                _hmsProcessingFrequency = value;
                config.Write(ConfigKey.HMSProcessingFrequency, value.ToString());
                OnPropertyChanged();
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Wave Height Cut-Off (Period)
        /////////////////////////////////////////////////////////////////////////////
        private double _waveHeightCutoff { get; set; }
        public double waveHeightCutoff
        {
            get
            {
                return _waveHeightCutoff;
            }
            set
            {
                _waveHeightCutoff = value;
                config.Write(ConfigKey.WaveHeightCutoff, _waveHeightCutoff.ToString());
                OnPropertyChanged();
            }
        }

        // Variabel oppdatert
        // Dersom navn ikke settes brukes kallende medlem sitt navn
        protected virtual void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    public class HelicopterWSILimit
    {
        public HelicopterType helicopterType { get; set; }
        public double limit;

        public HelicopterWSILimit(HelicopterWSILimitConfig data)
        {
            helicopterType = (HelicopterType)data.id;
            double.TryParse(data.limit, out limit);
        }
    }
}
