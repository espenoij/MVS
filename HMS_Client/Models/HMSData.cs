using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace HMS_Client
{
    // NB! Data definisjonene her er, og må være, lik de i server delen (HMS_Client\HMSData.cs)
    // Forskjellen er at server klassen har prosessering i tillegg. Det trenger ikke klient siden.

    public class HMSData : INotifyPropertyChanged
    {
        // Change notification
        public event PropertyChangedEventHandler PropertyChanged;

        // Initialize
        public HMSData()
        {
            status = DataStatus.TIMEOUT_ERROR;
            sensorGroupId = Constants.SensorIDNotSet;
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

        private int _dataId { get; set; }
        public int dataId
        {
            get
            {
                return _dataId;
            }
            set
            {
                _dataId = value;
                OnPropertyChanged();
            }
        }

        private double _data { get; set; }
        public double data
        {
            get
            {
                return _data;
            }
            set
            {
                _data = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(dataString));
                OnPropertyChanged(nameof(graphData));
            }
        }

        private double _data2 { get; set; }
        public double data2
        {
            get
            {
                return _data2;
            }
            set
            {
                _data2 = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(graphData2));
            }
        }

        private string _data3 { get; set; }
        public string data3
        {
            get
            {
                return _data3;
            }
            set
            {
                _data3 = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(dataString));
            }
        }
        public string dataString
        {
            get
            {
                if (!string.IsNullOrEmpty(_data3))
                    return _data3;
                else
                    return _data.ToString();
            }
        }

        private DateTime _timestamp { get; set; }
        public DateTime timestamp
        {
            get
            {
                return _timestamp;
            }
            set
            {
                _timestamp = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(timestampString));
            }
        }
        public string timestampString
        {
            get
            {
                return _timestamp.ToString(Constants.TimestampFormat, Constants.cultureInfo);
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

        private LimitStatus _limitStatus { get; set; }
        public LimitStatus limitStatus
        {
            get
            {
                return _limitStatus;
            }
            set
            {
                _limitStatus = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(limitStatusString));
            }
        }
        public string limitStatusString
        {
            get
            {
                return _limitStatus.ToString();
            }
        }

        private int _sensorGroupId { get; set; }
        public int sensorGroupId
        {
            get
            {
                return _sensorGroupId;
            }
            set
            {
                _sensorGroupId = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(sensorGroupIdString));
            }
        }
        public string sensorGroupIdString
        {
            get
            {
                if (_sensorGroupId == Constants.SensorIDNotSet)
                    return "None";
                else
                    return _sensorGroupId.ToString();
            }
        }

        private string _dbTableName { get; set; }
        public string dbColumn
        {
            get
            {
                return _dbTableName;
            }
            set
            {
                _dbTableName = value;
                OnPropertyChanged();
            }
        }

        public HMSData(HMSData inputData)
        {
            Set(inputData);
        }

        public void Set(HMSData inputData)
        {
            if (inputData != null)
            {
                id = inputData.id;
                name = inputData.name;
                dataId = inputData.dataId;
                data = inputData.data;
                data2 = inputData.data2;
                data3 = inputData.data3;
                timestamp = inputData.timestamp;
                status = inputData.status;
                limitStatus = inputData.limitStatus;
                sensorGroupId = inputData.sensorGroupId;
                dbColumn = inputData.dbColumn;
            }
        }

        // Variabel oppdatert
        // Dersom navn ikke settes brukes kallende medlem sitt navn
        protected virtual void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        // Klient spesifikk:
        ////////////////////////////////////////////////////////////////////
        ///
        public string nameString
        {
            get
            {
                if (!string.IsNullOrEmpty(_name))
                    return _name;
                else
                    return Constants.NameNotSet;
            }
        }

        // Data som skal ut i graf
        ////////////////////////////////////////////////////////////////////
        public double graphData
        {
            get
            {
                if (_status == DataStatus.OK)
                    return _data;
                else
                    // Ved å sende NaN til graf får vi mellomrom på graf-linjen
                    // der det ikke er gyldige data tilgjengelig.
                    return double.NaN;
            }
        }

        public double graphData2
        {
            get
            {
                if (_status == DataStatus.OK)
                    return _data2;
                else
                    // Ved å sende NaN til graf får vi mellomrom på graf-linjen
                    // der det ikke er gyldige data tilgjengelig.
                    return double.NaN;
            }
        }
    }
}