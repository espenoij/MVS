using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace MVS
{
    /// <summary>
    /// Represents a motion data set and provides change notification through the INotifyPropertyChanged interface.
    /// </summary>
    public class Project : INotifyPropertyChanged
    {
        // Change notification
        public event PropertyChangedEventHandler PropertyChanged;

        // Initialize
        public Project()
        {
            Name = string.Empty;
            Comments = string.Empty;
            StartTime = System.Data.SqlTypes.SqlDateTime.MinValue.Value;
            EndTime = System.Data.SqlTypes.SqlDateTime.MinValue.Value;
            InputMRUs = InputMRUType.ReferenceMRU_TestMRU;
        }

        public Project(int id, string name, string comments, InputMRUType inputMRUs)
        {
            Id = id;
            Name = name;
            Comments = comments;
            InputMRUs = inputMRUs;
        }

        public Project(Project session)
        {
            Set(session);
        }

        public void Set(Project session)
        {
            if (session != null)
            {
                Id = session.Id;
                Name = session.Name;
                Comments = session.Comments;
                StartTime = session.StartTime;
                EndTime = session.EndTime;
                InputMRUs = session.InputMRUs;
            }
        }

        private int _id { get; set; }
        public int Id
        {
            get
            {
                return _id;
            }
            set
            {
                _id = value;
                OnPropertyChanged();
            }
        }

        private string _name { get; set; }
        public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                _name = value;
                OnPropertyChanged();
            }
        }

        private string _comments { get; set; }
        public string Comments
        {
            get
            {
                return _comments;
            }
            set
            {
                _comments = value;
                OnPropertyChanged();
            }
        }

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
                OnPropertyChanged(nameof(StartTimeString));
                OnPropertyChanged(nameof(StartTimeString2));
                OnPropertyChanged(nameof(DateString));
                OnPropertyChanged(nameof(DurationString));
                OnPropertyChanged(nameof(projectStatus));
            }
        }
        public string DateString
        {
            get
            {
                if (_startTime == System.Data.SqlTypes.SqlDateTime.MinValue.Value)
                    return Constants.NotAvailable;
                else
                    return string.Format("{0}-{1}-{2}", _startTime.Year, _startTime.Month.ToString("00"), _startTime.Day.ToString("00"));
            }
        }
        public string StartTimeString
        {
            get
            {
                if (_startTime == System.Data.SqlTypes.SqlDateTime.MinValue.Value)
                    return Constants.NotAvailable;
                else
                    return string.Format("{0}", _startTime.ToString("HH:mm:ss"));
            }
        }
        public string StartTimeString2
        {
            get
            {
                if (_startTime == System.Data.SqlTypes.SqlDateTime.MinValue.Value)
                    return Constants.NotAvailable;
                else
                    return string.Format("{0} (UTC)", _startTime.ToString("HH:mm:ss"));
            }
        }

        private DateTime _endTime { get; set; }
        public DateTime EndTime
        {
            get
            {
                return _endTime;
            }
            set
            {
                _endTime = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(EndTimeString));
                OnPropertyChanged(nameof(EndTimeString2));
                OnPropertyChanged(nameof(DurationString));
                OnPropertyChanged(nameof(projectStatus));
            }
        }
        public string EndTimeString
        {
            get
            {
                if (_endTime == System.Data.SqlTypes.SqlDateTime.MinValue.Value)
                    return Constants.NotAvailable;
                else
                    return string.Format("{0}", _endTime.ToString("HH:mm:ss"));
            }
        }
        public string EndTimeString2
        {
            get
            {
                if (_endTime == System.Data.SqlTypes.SqlDateTime.MinValue.Value)
                    return Constants.NotAvailable;
                else
                    return string.Format("{0} (UTC)", _endTime.ToString("HH:mm:ss"));
            }
        }

        public string DurationString
        {
            get
            {
                if (_startTime == System.Data.SqlTypes.SqlDateTime.MinValue.Value ||
                    _endTime == System.Data.SqlTypes.SqlDateTime.MinValue.Value)
                {
                    return Constants.NotAvailable;
                }
                else
                {
                    return string.Format("{0}", (_endTime - _startTime).ToString(@"hh\:mm\:ss"));
                }
            }
        }

        public Visibility DurationWarning()
        {
            if (_startTime == System.Data.SqlTypes.SqlDateTime.MinValue.Value ||
                _endTime == System.Data.SqlTypes.SqlDateTime.MinValue.Value)
            {
                return Visibility.Collapsed;
            }
            else
            {
                TimeSpan duration = _endTime - _startTime;
                if (duration.TotalMinutes >= 20)
                {
                    return Visibility.Collapsed;
                }
                else
                {
                    return Visibility.Visible;
                }
            }
        }

        public bool DataSetHasData()
        {
            if (_startTime == System.Data.SqlTypes.SqlDateTime.MinValue.Value ||
                _endTime == System.Data.SqlTypes.SqlDateTime.MinValue.Value)
                return false;
            else
                return true;
        }

        public void ClearTimestamps()
        {
            StartTime = System.Data.SqlTypes.SqlDateTime.MinValue.Value;
            EndTime = System.Data.SqlTypes.SqlDateTime.MinValue.Value;
        }

        private InputMRUType _inputMRUs { get; set; }
        public InputMRUType InputMRUs
        {
            get
            {
                return _inputMRUs;
            }
            set
            {
                _inputMRUs = value;
                OnPropertyChanged();
            }
        }

        public ProjectStatusType projectStatus
        {
            get
            {
                if (DataSetHasData())
                {
                    TimeSpan duration = _endTime - _startTime;

                    if (duration.TotalMinutes >= 40)
                    {
                        return ProjectStatusType.GREEN;
                    }
                    else
                    if (duration.TotalMinutes >= 20)
                    {
                        return ProjectStatusType.AMBER;
                    }
                    else
                    {
                        return ProjectStatusType.RED;
                    }
                }
                else
                {
                    return ProjectStatusType.OFF;
                }
            }
        }

        // High-level lifecycle status for the verification workflow.
        // Drives the wizard, status badges and project list column.
        public ProjectDataStatus DataStatus
        {
            get
            {
                if (!DataSetHasData())
                    return ProjectDataStatus.None;

                if (HasCorrectionApplied)
                    return ProjectDataStatus.Analysed;

                return ProjectDataStatus.Captured;
            }
        }

        // Applied corrections persisted per project. These are the deviation
        // values that have been written back to the Test MRU to "zero it out".
        private double _appliedCorrectionPitch;
        public double AppliedCorrectionPitch
        {
            get { return _appliedCorrectionPitch; }
            set
            {
                _appliedCorrectionPitch = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasCorrectionApplied));
                OnPropertyChanged(nameof(DataStatus));
            }
        }

        private double _appliedCorrectionRoll;
        public double AppliedCorrectionRoll
        {
            get { return _appliedCorrectionRoll; }
            set
            {
                _appliedCorrectionRoll = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasCorrectionApplied));
                OnPropertyChanged(nameof(DataStatus));
            }
        }

        private double _appliedCorrectionHeave;
        public double AppliedCorrectionHeave
        {
            get { return _appliedCorrectionHeave; }
            set
            {
                _appliedCorrectionHeave = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasCorrectionApplied));
                OnPropertyChanged(nameof(DataStatus));
            }
        }

        private DateTime? _correctionAppliedAt;
        public DateTime? CorrectionAppliedAt
        {
            get { return _correctionAppliedAt; }
            set
            {
                _correctionAppliedAt = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CorrectionAppliedAtString));
                OnPropertyChanged(nameof(HasCorrectionApplied));
            }
        }

        public string CorrectionAppliedAtString
        {
            get
            {
                if (!_correctionAppliedAt.HasValue)
                    return Constants.NotAvailable;
                return _correctionAppliedAt.Value.ToString("yyyy-MM-dd HH:mm:ss") + " (UTC)";
            }
        }

        public bool HasCorrectionApplied
        {
            get
            {
                return _correctionAppliedAt.HasValue ||
                       _appliedCorrectionPitch != 0d ||
                       _appliedCorrectionRoll != 0d ||
                       _appliedCorrectionHeave != 0d;
            }
        }

        // Property change notification. If no name is supplied the calling
        // member name is used (CallerMemberName).
        protected virtual void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    // Lifecycle state of a project's verification data.
    public enum ProjectDataStatus
    {
        None,       // No recording yet
        Recording,  // Currently being recorded
        Captured,   // Has data, not yet analysed/applied
        Analysed    // Corrections have been applied
    }
}