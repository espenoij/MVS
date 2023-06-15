using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Threading;

namespace HMS_Client
{
    public class GeneralInformationVM : INotifyPropertyChanged
    {
        // Change notification
        public event PropertyChangedEventHandler PropertyChanged;

        private Config config;
        private AdminSettingsVM adminSettingsVM;

        private DispatcherTimer UIUpdateTimer = new DispatcherTimer();
        private DispatcherTimer TimeTimer = new DispatcherTimer();

        public void Init(Config config, AdminSettingsVM adminSettingsVM, SensorGroupStatus sensorStatus, Version version)
        {
            this.config = config;
            this.adminSettingsVM = adminSettingsVM;

            InitUI(version);

            // Oppdatere UI
            UIUpdateTimer.Interval = TimeSpan.FromMilliseconds(config.ReadWithDefault(ConfigKey.ClientUpdateFrequencyUI, Constants.ClientUIUpdateFrequencyDefault));
            UIUpdateTimer.Tick += UIUpdate;
            UIUpdateTimer.Start();

            void UIUpdate(object sender, EventArgs e)
            {
                // Sjekke om vi har data timeout
                if (sensorStatus.TimeoutCheck(latitude)) OnPropertyChanged(nameof(latitudeString));
                if (sensorStatus.TimeoutCheck(longitude)) OnPropertyChanged(nameof(longitudeString));
                if (sensorStatus.TimeoutCheck(helideckCategory)) OnPropertyChanged(nameof(helideckCategoryString));
                if (sensorStatus.TimeoutCheck(vesselName)) OnPropertyChanged(nameof(vesselNameString));

                OnPropertyChanged(nameof(clientMode));
                OnPropertyChanged(nameof(clientModeIcon));
            }

            // Oppdatere dato og klokke
            TimeTimer.Interval = TimeSpan.FromMilliseconds(200);
            TimeTimer.Tick += UpdateClock;
            TimeTimer.Start();

            void UpdateClock(object sender, EventArgs e)
            {
                time = DateTime.UtcNow;
            }
        }

        private void InitUI(Version version)
        {
            // Vessel Icon
            vesselImage = (VesselImage)Enum.Parse(typeof(VesselImage), config.ReadWithDefault(ConfigKey.VesselImage, VesselImage.Ship.ToString()));

            // Software Version
            softwareVersion = string.Format("Version {0} {1}",
                version.ToString(),
                Constants.SoftwareVersionPostfix);

            // Regulatory Standard
            regulatoryStandard = Constants.RegulatoryStandard;

            // HLL Limits Version
            hllLimitsVersion = Constants.HLLLimitsVersion;

            // Position
            _latitude = new HMSData();
            _longitude = new HMSData();
        }

        public void UpdateData(HMSDataCollection clientSensorList)
        {
            latitude = clientSensorList.GetData(ValueType.Latitude);
            longitude = clientSensorList.GetData(ValueType.Longitude);
            helideckCategory = clientSensorList.GetData(ValueType.SettingsHelideckCategory);
            vesselName = clientSensorList.GetData(ValueType.SettingsVesselName);
        }

