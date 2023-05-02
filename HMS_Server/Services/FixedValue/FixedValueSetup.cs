using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;

namespace HMS_Server
{
    public class FixedValueSetup : INotifyPropertyChanged
    {
        // Change notification
        public event PropertyChangedEventHandler PropertyChanged;

        // Fixed value timer
        private System.Timers.Timer timer;

        // Konstruktør
        /////////////////////////////////////////////////////////////////////////////
        public FixedValueSetup()
        {
            id = 0;
            frequency = 0;
            value = string.Empty;
            timestamp = DateTime.MinValue;
            portStatus = PortStatus.Closed;
        }

        // Konstruktør
        /////////////////////////////////////////////////////////////////////////////
        public FixedValueSetup(FixedValueSetup fv)
        {
            this.Set(fv);
        }

        public FixedValueSetup(SensorData fv)
        {
            this.Set(fv);
        }

        // Set
        /////////////////////////////////////////////////////////////////////////////
        public void Set(FixedValueSetup fv)
        {
            id = fv.id;
            frequency = fv.frequency;
            value = fv.value;
            timestamp = fv.timestamp;
            portStatus = fv.portStatus;
        }

        public void Set(SensorData sensorData)
        {
            id = sensorData.id;
            frequency = sensorData.fixedValue.frequency;
            value = sensorData.fixedValue.value;
            timestamp = sensorData.fixedValue.timestamp;
            portStatus = sensorData.fixedValue.portStatus;
        }

        // ID
        /////////////////////////////////////////////////////////////////////////////
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

        // Frequency
        /////////////////////////////////////////////////////////////////////////////
        private double _frequency { get; set; }
        public double frequency
        {
            get
            {
                return _frequency;
            }
            set
            {
                _frequency = value;
                OnPropertyChanged();
            }
        }

        // Verdi
        /////////////////////////////////////////////////////////////////////////////
        private string _value { get; set; }
        public string value
        {
            get
            {
                return _value;
            }
            set
            {
                _value = value;
                OnPropertyChanged();
            }
        }

        // Time Stamp
        /////////////////////////////////////////////////////////////////////////////
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

        // Lese Status
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

        public void StartReader(ErrorHandler errorHandler, FixedValueDataRetrieval.FixedValueReaderCallback fixedValueReaderCallback)
        {
            FixedValueSetup fixedValueData = new FixedValueSetup();

            // Overføre ID
            fixedValueData.id = id;

            // Timer
            timer = new System.Timers.Timer(frequency);

            try
            {
                // Timer parametre
                timer.AutoReset = true;
                timer.Elapsed += runReader;

                // Starte timer
                timer.Start();

                void runReader(Object source, EventArgs e)
                {
                    Thread thread = new Thread(() => runReaderTask());
                    thread.IsBackground = true;
                    thread.Start();

                    void runReaderTask()
                    {
                        // Data & timestamp
                        // value er satt i init
                        fixedValueData.value = value;

                        timestamp = DateTime.UtcNow;
                        fixedValueData.timestamp = timestamp;

                        // Port Status
                        portStatus = PortStatus.Reading;
                        fixedValueData.portStatus = portStatus;

                        // Callback for å sende lest data linje tilbake for prosessering
                        if (fixedValueReaderCallback != null)
                        {
                            fixedValueReaderCallback(fixedValueData);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                errorHandler.Insert(
                    new ErrorMessage(
                        DateTime.UtcNow,
                        ErrorMessageType.FileReader,
                        ErrorMessageCategory.User,
                        string.Format("Fixed Value Error (StartReader):\n\n{0}", ex.Message)));
            }
        }

        public void StopReader()
        {
            if (timer != null)
                timer.Stop();
        }

        // Variabel oppdatert
        // Dersom navn ikke settes brukes kallende medlem sitt navn
        /////////////////////////////////////////////////////////////////////////////
        protected virtual void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
