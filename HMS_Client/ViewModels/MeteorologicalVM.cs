using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Threading;

namespace HMS_Client
{
    public class MeteorologicalVM : INotifyPropertyChanged
    {
        // Change notification
        public event PropertyChangedEventHandler PropertyChanged;

        DispatcherTimer UIUpdateTimer = new DispatcherTimer();

        public MeteorologicalVM()
        {
            // Cloud Init
            cloudBase = new List<HMSData>();
            cloudCoverage = new List<HMSData>();
            for (int i = 0; i < Constants.TOTAL_CLOUD_LAYERS; i++)
            {
                cloudBase.Add(new HMSData());
                cloudCoverage.Add(new HMSData());
            }
        }

        public void Init(Config config, SensorGroupStatus sensorStatus)
        {
            // Oppdatere UI
            UIUpdateTimer.Interval = TimeSpan.FromMilliseconds(config.Read(ConfigKey.ClientUpdateFrequencyUI, Constants.ClientUpdateFrequencyUIDefault));
            UIUpdateTimer.Tick += UIUpdate;
            UIUpdateTimer.Start();

            void UIUpdate(object sender, EventArgs e)
            {
                // Sjekke om vi har data timeout
                if (sensorStatus.TimeoutCheck(airTemperature)) OnPropertyChanged(nameof(airTemperatureString));
                if (sensorStatus.TimeoutCheck(airHumidity)) OnPropertyChanged(nameof(airHumidityString));
                if (sensorStatus.TimeoutCheck(airDewPoint)) OnPropertyChanged(nameof(airDewPointString));
                if (sensorStatus.TimeoutCheck(airPressureQNH)) OnPropertyChanged(nameof(airPressureQNHString));
                if (sensorStatus.TimeoutCheck(airPressureQFE)) OnPropertyChanged(nameof(airPressureQFEString));
                if (sensorStatus.TimeoutCheck(visibility)) OnPropertyChanged(nameof(visibilityString));
                if (sensorStatus.TimeoutCheck(weather))
                {
                    OnPropertyChanged(nameof(weatherPhenomenaString));
                }

                if (sensorStatus.TimeoutCheck(cloudBase[0])) OnPropertyChanged(nameof(cloudBaseString1));
                if (sensorStatus.TimeoutCheck(cloudBase[1])) OnPropertyChanged(nameof(cloudBaseString2));
                if (sensorStatus.TimeoutCheck(cloudBase[2])) OnPropertyChanged(nameof(cloudBaseString3));
                if (sensorStatus.TimeoutCheck(cloudBase[3])) OnPropertyChanged(nameof(cloudBaseString4));

                if (sensorStatus.TimeoutCheck(cloudCoverage[0])) OnPropertyChanged(nameof(cloudCoverageString1));
                if (sensorStatus.TimeoutCheck(cloudCoverage[1])) OnPropertyChanged(nameof(cloudCoverageString2));
                if (sensorStatus.TimeoutCheck(cloudCoverage[2])) OnPropertyChanged(nameof(cloudCoverageString3));
                if (sensorStatus.TimeoutCheck(cloudCoverage[3])) OnPropertyChanged(nameof(cloudCoverageString4));
            }
        }

        public void UpdateData(HMSDataCollection clientSensorList)
        {
            airTemperature = clientSensorList.GetData(ValueType.AirTemperature);
            airHumidity = clientSensorList.GetData(ValueType.AirHumidity);
            airDewPoint = clientSensorList.GetData(ValueType.AirDewPoint);
            airPressureQNH = clientSensorList.GetData(ValueType.AirPressureQNH);
            airPressureQFE = clientSensorList.GetData(ValueType.AirPressureQFE);
            visibility = clientSensorList.GetData(ValueType.Visibility);
            weather = clientSensorList.GetData(ValueType.Weather);

            SetCloudData(0, clientSensorList.GetData(ValueType.CloudLayer1Base), clientSensorList.GetData(ValueType.CloudLayer1Coverage));
            SetCloudData(1, clientSensorList.GetData(ValueType.CloudLayer2Base), clientSensorList.GetData(ValueType.CloudLayer2Coverage));
            SetCloudData(2, clientSensorList.GetData(ValueType.CloudLayer3Base), clientSensorList.GetData(ValueType.CloudLayer3Coverage));
            SetCloudData(3, clientSensorList.GetData(ValueType.CloudLayer4Base), clientSensorList.GetData(ValueType.CloudLayer4Coverage));
        }

