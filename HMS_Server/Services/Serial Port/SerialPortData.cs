using System;

namespace HMS_Server
{
    class SerialPortData
    {
        public SerialPortData()
        {
            portName = string.Empty;
            data = string.Empty;
            firstRead = true;
            portStatus = PortStatus.Closed;
        }

        // Port Name
        public string portName { get; set; }

        // Data - raw data as read from the port
        public string data { get; set; }

        // Time Stamp
        public DateTime timestamp { get; set; }
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
        public PortStatus portStatus { get; set; }
        public string portStatusString
        {
            get
            {
                return portStatus.ToString();
            }
        }
    }

    public enum PortStatus
    {
        Closed,
        Open,
        Reading,
        NoData,
        Warning,
        OpenError
    }
}
