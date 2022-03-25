using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Threading;

namespace HMS_Client
{
    public class LandingStatusTrendVM : INotifyPropertyChanged
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

        private OnDeckStabilityLimitsVM onDeckStabilityLimitsVM;

        public void Init(Config config, OnDeckStabilityLimitsVM onDeckStabilityLimitsVM, HelideckStatusVM helideckStatusVM)
        {
            this.onDeckStabilityLimitsVM = onDeckStabilityLimitsVM;

            InitUI(config);

            // Oppdatere UI
            UIUpdateTimer.Interval = TimeSpan.FromMilliseconds(config.ReadWithDefault(ConfigKey.ClientUpdateFrequencyUI, Constants.ClientUIUpdateFrequencyDefault));
            UIUpdateTimer.Tick += UIUpdate;
            UIUpdateTimer.Start();

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
                GraphBuffer.TransferDisplayData(statusTrend20mList, landingTrend20mDispList, Constants.Minutes20);
                GraphBuffer.TransferDisplayData(statusTrend3hList, statusTrend3hDispList, Constants.Hours3);

                OnPropertyChanged(nameof(landingStatusTimeString));
                OnPropertyChanged(nameof(displayModeVisibilityPreLanding));
            }
        }

        private void InitUI(Config config)
        {
            // Landing Status Trend
            double uiUpdateFreq = config.ReadWithDefault(ConfigKey.ClientUpdateFrequencyUI, Constants.ClientUIUpdateFrequencyDefault);

            // Init av chart data
            for (int i = (int)(-Constants.Minutes20 * (1000 / uiUpdateFreq)); i <= 0; i++)
            {
                statusTrend20mList.Add(new HelideckStatus()
                {
                    status = HelideckStatusType.NO_DATA,
                    timestamp = DateTime.UtcNow.AddSeconds(i)
                });
            }

            for (int i = (int)(-Constants.Hours3 * (1000 / uiUpdateFreq)); i <= 0; i++)
            {
                statusTrend3hList.Add(new HelideckStatus()
                {
                    status = HelideckStatusType.NO_DATA,
                    timestamp = DateTime.UtcNow.AddSeconds(i)
                });
            }

            for (int i = 0; i < Constants.landingTrendDisplayListMax; i++)
            {
                landingTrend20mDispList.Add(new HelideckStatusType());
                statusTrend3hDispList.Add(new HelideckStatusType());
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
