using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Threading;

namespace HMS_Client
{
    public class HelideckReportVM : INotifyPropertyChanged
    {
        // Change notification
        public event PropertyChangedEventHandler PropertyChanged;

        // Variabel oppdatert
        // Dersom navn ikke settes brukes kallende medlem sitt navn
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private Config config;
        private AdminSettingsVM adminSettingsVM;

        private DispatcherTimer TimeTimer = new DispatcherTimer();
        private DispatcherTimer DataStatusTimer = new DispatcherTimer();

        public void Init(Config config, AdminSettingsVM adminSettingsVM, SensorGroupStatus sensorStatus)
        {
            this.config = config;
            this.adminSettingsVM = adminSettingsVM;

            // Lese settings fra config fil

            // Helideck Report
            nameOfHLO = config.Read(ConfigKey.NameOfHLO, ConfigType.Data);
            emailFrom = config.Read(ConfigKey.Email, ConfigType.Data);
            telephone = config.Read(ConfigKey.Telephone, ConfigType.Data);

            if (config.Read(ConfigKey.DynamicPositioning, ConfigType.Data) == "1")
                dynamicPositioning = true;
            else
                dynamicPositioning = false;

            if (config.Read(ConfigKey.AccurateMonitoringEquipment, ConfigType.Data) == "1")
                accurateMonitoringEquipment = true;
            else
                accurateMonitoringEquipment = false;

            // Log Info
            flightNumber = config.Read(ConfigKey.FlightNumber, ConfigType.Data);
            returnLoad = config.ReadWithDefault(ConfigKey.ReturnLoad, 0, ConfigType.Data);
            totalLoad = config.ReadWithDefault(ConfigKey.TotalLoad, 0, ConfigType.Data);
            luggage = config.ReadWithDefault(ConfigKey.Luggage, 0, ConfigType.Data);
            cargo = config.ReadWithDefault(ConfigKey.Cargo, 0, ConfigType.Data);

            if (config.Read(ConfigKey.HelifuelAvailable, ConfigType.Data) == "1")
                helifuelAvailable = true;
            else
                helifuelAvailable = false;

            fuelQuantity = config.ReadWithDefault(ConfigKey.FuelQuantity, 0, ConfigType.Data);

            // Resue and recovery available
            if (config.Read(ConfigKey.RescueRecoveryAvailable, ConfigType.Data) == "1")
                rescueRecoveryAvailable = true;
            else
                rescueRecoveryAvailable = false;

            // NDB Serviceable
            if (config.Read(ConfigKey.NDBServiceable, ConfigType.Data) == "1")
                ndbServiceable = true;
            else
                ndbServiceable = false;

            // Lightning Present
            if (config.Read(ConfigKey.LightningPresent, ConfigType.Data) == "1")
                lightningPresent = true;
            else
                lightningPresent = false;

            // Cold Flaring
            if (config.Read(ConfigKey.ColdFlaring, ConfigType.Data) == "1")
                coldFlaring = true;
            else
                coldFlaring = false;

            // Any unserviceable sensors
            if (config.Read(ConfigKey.AnyUnserviceableSensors, ConfigType.Data) == "1")
                anyUnserviceableSensors = true;
            else
                anyUnserviceableSensors = false;

            anyUnserviceableSensorsComments = config.Read(ConfigKey.AnyUnserviceableSensorsComments, ConfigType.Data);

            // Routing
            routing1 = config.Read(ConfigKey.Routing1, ConfigType.Data);
            routing2 = config.Read(ConfigKey.Routing2, ConfigType.Data);
            routing3 = config.Read(ConfigKey.Routing3, ConfigType.Data);
            routing4 = config.Read(ConfigKey.Routing4, ConfigType.Data);

            // Log Info remarks
            logInfoRemarks = config.Read(ConfigKey.LogInfoRemarks, ConfigType.Data);

            // Weather Observations
            if (config.Read(ConfigKey.SeaSprayObserved, ConfigType.Data) == "1")
                seaSprayObserved = true;
            else
                seaSprayObserved = false;

            otherWeatherInfo = config.Read(ConfigKey.OtherWeatherInfo, ConfigType.Data);

            // Email / Helicopter Operator
            emailTo = config.Read(ConfigKey.EmailTo, ConfigType.Data);
            emailCC = config.Read(ConfigKey.EmailCC, ConfigType.Data);
            if (config.Read(ConfigKey.SendHMSScreenCapture, ConfigType.Data) == "1")
                sendHMSScreenCapture = true;
            else
                sendHMSScreenCapture = false;

            if (adminSettingsVM.regulationStandard == RegulationStandard.CAP)
            {
                emailSubject = config.Read(ConfigKey.EmailSubject, ConfigType.Data);
                emailBody = config.Read(ConfigKey.EmailBody, ConfigType.Data);
            }

            // Cloud Init
            cloudBase = new List<HMSData>();
            cloudCoverage = new List<HMSData>();
            for (int i = 0; i < Constants.TOTAL_CLOUD_LAYERS; i++)
            {
                cloudBase.Add(new HMSData());
                cloudCoverage.Add(new HMSData());
            }

            // Oppdatere dato og klokke
            TimeTimer.Interval = TimeSpan.FromMilliseconds(200);
            TimeTimer.Tick += UpdateClock;
            TimeTimer.Start();

            void UpdateClock(object sender, EventArgs e)
            {
                time = DateTime.UtcNow;
            }

            // Sjekke sensor/data status
            DataStatusTimer.Interval = TimeSpan.FromMilliseconds(config.ReadWithDefault(ConfigKey.ClientUpdateFrequencyUI, Constants.ClientUIUpdateFrequencyDefault));
            DataStatusTimer.Tick += DataStatusCheck;
            DataStatusTimer.Start();

            void DataStatusCheck(object sender, EventArgs e)
            {
                // Sjekke om vi har data timeout
                if (sensorStatus.TimeoutCheck(vesselName)) OnPropertyChanged(nameof(vesselNameString));
                sensorStatus.TimeoutCheck(emailServer);
                sensorStatus.TimeoutCheck(emailPort);
                sensorStatus.TimeoutCheck(emailUsername);
                sensorStatus.TimeoutCheck(emailPassword);
                sensorStatus.TimeoutCheck(emailSecureConnection);

                if (sensorStatus.TimeoutCheck(restrictionSector))
                {
                    OnPropertyChanged(nameof(restrictionSectorFrom));
                    OnPropertyChanged(nameof(restrictionSectorTo));
                }

                if (sensorStatus.TimeoutCheck(ndbFreq))
                {
                    OnPropertyChanged(nameof(ndbFreqString_NOROG));
                    OnPropertyChanged(nameof(ndbFreqString_CAP));
                }
                if (sensorStatus.TimeoutCheck(ndbIdent)) OnPropertyChanged(nameof(ndbIdentString_CAP));
                if (sensorStatus.TimeoutCheck(vhfFreq)) OnPropertyChanged(nameof(vhfFreqString));
                if (sensorStatus.TimeoutCheck(logFreq)) OnPropertyChanged(nameof(logFreqString));
                if (sensorStatus.TimeoutCheck(marineChannel)) OnPropertyChanged(nameof(marineChannelString));

                if (sensorStatus.TimeoutCheck(helideckWindSensorHeight)) OnPropertyChanged(nameof(helideckWindSensorHeightString));
                if (sensorStatus.TimeoutCheck(helideckWindSensorDistance)) OnPropertyChanged(nameof(helideckWindSensorDistanceString));
                if (sensorStatus.TimeoutCheck(helideckWindDirection)) OnPropertyChanged(nameof(helideckWindDirectionString_NOROG));
                if (sensorStatus.TimeoutCheck(helideckWindVelocity)) OnPropertyChanged(nameof(helideckWindVelocityString));
                if (sensorStatus.TimeoutCheck(helideckWindGust)) OnPropertyChanged(nameof(helideckWindGustString));

                if (sensorStatus.TimeoutCheck(areaWindSensorHeight)) OnPropertyChanged(nameof(areaWindSensorHeightString));
                if (sensorStatus.TimeoutCheck(areaWindSensorDistance)) OnPropertyChanged(nameof(areaWindSensorDistanceString));
                if (sensorStatus.TimeoutCheck(areaWindDirection)) OnPropertyChanged(nameof(areaWindDirectionString));
                if (sensorStatus.TimeoutCheck(areaWindVelocity)) OnPropertyChanged(nameof(areaWindVelocityString));
                if (sensorStatus.TimeoutCheck(areaWindGust)) OnPropertyChanged(nameof(areaWindGustString));

                if (sensorStatus.TimeoutCheck(weather))
                {
                    OnPropertyChanged(nameof(weatherPhenomenaString));
                }

                if (sensorStatus.TimeoutCheck(airPressureQNH)) OnPropertyChanged(nameof(airPressureQNHString));
                if (sensorStatus.TimeoutCheck(airPressureQFE)) OnPropertyChanged(nameof(airPressureQFEString));

                if (sensorStatus.TimeoutCheck(significantHeaveRate)) OnPropertyChanged(nameof(significantHeaveRateString));
                if (sensorStatus.TimeoutCheck(maximumHeaveRate)) OnPropertyChanged(nameof(maximumHeaveRateString));

                if (sensorStatus.TimeoutCheck(significantWaveHeight)) OnPropertyChanged(nameof(significantWaveHeightString));
            }
        }

