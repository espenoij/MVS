using Google.Protobuf.WellKnownTypes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows.Documents;

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
            frequency = 0;
            value = string.Empty;
        }

        // Konstruktør
        /////////////////////////////////////////////////////////////////////////////
        public FixedValueSetup(FixedValueSetup fv)
        {
            this.Set(fv);
        }

        // Set
        public void Set(FixedValueSetup fv)
        {
            frequency = fv.frequency;
            value = fv.value;
            timestamp = fv.timestamp;
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

        public void StartReader(Config config, ErrorHandler errorHandler, FixedValueDataRetrieval.FixedValueReaderCallback fixedValueReaderCallback)
        {
            FixedValueSetup fixedValueData = new FixedValueSetup();

            // Timer
            timer = new System.Timers.Timer(config.ReadWithDefault(ConfigKey.HMSProcessingFrequency, Constants.HMSProcessingFrequencyDefault));

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
                        fixedValueData.value = "0"; // Default verdi. Fixed value settes i prosessering.
                        fixedValueData.timestamp = DateTime.UtcNow;

                        // Status
                        fixedValueData.portStatus = PortStatus.Reading;

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
                        string.Format("File Reader Error (StartReader):\n\n{0}", ex.Message)));

                // Callback for å sende lest data linje tilbake for prosessering
                if (fixedValueReaderCallback != null)
                    fixedValueReaderCallback(fixedValueData);
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
