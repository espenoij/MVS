using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MVS
{
    public class SensorGroup : INotifyPropertyChanged
    {
        // Change notification
        public event PropertyChangedEventHandler PropertyChanged;

        public SensorGroup()
        {
        }

        public SensorGroup(SensorGroup sensorGroup)
        {
            Set(sensorGroup);
        }

        public SensorGroup(SensorGroupIDConfig sensorIDConfig)
        {
            id = sensorIDConfig.id;
            name = sensorIDConfig.name;
            dbColumn = sensorIDConfig.dbColumn;
            active = bool.Parse(sensorIDConfig.active);
            status = DataStatus.OK;
        }

        public void Set(SensorGroup sensorGroup)
        {
            id = sensorGroup.id;
            name = sensorGroup.name;
            dbColumn = sensorGroup.dbColumn;
            active = sensorGroup.active;
            status = sensorGroup.status;
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

        private string _dbColumn { get; set; }
        public string dbColumn
        {
            get
            {
                return _dbColumn;
            }
            set
            {
                _dbColumn = value;
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
                return _status.ToString();
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
