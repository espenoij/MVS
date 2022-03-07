using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Threading;

namespace HMS_Client
{
    public class RelativeWindLimitsVM : INotifyPropertyChanged
    {
        // Change notification
        public event PropertyChangedEventHandler PropertyChanged;

        private DispatcherTimer UIUpdateTimer = new DispatcherTimer();

        // 20 minutters data liste
        public RadObservableCollectionEx<HelideckStatus> statusTrend20mList = new RadObservableCollectionEx<HelideckStatus>();
        public List<HelideckStatusType> landingTrend20mDispList = new List<HelideckStatusType>();

        // 3 timers data liste
        public RadObservableCollectionEx<HelideckStatus> statusTrend3hList = new RadObservableCollectionEx<HelideckStatus>();
        public List<HelideckStatusType> statusTrend3hDispList = new List<HelideckStatusType>();

        // Relative Wind graph buffer/data
        public RadObservableCollectionEx<HMSData> relativeWindDir30mDataList = new RadObservableCollectionEx<HMSData>();

        // Lagret helicopter heading
        private double savedHelicopterHeading = -1;

        private UserInputsVM userInputsVM;
        private OnDeckStabilityLimitsVM onDeckStabilityLimitsVM;

        public void Init(Config config, SensorGroupStatus sensorStatus, UserInputsVM userInputsVM, OnDeckStabilityLimitsVM onDeckStabilityLimitsVM, HelideckStatusVM helideckStatusVM)
        {
            this.userInputsVM = userInputsVM;
            this.onDeckStabilityLimitsVM = onDeckStabilityLimitsVM;

            InitUI(config);

            // Oppdatere UI
            UIUpdateTimer.Interval = TimeSpan.FromMilliseconds(config.ReadWithDefault(ConfigKey.ClientUpdateFrequencyUI, Constants.ClientUIUpdateFrequencyDefault));
            UIUpdateTimer.Tick += UIUpdate;

            void UIUpdate(object sender, EventArgs e)
            {
                // Landing Status Trend
                HelideckStatus newStatus = new HelideckStatus()
                {
                    status = helideckStatusVM.landingStatus,
                    timestamp = DateTime.UtcNow
                };

                // Legge inn ny status i buffer
                statusTrend20mList.Add(newStatus);
                statusTrend3hList.Add(newStatus);

                // Fjerne gamle data fra chart data
                GraphBuffer.RemoveOldData(statusTrend20mList, Constants.Minutes20);
                GraphBuffer.RemoveOldData(statusTrend3hList, Constants.Hours3);

                // Overføre til display data liste
                GraphBuffer.TransferDisplayData(statusTrend20mList, landingTrend20mDispList);
                GraphBuffer.TransferDisplayData(statusTrend3hList, statusTrend3hDispList);

                OnPropertyChanged(nameof(landingStatusTimeString));
                OnPropertyChanged(nameof(displayModeVisibilityPreLanding));

                // Sjekke om vi har data timeout
                sensorStatus.TimeoutCheck(windSpd);
                if (sensorStatus.TimeoutCheck(relativeWindDir)) OnPropertyChanged(nameof(relativeWindDirString));

                // Oppdatere data som skal ut i grafer
                if (rwdGraphData.status == DataStatus.OK)
                    GraphBuffer.UpdateWithCull(rwdGraphData, relativeWindDir30mDataList, Constants.GraphCullFrequency30m);

                // Fjerne gamle data fra chart data
                GraphBuffer.RemoveOldData(relativeWindDir30mDataList, Constants.Minutes30);
            }
        }

        private void InitUI(Config config)
        {
            _windSpd = new HMSData();
            _relativeWindDir = new HMSData();
            _rwdGraphData = new HMSData();

            // Landing Status Trend
            double uiUpdateFreq = config.ReadWithDefault(ConfigKey.ClientUpdateFrequencyUI, Constants.ClientUIUpdateFrequencyDefault);

            // Init av chart data
            for (int i = (int)(-Constants.Minutes20 * (1000 / uiUpdateFreq)); i <= 0; i++)
            {
                statusTrend20mList.Add(new HelideckStatus()
                {
                    status = HelideckStatusType.OFF,
                    timestamp = DateTime.UtcNow.AddSeconds(i)
                });
            }

            for (int i = (int)(-Constants.Hours3 * (1000 / uiUpdateFreq)); i <= 0; i++)
            {
                statusTrend3hList.Add(new HelideckStatus()
                {
                    status = HelideckStatusType.OFF,
                    timestamp = DateTime.UtcNow.AddSeconds(i)
                });
            }

            for (int i = 0; i < Constants.landingTrendDisplayListMax; i++)
            {
                landingTrend20mDispList.Add(new HelideckStatusType());
                statusTrend3hDispList.Add(new HelideckStatusType());
            }
        }

        public void Start()
        {
            UIUpdateTimer.Start();
        }

        public void Stop()
        {
            UIUpdateTimer.Stop();

            // Slette Graph buffer/data
            GraphBuffer.Clear(relativeWindDir30mDataList);
        }

