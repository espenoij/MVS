using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Threading;

namespace SensorMonitorClient
{
    public class HelideckRelativeWindLimitsVM : INotifyPropertyChanged
    {
        // Change notification
        public event PropertyChangedEventHandler PropertyChanged;

        private DispatcherTimer UIUpdateTimer = new DispatcherTimer();
        private DispatcherTimer ChartDataUpdateTimer20m = new DispatcherTimer();

        // Relative Wind graph buffer/data
        private RadObservableCollectionEx<HMSData> relativeWindDir20mBuffer = new RadObservableCollectionEx<HMSData>();
        public RadObservableCollectionEx<HMSData> relativeWindDir20mDataList = new RadObservableCollectionEx<HMSData>();

        public void Init(Config config, SensorStatus sensorStatus)
        {
            InitUI();

            // Oppdatere UI
            UIUpdateTimer.Interval = TimeSpan.FromMilliseconds(config.Read(ConfigKey.ClientUpdateFrequencyUI, Constants.ClientUpdateFrequencyUIDefault));
            UIUpdateTimer.Tick += UIUpdate;
            UIUpdateTimer.Start();

            void UIUpdate(object sender, EventArgs e)
            {
                // Sjekke om vi har data timeout
                sensorStatus.TimeoutCheck(windSpd);
                sensorStatus.TimeoutCheck(relativeWindDir);

                // Oppdatere data som skal ut i grafer
                if (rwdGraphData.dataStatus == DataStatus.OK)
                    UpdateChartBuffer(rwdGraphData, relativeWindDir20mBuffer);
            }

            // Oppdatere trend data i UI: 20 minutter
            ChartDataUpdateTimer20m.Interval = TimeSpan.FromMilliseconds(config.Read(ConfigKey.ChartDataUpdateFrequency20m, Constants.ChartUpdateFrequencyUI20mDefault));
            ChartDataUpdateTimer20m.Tick += ChartDataUpdate20m;
            ChartDataUpdateTimer20m.Start();

            void ChartDataUpdate20m(object sender, EventArgs e)
            {
                // Overføre data fra buffer til chart data: 20m
                TransferBuffer(relativeWindDir20mBuffer, relativeWindDir20mDataList);

                // Slette buffer
                ClearBuffer(relativeWindDir20mBuffer);

                // Fjerne gamle data fra chart data
                RemoveOldData(relativeWindDir20mDataList, Constants.Minutes20);
            }
        }

        private void InitUI()
        {
            _windSpd = new HMSData();
            _relativeWindDir = new HMSData();
            _rwdGraphData = new HMSData();
        }

        public void UpdateData(HMSDataCollection clientSensorList)
        {
            // Wind Data
            windSpd = clientSensorList.GetData(ValueType.HelideckWindSpeed2m);
            relativeWindDir = clientSensorList.GetData(ValueType.RelativeWindDir);

            if (windSpd.dataStatus == DataStatus.OK &&
                relativeWindDir.dataStatus == DataStatus.OK)
            {
                // Begrenser RWD i grafen til 60 grader
                double rwd = Math.Abs(relativeWindDir.data);
                if (rwd < 0)
                    rwd = 0;
                else
                if (rwd > 60)
                    rwd = 60;

                // Begrense vind i grafen til 60 kts
                double wind = windSpd.data;
                if (wind < 0)
                    wind = 0;
                else
                if (wind > 60)
                    wind = 60;

                rwdGraphData.data = rwd;
                rwdGraphData.data2 = wind;
                rwdGraphData.dataStatus = DataStatus.OK;
                rwdGraphData.timestamp = windSpd.timestamp;

                OnPropertyChanged(nameof(rwdGraphDataX));
                OnPropertyChanged(nameof(rwdGraphDataY));
            }
            else
            {
                rwdGraphData.data = 0;
                rwdGraphData.data2 = 0;
                rwdGraphData.dataStatus = DataStatus.TIMEOUT_ERROR;
                rwdGraphData.timestamp = windSpd.timestamp;

                OnPropertyChanged(nameof(rwdGraphDataX));
                OnPropertyChanged(nameof(rwdGraphDataY));
            }
        }

