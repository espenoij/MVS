using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Threading;

namespace HMS_Client
{
    public class WindHeadingChangeVM : INotifyPropertyChanged
    {
        // Change notification
        public event PropertyChangedEventHandler PropertyChanged;

        private DispatcherTimer UIUpdateTimer = new DispatcherTimer();

        // Graph buffer/data
        public RadObservableCollectionEx<HMSData> vesselHdg20mDataList = new RadObservableCollectionEx<HMSData>();
        private double vesselHdgDeltaAbsMax = 0;

        public RadObservableCollectionEx<HMSData> windDir20mDataList = new RadObservableCollectionEx<HMSData>();
        private double windDirDeltaAbsMax = 0;

        // 30 minutters RWD data liste
        private RadObservableCollectionEx<HelideckStatus> rwdTrend30mList = new RadObservableCollectionEx<HelideckStatus>();
        public List<HelideckStatusType> rwdTrend30mDispList = new List<HelideckStatusType>();

        public void Init(Config config, SensorGroupStatus sensorStatus, HelideckStatusVM helideckStatusVM)
        {
            InitUI();

            // Oppdatere UI
            UIUpdateTimer.Interval = TimeSpan.FromMilliseconds(config.ReadWithDefault(ConfigKey.ClientUpdateFrequencyUI, Constants.ClientUpdateFrequencyUIDefault));
            UIUpdateTimer.Tick += UIUpdate;

            void UIUpdate(object sender, EventArgs e)
            {
                // Disse korreksjonene legges inn for å få tidspunkt-label på X aksen til å vises korrekt
                int chartTimeCorrMin = 4;
                //int chartTimeCorrMax = -2;

                // Sjekke om vi har data timeout
                if (sensorStatus.TimeoutCheck(vesselHeadingDelta))
                {
                    OnPropertyChanged(nameof(vesselHeadingDeltaString));
                    OnPropertyChanged(nameof(vesselHeadingAxisMax));
                    OnPropertyChanged(nameof(vesselHeadingAxisMin));
                    OnPropertyChanged(nameof(vesselHeadingWarning));
                    OnPropertyChanged(nameof(windDirectionWarning));
                }
                
                if (sensorStatus.TimeoutCheck(windDirectionDelta))
                {
                    OnPropertyChanged(nameof(deltaWindDirectionString));
                    OnPropertyChanged(nameof(windDirectionAxisMax));
                    OnPropertyChanged(nameof(windDirectionAxisMin));
                }

                // Oppdatering av RWD status
                HelideckStatus newStatus = new HelideckStatus()
                {
                    status = helideckStatusVM.rwdStatus,
                    timestamp = DateTime.UtcNow
                };

                // Legge inn ny trend status i data liste
                rwdTrend30mList.Add(newStatus);

                // Fjerne gamle data fra trend data
                GraphBuffer.RemoveOldData(rwdTrend30mList, Constants.Minutes30);

                // Overføre til display data liste
                GraphBuffer.TransferDisplayData(rwdTrend30mList, rwdTrend30mDispList);

                // Oppdatere data som skal ut i grafer
                GraphBuffer.UpdateWithCull(vesselHeadingDelta, vesselHdg20mDataList, Constants.GraphCullFrequency30m);
                GraphBuffer.UpdateWithCull(windDirectionDelta, windDir20mDataList, Constants.GraphCullFrequency30m);

                // Fjerne gamle data fra chart data
                GraphBuffer.RemoveOldData(vesselHdg20mDataList, Constants.Minutes30 + chartTimeCorrMin);
                GraphBuffer.RemoveOldData(windDir20mDataList, Constants.Minutes30 + chartTimeCorrMin);

                // Finne absolute max verdi
                vesselHdgDeltaAbsMax = FindAbsMax(vesselHdg20mDataList);
                windDirDeltaAbsMax = FindAbsMax(windDir20mDataList);

                // Oppdatere alignment datetime (nåtid) til begge chart og trend line
                alignmentTime = DateTime.UtcNow;

                OnPropertyChanged(nameof(rwdStatusTimeString));
            }
        }

        public void Start()
        {
            // Slette Graph buffer/data
            GraphBuffer.Clear(rwdTrend30mList);
            rwdTrend30mDispList.Clear();

            GraphBuffer.Clear(vesselHdg20mDataList);
            GraphBuffer.Clear(windDir20mDataList);

            // Forhåndsfylle trend data med 0 data
            for (int i = -Constants.Minutes30; i <= 0; i++)
            {
                rwdTrend30mList.Add(new HelideckStatus()
                {
                    status = HelideckStatusType.OFF,
                    timestamp = DateTime.UtcNow.AddSeconds(i)
                });
            }

            for (int i = 0; i < Constants.rwdTrendDisplayListMax; i++)
            {
                rwdTrend30mDispList.Add(new HelideckStatusType());
            }

            UIUpdateTimer.Start();
        }

        public void Stop()
        {
            UIUpdateTimer.Stop();

            // Slette Graph buffer/data
            GraphBuffer.Clear(rwdTrend30mList);
            GraphBuffer.Clear(vesselHdg20mDataList);
            GraphBuffer.Clear(windDir20mDataList);
        }

        private void InitUI()
        {
            _vesselHeadingDelta = new HMSData();
            _windDirectionDelta = new HMSData();
        }

