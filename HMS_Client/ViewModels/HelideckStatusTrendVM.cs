using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Threading;

namespace HMS_Client
{
    public class HelideckStatusTrendVM : INotifyPropertyChanged
    {
        // Change notification
        public event PropertyChangedEventHandler PropertyChanged;

        private DispatcherTimer HelideckStatusUpdateTimer = new DispatcherTimer();
        private DispatcherTimer ChartDataUpdateTimer20m = new DispatcherTimer();
        private DispatcherTimer ChartDataUpdateTimer3h = new DispatcherTimer();

        // 20 minutters buffer
        private RadObservableCollectionEx<HelideckStatus> statusTrendBuffer20m = new RadObservableCollectionEx<HelideckStatus>();
        // 20 minutters data liste
        public RadObservableCollectionEx<HelideckStatus> statusTrend20mList = new RadObservableCollectionEx<HelideckStatus>();
        public List<HelideckStatusType> statusTrend20mDispList = new List<HelideckStatusType>();

        // 3 timers buffer
        private RadObservableCollectionEx<HelideckStatus> statusTrendBuffer3h = new RadObservableCollectionEx<HelideckStatus>();
        // 3 timers data liste
        public RadObservableCollectionEx<HelideckStatus> statusTrend3hList = new RadObservableCollectionEx<HelideckStatus>();
        public List<HelideckStatusType> statusTrend3hDispList = new List<HelideckStatusType>();

        private HelideckStabilityLimitsVM helideckStabilityLimitsVM;

        public void Init(HelideckStatusVM helideckStatusVM, HelideckStabilityLimitsVM helideckStabilityLimitsVM, Config config)
        {
            this.helideckStabilityLimitsVM = helideckStabilityLimitsVM;

            // Init av chart data
            for (int i = -Constants.Minutes20; i <= 0; i++)
            {
                statusTrend20mList.Add(new HelideckStatus()
                {
                    status = HelideckStatusType.OFF,
                    timestamp = DateTime.UtcNow.AddSeconds(i)
                });
            }

            for (int i = -Constants.Hours3; i <= 0; i++)
            {
                statusTrend3hList.Add(new HelideckStatus()
                {
                    status = HelideckStatusType.OFF,
                    timestamp = DateTime.UtcNow.AddSeconds(i)
                });
            }

            for (int i = 0; i < Constants.statusTrendDisplayListMax; i++)
            {
                statusTrend20mDispList.Add(new HelideckStatusType());
                statusTrend3hDispList.Add(new HelideckStatusType());
            }

            // Sample helideck status til trend buffer
            HelideckStatusUpdateTimer.Interval = TimeSpan.FromMilliseconds(1000);
            HelideckStatusUpdateTimer.Tick += HelideckStatusUpdate;
            HelideckStatusUpdateTimer.Start();

            void HelideckStatusUpdate(object sender, EventArgs e)
            {
                HelideckStatus newStatus = new HelideckStatus()
                {
                    status = helideckStatusVM.helideckStatus,
                    timestamp = DateTime.UtcNow
                };

                // Legge inn ny status i buffer
                statusTrendBuffer20m.Add(newStatus);

                // Legge inn ny status i buffer
                statusTrendBuffer3h.Add(newStatus);
            }

            // Oppdatere trend data i UI: 20 minutter
            ChartDataUpdateTimer20m.Interval = TimeSpan.FromMilliseconds(config.ReadWithDefault(ConfigKey.ChartDataUpdateFrequency20m, Constants.ChartUpdateFrequencyUI20mDefault));
            ChartDataUpdateTimer20m.Tick += ChartDataUpdate20m;
            ChartDataUpdateTimer20m.Start();

            void ChartDataUpdate20m(object sender, EventArgs e)
            {
                // Overføre data fra buffer til chart data: 20m
                TransferBuffer(statusTrendBuffer20m, statusTrend20mList);

                // Slette buffer
                ClearBuffer(statusTrendBuffer20m);

                // Fjerne gamle data fra chart data
                RemoveOldData(statusTrend20mList, Constants.Minutes20);

                // Overføre til display data liste
                TransferDisplayData(statusTrend20mList, statusTrend20mDispList);

                OnPropertyChanged(nameof(helideckStatusTimeString));
            }

            // Oppdatere trend data i UI: 3 hours
            ChartDataUpdateTimer3h.Interval = TimeSpan.FromMilliseconds(config.ReadWithDefault(ConfigKey.ChartDataUpdateFrequency3h, Constants.ChartUpdateFrequencyUI3hDefault));
            ChartDataUpdateTimer3h.Tick += ChartDataUpdate3h;
            ChartDataUpdateTimer3h.Start();

            void ChartDataUpdate3h(object sender, EventArgs e)
            {
                // Overføre data fra buffer til chart data: 3h
                TransferBuffer(statusTrendBuffer3h, statusTrend3hList);

                // Slette buffer
                ClearBuffer(statusTrendBuffer3h);

                // Fjerne gamle data fra chart data
                RemoveOldData(statusTrend3hList, Constants.Hours3);

                // Overføre til display data liste
                TransferDisplayData(statusTrend3hList, statusTrend3hDispList);

                OnPropertyChanged(nameof(helideckStatusTimeString));
            }
        }

