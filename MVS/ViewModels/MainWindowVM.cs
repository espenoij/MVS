using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
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
            RecordingSymbolVisibility = Visibility.Collapsed;
            DataViewTabEnabled = false;

            // Starttime Init
            StartTime = System.Data.SqlTypes.SqlDateTime.MinValue.Value;

            // Timer
            timer = new DispatcherTimer();

            // Timer parametre
            timer.Interval = TimeSpan.FromMilliseconds(250);
            timer.Tick += runTimer;

            void runTimer(Object source, EventArgs e)
            {
                OnPropertyChanged(nameof(OperationsModeString));
                OnPropertyChanged(nameof(recordingStatus));
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Timer Ops
        /////////////////////////////////////////////////////////////////////////////
        public void StartTimer()
        {
            // Sette start tid
            // Start tid sette 40 minutter frem i tid slik at vi har
            // 20 min snitt før vi begynner målinger og 20 min med måling
            StartTime = DateTime.UtcNow.AddMinutes(40);

            // Starte timer
            timer.Start();
        }

        public void StopTimer()
        {
            // Sette start tid
            StartTime = System.Data.SqlTypes.SqlDateTime.MinValue.Value;

            // Starte timer
            timer.Stop();

            OnPropertyChanged(nameof(recordingStatus));
        }

        /////////////////////////////////////////////////////////////////////////////
        // Selected Project
        /////////////////////////////////////////////////////////////////////////////
        private Project _selectedProject { get; set; }
        public Project SelectedProject
        {
            get
            {
                return _selectedProject;
            }
            set
            {
                if (value != _selectedProject)
                {
                    _selectedProject = value;

                    DataViewTabEnabled = true;

                    OnPropertyChanged();
                    OnPropertyChanged(nameof(SelectedProjectString));
                }
            }
        }
        public string SelectedProjectString
        {
            get
            {
                if (!string.IsNullOrEmpty(_selectedProject?.Name))
                    return string.Format("{0}", _selectedProject.Name);
                else
                    return string.Empty;
            }
        }
        public void SelectedSessionNameUpdated()
        {
            OnPropertyChanged(nameof(SelectedProjectString));
        }

        /////////////////////////////////////////////////////////////////////////////
        // Project Recording Sstatus
        /////////////////////////////////////////////////////////////////////////////
        public RecordingStatusType recordingStatus
        {
            get
            {
                if (_startTime == System.Data.SqlTypes.SqlDateTime.MinValue.Value)
                {
                    return RecordingStatusType.OFF;
                }
                else 
                {
                    if (_startTime < DateTime.UtcNow)
                    {
                        return RecordingStatusType.GREEN;
                    }
                    else
                    if (_startTime.AddMinutes(-20) < DateTime.UtcNow)
                    {
                        return RecordingStatusType.AMBER;
                    }
                    else
                    {
                        return RecordingStatusType.RED;
                    }
                }
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Time start
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
                            modeText = "(Test Run)";

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
                    case OperationsMode.ViewData:
                        return string.Empty;

                    default:
                        return "Error";
                }
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Knapper
        /////////////////////////////////////////////////////////////////////////////
        private bool _startButtonEnabled { get; set; }
        public bool StartButtonEnabled
        {
            get
            {
                return _startButtonEnabled;
            }
            set
            {
                _startButtonEnabled = value;
                OnPropertyChanged();
            }
        }

        private bool _stopButtonEnabled { get; set; }
        public bool StopButtonEnabled
        {
            get
            {
                return _stopButtonEnabled;
            }
            set
            {
                _stopButtonEnabled = value;
                OnPropertyChanged();
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Recording Symbol
        /////////////////////////////////////////////////////////////////////////////
        private Visibility _recordingSymbolVisibility { get; set; }
        public Visibility RecordingSymbolVisibility
        {
            get
            {
                return _recordingSymbolVisibility;
            }
            set
            {
                _recordingSymbolVisibility = value;
                OnPropertyChanged();
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Data View Tab
        /////////////////////////////////////////////////////////////////////////////
        private bool _dataViewTabEnabled { get; set; }
        public bool DataViewTabEnabled
        {
            get
            {
                return _dataViewTabEnabled;
            }
            set
            {
                _dataViewTabEnabled = value;
                OnPropertyChanged();
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

