﻿using System;
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
        private DispatcherTimer timerBlink;

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
                OnPropertyChanged(nameof(projectRecordingStatus));
            }

            // Timer 2
            timerBlink = new DispatcherTimer();

            // Timer 2 parametre
            timerBlink.Interval = TimeSpan.FromMilliseconds(100);
            timerBlink.Tick += runTimerBlink;

            void runTimerBlink(Object source, EventArgs e)
            {
                OnPropertyChanged(nameof(RecordingSymbolVisibility));
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
            timerBlink.Start();
        }

        public void StopTimer()
        {
            // Sette start tid
            StartTime = System.Data.SqlTypes.SqlDateTime.MinValue.Value;

            // Starte timer
            timer.Stop();
            timerBlink.Stop();

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
                else 
                {
                    if (_startTime < DateTime.UtcNow)
                    {
                        return ProjectStatusType.GREEN;
                    }
                    else
                    if (_startTime.AddMinutes(-20) < DateTime.UtcNow)
                    {
                        return ProjectStatusType.AMBER;
                    }
                    else
                    {
                        return ProjectStatusType.RED;
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
                if (_recordingSymbolVisibility == Visibility.Visible)
                {
                    if (DateTime.Now.Millisecond < 500)
                        return Visibility.Visible;
                    else
                        return Visibility.Collapsed;
                }
                else
                {
                    return Visibility.Collapsed;
                }
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