        private void TransferBuffer(RadObservableCollectionEx<HelideckStatus> buffer, RadObservableCollectionEx<HelideckStatus> dataList)
        {
            // Overfører alle data fra buffer til dataList
            dataList.AddRange(buffer);
        }

        private void ClearBuffer(RadObservableCollectionEx<HelideckStatus> buffer)
        {
            // Sletter alle data fra buffer
            buffer.Clear();
        }

        private void RemoveOldData(RadObservableCollectionEx<HelideckStatus> dataList, double timeInterval)
        {
            if (dataList != null)
            {
                bool doneRemovingOldData = false;

                while (!doneRemovingOldData && dataList.Count > 0)
                {
                    if (dataList[0].timestamp < DateTime.UtcNow.AddSeconds(-timeInterval))
                        dataList.RemoveAt(0);
                    else
                        doneRemovingOldData = true;
                }
            }
        }

        private void TransferDisplayData(RadObservableCollectionEx<HelideckStatus> list, List<HelideckStatusType> dispList)
        {
            // Denne funksjonen mapper et visst antall statuser til en status indikator posisjon på tidslinjen på skjermen.
            // Viser høyeste nivå fra sub-settet med statuser.

            int subSetCounter = 0;
            double subSetLength = (double)list.Count / (double)dispList.Count;

            HelideckStatusType status = HelideckStatusType.OFF;

            // Gå gjennom alle status data
            for (int i = 0; i < list.Count; i++)
            {
                // Har vi kommet til nytt subSet?
                if (i > (subSetLength * (double)subSetCounter) || i == list.Count - 1)
                {
                    // Sette status i display listen
                    if (subSetCounter < dispList.Count)
                        dispList[subSetCounter++] = status;

                    status = HelideckStatusType.OFF;
                }
                else
                {
                    // Finne høyeste status nivå
                    switch (list[i].status)
                    {
                        case HelideckStatusType.RED:
                            status = HelideckStatusType.RED;
                            break;

                        case HelideckStatusType.AMBER:
                            if (status != HelideckStatusType.RED)
                                status = HelideckStatusType.AMBER;
                            break;

                        case HelideckStatusType.BLUE:
                            if (status != HelideckStatusType.RED &&
                                status != HelideckStatusType.AMBER)
                                status = HelideckStatusType.BLUE;
                            break;
                    }
                }
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
        public string helideckStatusTimeString
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
                OnPropertyChanged(nameof(helideckStatusTimeString));
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

        // Variabel oppdatert
        // Dersom navn ikke er satt brukes kallende medlem sitt navn
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    public class HelideckStatus
    {
        public HelideckStatusType status { get; set; }
        public DateTime timestamp { get; set; }

        //public int statusNumeric
        //{
        //    get
        //    {
        //        return (int)status;
        //    }
        //}
    }
}
