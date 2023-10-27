using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MVS
{
    /// <summary>
    /// Represents a motion data set and provides change notification through the INotifyPropertyChanged interface.
    /// </summary>
    public class MotionDataSet : INotifyPropertyChanged
    {
        // Change notification
        public event PropertyChangedEventHandler PropertyChanged;

        // Initialize
        public MotionDataSet()
        {
        }
        public MotionDataSet(int id, string name, string description, DateTime startTime, DateTime endTime)
        {
            Id = id;
            Name = name; 
            Description = description;
            StartTime = startTime;
            EndTime = endTime;
        }

        public MotionDataSet(MotionDataSet dataSet)
        {
            Set(dataSet);
        }

        public void Set(MotionDataSet dataSet)
        {
            if (dataSet != null)
            {
                Id = dataSet.Id;
                Name = dataSet.Name;
                Description = dataSet.Description;
                StartTime = dataSet.StartTime;
                EndTime = dataSet.EndTime;
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
                OnPropertyChanged(nameof(DurationString));
            }
        }
        public string StartTimeString
        {
            get
            {
                if (_startTime == System.Data.SqlTypes.SqlDateTime.MinValue.Value)
                    return Constants.NotAvailable;
                else
                    return string.Format("{0} - {1} (UTC)", _startTime.ToShortDateString(), _startTime.ToString("HH:mm:ss"));
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
                    return string.Format("{0} - {1} (UTC)", _endTime.ToShortDateString(), _endTime.ToString("HH:mm:ss"));
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

        // Variabel oppdatert
        // Dersom navn ikke settes brukes kallende medlem sitt navn
        protected virtual void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}