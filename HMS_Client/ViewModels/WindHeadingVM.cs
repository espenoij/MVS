using DocumentFormat.OpenXml.Wordprocessing;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Threading;

namespace HMS_Client
{
    public class WindHeadingVM : INotifyPropertyChanged
    {
        // Change notification
        public event PropertyChangedEventHandler PropertyChanged;

        DispatcherTimer UIUpdateTimer = new DispatcherTimer();

        // Configuration settings
        private Config config;

        // User Inputs
        private UserInputsVM userInputsVM;

        // Admin settings
        private AdminSettingsVM adminSettingsVM;

        // Helideck Status
        private HelideckStatusVM helideckStatusVM;

        public void Init(HelideckStatusVM helideckStatusVM, Config config, SensorGroupStatus sensorStatus, UserInputsVM userInputsVM, AdminSettingsVM adminSettingsVM)
        {
            this.config = config;
            this.userInputsVM = userInputsVM;
            this.adminSettingsVM = adminSettingsVM;
            this.helideckStatusVM = helideckStatusVM;

            if (adminSettingsVM.regulationStandard == RegulationStandard.NOROG)
                windMeasurement = (WindMeasurement)Enum.Parse(typeof(WindMeasurement), config.ReadWithDefault(ConfigKey.WindMeasurement, WindMeasurement.TwoMinuteMean.ToString()));
            else
                windMeasurement = WindMeasurement.TwoMinuteMean;

            // Oppdatere UI
            UIUpdateTimer.Interval = TimeSpan.FromMilliseconds(config.ReadWithDefault(ConfigKey.ClientUpdateFrequencyUI, Constants.ClientUIUpdateFrequencyDefault));
            UIUpdateTimer.Tick += UIUpdate;
            UIUpdateTimer.Start();

            void UIUpdate(object sender, EventArgs e)
            {
                // Sjekke om vi har data timeout
                if (sensorStatus.TimeoutCheck(windDirectionRT) ||
                    sensorStatus.TimeoutCheck(windDirection2m) ||
                    sensorStatus.TimeoutCheck(windDirection10m))
                {
                    OnPropertyChanged(nameof(windDirectionString));
                    OnPropertyChanged(nameof(windDirectionRotation));
                    OnPropertyChanged(nameof(displayWindDirection));
                }

                if (sensorStatus.TimeoutCheck(windSpeedRT) ||
                    sensorStatus.TimeoutCheck(windSpeed2m) ||
                    sensorStatus.TimeoutCheck(windSpeed10m))
                {
                    OnPropertyChanged(nameof(windSpeedString));
                }

                if (sensorStatus.TimeoutCheck(windGust2m) ||
                    sensorStatus.TimeoutCheck(windGust10m))
                {
                    OnPropertyChanged(nameof(windGustString));
                }

                if (sensorStatus.TimeoutCheck(vesselHeading))
                {
                    OnPropertyChanged(nameof(vesselHeadingString));
                    OnPropertyChanged(nameof(vesselHeadingRotation));
                    OnPropertyChanged(nameof(helideckHeadingString));
                    OnPropertyChanged(nameof(helideckHeadingRotation));
                }

                if (sensorStatus.TimeoutCheck(vesselCOG))
                {
                    OnPropertyChanged(nameof(vesselCOGString));
                }

                if (sensorStatus.TimeoutCheck(vesselSOG))
                {
                    OnPropertyChanged(nameof(vesselSOGString));
                }

                if (sensorStatus.TimeoutCheck(helideckHeading))
                {
                    OnPropertyChanged(nameof(helideckHeadingString));
                    OnPropertyChanged(nameof(helideckHeadingRotation));
                }

                if (sensorStatus.TimeoutCheck(helicopterHeading))
                {
                    OnPropertyChanged(nameof(helicopterRotation));
                }

                OnPropertyChanged(nameof(displayModeVisibilityPreLanding));
                OnPropertyChanged(nameof(displayModeVisibilityOnDeck));
                OnPropertyChanged(nameof(displayOnDeckWindIndicator));
                OnPropertyChanged(nameof(displayHelicopterOnDeck));
                OnPropertyChanged(nameof(displayRWDArc));
                OnPropertyChanged(nameof(displayWindMeasurementSelection));
                OnPropertyChanged(nameof(notDisplayWindMeasurementSelection));

                OnPropertyChanged(nameof(displayVesselImageTriangle));
                OnPropertyChanged(nameof(displayVesselImageShip));
                OnPropertyChanged(nameof(displayVesselImageRig));
                OnPropertyChanged(nameof(displayVesselImageDot));
                OnPropertyChanged(nameof(displayVesselHeadingArrow));
                OnPropertyChanged(nameof(displayHelideckImage));

                OnPropertyChanged(nameof(windArrowColor));
            }
        }

