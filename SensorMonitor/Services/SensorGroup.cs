using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace SensorMonitor
{
    public class SensorGroup : INotifyPropertyChanged
    {
        // Change notification
        public event PropertyChangedEventHandler PropertyChanged;

        public SensorGroup(SensorGroup sensor)
        {
            id = sensor.id;
            name = sensor.name;
            active = sensor.active;
            status = sensor.status;
            previousStatus = sensor.previousStatus;
        }

        public SensorGroup(SensorGroupIDConfig sensorIDConfig)
        {
            id = sensorIDConfig.id;
            name = sensorIDConfig.name;
            active = bool.Parse(sensorIDConfig.active);
            status = DataStatus.OK;
            previousStatus = DataStatus.OK;
        }

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

        private DataStatus _status { get; set; }
        public DataStatus status
        {
            get
            {
                return _status;
            }
            set
            {
                _status = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(dataStatusString));
            }
        }
        public string dataStatusString
        {
            get
            {
                return status.ToString();
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

        public void Set(SensorGroup sensor)
        {
            id = sensor.id;
            name = sensor.name;
            active = sensor.active;
            status = sensor.status;
            previousStatus = sensor.previousStatus;
        }

        // Variabel oppdatert
        // Dersom navn ikke settes brukes kallende medlem sitt navn
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
