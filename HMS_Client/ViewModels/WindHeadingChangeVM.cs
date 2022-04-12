using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
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
        public RadObservableCollectionEx<HMSData> vesselHdg30mDataList = new RadObservableCollectionEx<HMSData>();
        private double vesselHdgDeltaAbsMax = 0;

        public RadObservableCollectionEx<HMSData> windDir30mDataList = new RadObservableCollectionEx<HMSData>();
        private double windDirDeltaAbsMax = 0;

        // 30 minutters RWD data liste
        private RadObservableCollectionEx<HelideckStatus> rwdTrend30mList = new RadObservableCollectionEx<HelideckStatus>();
        public List<HelideckStatusType> rwdTrend30mDispList = new List<HelideckStatusType>();

        public void Init(Config config, SensorGroupStatus sensorStatus, RelativeWindLimitsVM relativeWindLimitsVM)
        {
            InitUI();

            // Oppdatere UI
            UIUpdateTimer.Interval = TimeSpan.FromMilliseconds(config.ReadWithDefault(ConfigKey.ClientUpdateFrequencyUI, Constants.ClientUIUpdateFrequencyDefault));
            UIUpdateTimer.Tick += UIUpdate;

            void UIUpdate(object sender, EventArgs e)
            {
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
                HelideckStatus newStatus = new HelideckStatus();

                if (relativeWindLimitsVM.rwdGraphData.status == DataStatus.OK)
                    newStatus.status = GetRWDLimitState(relativeWindLimitsVM.rwdGraphData.wind, relativeWindLimitsVM.rwdGraphData.rwd);
                else
                    newStatus.status = HelideckStatusType.OFF;

                newStatus.timestamp = relativeWindLimitsVM.rwdGraphData.timestamp;
                newStatus.wind = relativeWindLimitsVM.rwdGraphData.wind;
                newStatus.rwd = relativeWindLimitsVM.rwdGraphData.rwd;

                // Legge inn ny trend status i data liste
                rwdTrend30mList.Add(newStatus);

                // Fjerne gamle data fra trend data
                GraphBuffer.RemoveOldData(rwdTrend30mList, Constants.Minutes30);

                // Overføre til display data liste
                GraphBuffer.TransferDisplayData(rwdTrend30mList, rwdTrend30mDispList);

                // Oppdatere data som skal ut i grafer
                GraphBuffer.UpdateWithCull(vesselHeadingDelta, vesselHdg30mDataList, Constants.GraphCullFrequency30m);
                GraphBuffer.UpdateWithCull(windDirectionDelta, windDir30mDataList, Constants.GraphCullFrequency30m);

                // Fjerne gamle data fra chart data
                GraphBuffer.RemoveOldData(vesselHdg30mDataList, Constants.Minutes30 + Constants.ChartTimeCorrMin);
                GraphBuffer.RemoveOldData(windDir30mDataList, Constants.Minutes30 + Constants.ChartTimeCorrMin);

                // Finne absolute max verdi
                vesselHdgDeltaAbsMax = FindAbsMax(vesselHdg30mDataList);
                windDirDeltaAbsMax = FindAbsMax(windDir30mDataList);

                // Oppdatere alignment datetime (nåtid) til begge chart og trend line
                alignmentTime = DateTime.UtcNow;

                OnPropertyChanged(nameof(rwdStatusTimeString));
            }
        }

        public void Start()
        {
            // Slette Graph buffer/data
            GraphBuffer.Clear(rwdTrend30mList);
            GraphBuffer.Clear(rwdTrend30mDispList);

            GraphBuffer.Clear(vesselHdg30mDataList);
            GraphBuffer.Clear(windDir30mDataList);

            // Forhåndsfylle trend data med 0 data
            for (double i = -Constants.Minutes30 * 1000; i <= 0; i += Constants.GraphCullFrequency30m)
            {
                rwdTrend30mList.Add(new HelideckStatus()
                {
                    status = HelideckStatusType.NO_DATA,
                    timestamp = DateTime.UtcNow.AddMilliseconds(i)
                });
            }

            for (int i = 0; i < Constants.rwdTrendDisplayListMax; i++)
            {
                rwdTrend30mDispList.Add(new HelideckStatusType());
            }

            // Delta vessel heading/Delta wind direction change exceedance
            DeltaChangeExceedanceAnnotations = new ObservableCollection<object>()
            {
                new MarkedZoneAnnotationModel()
                {
                    HorizontalFrom = DateTime.UtcNow.AddMinutes(-5),
                    HorizontalTo = DateTime.UtcNow.AddMinutes(5),
                    VerticalTo = 30,
                    VerticalFrom = -30,
                    Fill = Brushes.Green,
                }
            };

            // Starte oppdatering av graf data
            UIUpdateTimer.Start();
        }

        public void Stop()
        {
            UIUpdateTimer.Stop();

            // Slette Graph buffer/data
            GraphBuffer.Clear(rwdTrend30mList);
            GraphBuffer.Clear(vesselHdg30mDataList);
            GraphBuffer.Clear(windDir30mDataList);
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

        public void CorrectRWDTrend(double correction)
        {
            // Legge inn rekalkulerte RWD data
            foreach (var item in rwdTrend30mList)
            {
                if (item.status != HelideckStatusType.OFF)
                {
                    item.rwd += correction;
                    item.status = GetRWDLimitState(item.wind, item.rwd);
                }
            }

            // Overføre til display data liste
            GraphBuffer.TransferDisplayData(rwdTrend30mList, rwdTrend30mDispList);
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
        // Wind Calculations: Wind Speed
        /////////////////////////////////////////////////////////////////////////////
        public HMSData _helideckWindSpeed2m { get; set; }
        public HMSData helideckWindSpeed2m
        {
            get
            {
                return _helideckWindSpeed2m;
            }
            set
            {
                if (value != null)
                {
                    _helideckWindSpeed2m.Set(value);
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

                    OnPropertyChanged();
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
                return alignmentTime;
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

        /////////////////////////////////////////////////////////////////////////////
        // Relative Wind Direction Limit State
        /////////////////////////////////////////////////////////////////////////////
        public HelideckStatusType GetRWDLimitState(double wind, double rwdInput)
        {
            double rwd = Math.Abs(rwdInput);

            // NB! Samme kode som i server (GetRWDLimitState)
            if (wind <= 15 || rwd <= 25)
            {
                return HelideckStatusType.BLUE;
            }
            else
            {
                if (rwd > 45)
                {
                    if (wind <= 20)
                        return HelideckStatusType.AMBER;
                    else
                        return HelideckStatusType.RED;
                }
                else
                if (wind > 35)
                {
                    if (rwd <= 30)
                        return HelideckStatusType.AMBER;
                    else
                        return HelideckStatusType.RED;
                }
                else
                {
                    double maxWindRed = 20 + (45 - rwd);

                    if (wind > maxWindRed)
                    {
                        return HelideckStatusType.RED;
                    }
                    else
                    {
                        double maxWindAmber = 15 + (45 - rwd);

                        if (wind > maxWindAmber)
                            return HelideckStatusType.AMBER;
                        else
                            return HelideckStatusType.BLUE;
                    }
                }
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Delta vessel heading/Delta wind direction change exceedance
        /////////////////////////////////////////////////////////////////////////////
        public ObservableCollection<object> DeltaChangeExceedanceAnnotations { get; set; }


        // Variabel oppdatert
        // Dersom navn ikke er satt brukes kallende medlem sitt navn
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    public class MarkedZoneAnnotationModel
    {
        public DateTime HorizontalFrom { get; set; }
        public DateTime HorizontalTo { get; set; }
        public double VerticalFrom { get; set; }
        public double VerticalTo { get; set; }
        public Brush Fill { get; set; }
    }
}
