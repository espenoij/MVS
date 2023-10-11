using SharpDX.Direct2D1;
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
            _cloudBase = new List<HMSData>();
            _cloudCoverage = new List<HMSData>();
            for (int i = 0; i < Constants.TOTAL_CLOUD_LAYERS; i++)
            {
                _cloudBase.Add(new HMSData());
                _cloudCoverage.Add(new HMSData());
            }
        }

        public void Init(Config config, SensorGroupStatus sensorStatus)
        {
            // Oppdatere UI
            UIUpdateTimer.Interval = TimeSpan.FromMilliseconds(config.ReadWithDefault(ConfigKey.ClientUpdateFrequencyUI, Constants.ClientUIUpdateFrequencyDefault));
            UIUpdateTimer.Tick += UIUpdate;
            UIUpdateTimer.Start();

            void UIUpdate(object sender, EventArgs e)
            {
                // Sjekke om vi har data timeout
                if (sensorStatus.TimeoutCheck(seaTemperature))
                {
                    OnPropertyChanged(nameof(seaTemperatureData));
                    OnPropertyChanged(nameof(seaTemperatureString));
                }

                if (sensorStatus.TimeoutCheck(airTemperature))
                {
                    OnPropertyChanged(nameof(airTemperatureData));
                    OnPropertyChanged(nameof(airTemperatureString));
                }
                if (sensorStatus.TimeoutCheck(airHumidity))
                {
                    OnPropertyChanged(nameof(airHumidityData));
                    OnPropertyChanged(nameof(airHumidityString));
                }
                if (sensorStatus.TimeoutCheck(airDewPoint)) OnPropertyChanged(nameof(airDewPointString));
                if (sensorStatus.TimeoutCheck(airPressureQNH))
                {
                    OnPropertyChanged(nameof(airPressureQNHData));
                    OnPropertyChanged(nameof(airPressureQNHString));
                }
                if (sensorStatus.TimeoutCheck(airPressureQFE)) OnPropertyChanged(nameof(airPressureQFEString));
                if (sensorStatus.TimeoutCheck(visibility)) OnPropertyChanged(nameof(visibilityString));
                if (sensorStatus.TimeoutCheck(weather))
                {
                    OnPropertyChanged(nameof(weatherPhenomenaString));
                }

                if (sensorStatus.TimeoutCheck(_cloudBase[0])) OnPropertyChanged(nameof(cloudBaseString1));
                if (sensorStatus.TimeoutCheck(_cloudBase[1])) OnPropertyChanged(nameof(cloudBaseString2));
                if (sensorStatus.TimeoutCheck(_cloudBase[2])) OnPropertyChanged(nameof(cloudBaseString3));
                if (sensorStatus.TimeoutCheck(_cloudBase[3])) OnPropertyChanged(nameof(cloudBaseString4));

                if (sensorStatus.TimeoutCheck(_cloudCoverage[0])) OnPropertyChanged(nameof(cloudCoverageString1));
                if (sensorStatus.TimeoutCheck(_cloudCoverage[1])) OnPropertyChanged(nameof(cloudCoverageString2));
                if (sensorStatus.TimeoutCheck(_cloudCoverage[2])) OnPropertyChanged(nameof(cloudCoverageString3));
                if (sensorStatus.TimeoutCheck(_cloudCoverage[3])) OnPropertyChanged(nameof(cloudCoverageString4));
            }
        }

        public void UpdateData(HMSDataCollection clientSensorList)
        {
            seaTemperature = clientSensorList.GetData(ValueType.SeaTemperature);

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
        // Meteorological: Sea Temperature
        /////////////////////////////////////////////////////////////////////////////
        private HMSData _seaTemperature { get; set; } = new HMSData();
        public HMSData seaTemperature
        {
            get
            {
                return _seaTemperature;
            }
            set
            {
                if (value != null)
                {
                    if (value.data != _seaTemperature.data ||
                        value.timestamp != _seaTemperature.timestamp)
                    {
                        _seaTemperature.Set(value);

                        OnPropertyChanged(nameof(seaTemperatureData));
                        OnPropertyChanged(nameof(seaTemperatureString));
                    }
                }
            }
        }
        public double seaTemperatureData
        {
            get
            {
                if (_seaTemperature != null)
                {
                    // Sjekke om data er gyldig
                    if (_seaTemperature.status == DataStatus.OK)
                    {
                        return _seaTemperature.data;
                    }
                    else
                    {
                        return 0;
                    }
                }
                else
                {
                    return 0;
                }
            }
        }
        public string seaTemperatureString
        {
            get
            {
                if (_seaTemperature != null)
                {
                    // Sjekke om data er gyldig
                    if (_seaTemperature.status == DataStatus.OK)
                    {
                        return string.Format("{0} °C", _seaTemperature.data.ToString("0.0"));
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

                        OnPropertyChanged(nameof(airTemperatureData));
                        OnPropertyChanged(nameof(airTemperatureString));
                    }
                }
            }
        }
        public double airTemperatureData
        {
            get
            {
                if (_airTemperature != null)
                {
                    // Sjekke om data er gyldig
                    if (_airTemperature.status == DataStatus.OK)
                    {
                        return _airTemperature.data;
                    }
                    else
                    {
                        return 0;
                    }
                }
                else
                {
                    return 0;
                }
            }
        }
        public string airTemperatureString
        {
            get
            {
                if (_airTemperature != null)
                {
                    // Sjekke om data er gyldig
                    if (_airTemperature.status == DataStatus.OK)
                    {
                        return string.Format("{0} °C", _airTemperature.data.ToString("0.0"));
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

                        OnPropertyChanged(nameof(airHumidityData));
                        OnPropertyChanged(nameof(airHumidityString));
                    }
                }
            }
        }

        public double airHumidityData
        {
            get
            {
                if (_airHumidity != null)
                {
                    // Sjekke om data er gyldig
                    if (_airHumidity.status == DataStatus.OK)
                    {
                        return _airHumidity.data;
                    }
                    else
                    {
                        return 0;
                    }
                }
                else
                {
                    return 0;
                }
            }
        }

        public string airHumidityString
        {
            get
            {
                if (_airHumidity != null)
                {
                    // Sjekke om data er gyldig
                    if (_airHumidity.status == DataStatus.OK)
                    {
                        return string.Format("{0} %", _airHumidity.data.ToString("0.0"));
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
                if (_airDewPoint != null)
                {
                    // Sjekke om data er gyldig
                    if (_airDewPoint.status == DataStatus.OK)
                    {
                        return string.Format("{0} °C", _airDewPoint.data.ToString("0.0"));
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

                        OnPropertyChanged(nameof(airPressureQNHData));
                        OnPropertyChanged(nameof(airPressureQNHString));
                    }
                }
            }
        }

        public double airPressureQNHData
        {
            get
            {
                if (_airPressureQNH != null)
                {
                    // Sjekke om data er gyldig
                    if (_airPressureQNH.status == DataStatus.OK)
                    {
                        return _airPressureQNH.data;
                    }
                    else
                    {
                        return 0;
                    }
                }
                else
                {
                    return 0;
                }
            }
        }

        public string airPressureQNHString
        {
            get
            {
                if (_airPressureQNH != null)
                {
                    // Sjekke om data er gyldig
                    if (_airPressureQNH.status == DataStatus.OK)
                    {
                        return string.Format("{0} hPa", _airPressureQNH.data.ToString("0.0"));
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
                if (_airPressureQFE != null)
                {
                    // Sjekke om data er gyldig
                    if (_airPressureQFE.status == DataStatus.OK)
                    {
                        return string.Format("{0} hPa", _airPressureQFE.data.ToString("0.0"));
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
                if (_visibility != null)
                {
                    // Sjekke om data er gyldig
                    if (_visibility.status == DataStatus.OK)
                    {
                        double vis = _visibility.data;
                        if (vis > 9999)
                            vis = 9999;

                        return string.Format("{0} m", vis.ToString());
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
                if (_weather != null)
                {
                    // Sjekke om data er gyldig
                    if (_weather.status == DataStatus.OK)
                    {
                        string weatherPhenomenaString = string.Empty;

                        WeatherSeverity severity = Weather.DecodeSeverity((int)_weather.data);
                        WeatherPhenomena weather1 = Weather.DecodePhenomena1((int)_weather.data);
                        WeatherPhenomena weather2 = Weather.DecodePhenomena2((int)_weather.data);

                        // Severity
                        if (severity != WeatherSeverity.None)
                        {
                            weatherPhenomenaString = severity.GetDescription();
                        }

                        // Phenomena 1
                        if (weather1 != WeatherPhenomena.None)
                        {
                            weatherPhenomenaString += " " + weather1.GetDescription();
                        }

                        // Kombo: Drizzle or snow grains
                        if (weather1 == WeatherPhenomena.DZ &&
                            weather2 == WeatherPhenomena.SG)
                        {
                            weatherPhenomenaString += " or";
                        }
                        // Kombo: haze or smoke/dust
                        else
                        if (weather1 == WeatherPhenomena.HZ &&
                            (weather2 == WeatherPhenomena.FU ||
                             weather2 == WeatherPhenomena.DU))
                        {
                            weatherPhenomenaString += " or";
                        }
                        // Kombo: drizzle and rain
                        else
                        if (weather1 == WeatherPhenomena.DZ &&
                            weather2 == WeatherPhenomena.RA)
                        {
                            weatherPhenomenaString += " and";
                        }
                        // Kombo: rain and snow
                        else
                        if (weather1 == WeatherPhenomena.RA &&
                            weather2 == WeatherPhenomena.SN)
                        {
                            weatherPhenomenaString += " and";
                        }

                        // Phenomena 2
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
        private List<HMSData> _cloudBase { get; set; }
        private List<HMSData> _cloudCoverage { get; set; }

        private void SetCloudData(int layer, HMSData cBase, HMSData cCoverage)
        {
            if (layer < Constants.TOTAL_CLOUD_LAYERS && layer >= 0)
            {
                _cloudBase[layer]?.Set(cBase);
                _cloudCoverage[layer]?.Set(cCoverage);
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
                if (_cloudBase[layer]?.status == DataStatus.OK &&
                    !double.IsNaN(_cloudBase[layer].data) &&
                    _cloudBase[layer].data != 0)
                    return string.Format("{0} ft", _cloudBase[layer]?.data.ToString("0"));
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
                if (_cloudCoverage[layer]?.status == DataStatus.OK &&    // Coverage status ok
                    _cloudBase[layer]?.status == DataStatus.OK &&        // Base status ok
                    !double.IsNaN(_cloudBase[layer].data))               // Base er satt
                {
                    switch (_cloudCoverage[layer].data)
                    {
                        case 0:
                            if (layer == 0)
                                return "SKC";
                            else
                                return Constants.NotAvailable;
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
        protected virtual void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