        public void UpdateChartBuffer(HMSData data, RadObservableCollectionEx<HMSData> buffer)
        {
            // NB! Når vi har data tilgjengelig fores dette inn i grafene.
            // Når vi ikke har data tilgjengelig legges 0 data inn i grafene for å holde de gående.

            // Grunne til at vi buffrer data først er pga ytelsesproblemer dersom vi kjører data rett ut i grafene på skjerm.
            // Det takler ikke grafene fra Telerik. Buffrer data først og så oppdaterer vi grafene med jevne passende mellomrom.

            if (data?.dataStatus == DataStatus.OK)
            {
                // Lagre data i buffer
                buffer?.Add(new HMSData(data));
            }
            else
            {
                // Lagre 0 data
                buffer?.Add(new HMSData() { data = 0, timestamp = DateTime.UtcNow });
            }
        }

        public void TransferBuffer(RadObservableCollectionEx<HMSData> buffer, RadObservableCollectionEx<HMSData> dataList)
        {
            // Overfører alle data fra buffer til dataList
            dataList.AddRange(buffer);
        }

        public void ClearBuffer(RadObservableCollectionEx<HMSData> buffer)
        {
            // Sletter alle data fra buffer
            buffer.Clear();
        }

        public void RemoveOldData(RadObservableCollectionEx<HMSData> dataList, double timeInterval)
        {
            for (int i = 0; i < dataList.Count && i >= 0; i++)
            {
                if (dataList[i]?.timestamp < DateTime.UtcNow.AddSeconds(-timeInterval))
                    dataList.RemoveAt(i--);
                else
                    break;
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Wind Calculations: Wind Speed
        /////////////////////////////////////////////////////////////////////////////
        private HMSData _windSpd { get; set; }
        public HMSData windSpd
        {
            get
            {
                return _windSpd;
            }
            set
            {
                if (value != null)
                {
                    if (value.data != _windSpd.data ||
                        value.timestamp != _windSpd.timestamp)
                    {
                        _windSpd.Set(value);
                    }
                }
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Relative Wind Direction
        /////////////////////////////////////////////////////////////////////////////
        private HMSData _relativeWindDir { get; set; }
        public HMSData relativeWindDir
        {
            get
            {
                return _relativeWindDir;
            }
            set
            {
                if (value != null)
                {
                    if (_relativeWindDir.data != value.data)
                    {
                        OnPropertyChanged(nameof(relativeWindDirString));
                    }

                    _relativeWindDir.Set(value);
                }
            }
        }

        public string relativeWindDirString
        {
            get
            {
                if (_relativeWindDir != null)
                {
                    // Sjekke om data er gyldig
                    if (_relativeWindDir.dataStatus == DataStatus.OK)
                    {
                        if (relativeWindDir.data >= 0)
                            return string.Format("{0}° R", relativeWindDir.data.ToString("0"));
                        else
                            return string.Format("{0}° L", Math.Abs(relativeWindDir.data).ToString("0"));
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
        // Relative Wind Direction Graf data
        /////////////////////////////////////////////////////////////////////////////
        private HMSData _rwdGraphData { get; set; }
        public HMSData rwdGraphData
        {
            get
            {
                return _rwdGraphData;
            }
            set
            {
                if (value != null)
                {
                    if (_rwdGraphData.data != value.data)
                    {
                        OnPropertyChanged(nameof(rwdGraphDataX));
                        OnPropertyChanged(nameof(rwdGraphDataY));
                    }

                    _rwdGraphData.Set(value);
                }
            }
        }

        public double rwdGraphDataX
        {
            get
            {
                if (_rwdGraphData?.dataStatus == DataStatus.OK)
                    return _rwdGraphData.data2;
                else
                    return 0;
            }
        }

        public double rwdGraphDataY
        {
            get
            {
                if (_rwdGraphData?.dataStatus == DataStatus.OK)
                    return _rwdGraphData.data;
                else
                    return 0;
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Display Mode
        /////////////////////////////////////////////////////////////////////////////
        private DisplayMode _displayMode { get; set; }
        public DisplayMode displayMode
        {
            get
            {
                return _displayMode;
            }
            set
            {
                _displayMode = value;

                OnPropertyChanged(nameof(displayModeVisibilityOnDeck));
            }
        }

        public bool displayModeVisibilityOnDeck
        {
            get
            {
                if (_displayMode == DisplayMode.OnDeck)
                    return true;
                else
                    return false;
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
