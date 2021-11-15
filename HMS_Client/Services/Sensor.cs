using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace SensorMonitorClient
{
    public class Sensor : INotifyPropertyChanged
    {
        // Change notification
        public event PropertyChangedEventHandler PropertyChanged;

        private int _id { get; set; }
        public int id
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
        public string name
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

        private bool _active { get; set; }
        public bool active
        {
            get
            {
                return _active;
            }
            set
            {
                _active = value;
                OnPropertyChanged();
            }
        }

        private DataStatus _dataStatus { get; set; }
        public DataStatus dataStatus
        {
            get
            {
                return _dataStatus;
            }
            set
            {
                _dataStatus = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(dataStatusString));
            }
        }
        public string dataStatusString
        {
            get
            {
                return dataStatus.ToString();
            }
        }

        private DataStatus _previousStatus { get; set; }
        public DataStatus previousStatus
        {
            get
            {
                return _previousStatus;
            }
            set
            {
                _previousStatus = value;
                OnPropertyChanged();
            }
        }

        public void Set(Sensor sensor)
        {
            id = sensor.id;
            name = sensor.name;
            active = sensor.active;
            dataStatus = sensor.dataStatus;
            previousStatus = dataStatus;
        }

        // Variabel oppdatert
        // Dersom navn ikke settes brukes kallende medlem sitt navn
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
