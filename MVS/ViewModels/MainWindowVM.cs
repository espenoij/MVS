using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Threading;
using MVS.Models;

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
            IsRecording = false;
            DataViewTabEnabled = false;

            // Starttime Init
            StartTime = System.Data.SqlTypes.SqlDateTime.MinValue.Value;

            // Timer — fires 4×/s to keep elapsed-time labels fresh.
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(250);
            timer.Tick += (s, e) =>
            {
                OnPropertyChanged(nameof(projectRecordingStatus));
                OnPropertyChanged(nameof(OperationsModeString));
            };
        }

        /////////////////////////////////////////////////////////////////////////////
        // Timer Ops
        /////////////////////////////////////////////////////////////////////////////
        public void StartTimer()
        {
            StartTime = DateTime.UtcNow;
            IsRecording = true;
            timer.Start();
        }

        public void StopTimer()
        {
            StartTime = System.Data.SqlTypes.SqlDateTime.MinValue.Value;
            IsRecording = false;
            timer.Stop();
            OnPropertyChanged(nameof(projectRecordingStatus));
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
                    return string.Format("Project: {0}", _selectedProject.Name);
                else
                    return string.Empty;
            }
        }
        public void SelectedSessionNameUpdated()
        {
            OnPropertyChanged(nameof(SelectedProjectString));
        }

        /////////////////////////////////////////////////////////////////////////////
        // Project Recording Status
        /////////////////////////////////////////////////////////////////////////////
        public ProjectStatusType projectRecordingStatus
        {
            get
            {
                if (_startTime == System.Data.SqlTypes.SqlDateTime.MinValue.Value)
                {
                    return ProjectStatusType.OFF;
                }

                double elapsed = (DateTime.UtcNow - _startTime).TotalMinutes;

                if (elapsed >= 40.0)
                    return ProjectStatusType.GREEN;
                else if (elapsed >= 20.0)
                    return ProjectStatusType.AMBER;
                else
                    return ProjectStatusType.RED;
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
                            TimeSpan elapsed = DateTime.UtcNow - _startTime;
                            if (elapsed.TotalSeconds < 0) elapsed = TimeSpan.Zero;
                            return string.Format("{0} {1}", elapsed.ToString(@"hh\:mm\:ss"), modeText).TrimEnd();
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
        // Recording state (drives the animated indicator in the UI)
        /////////////////////////////////////////////////////////////////////////////
        private bool _isRecording;
        public bool IsRecording
        {
            get => _isRecording;
            set
            {
                _isRecording = value;
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

        /////////////////////////////////////////////////////////////////////////////
        // Data Analysis Progress
        /////////////////////////////////////////////////////////////////////////////
        private int _dataAnalysisProgress { get; set; }
        public int dataAnalysisProgress
        {
            get
            {
                return _dataAnalysisProgress;
            }
            set
            {
                _dataAnalysisProgress = value;
                OnPropertyChanged();
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Import Result
        /////////////////////////////////////////////////////////////////////////////
        public ImportResult Result { get; set; } = new ImportResult();

        // Variabel oppdatert
        // Dersom navn ikke er satt brukes kallende medlem sitt navn
        protected virtual void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}