        /////////////////////////////////////////////////////////////////////////////
        // Meteorological: Air Temperature
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
                if (airTemperature != null)
                {
                    // Sjekke om data er gyldig
                    if (airTemperature.status == DataStatus.OK)
                    {
                        return string.Format("{0} °C", airTemperature.data.ToString("0.0"));
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
        // Meteorological: Humidity
        /////////////////////////////////////////////////////////////////////////////
        private HMSData _airHumidity { get; set; } = new HMSData();
        public HMSData airHumidity
        {
            get
            {
                return _airHumidity;
            }
            set
            {
                if (value != null)
                {
                    if (value.data != _airHumidity.data ||
                        value.timestamp != _airHumidity.timestamp)
                    {
                        _airHumidity.Set(value);

                        OnPropertyChanged(nameof(airHumidityString));
                    }
                }
            }
        }

        public string airHumidityString
        {
            get
            {
                if (airHumidity != null)
                {
                    // Sjekke om data er gyldig
                    if (airHumidity.status == DataStatus.OK)
                    {
                        return string.Format("{0} %", airHumidity.data.ToString("0.0"));
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
        // Meteorological: Dew Point
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
                if (airDewPoint != null)
                {
                    // Sjekke om data er gyldig
                    if (airDewPoint.status == DataStatus.OK)
                    {
                        return string.Format("{0} °C", airDewPoint.data.ToString("0.0"));
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
        // Meteorological: Air Pressure QNH
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
                if (airPressureQNH != null)
                {
                    // Sjekke om data er gyldig
                    if (airPressureQNH.status == DataStatus.OK)
                    {
                        return string.Format("{0} hPa", airPressureQNH.data.ToString("0.0"));
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
        // Meteorological: Air Pressure QFE
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
                if (airPressureQFE != null)
                {
                    // Sjekke om data er gyldig
                    if (airPressureQFE.status == DataStatus.OK)
                    {
                        return string.Format("{0} hPa", airPressureQFE.data.ToString("0.0"));
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
        // Meteorological: Visibility
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
                if (visibility != null)
                {
                    // Sjekke om data er gyldig
                    if (visibility.status == DataStatus.OK)
                    {
                        return string.Format("{0} m", visibility.data.ToString());
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
        // Meteorological: Cloud Base Layer 1-4
        /////////////////////////////////////////////////////////////////////////////
        private List<HMSData> cloudBase { get; set; }
        private List<HMSData> cloudCoverage { get; set; }
        // 0 = SKC, CLR
        // 1 = FEW
        // 2 = SCT
        // 3 = BKN
        // 4 = OVC
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
                    OnPropertyChanged(nameof(cloudBaseString1));
                    OnPropertyChanged(nameof(cloudCoverageString1));
                    break;

                case 1:
                    OnPropertyChanged(nameof(cloudBaseString2));
                    OnPropertyChanged(nameof(cloudCoverageString2));
                    break;

                case 2:
                    OnPropertyChanged(nameof(cloudBaseString3));
                    OnPropertyChanged(nameof(cloudCoverageString3));
                    break;

                case 3:
                    OnPropertyChanged(nameof(cloudBaseString4));
                    OnPropertyChanged(nameof(cloudCoverageString4));
                    break;
            }
        }

        private string GetCloudBase(int layer)
        {
            if (layer < Constants.TOTAL_CLOUD_LAYERS && layer >= 0)
            {
                if (cloudBase[layer]?.status == DataStatus.OK &&
                    !double.IsNaN(cloudBase[layer].data))
                    return string.Format("{0} ft", cloudBase[layer]?.data.ToString("0"));
                else
                    return Constants.NotAvailable;
            }
            else
            {
                return Constants.NotAvailable;
            }
        }

        public string cloudBaseString1
        {
            get
            {
                return GetCloudBase(0);
            }
        }

        public string cloudBaseString2
        {
            get
            {
                return GetCloudBase(1);
            }
        }

        public string cloudBaseString3
        {
            get
            {
                return GetCloudBase(2);
            }
        }

        public string cloudBaseString4
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
                            return string.Empty;
                        case 1:
                            return "FEW";
                        case 2:
                            return "SCT";
                        case 3:
                            return "BKN";
                        case 4:
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

        public string cloudCoverageString1
        {
            get
            {
                return GetCloudCoverage(0);
            }
        }

        public string cloudCoverageString2
        {
            get
            {
                return GetCloudCoverage(1);
            }
        }

        public string cloudCoverageString3
        {
            get
            {
                return GetCloudCoverage(2);
            }
        }

        public string cloudCoverageString4
        {
            get
            {
                return GetCloudCoverage(3);
            }
        }

        // Variabel oppdatert
        // Dersom navn ikke sette brukes kallende medlem sitt navn
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
