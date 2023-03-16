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
            buffer_text = string.Empty;
            firstRead = true;
            portStatus = PortStatus.Closed;
            processingDone = false;
        }

        public SerialPortData(SerialPortData serialPortData)
        {
            portName = serialPortData.portName;
            buffer_text = serialPortData.buffer_text;
            firstRead = serialPortData.firstRead;
            portStatus = serialPortData.portStatus;
            timestamp = serialPortData.timestamp;
            processingDone = false;
        }

        public void Set(SerialPortData serialPortData)
        {
            portName = serialPortData.portName;
            buffer_text = serialPortData.buffer_text;
            firstRead = serialPortData.firstRead;
            portStatus = serialPortData.portStatus;
            timestamp = serialPortData.timestamp;
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

        // Buffer - Binary - raw data as read from the port
        private string _buffer_binary { get; set; }
        public string buffer_binary
        {
            get
            {
                return _buffer_binary;
            }
            set
            {
                _buffer_binary = value;
                OnPropertyChanged();
            }
        }

        // Buffer - Text - raw data as read from the port
        private string _buffer_text { get; set; }
        public string buffer_text
        {
            get
            {
                return _buffer_text;
            }
            set
            {
                _buffer_text = value;
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

        // Ferdig prosessert
        public bool processingDone { get; set; }

        // Variabel oppdatert
        // Dersom navn ikke settes brukes kallende medlem sitt navn
        protected virtual void OnPropertyChanged([CallerMemberName] string name = null)
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
