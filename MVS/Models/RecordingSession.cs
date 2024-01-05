using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MVS
{
    /// <summary>
    /// Represents a motion data set and provides change notification through the INotifyPropertyChanged interface.
    /// </summary>
    public class RecordingSession : INotifyPropertyChanged
    {
        // Change notification
        public event PropertyChangedEventHandler PropertyChanged;

        // Initialize
        public RecordingSession()
        {
            Name = string.Empty;
            Description = string.Empty;
            StartTime = System.Data.SqlTypes.SqlDateTime.MinValue.Value;
            EndTime = System.Data.SqlTypes.SqlDateTime.MinValue.Value;
            InputMRUs = InputMRUType.ReferenceMRU_TestMRU;
        }

        public RecordingSession(int id, string name, string description, InputMRUType inputMRUs)
        {
            Id = id;
            Name = name;
            Description = description;
            InputMRUs = inputMRUs;
        }

        public RecordingSession(RecordingSession session)
        {
            Set(session);
        }

        public void Set(RecordingSession session)
        {
            if (session != null)
            {
                Id = session.Id;
                Name = session.Name;
                Description = session.Description;
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

        private string _description { get; set; }
        public string Description
        {
            get
            {
                return _description;
            }
            set
            {
                _description = value;
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
            }
        }
        public string DateString
        {
            get
            {
                if (_startTime == System.Data.SqlTypes.SqlDateTime.MinValue.Value)
                    return Constants.NotAvailable;
                else
                    return string.Format("{0}", _startTime.ToShortDateString());
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

        // Variabel oppdatert
        // Dersom navn ikke settes brukes kallende medlem sitt navn
        protected virtual void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}