        public void UpdateData(HMSDataCollection clientSensorList)
        {
            windDirectionRT = clientSensorList.GetData(ValueType.HelideckWindDirectionRT);
            windDirection2m = clientSensorList.GetData(ValueType.HelideckWindDirection2m);
            windDirection10m = clientSensorList.GetData(ValueType.HelideckWindDirection10m);

            windSpeedRT = clientSensorList.GetData(ValueType.HelideckWindSpeedRT);
            windSpeed2m = clientSensorList.GetData(ValueType.HelideckWindSpeed2m);
            windSpeed10m = clientSensorList.GetData(ValueType.HelideckWindSpeed10m);

            windSpeedRT.data = Math.Round(windSpeedRT.data, 0, MidpointRounding.AwayFromZero);
            windSpeed2m.data = Math.Round(windSpeed2m.data, 0, MidpointRounding.AwayFromZero);
            windSpeed10m.data = Math.Round(windSpeed10m.data, 0, MidpointRounding.AwayFromZero);

            windGust2m = clientSensorList.GetData(ValueType.HelideckWindGust2m);
            windGust10m = clientSensorList.GetData(ValueType.HelideckWindGust10m);

            windGust2m.data = Math.Round(windGust2m.data, 0, MidpointRounding.AwayFromZero);
            windGust10m.data = Math.Round(windGust10m.data, 0, MidpointRounding.AwayFromZero);

            vesselHeading = clientSensorList.GetData(ValueType.VesselHeading);
            vesselCOG = clientSensorList.GetData(ValueType.VesselCOG);
            vesselSOG = clientSensorList.GetData(ValueType.VesselSOG);

            vesselImage = (VesselImage)Enum.Parse(typeof(VesselImage), clientSensorList.GetData(ValueType.SettingsVesselImage).data3);

            helideckHeading = clientSensorList.GetData(ValueType.HelideckHeading);

            helicopterHeading = clientSensorList.GetData(ValueType.HelicopterHeading);

            UpdateRWDAngles();
        }

        /////////////////////////////////////////////////////////////////////////////
        // Wind Direction
        /////////////////////////////////////////////////////////////////////////////
        private HMSData _windDirectionRT { get; set; } = new HMSData();
        public HMSData windDirectionRT
        {
            get
            {
                return _windDirectionRT;
            }
            set
            {
                if (value != null)
                {
                    if (value.data != _windDirectionRT.data ||
                        value.timestamp != _windDirectionRT.timestamp)
                    {
                        _windDirectionRT.Set(value);

                        if (_windMeasurement == WindMeasurement.RealTime)
                        {
                            OnPropertyChanged(nameof(windDirectionString));
                            OnPropertyChanged(nameof(windDirectionRotation));
                            OnPropertyChanged(nameof(displayWindDirection));
                        }
                    }
                }
            }
        }

        private HMSData _windDirection2m { get; set; } = new HMSData();
        public HMSData windDirection2m
        {
            get
            {
                return _windDirection2m;
            }
            set
            {
                if (value != null)
                {
                    if (value.data != _windDirection2m.data ||
                        value.timestamp != _windDirection2m.timestamp)
                    {
                        _windDirection2m.Set(value);

                        if (_windMeasurement == WindMeasurement.TwoMinuteMean)
                        {
                            OnPropertyChanged(nameof(windDirectionString));
                            OnPropertyChanged(nameof(windDirectionRotation));
                            OnPropertyChanged(nameof(displayWindDirection));
                        }
                    }
                }
            }
        }

