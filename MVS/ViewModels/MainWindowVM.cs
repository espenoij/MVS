using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Threading;

namespace MVS
{
    public class MainWindowVM : INotifyPropertyChanged
    {
        // Change notification
        public event PropertyChangedEventHandler PropertyChanged;

        // Reader timer
        private DispatcherTimer timer;

        public MainWindowVM()
        {
            // Init
            StartTime = System.Data.SqlTypes.SqlDateTime.MinValue.Value;

            // Timer
            timer = new DispatcherTimer();

            // Timer parametre
            timer.Interval = TimeSpan.FromMilliseconds(250);
            timer.Tick += runTimer;

            void runTimer(Object source, EventArgs e)
            {
                OnPropertyChanged(nameof(ElapsedTimeString));
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Timer Ops
        /////////////////////////////////////////////////////////////////////////////
        public void StartTimer()
        {
            // Sette start tid
            StartTime = DateTime.UtcNow;

            // Starte timer
            timer.Start();
        }

        public void StopTimer()
        {
            // Sette start tid
            StartTime = System.Data.SqlTypes.SqlDateTime.MinValue.Value;

            // Starte timer
            timer.Stop();
        }

        /////////////////////////////////////////////////////////////////////////////
        // Selected Motion Data Set
        /////////////////////////////////////////////////////////////////////////////
        private MotionDataSet _selectedMotionDataSet { get; set; }
        public MotionDataSet SelectedMotionDataSet
        {
            get
            {
                return _selectedMotionDataSet;
            }
            set
            {
                if (value != _selectedMotionDataSet)
                {
                    _selectedMotionDataSet = value;

                    OnPropertyChanged();
                }
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Elapsed Time
        /////////////////////////////////////////////////////////////////////////////
        private DateTime _startTime { get; set; }
        public DateTime StartTime
        {
            get
            {
                return _startTime;
            }
            set
            {
                _startTime = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ElapsedTimeString));
            }
        }
        public string ElapsedTimeString
        {
            get
            {
                if (_startTime == System.Data.SqlTypes.SqlDateTime.MinValue.Value)
                    return string.Empty;
                else
                    return string.Format("Elapsed Time: {0}", (DateTime.UtcNow - _startTime).ToString(@"hh\:mm\:ss"));
            }
        }

        // Variabel oppdatert
        // Dersom navn ikke er satt brukes kallende medlem sitt navn
        protected virtual void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}

