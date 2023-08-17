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
        private MainWindow.WarningBarMessage warningBarMessage;

        public void Init(Config config, ServerCom serverCom, MainWindow.WarningBarMessage warningBarMessage)
        {
            this.config = config;
            this.serverCom = serverCom;
            this.warningBarMessage = warningBarMessage;

            // Regulation Standard
            regulationStandard = (RegulationStandard)Enum.Parse(typeof(RegulationStandard), config.ReadWithDefault(ConfigKey.RegulationStandard, RegulationStandard.CAP.ToString()));

            // Client is Master
            if (config.Read(ConfigKey.ClientIsMaster) == "1")
                clientIsMaster = true;
            else
                clientIsMaster = false;

            // Timing
            dataRequestFrequency = config.ReadWithDefault(ConfigKey.DataRequestFrequency, Constants.DataRequestFrequencyDefault);
            sensorStatusRequestFrequency = config.ReadWithDefault(ConfigKey.SensorStatusRequestFrequency, Constants.SensorStatusRequestFrequencyDefault);
            uiUpdateFrequency = config.ReadWithDefault(ConfigKey.ClientUpdateFrequencyUI, Constants.ClientUIUpdateFrequencyDefault);
            dataTimeout = config.ReadWithDefault(ConfigKey.DataTimeout, Constants.DataTimeoutDefault);
            chartDataUpdateFrequency20m = config.ReadWithDefault(ConfigKey.ChartDataUpdateFrequency20m, Constants.ChartUpdateFrequencyUI20mDefault);
            chartDataUpdateFrequency3h = config.ReadWithDefault(ConfigKey.ChartDataUpdateFrequency3h, Constants.ChartUpdateFrequencyUI3hDefault);

            // Server Address
            serverAddress = config.ReadWithDefault(ConfigKey.ServerAddress, Constants.DefaultServerAddress);

            // Server Port
            serverPort = config.ReadWithDefault(ConfigKey.ServerPort, Constants.ServerPortDefault);

            // CAP: Helideck Category
            helideckCategory = (HelideckCategory)Enum.Parse(typeof(HelideckCategory), config.ReadWithDefault(ConfigKey.HelideckCategorySetting, HelideckCategory.Category1.ToString()));

            // CAP: Default Helideck Category
            defaultHelideckCategory = (HelideckCategory)Enum.Parse(typeof(HelideckCategory), config.ReadWithDefault(ConfigKey.HelideckCategorySettingDefault, HelideckCategory.Category1.ToString()));

            // CAP: Enable Report Email
            if (config.ReadWithDefault(ConfigKey.EnableReportEmail, "0") == "1")
                enableReportEmail = true;
            else
                enableReportEmail = false;

            // EMS: Enable EMS Page
            if (config.ReadWithDefault(ConfigKey.EnableEMS, "0") == "1")
                enableEMS = true;
            else
                enableEMS = false;
        }

        public void ApplicationRestartRequired()
        {
            // Callback funksjon for å vise restart app melding
            warningBarMessage(WarningBarMessageType.RestartRequired);
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
                config.Write(ConfigKey.ServerAddress, value.ToString());
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
        //private VesselImage _vesselImage { get; set; }
        //public VesselImage vesselImage
        //{
        //    get
        //    {
        //        return _vesselImage;
        //    }
        //    set
        //    {
        //        _vesselImage = value;
        //        config.Write(ConfigKey.VesselImage, value.ToString());
        //        OnPropertyChanged(nameof(vesselImageString));
        //    }
        //}
        //public string vesselImageString
        //{
        //    get
        //    {
        //        return _vesselImage.ToString();
        //    }
        //}

        /////////////////////////////////////////////////////////////////////////////
        // CAP: Helideck Category
        /////////////////////////////////////////////////////////////////////////////
        private HelideckCategory _helideckCategory { get; set; }
        public HelideckCategory helideckCategory
        {
            get
            {
                return _helideckCategory;
            }
            set
            {
                if (value != _helideckCategory)
                {
                    _helideckCategory = value;
                    config.Write(ConfigKey.HelideckCategorySetting, _helideckCategory.ToString());
                }
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // CAP: Default Helideck Category
        /////////////////////////////////////////////////////////////////////////////
        private HelideckCategory _defaultHelideckCategory { get; set; }
        public HelideckCategory defaultHelideckCategory
        {
            get
            {
                return _defaultHelideckCategory;
            }
            set
            {
                if (value != _defaultHelideckCategory)
                {
                    _defaultHelideckCategory = value;
                    config.Write(ConfigKey.HelideckCategorySettingDefault, _defaultHelideckCategory.ToString());
                }
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // CAP: Enable report email
        /////////////////////////////////////////////////////////////////////////////
        private bool _enableReportEmail { get; set; }
        public bool enableReportEmail
        {
            get
            {
                return _enableReportEmail;
            }
            set
            {
                _enableReportEmail = value;

                if (_enableReportEmail)
                {
                    config.Write(ConfigKey.EnableReportEmail, "1");
                }
                else
                {
                    config.Write(ConfigKey.EnableReportEmail, "0");
                }

                OnPropertyChanged();
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // EMS: Enable EMS Page
        /////////////////////////////////////////////////////////////////////////////
        private bool _enableEMS { get; set; }
        public bool enableEMS
        {
            get
            {
                return _enableEMS;
            }
            set
            {
                _enableEMS = value;

                if (_enableEMS)
                {
                    config.Write(ConfigKey.EnableEMS, "1");
                }
                else
                {
                    config.Write(ConfigKey.EnableEMS, "0");
                }

                OnPropertyChanged();
            }
        }

        // Variabel oppdatert
        // Dersom navn ikke sette brukes kallende medlem sitt navn
        protected virtual void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    public enum RegulationStandard
    {
        NOROG,
        CAP
    }
}