        private HMSData _windDirection10m { get; set; } = new HMSData();
        public HMSData windDirection10m
        {
            get
            {
                return _windDirection10m;
            }
            set
            {
                if (value != null)
                {
                    if (value.data != _windDirection10m.data ||
                        value.timestamp != _windDirection10m.timestamp)
                    {
                        _windDirection10m.Set(value);

                        if (_windMeasurement == WindMeasurement.TenMinuteMean)
                        {
                            OnPropertyChanged(nameof(windDirectionString));
                            OnPropertyChanged(nameof(windDirectionRotation));
                            OnPropertyChanged(nameof(displayWindDirection));
                        }
                    }
                }
            }
        }

        public string windDirectionString
        {
            get
            {
                int dir;

                switch (_windMeasurement)
                {
                    case WindMeasurement.RealTime:
                        // Sjekke om data er gyldig
                        if (_windDirectionRT.status == DataStatus.OK)
                        {
                            dir = (int)Math.Round(_windDirectionRT.data, 0, MidpointRounding.AwayFromZero);
                        }
                        else
                        {
                            return Constants.NotAvailable;
                        }
                        break;

                    case WindMeasurement.TwoMinuteMean:
                        // Sjekke om data er gyldig
                        if (_windDirection2m.status == DataStatus.OK)
                        {
                            dir = (int)Math.Round(_windDirection2m.data, 0, MidpointRounding.AwayFromZero);
                        }
                        else
                        {
                            return Constants.NotAvailable;
                        }
                        break;

                    case WindMeasurement.TenMinuteMean:
                        // Sjekke om data er gyldig
                        if (_windDirection10m.status == DataStatus.OK)
                        {
                            dir = (int)Math.Round(_windDirection10m.data, 0, MidpointRounding.AwayFromZero);
                        }
                        else
                        {
                            return Constants.NotAvailable;
                        }
                        break;

                    default:
                        return Constants.NotAvailable;
                }

                if (dir == 0)
                    dir = 360;

                return string.Format("{0}°", dir.ToString("000"));
            }
        }

        public double windDirectionRotation
        {
            get
            {
                switch (_windMeasurement)
                {
                    case WindMeasurement.RealTime:
                        // Sjekke om data er gyldig
                        if (_windDirectionRT.status == DataStatus.OK)
                        {
                            return _windDirectionRT.data;
                        }
                        else
                        {
                            return -1;
                        }

                    case WindMeasurement.TwoMinuteMean:
                        // Sjekke om data er gyldig
                        if (_windDirection2m.status == DataStatus.OK)
                        {
                            return _windDirection2m.data;
                        }
                        else
                        {
                            return -1;
                        }

                    case WindMeasurement.TenMinuteMean:
                        // Sjekke om data er gyldig
                        if (_windDirection10m.status == DataStatus.OK)
                        {
                            return _windDirection10m.data;
                        }
                        else
                        {
                            return -1;
                        }

                    default:
                        return -1;
                }
            }
        }