        public void UpdateData(HMSDataCollection clientSensorList)
        {
            // Wind Data
            windSpd = clientSensorList.GetData(ValueType.HelideckWindSpeed2m);
            relativeWindDir = clientSensorList.GetData(ValueType.RelativeWindDir);

            if (windSpd.status == DataStatus.OK &&
                relativeWindDir.status == DataStatus.OK)
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
                rwdGraphData.status = DataStatus.OK;
                rwdGraphData.timestamp = windSpd.timestamp;

                OnPropertyChanged(nameof(rwdGraphDataX));
                OnPropertyChanged(nameof(rwdGraphDataY));
            }
            else
            {
                rwdGraphData.data = 0;
                rwdGraphData.data2 = 0;
                rwdGraphData.status = DataStatus.TIMEOUT_ERROR;
                rwdGraphData.timestamp = windSpd.timestamp;

                OnPropertyChanged(nameof(rwdGraphDataX));
                OnPropertyChanged(nameof(rwdGraphDataY));
            }

            // On-deck display?
            if (userInputsVM.displayMode == DisplayMode.OnDeck)
            {
                // Er on-deck helicopter heading korrigert?
                if (userInputsVM.onDeckHelicopterHeading != savedHelicopterHeading)
                {
                    if (savedHelicopterHeading != -1)
                        CorrectRWDGraph(savedHelicopterHeading - userInputsVM.onDeckHelicopterHeading);

                    savedHelicopterHeading = userInputsVM.onDeckHelicopterHeading;
                }
            }
        }

        private void CorrectRWDGraph(double correction)
        {
            // Løper gjennom hele listen og legger til korreksjon på RWD komponenten
            foreach (var item in relativeWindDir30mDataList)
            {
                item.data += correction;
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Helideck Status
        /////////////////////////////////////////////////////////////////////////////
        private HelideckStatusType _helideckStatus { get; set; }
        public HelideckStatusType helideckStatus
        {
            set
            {
                if (_helideckStatus != value)
                {
                    _helideckStatus = value;
                }
            }
            get
            {
                return _helideckStatus;
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Helideck Status Trend Time
        /////////////////////////////////////////////////////////////////////////////
        public string landingStatusTimeString
        {
            get
            {
                RadObservableCollectionEx<HelideckStatus> statusList;
                string timeString;

                if (_selectedGraphTime == GraphTime.Minutes20)
                {
                    statusList = statusTrend20mList;
                    timeString = "20-minute";
                }
                else
                {
                    statusList = statusTrend3hList;
                    timeString = "3-hour";
                }

                if (statusList.Count > 0)
                {
                    return string.Format("{0} Trend ({1} - {2} UTC)",
                        timeString,
                        statusList[0].timestamp.ToShortTimeString(),
                        statusList[statusList.Count - 1].timestamp.ToShortTimeString());
                }
                else
                {
                    return string.Format("{0} Trend (--:-- - --:-- UTC)", timeString);
                }
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Selected Graph Time
        /////////////////////////////////////////////////////////////////////////////
        private GraphTime _selectedGraphTime { get; set; }
        public GraphTime selectedGraphTime
        {
            get
            {
                return _selectedGraphTime;
            }
            set
            {
                _selectedGraphTime = value;

                OnPropertyChanged(nameof(visibilityItems20m));
                OnPropertyChanged(nameof(visibilityItems3h));
                OnPropertyChanged(nameof(landingStatusTimeString));
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Visibility 20m / 3h items
        /////////////////////////////////////////////////////////////////////////////
        public bool visibilityItems20m
        {
            get
            {
                if (selectedGraphTime == GraphTime.Minutes20)
                    return true;
                else
                    return false;
            }
        }

        public bool visibilityItems3h
        {
            get
            {
                if (selectedGraphTime == GraphTime.Hours3)
                    return true;
                else
                    return false;
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Pre-Landing Visibility
        /////////////////////////////////////////////////////////////////////////////
        public bool displayModeVisibilityPreLanding
        {
            get
            {
                return onDeckStabilityLimitsVM.displayModeVisibilityPreLanding;
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
                    _relativeWindDir.Set(value);

                    OnPropertyChanged(nameof(relativeWindDirString));
                }
            }
        }

        public string relativeWindDirString
        {
            get
            {
                if (relativeWindDir != null)
                {
                    // Sjekke om data er gyldig
                    if (relativeWindDir.status == DataStatus.OK)
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
                    if (_rwdGraphData.data != value.data ||
                        _rwdGraphData.data2 != value.data2)
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
                if (_rwdGraphData?.status == DataStatus.OK)
                    return _rwdGraphData.data2;
                else
                    return 0;
            }
        }

        public double rwdGraphDataY
        {
            get
            {
                if (_rwdGraphData?.status == DataStatus.OK)
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

        /////////////////////////////////////////////////////////////////////////////
        // Relative Wind Direction Limit State
        /////////////////////////////////////////////////////////////////////////////
        public HelideckStatusType GetRWDLimitState(double wind, double rwd)
        {
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

        // Variabel oppdatert
        // Dersom navn ikke er satt brukes kallende medlem sitt navn
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
