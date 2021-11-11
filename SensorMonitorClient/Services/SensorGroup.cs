using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace SensorMonitorClient
{
    public class SensorGroup : INotifyPropertyChanged
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
                OnPropertyChanged(nameof(statusString));
            }
        }
        public string statusString
        {
            get
            {
                return status.ToString();
            }
        }

        public void Set(SensorGroup sensorGroup)
        {
            id = sensorGroup.id;
            name = sensorGroup.name;
            active = sensorGroup.active;
            //status = sensor.status;
        }

        // Variabel oppdatert
        // Dersom navn ikke settes brukes kallende medlem sitt navn
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
