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
                OnPropertyChanged(nameof(OperationsModeString));
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Timer Ops
        /////////////////////////////////////////////////////////////////////////////
        public void StartTimer()
        {
            // Sette start tid
            StartTime = DateTime.UtcNow.AddMinutes(20);

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
        // Selected Motion Verification Session
        /////////////////////////////////////////////////////////////////////////////
        private RecordingSession _selectedSession { get; set; }
        public RecordingSession SelectedSession
        {
            get
            {
                return _selectedSession;
            }
            set
            {
                if (value != _selectedSession)
                {
                    _selectedSession = value;

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
            }
        }
        /////////////////////////////////////////////////////////////////////////////
        // Operations mode
        /////////////////////////////////////////////////////////////////////////////
        private OperationsMode _operationsMode;
        public OperationsMode OperationsMode
        {
            get
            {
                return _operationsMode;
            }
            set
            {
                _operationsMode = value;
                OnPropertyChanged(nameof(OperationsModeString));
            }
        }
        public string OperationsModeString
        {
            get
            {
                switch (OperationsMode)
                {
                    case OperationsMode.Test:
                    case OperationsMode.Recording:

                        string modeText = string.Empty;
                        if (OperationsMode == OperationsMode.Test)
                            modeText = "- Test Run";

                        if (_startTime != System.Data.SqlTypes.SqlDateTime.MinValue.Value)
                        {
                            string sign = string.Empty;
                            if ((DateTime.UtcNow - _startTime).TotalSeconds < 0)
                                sign = "-";

                            return string.Format("{0}{1} {2}", sign, (DateTime.UtcNow - _startTime).ToString(@"hh\:mm\:ss"), modeText);
                        }
                        else
                        {
                            return string.Empty;
                        }

                    case OperationsMode.Stop:
                        return string.Empty;

                    default:
                        return "Error";
                }
            }
        }

        public bool StoreToDatabase()
        {
            // Lagrer kun til databasen når vi først har kjørt i 20 minutter.
            return (DateTime.UtcNow - _startTime).TotalSeconds >= 0;
        }

        // Variabel oppdatert
        // Dersom navn ikke er satt brukes kallende medlem sitt navn
        protected virtual void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}