        /////////////////////////////////////////////////////////////////////////////
        // Vessel Name Icon
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
                if (value != _vesselImage)
                {
                    _vesselImage = value;

                    OnPropertyChanged(nameof(vesselImageIcon));
                }
            }
        }

        public string vesselImageIcon
        {
            get
            {
                if (_vesselImage == VesselImage.Rig)
                    return "..\\Icons\\oil_rig_48dp.png";
                else
                    return "..\\Icons\\outline_directions_boat_black_48dp.png";
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Vessel Name / Installation Name
        /////////////////////////////////////////////////////////////////////////////
        private HMSData _vesselName { get; set; } = new HMSData();
        public HMSData vesselName
        {
            get
            {
                return _vesselName;
            }
            set
            {
                if (value != _vesselName)
                {
                    _vesselName.Set(value);

                    OnPropertyChanged(nameof(vesselNameString));
                }
            }
        }

        public string vesselNameString
        {
            get
            {
                if (_vesselName.status == DataStatus.OK && !string.IsNullOrEmpty(_vesselName.data3))
                    return _vesselName.data3.ToString();
                else
                    return Constants.NotAvailable;
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Helideck Category
        /////////////////////////////////////////////////////////////////////////////
        private HMSData _helideckCategory { get; set; } = new HMSData();
        public HMSData helideckCategory
        {
            get
            {
                return _helideckCategory;
            }
            set
            {
                if (value != _helideckCategory)
                {
                    _helideckCategory.Set(value);

                    OnPropertyChanged(nameof(helideckCategoryString));
                }
            }
        }
        public string helideckCategoryString
        {
            get
            {
                if (_helideckCategory.status == DataStatus.OK)
                {
                    return ((HelideckCategory)Enum.Parse(typeof(HelideckCategory), _helideckCategory.data.ToString())).GetDescription();
                }
                else
                    return Constants.NotAvailable;
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Latitude
        /////////////////////////////////////////////////////////////////////////////
        private HMSData _latitude { get; set; }
        public HMSData latitude
        {
            get
            {
                return _latitude;
            }
            set
            {
                if (value != null)
                {
                    if (value.data != _latitude.data ||
                        value.timestamp != _latitude.timestamp)
                    {
                        _latitude.Set(value);

                        OnPropertyChanged(nameof(latitudeString));
                    }
                }
            }
        }

        public string latitudeString
        {
            get
            {
                if (_latitude != null)
                {
                    // Sjekke om data er gyldig
                    if (_latitude.status == DataStatus.OK)
                    {
                        // Hente data
                        double lat = _latitude.data;

                        // Sjekke data
                        if (lat <= 90.0 && lat >= -90.0)
                        {
                            // Sette data
                            string dir;
                            if (lat >= 0.0)
                            {
                                dir = "N";
                            }
                            else
                            {
                                dir = "S";
                                lat *= -1.0;
                            }

                            int deg = (int)lat;
                            int min = (int)((lat - (double)deg) * 60.0);
                            double sec = (((lat - (double)deg) * 60.0) - (double)min) * 60.0;

                            return string.Format("{0}° {1}' {2}\" {3}",
                                deg.ToString("00"),
                                min.ToString("00"),
                                sec.ToString("00.0"),
                                dir);
                        }
                        else
                        {
                            return Constants.NotAvailable;
                        }
                    }
                    else
                    {
                        return Constants.NotAvailable;
                    }
                }
                else
                {
                    return Constants.NotAvailable;
                }
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Longitude
        /////////////////////////////////////////////////////////////////////////////
        private HMSData _longitude { get; set; }
        public HMSData longitude
        {
            get
            {
                return _longitude;
            }
            set
            {
                if (value != null)
                {
                    if (value.data != _longitude.data ||
                        value.timestamp != _longitude.timestamp)
                    {
                        _longitude.Set(value);

                        OnPropertyChanged(nameof(longitudeString));
                    }
                }
            }
        }

        public string longitudeString
        {
            get
            {
                if (_longitude != null)
                {
                    // Sjekke om data er gyldig
                    if (_longitude.status == DataStatus.OK)
                    {
                        // Hente data
                        double lon = _longitude.data;

                        // Sjekke data
                        if (lon <= 180.0 && lon >= -180.0)
                        {
                            // Sette data
                            string dir;
                            if (lon >= 0.0)
                            {
                                dir = "E";
                            }
                            else
                            {
                                dir = "W";
                                lon *= -1.0;
                            }

                            int deg = (int)lon;
                            int min = (int)((lon - (double)deg) * 60.0);
                            double sec = (((lon - (double)deg) * 60.0) - (double)min) * 60.0;

                            return string.Format("{0}° {1}' {2}\" {3}",
                                deg.ToString("000"),
                                min.ToString("00"),
                                sec.ToString("00.0"),
                                dir);
                        }
                        else
                        {
                            return Constants.NotAvailable;
                        }
                    }
                    else
                    {
                        return Constants.NotAvailable;
                    }
                }
                else
                {
                    return Constants.NotAvailable;
                }
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Time: Date & Clock
        /////////////////////////////////////////////////////////////////////////////
        private DateTime _time { get; set; }
        public DateTime time
        {
            get
            {
                return _time;
            }
            set
            {
                if (value != _time)
                {
                    _time = value;

                    OnPropertyChanged(nameof(dateString));
                    OnPropertyChanged(nameof(clockString));
                }
            }
        }
        public string dateString
        {
            get
            {
                return _time.ToShortDateString();
            }
        }
        public string clockString
        {
            get
            {
                return string.Format("{0} UTC", _time.ToString("HH:mm:ss"));
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Client Mode
        /////////////////////////////////////////////////////////////////////////////
        //private string _clientMode { get; set; }
        public string clientMode
        {
            get
            {
                if (adminSettingsVM.clientIsMaster)
                    return "Master";
                else
                    return "Observer";
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Client Mode Icon
        /////////////////////////////////////////////////////////////////////////////
        //private string _clientModeIcon { get; set; }
        public string clientModeIcon
        {
            get
            {
                if (adminSettingsVM.clientIsMaster)
                    return "..\\Icons\\shield-crown-outline.png";
                else
                    return "..\\Icons\\outline_person_black_48dp.png";
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Software Version
        /////////////////////////////////////////////////////////////////////////////
        private string _softwareVersion { get; set; }
        public string softwareVersion
        {
            get
            {
                return _softwareVersion;
            }
            set
            {
                if (value != _softwareVersion)
                {
                    _softwareVersion = value;

                    OnPropertyChanged();
                }
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Regulatory Standard
        /////////////////////////////////////////////////////////////////////////////
        private string _regulatoryStandard { get; set; }
        public string regulatoryStandard
        {
            get
            {
                return _regulatoryStandard;
            }
            set
            {
                if (value != _regulatoryStandard)
                {
                    _regulatoryStandard = value;

                    OnPropertyChanged();
                }
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // HLL Limit Version
        /////////////////////////////////////////////////////////////////////////////
        private string _hllLimitVersion { get; set; }
        public string hllLimitsVersion
        {
            get
            {
                return _hllLimitVersion;
            }
            set
            {
                if (value != _hllLimitVersion)
                {
                    _hllLimitVersion = value;

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
