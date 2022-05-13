using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace HMS_Server
{
    public class SerialPortData : INotifyPropertyChanged
    {
        // Change notification
        public event PropertyChangedEventHandler PropertyChanged;

        public SerialPortData()
        {
            portName = string.Empty;
            data = string.Empty;
            firstRead = true;
            portStatus = PortStatus.Closed;
        }

        public SerialPortData(SerialPortData serialPortData)
        {
            portName = serialPortData.portName;
            data = serialPortData.data;
            firstRead = serialPortData.firstRead;
            portStatus = serialPortData.portStatus;
        }

        public void Set(SerialPortData serialPortData)
        {
            portName = serialPortData.portName;
            data = serialPortData.data;
            firstRead = serialPortData.firstRead;
            portStatus = serialPortData.portStatus;
        }

        // Port Name
        private string _portName { get; set; }
        public string portName
        {
            get
            {
                return _portName;
            }
            set
            {
                _portName = value;
                OnPropertyChanged();
            }
        }

        // Data - raw data as read from the port
        private string _data { get; set; }
        public string data
        {
            get
            {
                return _data;
            }
            set
            {
                _data = value;
                OnPropertyChanged();
            }
        }

        // Time Stamp
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
                if (timestamp.Ticks != 0)
                    return timestamp.ToString(Constants.TimestampFormat, Constants.cultureInfo);
                else
                    return Constants.TimestampNotSet;
            }
        }

        // Data i første lesing dumpes - gjøres fordi den kan være mye buffrede data som venter,
        // og disse kan overbelaste grensesnittet.
        public bool firstRead { get; set; }

        // Port Status
        public PortStatus _portStatus { get; set; }
        public PortStatus portStatus
        {
            get
            {
                return _portStatus;
            }
            set
            {
                _portStatus = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(portStatusString));
            }
        }
        public string portStatusString
        {
            get
            {
                return portStatus.ToString();
            }
        }

        // Variabel oppdatert
        // Dersom navn ikke settes brukes kallende medlem sitt navn
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    public enum PortStatus
    {
        Closed,
        Open,
        Reading,
        NoData,
        Warning,
        OpenError,
        EndOfFile
    }
}