        public void UpdateData(HMSDataCollection clientSensorList)
        {
            // Heading Data
            vesselHeadingDelta = clientSensorList.GetData(ValueType.VesselHeadingDelta);
            windDirectionDelta = clientSensorList.GetData(ValueType.WindDirectionDelta);
        }

        private double FindAbsMax(RadObservableCollectionEx<HMSData> dataList)
        {
            double max = 0;

            foreach (var item in dataList)
                if (Math.Abs(item.data) > max)
                    max = Math.Abs(item.data);

            return max;
        }

        /////////////////////////////////////////////////////////////////////////////
        // Wind Calculations: Wind Speed
        /////////////////////////////////////////////////////////////////////////////
        public HMSData _vesselHeadingDelta { get; set; }
        public HMSData vesselHeadingDelta
        {
            get
            {
                return _vesselHeadingDelta;
            }
            set
            {
                if (value != null)
                {
                    OnPropertyChanged(nameof(vesselHeadingDeltaString));
                    OnPropertyChanged(nameof(vesselHeadingAxisMax));
                    OnPropertyChanged(nameof(vesselHeadingAxisMin));
                    OnPropertyChanged(nameof(vesselHeadingWarning));
                    OnPropertyChanged(nameof(windDirectionWarning));

                    _vesselHeadingDelta.Set(value);
                }
            }
        }

        public string vesselHeadingDeltaString
        {
            get
            {
                if (vesselHeadingDelta != null)
                {
                    // Sjekke om data er gyldig
                    if (vesselHeadingDelta.status == DataStatus.OK)
                    {
                        if (vesselHeadingDelta.data >= 0)
                            return string.Format("{0}° R", vesselHeadingDelta.data.ToString(" 0"));
                        else
                            return string.Format("{0}° L", Math.Abs(vesselHeadingDelta.data).ToString(" 0"));
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
        // Relative Wind Direction
        /////////////////////////////////////////////////////////////////////////////
        public HMSData _windDirectionDelta { get; set; }
        public HMSData windDirectionDelta
        {
            get
            {
                return _windDirectionDelta;
            }
            set
            {
                if (value != null)
                {
                    OnPropertyChanged(nameof(deltaWindDirectionString));
                    OnPropertyChanged(nameof(windDirectionAxisMax));
                    OnPropertyChanged(nameof(windDirectionAxisMin));

                    _windDirectionDelta.Set(value);
                }
            }
        }

        public string deltaWindDirectionString
        {
            get
            {
                if (_windDirectionDelta != null)
                {
                    // Sjekke om data er gyldig
                    if (_windDirectionDelta.status == DataStatus.OK)
                    {
                        if (windDirectionDelta.data >= 0)
                            return string.Format("{0}° R", windDirectionDelta.data.ToString("0"));
                        else
                            return string.Format("{0}° L", Math.Abs(windDirectionDelta.data).ToString("0"));
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
        // Chart Axis Min/Max
        /////////////////////////////////////////////////////////////////////////////
        public double vesselHeadingAxisMax
        {
            get
            {
                return 10 + ((int)vesselHdgDeltaAbsMax / 10) * 10;
            }
        }

        public double vesselHeadingAxisMin
        {
            get
            {
                return -10 - ((int)vesselHdgDeltaAbsMax / 10) * 10;
            }
        }

        public double windDirectionAxisMax
        {
            get
            {
                return 10 + ((int)windDirDeltaAbsMax / 10) * 10;
            }
        }

        public double windDirectionAxisMin
        {
            get
            {
                return -10 - ((int)windDirDeltaAbsMax / 10) * 10;
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

        /////////////////////////////////////////////////////////////////////////////
        // Chart X axis alignment
        /////////////////////////////////////////////////////////////////////////////
        private DateTime _alignmentTime;
        public DateTime alignmentTime
        {
            get
            {
                return _alignmentTime;
            }
            set
            {
                if (_alignmentTime != value)
                {
                    _alignmentTime = value;

                    OnPropertyChanged(nameof(alignmentTimeMin));
                    OnPropertyChanged(nameof(alignmentTimeMax));
                }
            }
        }

        public DateTime alignmentTimeMin
        {
            get
            {
                return DateTime.UtcNow.AddSeconds(-Constants.Minutes30);
            }
        }

        public DateTime alignmentTimeMax
        {
            get
            {
                return DateTime.UtcNow;
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Helideck Status Trend Time
        /////////////////////////////////////////////////////////////////////////////
        public string rwdStatusTimeString
        {
            get
            {
                if (rwdTrend30mList.Count > 0)
                {
                    return string.Format("30-minute RWD Trend ({0} - {1} UTC)",
                        rwdTrend30mList[0].timestamp.ToShortTimeString(),
                        rwdTrend30mList[rwdTrend30mList.Count - 1].timestamp.ToShortTimeString());
                }
                else
                {
                    return string.Format("30-minute RWD Trend (--:-- - --:-- UTC)");
                }
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Vessel Heading background warning
        /////////////////////////////////////////////////////////////////////////////
        public bool vesselHeadingWarning
        {
            get
            {
                if (Math.Abs(vesselHeadingDelta.data) > 10)
                    return true;
                else
                    return false;
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Wind Direction background warning
        /////////////////////////////////////////////////////////////////////////////
        public int windDirectionWarning
        {
            get
            {
                if (Math.Abs(_windDirectionDelta.data) > 30)
                    return 1;
                else
                    return 0;
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