        public bool displayWindDirection
        {
            get
            {
                if (windDirectionRotation != -1)
                    return true;
                else
                    return false;
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Wind Speed
        /////////////////////////////////////////////////////////////////////////////
        private HMSData _windSpeedRT { get; set; } = new HMSData();
        public HMSData windSpeedRT
        {
            get
            {
                return _windSpeedRT;
            }
            set
            {
                if (value != null)
                {
                    if (value.data != _windSpeedRT.data ||
                        value.timestamp != _windSpeedRT.timestamp)
                    {
                        _windSpeedRT.Set(value);

                        if (_windMeasurement == WindMeasurement.RealTime)
                            OnPropertyChanged(nameof(windSpeedString));
                    }
                }
            }
        }

        private HMSData _windSpeed2m { get; set; } = new HMSData();
        public HMSData windSpeed2m
        {
            get
            {
                return _windSpeed2m;
            }
            set
            {
                if (value != null)
                {
                    if (value.data != _windSpeed2m.data ||
                        value.timestamp != _windSpeed2m.timestamp)
                    {
                        _windSpeed2m.Set(value);

                        if (_windMeasurement == WindMeasurement.TwoMinuteMean)
                            OnPropertyChanged(nameof(windSpeedString));
                    }
                }
            }
        }

        private HMSData _windSpeed10m { get; set; } = new HMSData();
        public HMSData windSpeed10m
        {
            get
            {
                return _windSpeed10m;
            }
            set
            {
                if (value != null)
                {
                    if (value.data != _windSpeed10m.data ||
                        value.timestamp != _windSpeed10m.timestamp)
                    {
                        _windSpeed10m.Set(value);

                        if (_windMeasurement == WindMeasurement.TenMinuteMean)
                            OnPropertyChanged(nameof(windSpeedString));
                    }
                }
            }
        }

        public string windSpeedString
        {
            get
            {
                if (_windSpeedRT != null)
                {
                    switch (_windMeasurement)
                    {
                        case WindMeasurement.RealTime:
                            // Sjekke om data er gyldig
                            if (_windSpeedRT.status == DataStatus.OK)
                            {
                                return _windSpeedRT.data.ToString("00");
                            }
                            else
                            {
                                return Constants.NotAvailable;
                            }

                        case WindMeasurement.TwoMinuteMean:
                            // Sjekke om data er gyldig
                            if (_windSpeed2m.status == DataStatus.OK)
                            {
                                return _windSpeed2m.data.ToString("00");
                            }
                            else
                            {
                                return Constants.NotAvailable;
                            }

                        case WindMeasurement.TenMinuteMean:
                            // Sjekke om data er gyldig
                            if (_windSpeed10m.status == DataStatus.OK)
                            {
                                return _windSpeed10m.data.ToString("00");
                            }
                            else
                            {
                                return Constants.NotAvailable;
                            }

                        default:
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
        // Wind Gust
        /////////////////////////////////////////////////////////////////////////////
        private HMSData _windGust2m { get; set; } = new HMSData();
        public HMSData windGust2m
        {
            get
            {
                return _windGust2m;
            }
            set
            {
                if (value != null)
                {
                    if (value.data != _windGust2m.data ||
                        value.timestamp != _windGust2m.timestamp)
                    {
                        _windGust2m.Set(value);

                        if (_windMeasurement == WindMeasurement.TwoMinuteMean)
                            OnPropertyChanged(nameof(windGustString));
                    }
                }
            }
        }

        private HMSData _windGust10m { get; set; } = new HMSData();
        public HMSData windGust10m
        {
            get
            {
                return _windGust10m;
            }
            set
            {
                if (value != null)
                {
                    if (value.data != _windGust10m.data ||
                        value.timestamp != _windGust10m.timestamp)
                    {
                        _windGust10m.Set(value);

                        if (_windMeasurement == WindMeasurement.TenMinuteMean)
                            OnPropertyChanged(nameof(windGustString));
                    }
                }
            }
        }

        public string windGustString
        {
            get
            {
                if (_windGust2m != null)
                {
                    switch (_windMeasurement)
                    {
                        case WindMeasurement.TwoMinuteMean:
                            // Sjekke om data er gyldig
                            if (_windGust2m.status == DataStatus.OK)
                            {
                                return _windGust2m.data.ToString("00");
                            }
                            else
                            {
                                return Constants.NotAvailable;
                            }

                        case WindMeasurement.TenMinuteMean:
                            // Sjekke om data er gyldig
                            if (_windGust10m.status == DataStatus.OK)
                            {
                                return _windGust10m.data.ToString("00");
                            }
                            else
                            {
                                return Constants.NotAvailable;
                            }

                        default:
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
        // Vessel Heading
        /////////////////////////////////////////////////////////////////////////////
        private HMSData _vesselHeading { get; set; } = new HMSData();
        public HMSData vesselHeading
        {
            get
            {
                return _vesselHeading;
            }
            set
            {
                if (value != null)
                {
                    if (value.data != _vesselHeading.data ||
                        value.timestamp != _vesselHeading.timestamp)
                    {
                        _vesselHeading.Set(value);

                        OnPropertyChanged(nameof(vesselHeadingString));
                        OnPropertyChanged(nameof(vesselHeadingRotation));
                        OnPropertyChanged(nameof(helideckHeadingString));
                        OnPropertyChanged(nameof(helideckHeadingRotation));
                    }
                }
            }
        }

        public string vesselHeadingString
        {
            get
            {
                int dir;

                if (_vesselHeading != null)
                {
                    // Sjekke om data er gyldig
                    if (_vesselHeading.status == DataStatus.OK)
                    {
                        dir = (int)Math.Round(_vesselHeading.data, 0, MidpointRounding.AwayFromZero);
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

                if (dir == 0)
                    dir = 360;

                return string.Format("{0}°", dir.ToString("000"));
            }
        }

        public double vesselHeadingRotation
        {
            get
            {
                if (_vesselHeading != null)
                {
                    // Sjekke om data er gyldig
                    if (_vesselHeading.status == DataStatus.OK)
                    {
                        return _vesselHeading.data;
                    }
                    else
                    {
                        return -1;
                    }
                }
                else
                {
                    return -1;
                }
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Vessel COG
        /////////////////////////////////////////////////////////////////////////////
        private HMSData _vesselCOG { get; set; } = new HMSData();
        public HMSData vesselCOG
        {
            get
            {
                return _vesselCOG;
            }
            set
            {
                if (value != null)
                {
                    if (value.data != _vesselCOG.data ||
                        value.timestamp != _vesselCOG.timestamp)
                    {
                        _vesselCOG.Set(value);

                        OnPropertyChanged(nameof(vesselCOGString));
                    }
                }
            }
        }

        public string vesselCOGString
        {
            get
            {
                if (_vesselCOG != null)
                {
                    // Sjekke om data er gyldig
                    if (_vesselCOG.status == DataStatus.OK)
                    {
                        return string.Format("{0}°", _vesselCOG.data.ToString("000"));
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
        // Vessel SOG
        /////////////////////////////////////////////////////////////////////////////
        private HMSData _vesselSOG { get; set; } = new HMSData();
        public HMSData vesselSOG
        {
            get
            {
                return _vesselSOG;
            }
            set
            {
                if (value != null &&
                    _vesselSOG != null)
                {
                    if (value.data != _vesselSOG.data ||
                        value.timestamp != _vesselSOG.timestamp)
                    {
                        _vesselSOG.Set(value);

                        OnPropertyChanged(nameof(vesselSOGString));
                    }
                }
            }
        }

        public string vesselSOGString
        {
            get
            {
                if (_vesselSOG != null)
                {
                    // Sjekke om data er gyldig
                    if (_vesselSOG.status == DataStatus.OK)
                    {
                        return _vesselSOG.data.ToString("0.0");
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
                if (value != _vesselImage)
                {
                    _vesselImage = value;

                    OnPropertyChanged(nameof(vesselImageString));
                }
            }
        }

        public string vesselImageString
        {
            get
            {
                return _vesselImage.ToString();
            }
        }

        public bool displayVesselImageTriangle
        {
            get
            {
                if (vesselHeadingRotation != -1 &&
                    _vesselImage == VesselImage.Triangle)
                    return true;
                else
                    return false;
            }
        }

        public bool displayVesselImageShip
        {
            get
            {
                if (vesselHeadingRotation != -1 &&
                    _vesselImage == VesselImage.Ship)
                    return true;
                else
                    return false;
            }
        }

        public bool displayVesselImageRig
        {
            get
            {
                if (vesselHeadingRotation != -1 &&
                    _vesselImage == VesselImage.Rig)
                    return true;
                else
                    return false;
            }
        }

        public bool displayVesselImageDot
        {
            get
            {
                if (vesselHeadingRotation == -1)
                    return true;
                else
                    return false;
            }
        }

        public bool displayVesselHeadingArrow
        {
            get
            {
                if (vesselHeadingRotation != -1)
                    return true;
                else
                    return false;
            }
        }

        public bool displayHelideckImage
        {
            get
            {
                if (vesselHeadingRotation != -1)
                    return true;
                else
                    return false;
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Wind Measurement
        /////////////////////////////////////////////////////////////////////////////
        private WindMeasurement _windMeasurement { get; set; }
        public WindMeasurement windMeasurement
        {
            get
            {
                return _windMeasurement;
            }
            set
            {
                if (value != _windMeasurement)
                {
                    _windMeasurement = value;
                    config?.Write(ConfigKey.WindMeasurement, _windMeasurement.ToString());

                    OnPropertyChanged(nameof(windDirectionRotation));
                    OnPropertyChanged(nameof(windDirectionString));
                    OnPropertyChanged(nameof(windSpeedString));
                    OnPropertyChanged(nameof(windGustString));
                    OnPropertyChanged(nameof(displayWindDirection));
                    OnPropertyChanged(nameof(windMeasurementString));
                }
            }
        }

        public string windMeasurementString
        {
            get
            {
                return _windMeasurement.GetDescription();
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Helideck Heading
        /////////////////////////////////////////////////////////////////////////////
        private HMSData _helideckHeading { get; set; } = new HMSData();
        public HMSData helideckHeading
        {
            get
            {
                return _helideckHeading;
            }
            set
            {
                if (value != null)
                {
                    if (value.data != _helideckHeading.data ||
                        value.timestamp != _helideckHeading.timestamp)
                    {
                        _helideckHeading.Set(value);

                        OnPropertyChanged(nameof(helideckHeadingString));
                        OnPropertyChanged(nameof(helideckHeadingRotation));
                    }
                }
            }
        }

        public string helideckHeadingString
        {
            get
            {
                if (_helideckHeading.status == DataStatus.OK)
                {
                    return string.Format("{0}°", _helideckHeading.data.ToString("000"));
                }
                else
                {
                    return Constants.NotAvailable;
                }
            }
        }

        public double helideckHeadingRotation
        {
            get
            {
                if (_helideckHeading != null)
                {
                    return _helideckHeading.data;
                }
                else
                {
                    return -1;
                }
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Display Mode
        /////////////////////////////////////////////////////////////////////////////
        public bool displayModeVisibilityPreLanding
        {
            get
            {
                if (userInputsVM.displayMode == DisplayMode.PreLanding)
                    return true;
                else
                    return false;
            }
        }

        public bool displayModeVisibilityOnDeck
        {
            get
            {
                if (userInputsVM.displayMode == DisplayMode.OnDeck)
                    return true;
                else
                    return false;
            }
        }

        public bool displayOnDeckWindIndicator
        {
            get
            {
                if (userInputsVM.displayMode == DisplayMode.OnDeck &&
                    _windMeasurement == WindMeasurement.TwoMinuteMean)
                    return true;
                else
                    return false;
            }
        }

        public bool displayHelicopterOnDeck
        {
            get
            {
                if (userInputsVM.displayMode == DisplayMode.OnDeck &&
                    vesselHeadingRotation != -1)
                    return true;
                else
                    return false;
            }
        }

        public bool displayRWDArc
        {
            get
            {
                if (userInputsVM.displayMode == DisplayMode.OnDeck &&
                    vesselHeadingRotation != -1 &&
                    helideckStatusVM.rwdStatus != HelideckStatusType.OFF)
                    return true;
                else
                    return false;
            }
        }

        public bool displayWindMeasurementSelection
        {
            get
            {
                if (adminSettingsVM.regulationStandard == RegulationStandard.NOROG ||
                    (adminSettingsVM.regulationStandard == RegulationStandard.CAP && userInputsVM.displayMode == DisplayMode.PreLanding))
                    return true;
                else
                    return false;
            }
        }

        public bool notDisplayWindMeasurementSelection       
        {
            get
            {
                return !displayWindMeasurementSelection;
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Helicopter Heading
        /////////////////////////////////////////////////////////////////////////////
        private HMSData _helicopterHeading { get; set; } = new HMSData();
        public HMSData helicopterHeading
        {
            get
            {
                return _helicopterHeading;
            }
            set
            {
                if (value != null)
                {
                    if (value.data != _helicopterHeading.data ||
                        value.timestamp != _helicopterHeading.timestamp)
                    {
                        _helicopterHeading.Set(value);

                        OnPropertyChanged(nameof(helicopterRotation));
                    }
                }
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Helicopter Rotasjon
        /////////////////////////////////////////////////////////////////////////////
        public double helicopterRotation
        {
            get
            {
                if (_helicopterHeading?.status == DataStatus.OK)
                {
                    return _helicopterHeading.data;
                }
                else
                {
                    return 0;
                }
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Relative Wind Direction Limits
        /////////////////////////////////////////////////////////////////////////////
        private void UpdateRWDAngles()
        {
            double wind = windSpeed2m.data;

            double angleBlue = 60;
            double angleAmber = 60;
            double angleRed = 60;

            // Blå
            if (wind <= 15)
            {
                angleBlue = 179.99;

                rwdRedShow = false;
                rwdAmberShow = false;
            }
            else
            {
                rwdAmberShow = true;

                // Blå
                if (wind <= 35)
                {
                    angleBlue = 45 - (wind - 15);
                }
                else
                {
                    angleBlue = 25;
                }

                // Gul
                if (wind <= 20)
                {
                    angleAmber = 179.99;

                    rwdRedShow = false;
                }
                else
                {
                    angleAmber = angleBlue + 5;

                    // Rød
                    rwdRedShow = true;
                    angleRed = 179.99;
                }
            }

            rwdRedStartAngle = (angleRed * -1) - 90;
            rwdRedEndAngle = angleRed - 90;

            rwdAmberStartAngle = (angleAmber * -1) - 90;
            rwdAmberEndAngle = angleAmber - 90;

            rwdBlueStartAngle = (angleBlue * -1) - 90;
            rwdBlueEndAngle = angleBlue - 90;

            OnPropertyChanged(nameof(rwdRedShow));
            OnPropertyChanged(nameof(rwdRedStartAngle));
            OnPropertyChanged(nameof(rwdRedEndAngle));

            OnPropertyChanged(nameof(rwdAmberShow));
            OnPropertyChanged(nameof(rwdAmberStartAngle));
            OnPropertyChanged(nameof(rwdAmberEndAngle));

            OnPropertyChanged(nameof(rwdBlueStartAngle));
            OnPropertyChanged(nameof(rwdBlueEndAngle));
        }

        public bool rwdRedShow { get; set; }
        public double rwdRedStartAngle { get; set; }
        public double rwdRedEndAngle { get; set; }

        public bool rwdAmberShow { get; set; }
        public double rwdAmberStartAngle { get; set; }
        public double rwdAmberEndAngle { get; set; }

        public double rwdBlueStartAngle { get; set; }
        public double rwdBlueEndAngle { get; set; }

        /////////////////////////////////////////////////////////////////////////////
        // Wind Arrow Color
        /////////////////////////////////////////////////////////////////////////////
        public string windArrowColor
        {
            get
            {
                if (userInputsVM.displayMode == DisplayMode.OnDeck)
                {
                    switch (helideckStatusVM.rwdStatus)
                    {
                        case HelideckStatusType.OFF:
                            return "white";

                        case HelideckStatusType.BLUE:
                            return "blue";

                        case HelideckStatusType.AMBER:
                            return "amber";

                        case HelideckStatusType.RED:
                            return "red";

                        case HelideckStatusType.GREEN:
                            return "green";

                        default:
                            return "white";
                    }
                }
                else
                {
                    return "white";
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
