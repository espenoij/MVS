using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MVS
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
                return _timestamp.ToString();
            }
        }

        // Funksjon for å sjekke om timestamp har endret seg side sist vi sjekket
        private DateTime _timestampCheck { get; set; }
        public bool TimeStampCheck
        {
            get
            {
                bool result = false;

                if (_timestamp != _timestampCheck &&
                    _timestamp != DateTime.MinValue)
                {
                    result = true;
                    _timestampCheck = _timestamp;
                }

                return result;
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
                dbColumn = inputData.dbColumn;
            }
        }

        public void Clear()
        {
            id = 0;
            name = string.Empty;
            dataId = 0;
            data = 0;
            data2 = 0;
            data3 = string.Empty;
            timestamp = DateTime.MinValue;
            status = DataStatus.NONE;
            dbColumn = string.Empty;
        }

        // Variabel oppdatert
        // Dersom navn ikke settes brukes kallende medlem sitt navn
        protected virtual void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        // Server spesifikk:
        ////////////////////////////////////////////////////////////////////

        // Data Prosessering
        private MVSDataProcessing dataProcess = new MVSDataProcessing();

        // Init av prosessering med error handler og error type
        public void InitProcessing(ErrorHandler errorHandler, ErrorMessageCategory errorMessageCat, AdminSettingsVM adminSettingsVM)
        {
            dataProcess.Init(errorHandler, errorMessageCat, adminSettingsVM);
        }

        // Legger til en prosessering med type og parameter
        public void AddProcessing(CalculationType type, double parameter)
        {
            dataProcess.AddProcessing(type, parameter);
        }

        // Utfør alle innlagte prosesseringer/kalkulasjoner
        public void DoProcessing(HMSData newData)
        {
            // Null sjekk
            if (newData != null)
            {
                // Sjekke at input innholder reell verdi
                if (!double.IsNaN(newData.data))
                {
                    if (newData.status == DataStatus.OK ||
                        newData.status == DataStatus.OK_NA)
                    {
                        // Kalkulere nye data
                        data = dataProcess.DoProcessing(newData);
                    }

                    // Setter timestamp og sensorGroup lik inndata
                    timestamp = newData.timestamp;
                    status = newData.status;
                }
            }
        }

        public void BufferFillCheck(int dataCalcPos, double targetCount)
        {
            // NB! dataCalcPos refererer til posisjonen i dataCalculations[] listen hvor bufferet befinner seg

            // Sjekk status, dersom OK
            if (_status == DataStatus.OK)
                // Er bufferet fyllt opp
                if (dataProcess.BufferCount(dataCalcPos) < targetCount)
                    // Set OK, men ikke tilgjengelig
                    status = DataStatus.OK_NA;
        }

        public int BufferSize(int dataCalcPos)
        {
            return (int)dataProcess.BufferCount(dataCalcPos);
        }

        public void ResetDataCalculations()
        {
            // Resette dataCalculations
            dataProcess.ResetDataCalculations();
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