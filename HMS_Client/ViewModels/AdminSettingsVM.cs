using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace HMS_Client
{
    public class AdminSettingsVM : INotifyPropertyChanged
    {
        // Change notification
        public event PropertyChangedEventHandler PropertyChanged;

        // Configuration settings
        private Config config;

        // Server Communications modul
        private ServerCom serverCom;

        // App Restart Required
        private MainWindow.RestartRequiredCallback restartRequired;

        public void Init(Config config, ServerCom serverCom, MainWindow.RestartRequiredCallback restartRequired)
        {
            this.config = config;
            this.serverCom = serverCom;
            this.restartRequired = restartRequired;

            // Regulation Standard
            regulationStandard = (RegulationStandard)Enum.Parse(typeof(RegulationStandard), config.ReadWithDefault(ConfigKey.RegulationStandard, RegulationStandard.NOROG.ToString()));

            // Client is Master
            if (config.Read(ConfigKey.ClientIsMaster) == "1")
                clientIsMaster = true;
            else
                clientIsMaster = false;

            // Update Frequencies
            uiUpdateFrequency = config.ReadWithDefault(ConfigKey.ClientUpdateFrequencyUI, Constants.ClientUpdateFrequencyUIDefault);
            dataTimeout = config.ReadWithDefault(ConfigKey.DataTimeout, Constants.DataTimeoutDefault);
            chartDataUpdateFrequency20m = config.ReadWithDefault(ConfigKey.ChartDataUpdateFrequency20m, Constants.ChartUpdateFrequencyUI20mDefault);
            chartDataUpdateFrequency3h = config.ReadWithDefault(ConfigKey.ChartDataUpdateFrequency3h, Constants.ChartUpdateFrequencyUI3hDefault);

            // Server Address
            serverAddress = config.Read(ConfigKey.ServerAddress);

            // Server Port
            serverPort = config.ReadWithDefault(ConfigKey.ServerPort, Constants.ServerPortDefault);

            // Server: Data Request Frequency
            dataRequestFrequency = config.ReadWithDefault(ConfigKey.DataRequestFrequency, Constants.DataRequestFrequencyDefault);

            // Server: Sensor Status Request Frequency
            sensorStatusRequestFrequency = config.ReadWithDefault(ConfigKey.SensorStatusRequestFrequency, Constants.SensorStatusRequestFrequencyDefault);
        }

        public void ApplicationRestartRequired(bool showMessage = true)
        {
            // Callback funksjon for å vise restart app melding
            restartRequired(showMessage);
        }

        /////////////////////////////////////////////////////////////////////////////
        // General Settings: Regulation Standard
        /////////////////////////////////////////////////////////////////////////////
        private RegulationStandard _regulationStandard { get; set; }
        public RegulationStandard regulationStandard
        {
            get
            {
                return _regulationStandard;
            }
            set
            {
                _regulationStandard = value;
                config.Write(ConfigKey.RegulationStandard, value.ToString());
                OnPropertyChanged(nameof(regulationStandardString));
            }
        }
        public string regulationStandardString
        {
            get
            {
                return _regulationStandard.ToString();
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Client Is Master
        /////////////////////////////////////////////////////////////////////////////
        private bool _clientIsMaster { get; set; }
        public bool clientIsMaster
        {
            get
            {
                return _clientIsMaster;
            }
            set
            {
                _clientIsMaster = value;

                if (_clientIsMaster)
                {
                    config.Write(ConfigKey.ClientIsMaster, "1");
                    //serverCom.StopUserInputsRequest();
                }
                else
                {
                    config.Write(ConfigKey.ClientIsMaster, "0");
                    //serverCom.StartUserInputsRequest();
                }

                OnPropertyChanged();
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // UI Update Frequency
        /////////////////////////////////////////////////////////////////////////////
        private double _uiUpdateFrequency { get; set; }
        public double uiUpdateFrequency
        {
            get
            {
                return _uiUpdateFrequency;
            }
            set
            {
                _uiUpdateFrequency = value;
                config.Write(ConfigKey.ClientUpdateFrequencyUI, value.ToString());
                OnPropertyChanged();
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Data Timeout
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
        // Chart Data Update Frequency
        /////////////////////////////////////////////////////////////////////////////
        private double _chartDataUpdateFrequency20m { get; set; }
        public double chartDataUpdateFrequency20m
        {
            get
            {
                return _chartDataUpdateFrequency20m;
            }
            set
            {
                _chartDataUpdateFrequency20m = value;
                config.Write(ConfigKey.ChartDataUpdateFrequency20m, value.ToString());
                OnPropertyChanged();
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Chart Data Update Frequency
        /////////////////////////////////////////////////////////////////////////////
        private double _chartDataUpdateFrequency3h { get; set; }
        public double chartDataUpdateFrequency3h
        {
            get
            {
                return _chartDataUpdateFrequency3h;
            }
            set
            {
                _chartDataUpdateFrequency3h = value;
                config.Write(ConfigKey.ChartDataUpdateFrequency3h, value.ToString());
                OnPropertyChanged();
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Server Address
        /////////////////////////////////////////////////////////////////////////////
        private string _serverAddress { get; set; }
        public string serverAddress
        {
            get
            {
                return _serverAddress;
            }
            set
            {
                _serverAddress = value;

                OnPropertyChanged();
            }
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
        // Data Request Frequency
        /////////////////////////////////////////////////////////////////////////////
        private double _dataRequestFrequency { get; set; }
        public double dataRequestFrequency
        {
            get
            {
                return _dataRequestFrequency;
            }
            set
            {
                _dataRequestFrequency = value;
                config.Write(ConfigKey.DataRequestFrequency, value.ToString());
                OnPropertyChanged();
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Sensor Status Request Frequency
        /////////////////////////////////////////////////////////////////////////////
        private double _sensorStatusRequestFrequency { get; set; }
        public double sensorStatusRequestFrequency
        {
            get
            {
                return _sensorStatusRequestFrequency;
            }
            set
            {
                _sensorStatusRequestFrequency = value;
                config.Write(ConfigKey.SensorStatusRequestFrequency, value.ToString());
                OnPropertyChanged();
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Visualization: Vessel Image
        /////////////////////////////////////////////////////////////////////////////
        private VesselImage _vesselImage { get; set; }
        public VesselImage vesselImage
        {
            get
            {
                return _vesselImage;
            }
            set
            {
                _vesselImage = value;
                config.Write(ConfigKey.VesselImage, value.ToString());
                OnPropertyChanged(nameof(vesselImageString));
            }
        }
        public string vesselImageString
        {
            get
            {
                return _vesselImage.ToString();
            }
        }

        // Variabel oppdatert
        // Dersom navn ikke sette brukes kallende medlem sitt navn
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    public enum RegulationStandard
    {
        NOROG,
        CAP
    }

    public enum VesselImage
    {
        None,
        Triangle,
        Rig,
        Ship
    }
}
