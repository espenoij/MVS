using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace HMS_Server
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

            // Regulation Standard
            regulationStandard = (RegulationStandard)Enum.Parse(typeof(RegulationStandard), config.ReadWithDefault(ConfigKey.RegulationStandard, RegulationStandard.NOROG.ToString()));

            // Vessel Name
            vesselName = config.Read(ConfigKey.VesselName);

            // Helideck Heading Offset
            helideckHeadingOffset = config.ReadWithDefault(ConfigKey.HelideckHeadingOffset, Constants.HeadingDefault);

            // CAP
            msiCorrectionR = config.ReadWithDefault(ConfigKey.MSICorrectionR, Constants.MSICorrectionRMin);
            windSensorSampleRate = config.ReadWithDefault(ConfigKey.WindSamplesPerTransmission, Constants.WindSamplesPerTransmissionDefault);

            // CAP: Override Wind Buffer
            if (config.ReadWithDefault(ConfigKey.OverrideWindBuffer, "0") == "1")
                overrideWindBuffer = true;
            else
                overrideWindBuffer = false;

            // CAP: Override Wind Buffer
            if (config.ReadWithDefault(ConfigKey.OverrideMotionBuffer, "0") == "1")
                overrideMotionBuffer = true;
            else
                overrideMotionBuffer = false;

            // CAP: Restricted Sector
            restrictedSectorFrom = config.ReadWithDefault(ConfigKey.RestrictedSectorFrom, Constants.HeadingDefault).ToString();
            restrictedSectorTo = config.ReadWithDefault(ConfigKey.RestrictedSectorTo, Constants.HeadingDefault).ToString();

            // Helideck Category
            helideckCategory = (HelideckCategory)Enum.Parse(typeof(HelideckCategory), config.ReadWithDefault(ConfigKey.HelideckCategory, HelideckCategory.Category1.ToString()));

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
            lightsOutputFrequency = config.ReadWithDefault(ConfigKey.LightsOutputFrequency, Constants.LightsOutputFrequencyDefault);

            // Helideck Report
            if (config.Read(ConfigKey.NDBInstalled_NOROG) == "1")
                ndbInstalled_NOROG = true;
            else
                ndbInstalled_NOROG = false;

            if (config.Read(ConfigKey.NDBInstalled_CAP) == "1")
                ndbInstalled_CAP = true;
            else
                ndbInstalled_CAP = false;

            ndbFreq_NOROG = config.ReadWithDefault(ConfigKey.NDBFrequency_NOROG, Constants.NDBFrequencyDefault).ToString("000.000");
            ndbFreq_CAP = config.ReadWithDefault(ConfigKey.NDBFrequency_CAP, Constants.NDBFrequencyDefault).ToString("000.000");
            ndbIdent = config.Read(ConfigKey.NDBIdent);
            vhfFreq = config.ReadWithDefault(ConfigKey.VHFFrequency, Constants.VHFFrequencyDefault).ToString("000.000");
            logFreq = config.ReadWithDefault(ConfigKey.LogFrequency, Constants.VHFFrequencyDefault).ToString("000.000");
            marineChannel = (int)config.ReadWithDefault(ConfigKey.MarineChannel, Constants.MarineChannelDefault);

            // Email Server
            emailServer = config.Read(ConfigKey.EmailServer);
            emailPort = config.ReadWithDefault(ConfigKey.EmailPort, Constants.DefaultSMTPPort);
            emailUsername = Encryption.ToInsecureString(Encryption.DecryptString(config.Read(ConfigKey.EmailUsername)));
            emailPassword = Encryption.ToInsecureString(Encryption.DecryptString(config.Read(ConfigKey.EmailPassword)));
            if (config.Read(ConfigKey.EmailSecureConnection) == "1")
                emailSecureConnection = true;
            else
                emailSecureConnection = false;

            // Data Verification
            if (config.Read(ConfigKey.DataVerificationEnabled) == "1")
                dataVerificationEnabled = true;
            else
                dataVerificationEnabled = false;

            // Magnetic Declination
            magneticDeclination = config.ReadWithDefault(ConfigKey.MagneticDeclination, Constants.MagneticDeclinationDefault);

            // Helideck Lights Output
            if (config.ReadWithDefault(ConfigKey.HelideckLightsOutput, "0") == "1")
                helideckLightsOutput = true;
            else
                helideckLightsOutput = false;

            // EMS: Enable EMS Module
            if (config.ReadWithDefault(ConfigKey.EnableEMS, "0") == "1")
                enableEMS = true;
            else
                enableEMS = false;

            // Auto Start HMS Processing
            if (config.ReadWithDefault(ConfigKey.AutoStartHMS, "1") == "1")
                autoStartHMS = true;
            else
                autoStartHMS = false;

            // Sensor
            windDirRef = (DirectionReference)Enum.Parse(typeof(DirectionReference), config.ReadWithDefault(ConfigKey.WindDirectionReference, DirectionReference.VesselHeading.ToString()));
            vesselHdgRef = (DirectionReference)Enum.Parse(typeof(DirectionReference), config.ReadWithDefault(ConfigKey.VesselHeadingReference, DirectionReference.MagneticNorth.ToString()));
            helideckHeight = config.ReadWithDefault(ConfigKey.HelideckHeight, Constants.HelideckHeightDefault);
            windSensorHeight = config.ReadWithDefault(ConfigKey.WindSensorHeight, Constants.WindSensorHeightDefault);
            windSensorDistance = config.ReadWithDefault(ConfigKey.WindSensorDistance, Constants.WindSensorDistanceDefault);
            airPressureSensorHeight = config.ReadWithDefault(ConfigKey.AirPressureSensorHeight, Constants.WindSensorHeightDefault);
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
                if (value != _regulationStandard)
                {
                    _regulationStandard = value;
                    config.Write(ConfigKey.RegulationStandard, _regulationStandard.ToString());
                    OnPropertyChanged(nameof(regulationStandardString));
                }
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
        // General Settings: Vessel Name
        /////////////////////////////////////////////////////////////////////////////
        private string _vesselName { get; set; }
        public string vesselName
        {
            get
            {
                return _vesselName;
            }
            set
            {
                if (value != null)
                {
                    _vesselName = value;
                    config.Write(ConfigKey.VesselName, value);
                    OnPropertyChanged();
                }
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Helideck Heading Offset
        /////////////////////////////////////////////////////////////////////////////
        private int _helideckHeadingOffset { get; set; }
        public int helideckHeadingOffset
        {
            get
            {
                return _helideckHeadingOffset;
            }
            set
            {
                _helideckHeadingOffset = value;
                config.Write(ConfigKey.HelideckHeadingOffset, value.ToString());
                OnPropertyChanged();
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Vessel Heading Reference
        /////////////////////////////////////////////////////////////////////////////
        private DirectionReference _vesselHdgRef { get; set; }
        public DirectionReference vesselHdgRef
        {
            get
            {
                return _vesselHdgRef;
            }
            set
            {
                _vesselHdgRef = value;
                config.Write(ConfigKey.VesselHeadingReference, value.ToString());
                OnPropertyChanged();
                OnPropertyChanged(nameof(VesselHdgRefString));
            }
        }
        public string VesselHdgRefString
        {
            get
            {
                return _vesselHdgRef.GetDescription();
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Helideck Category
        /////////////////////////////////////////////////////////////////////////////
        public HelideckCategory _helideckCategory { get; set; }
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
                    config.Write(ConfigKey.HelideckCategory, _helideckCategory.ToString());
                }
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // CAP: MSI Correction R
        /////////////////////////////////////////////////////////////////////////////
        private double _msiCorrectionR { get; set; }
        public double msiCorrectionR
        {
            get
            {
                return _msiCorrectionR;
            }
            set
            {
                _msiCorrectionR = value;
                config.Write(ConfigKey.MSICorrectionR, _msiCorrectionR.ToString());

                OnPropertyChanged();
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // CAP: Wind Samples Received per Transmission
        /////////////////////////////////////////////////////////////////////////////
        private double _windSensorSampleRate { get; set; }
        public double windSensorSampleRate
        {
            get
            {
                return _windSensorSampleRate;
            }
            set
            {
                _windSensorSampleRate = value;
                config.Write(ConfigKey.WindSamplesPerTransmission, _windSensorSampleRate.ToString());

                OnPropertyChanged();
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // CAP: Override CAP 9c Wind Buffer
        /////////////////////////////////////////////////////////////////////////////
        private bool _overrideWindBuffer { get; set; }
        public bool overrideWindBuffer
        {
            get
            {
                return _overrideWindBuffer;
            }
            set
            {
                _overrideWindBuffer = value;

                if (_overrideWindBuffer)
                {
                    config.Write(ConfigKey.OverrideWindBuffer, "1");
                }
                else
                {
                    config.Write(ConfigKey.OverrideWindBuffer, "0");
                }

                OnPropertyChanged();
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // CAP: Override CAP 9c Motion Buffer
        /////////////////////////////////////////////////////////////////////////////
        private bool _overrideMotionBuffer { get; set; }
        public bool overrideMotionBuffer
        {
            get
            {
                return _overrideMotionBuffer;
            }
            set
            {
                _overrideMotionBuffer = value;

                if (_overrideMotionBuffer)
                {
                    config.Write(ConfigKey.OverrideMotionBuffer, "1");
                }
                else
                {
                    config.Write(ConfigKey.OverrideMotionBuffer, "0");
                }

                OnPropertyChanged();
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Restricted Sector: From
        /////////////////////////////////////////////////////////////////////////////
        private string _restrictedSectorFrom { get; set; }
        public string restrictedSectorFrom
        {
            get
            {
                return _restrictedSectorFrom;
            }
            set
            {
                if (value != null)
                {
                    _restrictedSectorFrom = value;
                    config.Write(ConfigKey.RestrictedSectorFrom, _restrictedSectorFrom.ToString());
                }
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Restricted Sector: To
        /////////////////////////////////////////////////////////////////////////////
        private string _restrictedSectorTo { get; set; }
        public string restrictedSectorTo
        {
            get
            {
                return _restrictedSectorTo;
            }
            set
            {
                if (value != null)
                {
                    _restrictedSectorTo = value;
                    config.Write(ConfigKey.RestrictedSectorTo, _restrictedSectorTo.ToString());
                }
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Wind Direction Reference
        /////////////////////////////////////////////////////////////////////////////
        private DirectionReference _windDirRef { get; set; }
        public DirectionReference windDirRef
        {
            get
            {
                return _windDirRef;
            }
            set
            {
                _windDirRef = value;
                config.Write(ConfigKey.WindDirectionReference, value.ToString());
                OnPropertyChanged();
                OnPropertyChanged(nameof(windDirRefString));
            }
        }
        public string windDirRefString
        {
            get
            {
                return _windDirRef.GetDescription();
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Magnetic Declination
        /////////////////////////////////////////////////////////////////////////////
        private double _magneticDeclination { get; set; }
        public double magneticDeclination
        {
            get
            {
                return _magneticDeclination;
            }
            set
            {
                _magneticDeclination = value;
                config.Write(ConfigKey.MagneticDeclination, _magneticDeclination.ToString());
                OnPropertyChanged();
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Helideck Lights Output
        /////////////////////////////////////////////////////////////////////////////
        private bool _helideckLightsOutput { get; set; }
        public bool helideckLightsOutput
        {
            get
            {
                return _helideckLightsOutput;
            }
            set
            {
                _helideckLightsOutput = value;

                if (_helideckLightsOutput)
                    config.Write(ConfigKey.HelideckLightsOutput, "1");
                else
                    config.Write(ConfigKey.HelideckLightsOutput, "0");

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

        /////////////////////////////////////////////////////////////////////////////
        // EMS: Enable EMS Page
        /////////////////////////////////////////////////////////////////////////////
        private bool _autoStartHMS { get; set; }
        public bool autoStartHMS
        {
            get
            {
                return _autoStartHMS;
            }
            set
            {
                _autoStartHMS = value;

                if (_autoStartHMS)
                {
                    config.Write(ConfigKey.AutoStartHMS, "1");
                }
                else
                {
                    config.Write(ConfigKey.AutoStartHMS, "0");
                }

                OnPropertyChanged();
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Helideck Height
        /////////////////////////////////////////////////////////////////////////////
        private double _helideckHeight { get; set; }
        public double helideckHeight
        {
            get
            {
                return _helideckHeight;
            }
            set
            {
                _helideckHeight = value;
                config.Write(ConfigKey.HelideckHeight, value.ToString());
                OnPropertyChanged();
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Wind Sensor Height
        /////////////////////////////////////////////////////////////////////////////
        private double _windSensorHeight { get; set; }
        public double windSensorHeight
        {
            get
            {
                return _windSensorHeight;
            }
            set
            {
                _windSensorHeight = value;
                config.Write(ConfigKey.WindSensorHeight, _windSensorHeight.ToString());
                OnPropertyChanged();
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Wind Sensor Distance
        /////////////////////////////////////////////////////////////////////////////
        private double _windSensorDistance { get; set; }
        public double windSensorDistance
        {
            get
            {
                return _windSensorDistance;
            }
            set
            {
                _windSensorDistance = value;
                config.Write(ConfigKey.WindSensorDistance, _windSensorDistance.ToString());
                OnPropertyChanged();
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Air Pressure Sensor Distance
        /////////////////////////////////////////////////////////////////////////////
        private double _airPressureSensorHeight { get; set; }
        public double airPressureSensorHeight
        {
            get
            {
                return _airPressureSensorHeight;
            }
            set
            {
                _airPressureSensorHeight = value;
                config.Write(ConfigKey.AirPressureSensorHeight, _airPressureSensorHeight.ToString());
                OnPropertyChanged();
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // NDB Installed NOROG
        /////////////////////////////////////////////////////////////////////////////
        private bool _ndbInstalled_NOROG { get; set; }
        public bool ndbInstalled_NOROG
        {
            get
            {
                return _ndbInstalled_NOROG;
            }
            set
            {
                if (_ndbInstalled_NOROG != value)
                {
                    _ndbInstalled_NOROG = value;
                    if (value)
                        config.Write(ConfigKey.NDBInstalled_NOROG, "1");
                    else
                        config.Write(ConfigKey.NDBInstalled_NOROG, "0");
                    OnPropertyChanged();
                }
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // NDB Installed CAP
        /////////////////////////////////////////////////////////////////////////////
        private bool _ndbInstalled_CAP { get; set; }
        public bool ndbInstalled_CAP
        {
            get
            {
                return _ndbInstalled_CAP;
            }
            set
            {
                if (_ndbInstalled_CAP != value)
                {
                    _ndbInstalled_CAP = value;
                    if (value)
                        config.Write(ConfigKey.NDBInstalled_CAP, "1");
                    else
                        config.Write(ConfigKey.NDBInstalled_CAP, "0");
                    OnPropertyChanged();
                }
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // NDB Frequency NOROG
        /////////////////////////////////////////////////////////////////////////////
        private string _ndbFreq_NOROG { get; set; }
        public string ndbFreq_NOROG
        {
            get
            {
                return _ndbFreq_NOROG;
            }
            set
            {
                if (value != null)
                {
                    _ndbFreq_NOROG = value;
                    config.Write(ConfigKey.NDBFrequency_NOROG, value);
                    OnPropertyChanged();
                }
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // NDB Frequency CAP
        /////////////////////////////////////////////////////////////////////////////
        private string _ndbFreq_CAP { get; set; }
        public string ndbFreq_CAP
        {
            get
            {
                return _ndbFreq_CAP;
            }
            set
            {
                if (value != null)
                {
                    _ndbFreq_CAP = value;
                    config.Write(ConfigKey.NDBFrequency_CAP, value);
                    OnPropertyChanged();
                }
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // NDB Ident
        /////////////////////////////////////////////////////////////////////////////
        private string _ndbIdent { get; set; }
        public string ndbIdent
        {
            get
            {
                return _ndbIdent;
            }
            set
            {
                if (value != null)
                {
                    _ndbIdent = value;
                    config.Write(ConfigKey.NDBIdent, value);
                    OnPropertyChanged();
                }
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // VHF Frequency
        /////////////////////////////////////////////////////////////////////////////
        private string _vhfFreq { get; set; }
        public string vhfFreq
        {
            get
            {
                return _vhfFreq;
            }
            set
            {
                if (value != null)
                {
                    _vhfFreq = value;
                    config.Write(ConfigKey.VHFFrequency, value);
                    OnPropertyChanged();
                }
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Traffic Frequency
        /////////////////////////////////////////////////////////////////////////////
        private string _trafficFreq { get; set; }
        public string trafficFreq
        {
            get
            {
                return _trafficFreq;
            }
            set
            {
                if (value != null)
                {
                    _trafficFreq = value;
                    config.Write(ConfigKey.TrafficFrequency, value);
                    OnPropertyChanged();
                }
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Log Frequency
        /////////////////////////////////////////////////////////////////////////////
        private string _logFreq { get; set; }
        public string logFreq
        {
            get
            {
                return _logFreq;
            }
            set
            {
                if (value != null)
                {
                    _logFreq = value;
                    config.Write(ConfigKey.LogFrequency, value);
                    OnPropertyChanged();
                }
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Marine Channel
        /////////////////////////////////////////////////////////////////////////////
        private int _marineChannel { get; set; }
        public int marineChannel
        {
            get
            {
                return _marineChannel;
            }
            set
            {
                _marineChannel = value;
                config.Write(ConfigKey.MarineChannel, value.ToString());
                OnPropertyChanged();
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Helicopter WSI limit
        /////////////////////////////////////////////////////////////////////////////
        public RadObservableCollectionEx<HelicopterWSILimit> helicopterWSILimitList = new RadObservableCollectionEx<HelicopterWSILimit>();

        public double GetWSILimit(HelicopterType type)
        {
            if ((int)type < helicopterWSILimitList.Count && (int)type >= 0)
                return helicopterWSILimitList[(int)type].limit;
            else
                return Constants.HelicopterWSIDefault;
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
        // Timing: Data Timeout
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
        // Timing: Lights Output Frequency (CAP)
        /////////////////////////////////////////////////////////////////////////////
        private double _lightsOutputFrequency { get; set; }
        public double lightsOutputFrequency
        {
            get
            {
                return _lightsOutputFrequency;
            }
            set
            {
                _lightsOutputFrequency = value;
                config.Write(ConfigKey.LightsOutputFrequency, value.ToString());
                OnPropertyChanged();
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Email server
        /////////////////////////////////////////////////////////////////////////////
        private string _emailServer { get; set; }
        public string emailServer
        {
            get
            {
                return _emailServer;
            }
            set
            {
                if (value != null)
                {
                    _emailServer = value;
                    config.Write(ConfigKey.EmailServer, value);
                    OnPropertyChanged();
                }
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Email Server Port
        /////////////////////////////////////////////////////////////////////////////
        private double _emailPort { get; set; }
        public double emailPort
        {
            get
            {
                return _emailPort;
            }
            set
            {
                _emailPort = value;
                config.Write(ConfigKey.EmailPort, value.ToString());
                OnPropertyChanged();
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Email Server Username
        /////////////////////////////////////////////////////////////////////////////
        private string _emailUsername { get; set; }
        public string emailUsername
        {
            get
            {
                return _emailUsername;
            }
            set
            {
                if (value != null)
                {
                    _emailUsername = value;
                    config.Write(ConfigKey.EmailUsername, Encryption.EncryptString(Encryption.ToSecureString(value)));
                    OnPropertyChanged();
                }
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Email Server Password
        /////////////////////////////////////////////////////////////////////////////
        private string _emailPassword { get; set; }
        public string emailPassword
        {
            get
            {
                return _emailPassword;
            }
            set
            {
                if (value != null)
                {
                    _emailPassword = value;
                    config.Write(ConfigKey.EmailPassword, Encryption.EncryptString(Encryption.ToSecureString(value)));
                    OnPropertyChanged();
                }
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Email Server Secure Connection
        /////////////////////////////////////////////////////////////////////////////
        private bool _emailSecureConnection { get; set; }
        public bool emailSecureConnection
        {
            get
            {
                return _emailSecureConnection;
            }
            set
            {
                _emailSecureConnection = value;

                if (_emailSecureConnection)
                    config.Write(ConfigKey.EmailSecureConnection, "1");
                else
                    config.Write(ConfigKey.EmailSecureConnection, "0");

                OnPropertyChanged();
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Data Verification: Enable
        /////////////////////////////////////////////////////////////////////////////
        private bool _dataVerificationEnabled { get; set; }
        public bool dataVerificationEnabled
        {
            get
            {
                return _dataVerificationEnabled;
            }
            set
            {
                _dataVerificationEnabled = value;

                if (_dataVerificationEnabled)
                    config.Write(ConfigKey.DataVerificationEnabled, "1");
                else
                    config.Write(ConfigKey.DataVerificationEnabled, "0");

                OnPropertyChanged();
            }
        }

        // Variabel oppdatert
        // Dersom navn ikke settes brukes kallende medlem sitt navn
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
