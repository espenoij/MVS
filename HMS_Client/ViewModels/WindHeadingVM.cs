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

            // Oppdatere UI
            UIUpdateTimer.Interval = TimeSpan.FromMilliseconds(config.ReadWithDefault(ConfigKey.ClientUpdateFrequencyUI, Constants.ClientUpdateFrequencyUIDefault));
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

                if (sensorStatus.TimeoutCheck(vesselSpeed))
                {
                    OnPropertyChanged(nameof(vesselSpeedString));
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
                    OnPropertyChanged(nameof(helicopterHeadingString));
                    OnPropertyChanged(nameof(helicopterRotation));
                }

                OnPropertyChanged(nameof(displayModeVisibilityPreLanding));
                OnPropertyChanged(nameof(displayModeVisibilityOnDeck));
                OnPropertyChanged(nameof(displayOnDeckWindIndicator));
                OnPropertyChanged(nameof(displayHelicopterOnDeck));
                OnPropertyChanged(nameof(displayCAP));
                OnPropertyChanged(nameof(displayNOROG));

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

            windGust2m = clientSensorList.GetData(ValueType.HelideckWindGust2m);
            windGust10m = clientSensorList.GetData(ValueType.HelideckWindGust10m);

            vesselHeading = clientSensorList.GetData(ValueType.VesselHeading);
            vesselCOG = clientSensorList.GetData(ValueType.VesselCOG);
            vesselSpeed = clientSensorList.GetData(ValueType.VesselSpeed);
            vesselSOG = clientSensorList.GetData(ValueType.VesselSOG);

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

                        if (windMeasurement == WindMeasurement.RealTime)
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

                        if (windMeasurement == WindMeasurement.TwoMinuteMean)
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

                        if (windMeasurement == WindMeasurement.TenMinuteMean)
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
                if (windDirectionRT != null)
                {
                    switch (windMeasurement)
                    {
                        case WindMeasurement.RealTime:
                            // Sjekke om data er gyldig
                            if (windDirectionRT.status == DataStatus.OK)
                            {
                                return string.Format("{0}°", windDirectionRT.data.ToString("000"));
                            }
                            else
                            {
                                return Constants.NotAvailable;
                            }

                        case WindMeasurement.TwoMinuteMean:
                            // Sjekke om data er gyldig
                            if (windDirection2m.status == DataStatus.OK)
                            {
                                return string.Format("{0}°", windDirection2m.data.ToString("000"));
                            }
                            else
                            {
                                return Constants.NotAvailable;
                            }

                        case WindMeasurement.TenMinuteMean:
                            // Sjekke om data er gyldig
                            if (windDirection10m.status == DataStatus.OK)
                            {
                                return string.Format("{0}°", windDirection10m.data.ToString("000"));
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

        public double windDirectionRotation
        {
            get
            {
                if (windDirectionRT != null)
                {
                    switch (windMeasurement)
                    {
                        case WindMeasurement.RealTime:
                            // Sjekke om data er gyldig
                            if (windDirectionRT.status == DataStatus.OK)
                            {
                                return windDirectionRT.data;
                            }
                            else
                            {
                                return -1;
                            }

                        case WindMeasurement.TwoMinuteMean:
                            // Sjekke om data er gyldig
                            if (windDirection2m.status == DataStatus.OK)
                            {
                                return windDirection2m.data;
                            }
                            else
                            {
                                return -1;
                            }

                        case WindMeasurement.TenMinuteMean:
                            // Sjekke om data er gyldig
                            if (windDirection10m.status == DataStatus.OK)
                            {
                                return windDirection10m.data;
                            }
                            else
                            {
                                return -1;
                            }

                        default:
                            return -1;
                    }
                }
                else
                {
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

                        if (windMeasurement == WindMeasurement.RealTime)
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

                        if (windMeasurement == WindMeasurement.TwoMinuteMean)
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

                        if (windMeasurement == WindMeasurement.TenMinuteMean)
                            OnPropertyChanged(nameof(windSpeedString));
                    }
                }
            }
        }

        public string windSpeedString
        {
            get
            {
                if (windSpeedRT != null)
                {
                    switch (windMeasurement)
                    {
                        case WindMeasurement.RealTime:
                            // Sjekke om data er gyldig
                            if (windSpeedRT.status == DataStatus.OK)
                            {
                                return windSpeedRT.data.ToString("00");
                            }
                            else
                            {
                                return Constants.NotAvailable;
                            }

                        case WindMeasurement.TwoMinuteMean:
                            // Sjekke om data er gyldig
                            if (windSpeed2m.status == DataStatus.OK)
                            {
                                return windSpeed2m.data.ToString("00");
                            }
                            else
                            {
                                return Constants.NotAvailable;
                            }

                        case WindMeasurement.TenMinuteMean:
                            // Sjekke om data er gyldig
                            if (windSpeed10m.status == DataStatus.OK)
                            {
                                return windSpeed10m.data.ToString("00");
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

                        if (windMeasurement == WindMeasurement.TwoMinuteMean)
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

                        if (windMeasurement == WindMeasurement.TenMinuteMean)
                            OnPropertyChanged(nameof(windGustString));
                    }
                }
            }
        }

        public string windGustString
        {
            get
            {
                if (windGust2m != null)
                {
                    switch (windMeasurement)
                    {
                        case WindMeasurement.TwoMinuteMean:
                            // Sjekke om data er gyldig
                            if (windGust2m.status == DataStatus.OK)
                            {
                                return windGust2m.data.ToString("00");
                            }
                            else
                            {
                                return Constants.NotAvailable;
                            }

                        case WindMeasurement.TenMinuteMean:
                            // Sjekke om data er gyldig
                            if (windGust10m.status == DataStatus.OK)
                            {
                                return windGust10m.data.ToString("00");
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
                if (vesselHeading != null)
                {
                    // Sjekke om data er gyldig
                    if (vesselHeading.status == DataStatus.OK)
                    {
                        return string.Format("{0}°", vesselHeading.data.ToString("000"));
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

        public double vesselHeadingRotation
        {
            get
            {
                if (vesselHeading != null)
                {
                    // Sjekke om data er gyldig
                    if (vesselHeading.status == DataStatus.OK)
                    {
                        return vesselHeading.data;
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
                if (vesselCOG != null)
                {
                    // Sjekke om data er gyldig
                    if (vesselCOG.status == DataStatus.OK)
                    {
                        return string.Format("{0}°", vesselCOG.data.ToString("000"));
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
        // Vessel Speed
        /////////////////////////////////////////////////////////////////////////////
        private HMSData _vesselSpeed { get; set; } = new HMSData();
        public HMSData vesselSpeed
        {
            get
            {
                return _vesselSpeed;
            }
            set
            {
                if (value != null &&
                    _vesselSpeed != null)
                {
                    if (value.data != _vesselSpeed.data ||
                        value.timestamp != _vesselSpeed.timestamp)
                    {
                        _vesselSpeed.Set(value);

                        OnPropertyChanged(nameof(vesselSpeedString));
                    }
                }
            }
        }

        public string vesselSpeedString
        {
            get
            {
                if (vesselSpeed != null)
                {
                    // Sjekke om data er gyldig
                    if (vesselSpeed.status == DataStatus.OK)
                    {
                        return vesselSpeed.data.ToString("0.0");
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
                if (vesselSOG != null)
                {
                    // Sjekke om data er gyldig
                    if (vesselSOG.status == DataStatus.OK)
                    {
                        return vesselSOG.data.ToString("0.0");
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
                if (helideckHeading.status == DataStatus.OK)
                {
                    return string.Format("{0}°", helideckHeading.data.ToString("000"));
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
                if (helideckHeading != null)
                {
                    return helideckHeading.data;
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

        public bool displayCAP
        {
            get
            {
                if (adminSettingsVM.regulationStandard == RegulationStandard.CAP)
                    return true;
                else
                    return false;
            }
        }

        public bool displayNOROG
        {
            get
            {
                if (adminSettingsVM.regulationStandard == RegulationStandard.NOROG)
                    return true;
                else
                    return false;
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

                        OnPropertyChanged(nameof(helicopterHeadingString));
                        OnPropertyChanged(nameof(helicopterRotation));
                    }
                }
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Helicopter Heading / Rotasjon
        /////////////////////////////////////////////////////////////////////////////
        public string helicopterHeadingString
        {
            get
            {
                if (userInputsVM.displayMode == DisplayMode.OnDeck &&
                    helicopterHeading.status == DataStatus.OK)
                {
                    return string.Format("{0}°", helicopterHeading.data.ToString("000"));
                }
                else
                {
                    return Constants.NotAvailable;
                }
            }
        }

        public double helicopterRotation
        {
            get
            {
                if (helicopterHeading?.status == DataStatus.OK)
                {
                    return helicopterHeading.data;
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
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