        public void UpdateData(HMSDataCollection clientSensorList)
        {
            // Installation
            vesselName = clientSensorList.GetData(ValueType.SettingsVesselName);

            // Position
            latitude = clientSensorList.GetData(ValueType.Latitude);
            longitude = clientSensorList.GetData(ValueType.Longitude);

            // Navigation Frequencies
            ndbFreq = clientSensorList.GetData(ValueType.SettingsNDBFrequency);
            ndbIdent = clientSensorList.GetData(ValueType.SettingsNDBIdent);
            vhfFreq = clientSensorList.GetData(ValueType.SettingsVHFFrequency);
            logFreq = clientSensorList.GetData(ValueType.SettingsLogFrequency);
            marineChannel = clientSensorList.GetData(ValueType.SettingsMarineChannel);

            // Helideck Wind
            helideckWindSensorHeight = clientSensorList.GetData(ValueType.SettingsHelideckWindSensorHeight);
            helideckWindSensorDistance = clientSensorList.GetData(ValueType.SettingsHelideckWindSensorDistance);
            helideckWindDirection = clientSensorList.GetData(ValueType.HelideckWindDirection2m);
            helideckWindVelocity = clientSensorList.GetData(ValueType.HelideckWindSpeed2m);
            helideckWindGust = clientSensorList.GetData(ValueType.HelideckWindGust2m);

            // Area Wind
            areaWindSensorHeight = clientSensorList.GetData(ValueType.SettingsAreaWindSensorHeight);
            areaWindSensorDistance = clientSensorList.GetData(ValueType.SettingsAreaWindSensorDistance);
            areaWindDirection = clientSensorList.GetData(ValueType.AreaWindDirection2m);
            areaWindVelocity = clientSensorList.GetData(ValueType.AreaWindSpeed2m);
            areaWindGust = clientSensorList.GetData(ValueType.AreaWindGust2m);

            visibility = clientSensorList.GetData(ValueType.Visibility);

            vesselHeading = clientSensorList.GetData(ValueType.VesselHeading);

            helideckHeading = clientSensorList.GetData(ValueType.HelideckHeading);

            weather = clientSensorList.GetData(ValueType.Weather);

            // Cloud level (coverage of FEW or more)
            if (clientSensorList.GetData(ValueType.CloudLayer1Coverage).data > 0)
            {
                cloudLayer_NOROG = clientSensorList.GetData(ValueType.CloudLayer1Base);
            }
            else
            {
                cloudLayer_NOROG.status = DataStatus.TIMEOUT_ERROR;
                OnPropertyChanged(nameof(cloudLayerString_NOROG));
            }

            SetCloudData(0, clientSensorList.GetData(ValueType.CloudLayer1Base), clientSensorList.GetData(ValueType.CloudLayer1Coverage));
            SetCloudData(1, clientSensorList.GetData(ValueType.CloudLayer2Base), clientSensorList.GetData(ValueType.CloudLayer2Coverage));
            SetCloudData(2, clientSensorList.GetData(ValueType.CloudLayer3Base), clientSensorList.GetData(ValueType.CloudLayer3Coverage));
            SetCloudData(3, clientSensorList.GetData(ValueType.CloudLayer4Base), clientSensorList.GetData(ValueType.CloudLayer4Coverage));

            // Temperature
            airTemperature = clientSensorList.GetData(ValueType.AirTemperature);
            airDewPoint = clientSensorList.GetData(ValueType.AirDewPoint);

            // Air Pressure
            airPressureQNH = clientSensorList.GetData(ValueType.AirPressureQNH);
            airPressureQFE = clientSensorList.GetData(ValueType.AirPressureQFE);

            // Significant Wave Height
            significantWaveHeight = clientSensorList.GetData(ValueType.SignificantWaveHeight);

            // Helideck Movement
            installationCategory = clientSensorList.GetData(ValueType.SettingsHelideckCategory);
            pitchUp = clientSensorList.GetData(ValueType.PitchMaxUp20m);
            pitchDown = clientSensorList.GetData(ValueType.PitchMaxDown20m);
            rollLeft = clientSensorList.GetData(ValueType.RollMaxLeft20m);
            rollRight = clientSensorList.GetData(ValueType.RollMaxRight20m);
            inclination = clientSensorList.GetData(ValueType.InclinationMax20m);
            maxHeave = clientSensorList.GetData(ValueType.HeaveHeightMax20m);
            heavePeriod = clientSensorList.GetData(ValueType.HeavePeriodMean);
            significantHeaveRate = clientSensorList.GetData(ValueType.SignificantHeaveRate);
            maximumHeaveRate = clientSensorList.GetData(ValueType.MaxHeaveRate);

            // Email Server
            emailServer = clientSensorList.GetData(ValueType.SettingsEmailServer);
            emailPort = clientSensorList.GetData(ValueType.SettingsEmailPort);
            emailUsername = clientSensorList.GetData(ValueType.SettingsEmailUsername);
            emailPassword = clientSensorList.GetData(ValueType.SettingsEmailPassword);
            emailSecureConnection = clientSensorList.GetData(ValueType.SettingsEmailSecureConnection);

            // Restriction Sector
            restrictionSector = clientSensorList.GetData(ValueType.SettingsRestrictedSector);
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

                    OnPropertyChanged(nameof(dateString_NOROG));
                    OnPropertyChanged(nameof(clockString_NOROG));
                    OnPropertyChanged(nameof(dateDayString_CAP));
                    OnPropertyChanged(nameof(dateMonthString_CAP));
                    OnPropertyChanged(nameof(dateYearString_CAP));
                    OnPropertyChanged(nameof(clockHoursString_CAP));
                    OnPropertyChanged(nameof(clockMinutesString_CAP));
                }
            }
        }
        public string dateString_NOROG
        {
            get
            {
                return _time.ToShortDateString();
            }
        }
        public string clockString_NOROG
        {
            get
            {
                return _time.ToString("HH:mm");
            }
        }
        public string dateDayString_CAP
        {
            get
            {
                return _time.Day.ToString();
            }
        }
        public string dateMonthString_CAP
        {
            get
            {
                return _time.Month.ToString();
            }
        }
        public string dateYearString_CAP
        {
            get
            {
                return _time.Year.ToString();
            }
        }
        public string clockHoursString_CAP
        {
            get
            {
                return _time.Hour.ToString("00");
            }
        }
        public string clockMinutesString_CAP
        {
            get
            {
                return _time.Minute.ToString("00");
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Name of HLO
        /////////////////////////////////////////////////////////////////////////////
        private string _nameOfHLO { get; set; }
        public string nameOfHLO
        {
            get
            {
                return _nameOfHLO;
            }
            set
            {
                if (value != null)
                {
                    _nameOfHLO = value;
                    config.Write(ConfigKey.NameOfHLO, value, ConfigType.Data);
                    OnPropertyChanged();
                }
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Email: From
        /////////////////////////////////////////////////////////////////////////////
        private string _emailFrom { get; set; }
        public string emailFrom
        {
            get
            {
                return _emailFrom;
            }
            set
            {
                if (value != null)
                {
                    _emailFrom = value;
                    config.Write(ConfigKey.Email, value, ConfigType.Data);
                    OnPropertyChanged();
                }
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Telephone
        /////////////////////////////////////////////////////////////////////////////
        private string _telephone { get; set; }
        public string telephone
        {
            get
            {
                return _telephone;
            }
            set
            {
                if (value != null)
                {
                    _telephone = value;
                    config.Write(ConfigKey.Telephone, value, ConfigType.Data);
                    OnPropertyChanged();
                }
            }
        }
        /////////////////////////////////////////////////////////////////////////////
        // Latitude
        /////////////////////////////////////////////////////////////////////////////
        private HMSData _latitude { get; set; } = new HMSData();
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

                        OnPropertyChanged(nameof(positionString_NOROG));
                        OnPropertyChanged(nameof(posLatHemisphereString_CAP));
                        OnPropertyChanged(nameof(posLatDegreesString_CAP));
                        OnPropertyChanged(nameof(posLatMinutesString_CAP));
                        OnPropertyChanged(nameof(posLonMeridianString_CAP));
                        OnPropertyChanged(nameof(posLonDegreesString_CAP));
                        OnPropertyChanged(nameof(posLonMinutesString_CAP));
                    }
                }
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Longitude
        /////////////////////////////////////////////////////////////////////////////
        private HMSData _longitude { get; set; } = new HMSData();
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

                        OnPropertyChanged(nameof(positionString_NOROG));
                        OnPropertyChanged(nameof(posLatHemisphereString_CAP));
                        OnPropertyChanged(nameof(posLatDegreesString_CAP));
                        OnPropertyChanged(nameof(posLatMinutesString_CAP));
                        OnPropertyChanged(nameof(posLonMeridianString_CAP));
                        OnPropertyChanged(nameof(posLonDegreesString_CAP));
                        OnPropertyChanged(nameof(posLonMinutesString_CAP));
                    }
                }
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Position String (NOROG)
        /////////////////////////////////////////////////////////////////////////////
        public string positionString_NOROG
        {
            get
            {
                if (latitude != null &&
                    longitude != null)
                {
                    // Sjekke om data er gyldig
                    if (latitude.status == DataStatus.OK &&
                        longitude.status == DataStatus.OK)
                    {
                        string latString;
                        string lonString;

                        // Hente data
                        double lon = longitude.data;
                        double lat = latitude.data;

                        // Sjekke data
                        if (lat <= 90.0 && lat >= -90.0)
                        {
                            string dir;
                            int deg;
                            int min;
                            double sec;

                            // Sette data
                            if (lat >= 0.0)
                            {
                                dir = "N";
                            }
                            else
                            {
                                dir = "S";
                                lat *= -1.0;
                            }

                            deg = (int)lat;
                            min = (int)((lat - (double)deg) * 60.0);
                            sec = (((lat - (double)deg) * 60.0) - (double)min) * 60.0;

                            latString = string.Format("{0}° {1}' {2}\" {3}",
                                deg.ToString("00"),
                                min.ToString("00"),
                                sec.ToString("00.0"),
                                dir);

                            // Sjekke data
                            if (lon <= 180.0 && lon >= -180.0)
                            {
                                // Sette data
                                if (lon >= 0.0)
                                {
                                    dir = "E";
                                }
                                else
                                {
                                    dir = "W";
                                    lon *= -1.0;
                                }

                                deg = (int)lon;
                                min = (int)((lon - (double)deg) * 60.0);
                                sec = (((lon - (double)deg) * 60.0) - (double)min) * 60.0;

                                lonString = string.Format("{0}° {1}' {2}\" {3}",
                                    deg.ToString("000"),
                                    min.ToString("00"),
                                    sec.ToString("00.0"),
                                    dir);

                                // Ferdig posisjon
                                return string.Format("{0} / {1}", latString, lonString);
                            }
                            else
                            {
                                return Constants.HelideckReportNoData;
                            }
                        }
                        else
                        {
                            return Constants.HelideckReportNoData;
                        }
                    }
                    else
                    {
                        return Constants.HelideckReportNoData;
                    }
                }
                else
                {
                    return Constants.HelideckReportNoData;
                }
            }
        }

        public string posLatHemisphereString_CAP
        {
            get
            {
                // Sjekke om data er gyldig
                if (latitude.status == DataStatus.OK)
                {
                    if (latitude.data >= 0)
                        return "N";
                    else
                        return "S";
                }
                else
                {
                    return Constants.HelideckReportNoData;
                }
            }
        }

        public string posLatDegreesString_CAP
        {
            get
            {
                // Sjekke om data er gyldig
                if (latitude.status == DataStatus.OK)
                {
                    if (latitude.data >= 0)
                        return ((int)latitude.data).ToString("00");
                    else
                        return ((int)latitude.data * -1).ToString("00");
                }
                else
                {
                    return Constants.HelideckReportNoData;
                }
            }
        }

        public string posLatMinutesString_CAP
        {
            get
            {
                // Sjekke om data er gyldig
                if (latitude.status == DataStatus.OK)
                {
                    if (latitude.data >= 0)
                        return ((latitude.data - (double)((int)latitude.data)) * 60).ToString("00.000");
                    else
                        return ((latitude.data - (double)((int)latitude.data)) * -60).ToString("00.000");
                }
                else
                {
                    return Constants.HelideckReportNoData;
                }
            }
        }

        public string posLonMeridianString_CAP
        {
            get
            {
                // Sjekke om data er gyldig
                if (longitude.status == DataStatus.OK)
                {
                    if (longitude.data >= 0)
                        return "E";
                    else
                        return "W";
                }
                else
                {
                    return Constants.HelideckReportNoData;
                }
            }
        }

        public string posLonDegreesString_CAP
        {
            get
            {
                // Sjekke om data er gyldig
                if (longitude.status == DataStatus.OK)
                {
                    if (longitude.data >= 0)
                        return ((int)longitude.data).ToString("000");
                    else
                        return ((int)longitude.data * -1).ToString("000");
                }
                else
                {
                    return Constants.HelideckReportNoData;
                }
            }
        }

        public string posLonMinutesString_CAP
        {
            get
            {
                // Sjekke om data er gyldig
                if (longitude.status == DataStatus.OK)
                {
                    if (longitude.data >= 0)
                        return ((longitude.data - (double)((int)longitude.data)) * 60).ToString("00.000");
                    else
                        return ((longitude.data - (double)((int)longitude.data)) * -60).ToString("00.000");
                }
                else
                {
                    return Constants.HelideckReportNoData;
                }
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Dynamic Positioning
        /////////////////////////////////////////////////////////////////////////////
        private bool _dynamicPositioning { get; set; }
        public bool dynamicPositioning
        {
            get
            {
                return _dynamicPositioning;
            }
            set
            {
                _dynamicPositioning = value;

                if (_dynamicPositioning)
                    config.Write(ConfigKey.DynamicPositioning, "1");
                else
                    config.Write(ConfigKey.DynamicPositioning, "0");

                OnPropertyChanged(nameof(dynamicPositioningYes));
                OnPropertyChanged(nameof(dynamicPositioningNo));
            }
        }

        public Visibility dynamicPositioningYes
        {
            get
            {
                if (dynamicPositioning)
                    return Visibility.Visible;
                else
                    return Visibility.Collapsed;
            }
        }

        public Visibility dynamicPositioningNo
        {
            get
            {
                if (!dynamicPositioning)
                    return Visibility.Visible;
                else
                    return Visibility.Collapsed;
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Accurate Monitoring System
        /////////////////////////////////////////////////////////////////////////////
        private bool _accurateMonitoringEquipment { get; set; }
        public bool accurateMonitoringEquipment
        {
            get
            {
                return _accurateMonitoringEquipment;
            }
            set
            {
                _accurateMonitoringEquipment = value;

                if (_accurateMonitoringEquipment)
                    config.Write(ConfigKey.AccurateMonitoringEquipment, "1");
                else
                    config.Write(ConfigKey.AccurateMonitoringEquipment, "0");

                OnPropertyChanged(nameof(accurateMonitoringEquipmentYes));
                OnPropertyChanged(nameof(accurateMonitoringEquipmentNo));
            }
        }

        public Visibility accurateMonitoringEquipmentYes
        {
            get
            {
                if (accurateMonitoringEquipment)
                    return Visibility.Visible;
                else
                    return Visibility.Collapsed;
            }
        }

        public Visibility accurateMonitoringEquipmentNo
        {
            get
            {
                if (!accurateMonitoringEquipment)
                    return Visibility.Visible;
                else
                    return Visibility.Collapsed;
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // NDB Frequency
        /////////////////////////////////////////////////////////////////////////////
        private HMSData _ndbFreq { get; set; } = new HMSData();
        public HMSData ndbFreq
        {
            get
            {
                return _ndbFreq;
            }
            set
            {
                if (value != null)
                {
                    if (value.data != _ndbFreq.data ||
                        value.timestamp != _ndbFreq.timestamp)
                    {
                        _ndbFreq.Set(value);
                        OnPropertyChanged(nameof(ndbFreqString_NOROG));
                        OnPropertyChanged(nameof(ndbFreqString_CAP));
                    }
                }
            }
        }

        public string ndbFreqString_NOROG
        {
            get
            {
                if (ndbFreq.status == DataStatus.OK)
                    return ndbFreq.data.ToString("0.000", Constants.cultureInfo);
                else
                    return Constants.HelideckReportNoData;
            }
        }

        public string ndbFreqString_CAP
        {
            get
            {
                if (ndbFreq.status == DataStatus.OK && ndbServiceable)
                    return ndbFreq.data.ToString("0.000", Constants.cultureInfo);
                else
                    return Constants.HelideckReportNoData;
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // NDB Identifier
        /////////////////////////////////////////////////////////////////////////////
        private HMSData _ndbIdent { get; set; } = new HMSData();
        public HMSData ndbIdent
        {
            get
            {
                return _ndbIdent;
            }
            set
            {
                if (value != null)
                {
                    if (value.data != _ndbIdent.data ||
                        value.timestamp != _ndbIdent.timestamp)
                    {
                        _ndbIdent.Set(value);
                        OnPropertyChanged(nameof(ndbIdentString_CAP));
                    }
                }
            }
        }

        public string ndbIdentString_CAP
        {
            get
            {
                if (ndbIdent.status == DataStatus.OK && ndbServiceable)
                    return ndbIdent.data3;
                else
                    return Constants.HelideckReportNoData;
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // VHF Frequency
        /////////////////////////////////////////////////////////////////////////////
        private HMSData _vhfFreq { get; set; } = new HMSData();
        public HMSData vhfFreq
        {
            get
            {
                return _vhfFreq;
            }
            set
            {
                if (value != null)
                {
                    if (value.data != _vhfFreq.data ||
                        value.timestamp != _vhfFreq.timestamp)
                    {
                        _vhfFreq.Set(value);
                        OnPropertyChanged(nameof(vhfFreqString));
                    }
                }
            }
        }

        public string vhfFreqString
        {
            get
            {
                if (vhfFreq.status == DataStatus.OK)
                    return vhfFreq.data.ToString("0.000", Constants.cultureInfo);
                else
                    return Constants.HelideckReportNoData;
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Log Frequency
        /////////////////////////////////////////////////////////////////////////////
        private HMSData _logFreq { get; set; } = new HMSData();
        public HMSData logFreq
        {
            get
            {
                return _logFreq;
            }
            set
            {
                if (value != null)
                {
                    if (value.data != _logFreq.data ||
                        value.timestamp != _logFreq.timestamp)
                    {
                        _logFreq.Set(value);
                        OnPropertyChanged(nameof(logFreqString));
                    }
                }
            }
        }

        public string logFreqString
        {
            get
            {
                if (logFreq.status == DataStatus.OK)
                    return logFreq.data.ToString("0.000", Constants.cultureInfo);
                else
                    return Constants.HelideckReportNoData;
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Marine Channel
        /////////////////////////////////////////////////////////////////////////////
        private HMSData _marineChannel { get; set; } = new HMSData();
        public HMSData marineChannel
        {
            get
            {
                return _marineChannel;
            }
            set
            {
                if (value != null)
                {
                    if (value.data != _marineChannel.data ||
                        value.timestamp != _marineChannel.timestamp)
                    {
                        _marineChannel.Set(value);
                        OnPropertyChanged(nameof(marineChannelString));
                    }
                }
            }
        }

        public string marineChannelString
        {
            get
            {
                if (marineChannel.status == DataStatus.OK)
                    return marineChannel.data.ToString("0");
                else
                    return Constants.HelideckReportNoData;
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Flight Number
        /////////////////////////////////////////////////////////////////////////////
        private string _flightNumber { get; set; }
        public string flightNumber
        {
            get
            {
                return _flightNumber;
            }
            set
            {
                _flightNumber = value;
                config.Write(ConfigKey.FlightNumber, value, ConfigType.Data);
                OnPropertyChanged();
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Return Load
        /////////////////////////////////////////////////////////////////////////////
        private int _returnLoad { get; set; }
        public int returnLoad
        {
            get
            {
                return _returnLoad;
            }
            set
            {
                _returnLoad = value;
                config.Write(ConfigKey.ReturnLoad, value.ToString(), ConfigType.Data);
                OnPropertyChanged();
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Total Load
        /////////////////////////////////////////////////////////////////////////////
        private int _totalLoad { get; set; }
        public int totalLoad
        {
            get
            {
                return _totalLoad;
            }
            set
            {
                _totalLoad = value;
                config.Write(ConfigKey.TotalLoad, value.ToString(), ConfigType.Data);
                OnPropertyChanged();
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Luggage
        /////////////////////////////////////////////////////////////////////////////
        private int _luggage { get; set; }
        public int luggage
        {
            get
            {
                return _luggage;
            }
            set
            {
                _luggage = value;
                config.Write(ConfigKey.Luggage, value.ToString(), ConfigType.Data);
                OnPropertyChanged();
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Cargo
        /////////////////////////////////////////////////////////////////////////////
        private int _cargo { get; set; }
        public int cargo
        {
            get
            {
                return _cargo;
            }
            set
            {
                _cargo = value;
                config.Write(ConfigKey.Cargo, value.ToString(), ConfigType.Data);
                OnPropertyChanged();
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Helifuel Available
        /////////////////////////////////////////////////////////////////////////////
        private bool _helifuelAvailable { get; set; }
        public bool helifuelAvailable
        {
            get
            {
                return _helifuelAvailable;
            }
            set
            {
                _helifuelAvailable = value;

                if (_helifuelAvailable)
                    config.Write(ConfigKey.HelifuelAvailable, "1", ConfigType.Data);
                else
                    config.Write(ConfigKey.HelifuelAvailable, "0", ConfigType.Data);

                OnPropertyChanged();
                OnPropertyChanged(nameof(helifuelAvailableYes));
                OnPropertyChanged(nameof(helifuelAvailableNo));
                OnPropertyChanged(nameof(helifuelAvailableString_CAP));
                OnPropertyChanged(nameof(fuelQuantityString));
            }
        }

        public Visibility helifuelAvailableYes
        {
            get
            {
                if (helifuelAvailable)
                    return Visibility.Visible;
                else
                    return Visibility.Collapsed;
            }
        }

        public Visibility helifuelAvailableNo
        {
            get
            {
                if (!helifuelAvailable)
                    return Visibility.Visible;
                else
                    return Visibility.Collapsed;
            }
        }

        public string helifuelAvailableString_CAP
        {
            get
            {
                if (helifuelAvailable)
                    return "Yes";
                else
                    return "No";
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Fuel Quantity
        /////////////////////////////////////////////////////////////////////////////
        private int _fuelQuantity { get; set; }
        public int fuelQuantity
        {
            get
            {
                return _fuelQuantity;
            }
            set
            {
                _fuelQuantity = value;
                config.Write(ConfigKey.FuelQuantity, value.ToString(), ConfigType.Data);
                OnPropertyChanged();
                OnPropertyChanged(nameof(fuelQuantityString));
            }
        }

        public string fuelQuantityString
        {
            get
            {
                if (_helifuelAvailable)
                    return _fuelQuantity.ToString();
                else
                    return Constants.HelideckReportNoData;
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Rescue and Recovery Available
        /////////////////////////////////////////////////////////////////////////////
        private bool _rescueRecoveryAvailable { get; set; }
        public bool rescueRecoveryAvailable
        {
            get
            {
                return _rescueRecoveryAvailable;
            }
            set
            {
                _rescueRecoveryAvailable = value;

                if (_rescueRecoveryAvailable)
                    config.Write(ConfigKey.RescueRecoveryAvailable, "1", ConfigType.Data);
                else
                    config.Write(ConfigKey.RescueRecoveryAvailable, "0", ConfigType.Data);

                OnPropertyChanged();
                OnPropertyChanged(nameof(rescueRecoveryAvailableString));
            }
        }

        public string rescueRecoveryAvailableString
        {
            get
            {
                if (_rescueRecoveryAvailable)
                    return "Yes";
                else
                    return "No";
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // NDB serviceable
        /////////////////////////////////////////////////////////////////////////////
        private bool _ndbServiceable { get; set; }
        public bool ndbServiceable
        {
            get
            {
                return _ndbServiceable;
            }
            set
            {
                _ndbServiceable = value;

                if (_ndbServiceable)
                    config.Write(ConfigKey.NDBServiceable, "1", ConfigType.Data);
                else
                    config.Write(ConfigKey.NDBServiceable, "0", ConfigType.Data);

                OnPropertyChanged();
                OnPropertyChanged(nameof(ndbServiceableString));
                OnPropertyChanged(nameof(ndbFreqString_CAP));
                OnPropertyChanged(nameof(ndbIdentString_CAP));
            }
        }

        public string ndbServiceableString
        {
            get
            {
                if (_ndbServiceable)
                    return "Yes";
                else
                    return "No";
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Lightning Present
        /////////////////////////////////////////////////////////////////////////////
        private bool _lightningPresent { get; set; }
        public bool lightningPresent
        {
            get
            {
                return _lightningPresent;
            }
            set
            {
                _lightningPresent = value;

                if (_lightningPresent)
                    config.Write(ConfigKey.LightningPresent, "1", ConfigType.Data);
                else
                    config.Write(ConfigKey.LightningPresent, "0", ConfigType.Data);

                OnPropertyChanged();
                OnPropertyChanged(nameof(lightningPresentString_CAP));
            }
        }

        public string lightningPresentString_CAP
        {
            get
            {
                if (_lightningPresent)
                    return "Yes";
                else
                    return "No";
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Cold Flaring
        /////////////////////////////////////////////////////////////////////////////
        private bool _coldFlaring { get; set; }
        public bool coldFlaring
        {
            get
            {
                return _coldFlaring;
            }
            set
            {
                _coldFlaring = value;

                if (_coldFlaring)
                    config.Write(ConfigKey.ColdFlaring, "1", ConfigType.Data);
                else
                    config.Write(ConfigKey.ColdFlaring, "0", ConfigType.Data);

                OnPropertyChanged();
                OnPropertyChanged(nameof(coldFlaringString_CAP));
            }
        }

        public string coldFlaringString_CAP
        {
            get
            {
                if (_coldFlaring)
                    return "Yes";
                else
                    return "No";
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Any unserviceable sensors
        /////////////////////////////////////////////////////////////////////////////
        private bool _anyUnserviceableSensors { get; set; }
        public bool anyUnserviceableSensors
        {
            get
            {
                return _anyUnserviceableSensors;
            }
            set
            {
                _anyUnserviceableSensors = value;

                if (_anyUnserviceableSensors)
                    config.Write(ConfigKey.AnyUnserviceableSensors, "1", ConfigType.Data);
                else
                    config.Write(ConfigKey.AnyUnserviceableSensors, "0", ConfigType.Data);

                OnPropertyChanged();
                OnPropertyChanged(nameof(anyUnserviceableSensorsString_CAP));
                OnPropertyChanged(nameof(anyUnserviceableSensorsCommentsString));
            }
        }

        public string anyUnserviceableSensorsString_CAP
        {
            get
            {
                if (_anyUnserviceableSensors)
                    return "Yes";
                else
                    return "No";
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Any unserviceable sensors comments
        /////////////////////////////////////////////////////////////////////////////
        private string _anyUnserviceableSensorsComments { get; set; }
        public string anyUnserviceableSensorsComments
        {
            get
            {
                return _anyUnserviceableSensorsComments;
            }
            set
            {
                _anyUnserviceableSensorsComments = value;
                config.Write(ConfigKey.AnyUnserviceableSensorsComments, value, ConfigType.Data);
                OnPropertyChanged();
                OnPropertyChanged(nameof(anyUnserviceableSensorsCommentsString));
            }
        }

        public string anyUnserviceableSensorsCommentsString
        {
            get
            {
                if (anyUnserviceableSensors)
                    return _anyUnserviceableSensorsComments;
                else
                    return string.Empty;
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Routing 1
        /////////////////////////////////////////////////////////////////////////////
        private string _routing1 { get; set; }
        public string routing1
        {
            get
            {
                return _routing1;
            }
            set
            {
                _routing1 = value;
                config.Write(ConfigKey.Routing1, value, ConfigType.Data);
                OnPropertyChanged();
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Routing 2
        /////////////////////////////////////////////////////////////////////////////
        private string _routing2 { get; set; }
        public string routing2
        {
            get
            {
                return _routing2;
            }
            set
            {
                _routing2 = value;
                config.Write(ConfigKey.Routing2, value, ConfigType.Data);
                OnPropertyChanged();
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Routing 3
        /////////////////////////////////////////////////////////////////////////////
        private string _routing3 { get; set; }
        public string routing3
        {
            get
            {
                return _routing3;
            }
            set
            {
                _routing3 = value;
                config.Write(ConfigKey.Routing3, value, ConfigType.Data);
                OnPropertyChanged();
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Routing 4
        /////////////////////////////////////////////////////////////////////////////
        private string _routing4 { get; set; }
        public string routing4
        {
            get
            {
                return _routing4;
            }
            set
            {
                _routing4 = value;
                config.Write(ConfigKey.Routing4, value, ConfigType.Data);
                OnPropertyChanged();
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Log Info Remakrs
        /////////////////////////////////////////////////////////////////////////////
        private string _logInfoRemarks { get; set; }
        public string logInfoRemarks
        {
            get
            {
                return _logInfoRemarks;
            }
            set
            {
                _logInfoRemarks = value;
                config.Write(ConfigKey.LogInfoRemarks, value, ConfigType.Data);
                OnPropertyChanged();
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Helideck Wind Sensor Height
        /////////////////////////////////////////////////////////////////////////////
        private HMSData _helideckWindSensorHeight { get; set; } = new HMSData();
        public HMSData helideckWindSensorHeight
        {
            get
            {
                return _helideckWindSensorHeight;
            }
            set
            {
                if (value != null)
                {
                    if (value.data != _helideckWindSensorHeight.data ||
                        value.timestamp != _helideckWindSensorHeight.timestamp)
                    {
                        _helideckWindSensorHeight.Set(value);
                        OnPropertyChanged(nameof(helideckWindSensorHeightString));
                    }
                }
            }
        }

        public string helideckWindSensorHeightString
        {
            get
            {
                // Sjekke om data er gyldig
                if (helideckWindSensorHeight.status == DataStatus.OK)
                    return helideckWindSensorHeight.data.ToString("0");
                else
                    return Constants.HelideckReportNoData;
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Helideck Wind Sensor Distance
        /////////////////////////////////////////////////////////////////////////////
        private HMSData _helideckWindSensorDistance { get; set; } = new HMSData();
        public HMSData helideckWindSensorDistance
        {
            get
            {
                return _helideckWindSensorDistance;
            }
            set
            {
                if (value != null)
                {
                    if (value.data != _helideckWindSensorDistance.data ||
                        value.timestamp != _helideckWindSensorDistance.timestamp)
                    {
                        _helideckWindSensorDistance.Set(value);
                        OnPropertyChanged(nameof(helideckWindSensorDistanceString));
                    }
                }
            }
        }

        public string helideckWindSensorDistanceString
        {
            get
            {
                // Sjekke om data er gyldig
                if (helideckWindSensorDistance.status == DataStatus.OK)
                    return helideckWindSensorDistance.data.ToString("0");
                else
                    return Constants.HelideckReportNoData;
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Helideck Wind Direction
        /////////////////////////////////////////////////////////////////////////////
        private HMSData _helideckWindDirection { get; set; } = new HMSData();
        public HMSData helideckWindDirection
        {
            get
            {
                return _helideckWindDirection;
            }
            set
            {
                if (value != null)
                {
                    if (value.data != _helideckWindDirection.data ||
                        value.timestamp != _helideckWindDirection.timestamp)
                    {
                        _helideckWindDirection.Set(value);

                        OnPropertyChanged(nameof(helideckWindDirectionString_NOROG));
                        OnPropertyChanged(nameof(helideckWindDirectionString_CAP));
                    }
                }
            }
        }

        public string helideckWindDirectionString_NOROG
        {
            get
            {
                // Sjekke om data er gyldig
                if (helideckWindDirection.status == DataStatus.OK)
                {
                    int dir = (int)Math.Round(helideckWindDirection.data, 0, MidpointRounding.AwayFromZero);
                    if (dir == 0)
                        dir = 360;
                    return string.Format("{0}°", dir.ToString("000"));
                }
                else
                {
                    return Constants.HelideckReportNoData;
                }
            }
        }

        public string helideckWindDirectionString_CAP
        {
            get
            {
                // Sjekke om data er gyldig
                if (helideckWindDirection.status == DataStatus.OK)
                {
                    int dir = (int)Math.Round(helideckWindDirection.data, 0, MidpointRounding.AwayFromZero);

                    if (dir == 0)
                        dir = 360;

                    return dir.ToString("000");
                }
                else
                {
                    return Constants.HelideckReportNoData;
                }
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Helideck Wind Velocity
        /////////////////////////////////////////////////////////////////////////////
        private HMSData _helideckWindVelocity { get; set; } = new HMSData();
        public HMSData helideckWindVelocity
        {
            get
            {
                return _helideckWindVelocity;
            }
            set
            {
                if (value != null)
                {
                    if (value.data != _helideckWindVelocity.data ||
                        value.timestamp != _helideckWindVelocity.timestamp)
                    {
                        _helideckWindVelocity.Set(value);

                        OnPropertyChanged(nameof(helideckWindVelocityString));
                    }
                }
            }
        }

        public string helideckWindVelocityString
        {
            get
            {
                // Sjekke om data er gyldig
                if (helideckWindVelocity.status == DataStatus.OK)
                {
                    return helideckWindVelocity.data.ToString("00");
                }
                else
                {
                    return Constants.HelideckReportNoData;
                }
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Helideck Wind Gust (2m)
        /////////////////////////////////////////////////////////////////////////////
        private HMSData _helideckWindGust { get; set; } = new HMSData();
        public HMSData helideckWindGust
        {
            get
            {
                return _helideckWindGust;
            }
            set
            {
                if (value != null)
                {
                    if (value.data != _helideckWindGust.data ||
                        value.timestamp != _helideckWindGust.timestamp)
                    {
                        _helideckWindGust.Set(value);

                        OnPropertyChanged(nameof(helideckWindGustString));
                    }
                }
            }
        }

        public string helideckWindGustString
        {
            get
            {
                // Sjekke om data er gyldig
                if (helideckWindGust.status == DataStatus.OK)
                {
                    return helideckWindGust.data.ToString("00");
                }
                else
                {
                    return Constants.HelideckReportNoData;
                }
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Area Wind Sensor Height
        /////////////////////////////////////////////////////////////////////////////
        private HMSData _areaWindSensorHeight { get; set; } = new HMSData();
        public HMSData areaWindSensorHeight
        {
            get
            {
                return _areaWindSensorHeight;
            }
            set
            {
                if (value != null)
                {
                    if (value.data != _areaWindSensorHeight.data ||
                        value.timestamp != _areaWindSensorHeight.timestamp)
                    {
                        _areaWindSensorHeight.Set(value);
                        OnPropertyChanged(nameof(areaWindSensorHeightString));
                    }
                }
            }
        }

        public string areaWindSensorHeightString
        {
            get
            {
                // Sjekke om data er gyldig
                if (areaWindSensorHeight.status == DataStatus.OK)
                    return areaWindSensorHeight.data.ToString("0");
                else
                    return Constants.HelideckReportNoData;
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Area Wind Sensor Distance
        /////////////////////////////////////////////////////////////////////////////
        private HMSData _areaWindSensorDistance { get; set; } = new HMSData();
        public HMSData areaWindSensorDistance
        {
            get
            {
                return _areaWindSensorDistance;
            }
            set
            {
                if (value != null)
                {
                    if (value.data != _areaWindSensorDistance.data ||
                        value.timestamp != _areaWindSensorDistance.timestamp)
                    {
                        _areaWindSensorDistance.Set(value);
                        OnPropertyChanged(nameof(areaWindSensorDistanceString));
                    }
                }
            }
        }

        public string areaWindSensorDistanceString
        {
            get
            {
                // Sjekke om data er gyldig
                if (areaWindSensorDistance.status == DataStatus.OK)
                    return areaWindSensorDistance.data.ToString("0");
                else
                    return Constants.HelideckReportNoData;
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Area Wind Direction
        /////////////////////////////////////////////////////////////////////////////
        private HMSData _areaWindDirection { get; set; } = new HMSData();
        public HMSData areaWindDirection
        {
            get
            {
                return _areaWindDirection;
            }
            set
            {
                if (value != null)
                {
                    if (value.data != _areaWindDirection.data ||
                        value.timestamp != _areaWindDirection.timestamp)
                    {
                        _areaWindDirection.Set(value);

                        OnPropertyChanged(nameof(areaWindDirectionString));
                    }
                }
            }
        }

        public string areaWindDirectionString
        {
            get
            {
                // Sjekke om data er gyldig
                if (areaWindDirection.status == DataStatus.OK)
                {
                    int dir = (int)Math.Round(areaWindDirection.data, 0, MidpointRounding.AwayFromZero);
                    if (dir == 0)
                        dir = 360;
                    return string.Format("{0}°", dir.ToString("000"));
                }
                else
                {
                    return Constants.HelideckReportNoData;
                }
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Area Wind Velocity
        /////////////////////////////////////////////////////////////////////////////
        private HMSData _areaWindVelocity { get; set; } = new HMSData();
        public HMSData areaWindVelocity
        {
            get
            {
                return _areaWindVelocity;
            }
            set
            {
                if (value != null)
                {
                    if (value.data != _areaWindVelocity.data ||
                        value.timestamp != _areaWindVelocity.timestamp)
                    {
                        _areaWindVelocity.Set(value);

                        OnPropertyChanged(nameof(areaWindVelocityString));
                    }
                }
            }
        }

        public string areaWindVelocityString
        {
            get
            {
                // Sjekke om data er gyldig
                if (areaWindVelocity.status == DataStatus.OK)
                {
                    return areaWindVelocity.data.ToString("00");
                }
                else
                {
                    return Constants.HelideckReportNoData;
                }
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Area Wind Gust (2m)
        /////////////////////////////////////////////////////////////////////////////
        private HMSData _areaWindGust { get; set; } = new HMSData();
        public HMSData areaWindGust
        {
            get
            {
                return _areaWindGust;
            }
            set
            {
                if (value != null)
                {
                    if (value.data != _areaWindGust.data ||
                        value.timestamp != _areaWindGust.timestamp)
                    {
                        _areaWindGust.Set(value);

                        OnPropertyChanged(nameof(areaWindGustString));
                    }
                }
            }
        }

        public string areaWindGustString
        {
            get
            {
                // Sjekke om data er gyldig
                if (areaWindGust.status == DataStatus.OK)
                {
                    return areaWindGust.data.ToString("00");
                }
                else
                {
                    return Constants.HelideckReportNoData;
                }
            }
        }


        /////////////////////////////////////////////////////////////////////////////
        // Sea spray observed on helideck
        /////////////////////////////////////////////////////////////////////////////
        private bool _seaSprayObserved { get; set; }
        public bool seaSprayObserved
        {
            get
            {
                return _seaSprayObserved;
            }
            set
            {
                _seaSprayObserved = value;

                if (_seaSprayObserved)
                    config.Write(ConfigKey.SeaSprayObserved, "1", ConfigType.Data);
                else
                    config.Write(ConfigKey.SeaSprayObserved, "0", ConfigType.Data);

                OnPropertyChanged();
                OnPropertyChanged(nameof(seaSprayObservedYes));
                OnPropertyChanged(nameof(seaSprayObservedNo));
            }
        }

        public Visibility seaSprayObservedYes
        {
            get
            {
                if (seaSprayObserved)
                    return Visibility.Visible;
                else
                    return Visibility.Collapsed;
            }
        }

        public Visibility seaSprayObservedNo
        {
            get
            {
                if (!seaSprayObserved)
                    return Visibility.Visible;
                else
                    return Visibility.Collapsed;
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Visibility
        /////////////////////////////////////////////////////////////////////////////
        private HMSData _visibility { get; set; } = new HMSData();
        public HMSData visibility
        {
            get
            {
                return _visibility;
            }
            set
            {
                if (value != null)
                {
                    if (value.data != _visibility.data ||
                        value.timestamp != _visibility.timestamp)
                    {
                        _visibility.Set(value);
                        OnPropertyChanged(nameof(visibilityString));
                    }
                }
            }
        }

        public string visibilityString
        {
            get
            {
                // Sjekke om data er gyldig
                if (visibility.status == DataStatus.OK)
                    return visibility.data.ToString();
                else
                    return Constants.HelideckReportNoData;
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // QNH
        /////////////////////////////////////////////////////////////////////////////
        private HMSData _airPressureQNH { get; set; } = new HMSData();
        public HMSData airPressureQNH
        {
            get
            {
                return _airPressureQNH;
            }
            set
            {
                if (value != null)
                {
                    if (value.data != _airPressureQNH.data ||
                        value.timestamp != _airPressureQNH.timestamp)
                    {
                        _airPressureQNH.Set(value);
                        OnPropertyChanged(nameof(airPressureQNHString));
                    }
                }
            }
        }

        public string airPressureQNHString
        {
            get
            {
                // Sjekke om data er gyldig
                if (airPressureQNH.status == DataStatus.OK)
                    return airPressureQNH.data.ToString("0.0");
                else
                    return Constants.HelideckReportNoData;
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // QFE
        /////////////////////////////////////////////////////////////////////////////
        private HMSData _airPressureQFE { get; set; } = new HMSData();
        public HMSData airPressureQFE
        {
            get
            {
                return _airPressureQFE;
            }
            set
            {
                if (value != null)
                {
                    if (value.data != _airPressureQFE.data ||
                        value.timestamp != _airPressureQFE.timestamp)
                    {
                        _airPressureQFE.Set(value);
                        OnPropertyChanged(nameof(airPressureQFEString));
                    }
                }
            }
        }

        public string airPressureQFEString
        {
            get
            {
                // Sjekke om data er gyldig
                if (_airPressureQFE.status == DataStatus.OK)
                    return _airPressureQFE.data.ToString("0.0");
                else
                    return Constants.HelideckReportNoData;
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Significant Wave Height
        /////////////////////////////////////////////////////////////////////////////
        private HMSData _significantWaveHeight { get; set; } = new HMSData();
        public HMSData significantWaveHeight
        {
            get
            {
                return _significantWaveHeight;
            }
            set
            {
                if (value != null)
                {
                    if (value.data != _significantWaveHeight.data ||
                        value.timestamp != _significantWaveHeight.timestamp)
                    {
                        _significantWaveHeight.Set(value);
                        OnPropertyChanged(nameof(significantWaveHeightString));
                    }
                }
            }
        }

        public string significantWaveHeightString
        {
            get
            {
                // Sjekke om data er gyldig
                if (significantWaveHeight.status == DataStatus.OK)
                    return significantWaveHeight.data.ToString("0.0");
                else
                    return Constants.HelideckReportNoData;
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
                    }
                }
            }
        }

        public string helideckHeadingString
        {
            get
            {
                // Sjekke om data er gyldig
                if (helideckHeading.status == DataStatus.OK)
                    return string.Format("{0}°", helideckHeading.data.ToString("000"));
                else
                    return Constants.HelideckReportNoData;
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
                        OnPropertyChanged(nameof(vesselHeadingString_NOROG));
                        OnPropertyChanged(nameof(vesselHeadingString_CAP));
                    }
                }
            }
        }

        public string vesselHeadingString_NOROG
        {
            get
            {
                // Sjekke om data er gyldig
                if (vesselHeading.status == DataStatus.OK)
                    return string.Format("{0}°", vesselHeading.data.ToString("000"));
                else
                    return Constants.HelideckReportNoData;
            }
        }

        public string vesselHeadingString_CAP
        {
            get
            {
                // Sjekke om data er gyldig
                if (vesselHeading.status == DataStatus.OK)
                    return vesselHeading.data.ToString("000");
                else
                    return Constants.HelideckReportNoData;
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Meteorological: Weather Phenomena
        /////////////////////////////////////////////////////////////////////////////
        private HMSData _weather { get; set; } = new HMSData();
        public HMSData weather
        {
            get
            {
                return _weather;
            }
            set
            {
                if (value != null)
                {
                    if (value.data != _weather.data ||
                        value.timestamp != _weather.timestamp)
                    {
                        _weather.Set(value);

                        OnPropertyChanged(nameof(weatherPhenomenaString));
                    }
                }
            }
        }

        public string weatherPhenomenaString
        {
            get
            {
                if (weather != null)
                {
                    // Sjekke om data er gyldig
                    if (weather.status == DataStatus.OK)
                    {
                        string weatherPhenomenaString = string.Empty;

                        WeatherSeverity severity = Weather.DecodeSeverity((int)weather.data);
                        WeatherPhenomena weather1 = Weather.DecodePhenomena1((int)weather.data);
                        WeatherPhenomena weather2 = Weather.DecodePhenomena2((int)weather.data);

                        if (severity != WeatherSeverity.None)
                        {
                            weatherPhenomenaString = severity.GetDescription();
                        }

                        if (weather1 != WeatherPhenomena.None)
                        {
                            weatherPhenomenaString += " " + weather1.GetDescription();
                        }

                        if (weather2 != WeatherPhenomena.None)
                        {
                            weatherPhenomenaString += " " + weather2.GetDescription();
                        }

                        return weatherPhenomenaString;
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
        // Temperature
        /////////////////////////////////////////////////////////////////////////////
        private HMSData _airTemperature { get; set; } = new HMSData();
        public HMSData airTemperature
        {
            get
            {
                return _airTemperature;
            }
            set
            {
                if (value != null)
                {
                    if (value.data != _airTemperature.data ||
                        value.timestamp != _airTemperature.timestamp)
                    {
                        _airTemperature.Set(value);
                        OnPropertyChanged(nameof(airTemperatureString));
                    }
                }
            }
        }

        public string airTemperatureString
        {
            get
            {
                // Sjekke om data er gyldig
                if (airTemperature.status == DataStatus.OK)
                    return airTemperature.data.ToString("0");
                else
                    return Constants.HelideckReportNoData;
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Dewpoint
        /////////////////////////////////////////////////////////////////////////////
        private HMSData _airDewPoint { get; set; } = new HMSData();
        public HMSData airDewPoint
        {
            get
            {
                return _airDewPoint;
            }
            set
            {
                if (value != null)
                {
                    if (value.data != _airDewPoint.data ||
                        value.timestamp != _airDewPoint.timestamp)
                    {
                        _airDewPoint.Set(value);
                        OnPropertyChanged(nameof(airDewPointString));
                    }
                }
            }
        }

        public string airDewPointString
        {
            get
            {
                // Sjekke om data er gyldig
                if (airDewPoint.status == DataStatus.OK)
                    return airDewPoint.data.ToString("0");
                else
                    return Constants.HelideckReportNoData;
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Cloud Layer (NOROG)
        /////////////////////////////////////////////////////////////////////////////
        private HMSData _cloudLayer_NOROG { get; set; } = new HMSData();
        public HMSData cloudLayer_NOROG
        {
            get
            {
                return _cloudLayer_NOROG;
            }
            set
            {
                if (value != null)
                {
                    if (value.data != _cloudLayer_NOROG.data ||
                        value.timestamp != _cloudLayer_NOROG.timestamp)
                    {
                        _cloudLayer_NOROG.Set(value);
                        OnPropertyChanged(nameof(cloudLayerString_NOROG));
                    }
                }
            }
        }

        public string cloudLayerString_NOROG
        {
            get
            {
                // Sjekke om data er gyldig
                if (cloudLayer_NOROG.status == DataStatus.OK)
                    return cloudLayer_NOROG.data.ToString("0");
                else
                    return Constants.HelideckReportNoData;
            }
        }


        /////////////////////////////////////////////////////////////////////////////
        // Meteorological: Cloud Base Layer 1-4
        /////////////////////////////////////////////////////////////////////////////
        private List<HMSData> cloudBase { get; set; }
        private List<HMSData> cloudCoverage { get; set; }

        private void SetCloudData(int layer, HMSData cBase, HMSData cCoverage)
        {
            if (layer < Constants.TOTAL_CLOUD_LAYERS && layer >= 0)
            {
                cloudBase[layer]?.Set(cBase);
                cloudCoverage[layer]?.Set(cCoverage);
            }

            switch (layer)
            {
                case 0:
                    OnPropertyChanged(nameof(cloudBaseString1_CAP));
                    OnPropertyChanged(nameof(cloudCoverageString1_CAP));
                    break;

                case 1:
                    OnPropertyChanged(nameof(cloudBaseString2_CAP));
                    OnPropertyChanged(nameof(cloudCoverageString2_CAP));
                    break;

                case 2:
                    OnPropertyChanged(nameof(cloudBaseString3_CAP));
                    OnPropertyChanged(nameof(cloudCoverageString3_CAP));
                    break;

                case 3:
                    OnPropertyChanged(nameof(cloudBaseString4_CAP));
                    OnPropertyChanged(nameof(cloudCoverageString4_CAP));
                    break;
            }
        }

        private string GetCloudBase(int layer)
        {
            if (layer < Constants.TOTAL_CLOUD_LAYERS && layer >= 0)
            {
                if (cloudBase[layer]?.status == DataStatus.OK &&
                    !double.IsNaN(cloudBase[layer].data))
                    return cloudBase[layer]?.data.ToString("0");
                else
                    return Constants.NotAvailable;
            }
            else
            {
                return Constants.NotAvailable;
            }
        }

        public string cloudBaseString1_CAP
        {
            get
            {
                return GetCloudBase(0);
            }
        }

        public string cloudBaseString2_CAP
        {
            get
            {
                return GetCloudBase(1);
            }
        }

        public string cloudBaseString3_CAP
        {
            get
            {
                return GetCloudBase(2);
            }
        }

        public string cloudBaseString4_CAP
        {
            get
            {
                return GetCloudBase(3);
            }
        }

        private string GetCloudCoverage(int layer)
        {
            if (layer < Constants.TOTAL_CLOUD_LAYERS && layer >= 0)
            {
                if (cloudCoverage[layer]?.status == DataStatus.OK)
                {
                    switch (cloudCoverage[layer].data)
                    {
                        case 0:
                            return "SKC";
                        case 1:
                        case 2:
                            return "FEW";
                        case 3:
                        case 4:
                            return "SCT";
                        case 5:
                        case 6:
                        case 7:
                            return "BKN";
                        case 8:
                            return "OVC";
                        default:
                            return string.Empty;
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

        public string cloudCoverageString1_CAP
        {
            get
            {
                return GetCloudCoverage(0);
            }
        }

        public string cloudCoverageString2_CAP
        {
            get
            {
                return GetCloudCoverage(1);
            }
        }

        public string cloudCoverageString3_CAP
        {
            get
            {
                return GetCloudCoverage(2);
            }
        }

        public string cloudCoverageString4_CAP
        {
            get
            {
                return GetCloudCoverage(3);
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Installation Category
        /////////////////////////////////////////////////////////////////////////////
        private HMSData _installationCategory { get; set; } = new HMSData();
        public HMSData installationCategory
        {
            get
            {
                return _installationCategory;
            }
            set
            {
                if (value != null)
                {
                    if (value.data != _installationCategory.data ||
                        value.timestamp != _installationCategory.timestamp)
                    {
                        _installationCategory.Set(value);
                        OnPropertyChanged(nameof(installationCategoryString));
                    }
                }
            }
        }

        public string installationCategoryString
        {
            get
            {
                // Sjekke om data er gyldig
                if (installationCategory.status == DataStatus.OK)
                {
                    switch (_installationCategory.data)
                    {
                        case 0:
                            return "1";
                        case 1:
                            return "2";
                        case 2:
                            return "3";
                        default:
                            return Constants.HelideckReportNoData;
                    }
                }
                else
                {
                    return Constants.HelideckReportNoData;
                }
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Motion: Pitch Up
        /////////////////////////////////////////////////////////////////////////////
        private HMSData _pitchUp { get; set; } = new HMSData();
        public HMSData pitchUp
        {
            get
            {
                return _pitchUp;
            }
            set
            {
                if (value != null)
                {
                    if (value.data != _pitchUp.data ||
                        value.timestamp != _pitchUp.timestamp)
                    {
                        _pitchUp.Set(value);
                        OnPropertyChanged(nameof(pitchUpString));
                    }
                }
            }
        }

        public string pitchUpString
        {
            get
            {
                // Sjekke om data er gyldig
                if (pitchUp.status == DataStatus.OK)
                    return Math.Abs(pitchUp.data).ToString("0.0");
                else
                    return Constants.HelideckReportNoData;
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Motion: Pitch Down
        /////////////////////////////////////////////////////////////////////////////
        private HMSData _pitchDown { get; set; } = new HMSData();
        public HMSData pitchDown
        {
            get
            {
                return _pitchDown;
            }
            set
            {
                if (value != null)
                {
                    if (value.data != _pitchDown.data ||
                        value.timestamp != _pitchDown.timestamp)
                    {
                        _pitchDown.Set(value);
                        OnPropertyChanged(nameof(pitchDownString));
                    }
                }
            }
        }

        public string pitchDownString
        {
            get
            {
                // Sjekke om data er gyldig
                if (pitchDown.status == DataStatus.OK)
                    return Math.Abs(pitchDown.data).ToString("0.0");
                else
                    return Constants.HelideckReportNoData;
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Motion: Roll Left
        /////////////////////////////////////////////////////////////////////////////
        private HMSData _rollLeft { get; set; } = new HMSData();
        public HMSData rollLeft
        {
            get
            {
                return _rollLeft;
            }
            set
            {
                if (value != null)
                {
                    if (value.data != _rollLeft.data ||
                        value.timestamp != _rollLeft.timestamp)
                    {
                        _rollLeft.Set(value);
                        OnPropertyChanged(nameof(rollLeftString));
                    }
                }
            }
        }

        public string rollLeftString
        {
            get
            {
                // Sjekke om data er gyldig
                if (rollLeft.status == DataStatus.OK)
                    return Math.Abs(rollLeft.data).ToString("0.0");
                else
                    return Constants.HelideckReportNoData;
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Motion: Roll Right
        /////////////////////////////////////////////////////////////////////////////
        private HMSData _rollRight { get; set; } = new HMSData();
        public HMSData rollRight
        {
            get
            {
                return _rollRight;
            }
            set
            {
                if (value != null)
                {
                    if (value.data != _rollRight.data ||
                        value.timestamp != _rollRight.timestamp)
                    {
                        _rollRight.Set(value);
                        OnPropertyChanged(nameof(rollRightString));
                    }
                }
            }
        }

        public string rollRightString
        {
            get
            {
                // Sjekke om data er gyldig
                if (rollRight.status == DataStatus.OK)
                    return Math.Abs(rollRight.data).ToString("0.0");
                else
                    return Constants.HelideckReportNoData;
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Motion: Inclination
        /////////////////////////////////////////////////////////////////////////////
        private HMSData _inclination { get; set; } = new HMSData();
        public HMSData inclination
        {
            get
            {
                return _inclination;
            }
            set
            {
                if (value != null)
                {
                    if (value.data != _inclination.data ||
                        value.timestamp != _inclination.timestamp)
                    {
                        _inclination.Set(value);
                        OnPropertyChanged(nameof(inclinationString));
                    }
                }
            }
        }

        public string inclinationString
        {
            get
            {
                // Sjekke om data er gyldig
                if (inclination.status == DataStatus.OK)
                    return inclination.data.ToString("0.0");
                else
                    return Constants.HelideckReportNoData;
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Motion: Max Heave
        /////////////////////////////////////////////////////////////////////////////
        private HMSData _maxHeave { get; set; } = new HMSData();
        public HMSData maxHeave
        {
            get
            {
                return _maxHeave;
            }
            set
            {
                if (value != null)
                {
                    if (value.data != _maxHeave.data ||
                        value.timestamp != _maxHeave.timestamp)
                    {
                        _maxHeave.Set(value);
                        OnPropertyChanged(nameof(maxHeaveString));
                    }
                }
            }
        }

        public string maxHeaveString
        {
            get
            {
                // Sjekke om data er gyldig
                if (maxHeave.status == DataStatus.OK)
                    return maxHeave.data.ToString("0.0");
                else
                    return Constants.HelideckReportNoData;
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Motion: Heave Period
        /////////////////////////////////////////////////////////////////////////////
        private HMSData _heavePeriod { get; set; } = new HMSData();
        public HMSData heavePeriod
        {
            get
            {
                return _heavePeriod;
            }
            set
            {
                if (value != null)
                {
                    if (value.data != _heavePeriod.data ||
                        value.timestamp != _heavePeriod.timestamp)
                    {
                        _heavePeriod.Set(value);
                        OnPropertyChanged(nameof(heavePeriodString));
                    }
                }
            }
        }

        public string heavePeriodString
        {
            get
            {
                // Sjekke om data er gyldig
                if (heavePeriod.status == DataStatus.OK)
                    return heavePeriod.data.ToString("0.0");
                else
                    return Constants.HelideckReportNoData;
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Motion: Significant Heave Rate
        /////////////////////////////////////////////////////////////////////////////
        private HMSData _significantHeaveRate { get; set; } = new HMSData();
        public HMSData significantHeaveRate
        {
            get
            {
                return _significantHeaveRate;
            }
            set
            {
                if (value != null)
                {
                    if (value.data != _significantHeaveRate.data ||
                        value.timestamp != _significantHeaveRate.timestamp)
                    {
                        _significantHeaveRate.Set(value);
                        OnPropertyChanged(nameof(significantHeaveRateString));
                    }
                }
            }
        }

        public string significantHeaveRateString
        {
            get
            {
                // Sjekke om data er gyldig
                if (_significantHeaveRate.status == DataStatus.OK)
                    return _significantHeaveRate.data.ToString("0.0");
                else
                    return Constants.HelideckReportNoData;
            }
        }


        /////////////////////////////////////////////////////////////////////////////
        // Motion: Maximum Heave Rate
        /////////////////////////////////////////////////////////////////////////////
        private HMSData _maximumHeaveRate { get; set; } = new HMSData();
        public HMSData maximumHeaveRate
        {
            get
            {
                return _maximumHeaveRate;
            }
            set
            {
                if (value != null)
                {
                    if (value.data != _maximumHeaveRate.data ||
                        value.timestamp != _maximumHeaveRate.timestamp)
                    {
                        _maximumHeaveRate.Set(value);
                        OnPropertyChanged(nameof(maximumHeaveRateString));
                    }
                }
            }
        }

        public string maximumHeaveRateString
        {
            get
            {
                // Sjekke om data er gyldig
                if (_maximumHeaveRate.status == DataStatus.OK)
                    return _maximumHeaveRate.data.ToString("0.0");
                else
                    return Constants.HelideckReportNoData;
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Other weather info
        /////////////////////////////////////////////////////////////////////////////
        private string _otherWeatherInfo { get; set; }
        public string otherWeatherInfo
        {
            get
            {
                return _otherWeatherInfo;
            }
            set
            {
                _otherWeatherInfo = value;
                config.Write(ConfigKey.OtherWeatherInfo, value, ConfigType.Data);
                OnPropertyChanged();
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Email: To
        /////////////////////////////////////////////////////////////////////////////
        private string _emailTo { get; set; }
        public string emailTo
        {
            get
            {
                return _emailTo;
            }
            set
            {
                if (value != null)
                {
                    _emailTo = value;
                    config.Write(ConfigKey.EmailTo, value, ConfigType.Data);
                    OnPropertyChanged();
                }
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Email: CC
        /////////////////////////////////////////////////////////////////////////////
        private string _emailCC { get; set; }
        public string emailCC
        {
            get
            {
                return _emailCC;
            }
            set
            {
                if (value != null)
                {
                    _emailCC = value;
                    config.Write(ConfigKey.EmailCC, value, ConfigType.Data);
                    OnPropertyChanged();
                }
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Email: Subject
        /////////////////////////////////////////////////////////////////////////////
        private string _emailSubject { get; set; }
        public string emailSubject
        {
            get
            {
                return _emailSubject;
            }
            set
            {
                if (value != null)
                {
                    _emailSubject = value;
                    config.Write(ConfigKey.EmailSubject, value, ConfigType.Data);
                    OnPropertyChanged();
                }
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Email: Body
        /////////////////////////////////////////////////////////////////////////////
        private string _emailBody { get; set; }
        public string emailBody
        {
            get
            {
                return _emailBody;
            }
            set
            {
                if (value != null)
                {
                    _emailBody = value;
                    config.Write(ConfigKey.EmailBody, value, ConfigType.Data);
                    OnPropertyChanged();
                }
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Email: Attachment 1: Helideck Report PDF
        /////////////////////////////////////////////////////////////////////////////
        private string _helideckReportFile { get; set; }
        public string helideckReportFile
        {
            get
            {
                return _helideckReportFile;
            }
            set
            {
                if (value != null)
                {
                    _helideckReportFile = value;
                    OnPropertyChanged(nameof(emailAttachment1));
                }
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Email: Attachment 2: Screen Capture JPG
        /////////////////////////////////////////////////////////////////////////////
        private string _screenCaptureFile { get; set; }
        public string screenCaptureFile
        {
            get
            {
                return _screenCaptureFile;
            }
            set
            {
                if (value != null)
                {
                    _screenCaptureFile = value;
                    OnPropertyChanged(nameof(emailAttachment2));
                }
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Email: Attachment 1
        /////////////////////////////////////////////////////////////////////////////
        public string emailAttachment1
        {
            get
            {
                return helideckReportFile;
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Email: Attachment 2
        /////////////////////////////////////////////////////////////////////////////
        public string emailAttachment2
        {
            get
            {
                if (sendHMSScreenCapture)
                {
                    return screenCaptureFile;
                }
                else
                {
                    return string.Empty;
                }
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Email: Warning Message
        /////////////////////////////////////////////////////////////////////////////
        private string _warningMessage { get; set; }
        public string warningMessage
        {
            get
            {
                return _warningMessage;
            }
            set
            {
                if (value != null)
                {
                    _warningMessage = value;
                    OnPropertyChanged();
                }
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Sea spray observed on helideck
        /////////////////////////////////////////////////////////////////////////////
        private bool _sendHMSScreenCapture { get; set; }
        public bool sendHMSScreenCapture
        {
            get
            {
                return _sendHMSScreenCapture;
            }
            set
            {
                _sendHMSScreenCapture = value;

                if (_sendHMSScreenCapture)
                    config.Write(ConfigKey.SendHMSScreenCapture, "1", ConfigType.Data);
                else
                    config.Write(ConfigKey.SendHMSScreenCapture, "0", ConfigType.Data);

                OnPropertyChanged();
                OnPropertyChanged(nameof(emailAttachment2));
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
        // Email Server
        /////////////////////////////////////////////////////////////////////////////
        private HMSData _emailServer { get; set; } = new HMSData();
        public HMSData emailServer
        {
            get
            {
                return _emailServer;
            }
            set
            {
                _emailServer.Set(value);
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Email Port
        /////////////////////////////////////////////////////////////////////////////
        private HMSData _emailPort { get; set; } = new HMSData();
        public HMSData emailPort
        {
            get
            {
                return _emailPort;
            }
            set
            {
                _emailPort.Set(value);
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Email Username
        /////////////////////////////////////////////////////////////////////////////
        private HMSData _emailUsername { get; set; } = new HMSData();
        public HMSData emailUsername
        {
            get
            {
                return _emailUsername;
            }
            set
            {
                _emailUsername.Set(value);
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Email Password
        /////////////////////////////////////////////////////////////////////////////
        private HMSData _emailPassword { get; set; } = new HMSData();
        public HMSData emailPassword
        {
            get
            {
                return _emailPassword;
            }
            set
            {
                _emailPassword.Set(value);
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Email Secure Connection
        /////////////////////////////////////////////////////////////////////////////
        private HMSData _emailSecureConnection { get; set; } = new HMSData();
        public HMSData emailSecureConnection
        {
            get
            {
                return _emailSecureConnection;
            }
            set
            {
                _emailSecureConnection.Set(value);
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Is Sending Email
        /////////////////////////////////////////////////////////////////////////////
        //public bool isSendingEmail { get; set; }
        //public bool IsSendingEmail
        //{
        //    get
        //    {
        //        return isSendingEmail;
        //    }
        //    set
        //    {
        //        if (value != isSendingEmail)
        //        {
        //            isSendingEmail = value;
        //            OnPropertyChanged();
        //        }
        //    }
        //}

        /////////////////////////////////////////////////////////////////////////////
        // Email Status
        /////////////////////////////////////////////////////////////////////////////
        private EmailStatus emailStatus { get; set; }
        public EmailStatus EmailStatus
        {
            get
            {
                return emailStatus;
            }
            set
            {
                if (value != emailStatus)
                {
                    emailStatus = value;

                    OnPropertyChanged(nameof(emailStatusPreview));
                    OnPropertyChanged(nameof(emailStatusSending));
                    OnPropertyChanged(nameof(emailStatusSuccess));
                    OnPropertyChanged(nameof(emailStatusFailed));
                }
            }
        }

        public Visibility emailStatusPreview
        {
            get
            {
                if (emailStatus == EmailStatus.PREVIEW)
                    return Visibility.Visible;
                else
                    return Visibility.Collapsed;
            }
        }

        public Visibility emailStatusSending
        {
            get
            {
                if (emailStatus == EmailStatus.SENDING)
                    return Visibility.Visible;
                else
                    return Visibility.Collapsed;
            }
        }

        public Visibility emailStatusSuccess
        {
            get
            {
                if (emailStatus == EmailStatus.SUCCESS)
                    return Visibility.Visible;
                else
                    return Visibility.Collapsed;
            }
        }

        public Visibility emailStatusFailed
        {
            get
            {
                if (emailStatus == EmailStatus.FAILED)
                    return Visibility.Visible;
                else
                    return Visibility.Collapsed;
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Restriction Sector
        /////////////////////////////////////////////////////////////////////////////
        private HMSData _restrictionSector { get; set; } = new HMSData();
        public HMSData restrictionSector
        {
            get
            {
                return _restrictionSector;
            }
            set
            {
                _restrictionSector.Set(value);

                OnPropertyChanged(nameof(restrictionSectorFrom));
                OnPropertyChanged(nameof(restrictionSectorTo));
            }
        }

        public string restrictionSectorFrom
        {
            get
            {
                if (_restrictionSector.status == DataStatus.OK &&
                    restrictionSector.data != restrictionSector.data2)  // Dersom to/from sector er lik setter vi blank
                    return _restrictionSector.data.ToString("000");
                else
                    return Constants.HelideckReportNoData;
            }
        }

        public string restrictionSectorTo
        {
            get
            {
                if (_restrictionSector.status == DataStatus.OK &&
                    restrictionSector.data != restrictionSector.data2)  // Dersom to/from sector er lik setter vi blank
                    return _restrictionSector.data2.ToString("000");
                else
                    return Constants.HelideckReportNoData;
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // CAP: Enable Report Email
        /////////////////////////////////////////////////////////////////////////////
        public Visibility reportEmailVisibility
        {
            get
            {
                if (adminSettingsVM.enableReportEmail)
                    return Visibility.Visible;
                else
                    return Visibility.Collapsed;
            }
        }

    }
}