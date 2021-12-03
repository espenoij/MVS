﻿using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace HMS_Server
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
                if (!string.IsNullOrEmpty(data3))
                    return data3;
                else
                    return data.ToString();
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
                return timestamp.ToString();
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
                return limitStatus.ToString();
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
                if (sensorGroupId == Constants.SensorIDNotSet)
                    return "None";
                else
                    return sensorGroupId.ToString();
            }
        }

        private string _dbTableName { get; set; }
        public string dbTableName
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
                dbTableName = inputData.dbTableName;
            }
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
                dbTableName = inputData.dbTableName;
            }
        }

        // Variabel oppdatert
        // Dersom navn ikke settes brukes kallende medlem sitt navn
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        // Data Prosessering
        ////////////////////////////////////////////////////////////////////
        ///
        private HMSDataProcessing dataProcess = new HMSDataProcessing();

        // Init av prosessering med error handler og error type
        public void InitProcessing(ErrorHandler errorHandler, ErrorMessageCategory errorMessageCat)
        {
            dataProcess.Init(errorHandler, errorMessageCat);
        }

        // Legger til en prosessering med type og parameter
        public void AddProcessing(CalculationType type, double parameter)
        {
            dataProcess.AddProcessing(type, parameter);
        }

        // Utfør alle innlagte prosesseringer/kalkulasjoner
        public void DoProcessing(HMSData newData)
        {
            if (newData != null)
            {
                // Kalkulere nye data
                data = dataProcess.DoProcessing(newData);

                // Setter timestamp og status lik inndata
                timestamp = newData.timestamp;
                status = newData.status;
                sensorGroupId = newData.sensorGroupId;
            }
        }

        public void ResetDataCalculations()
        {
            // Resette dataCalculations
            dataProcess.ResetDataCalculations();
        }
    }
}