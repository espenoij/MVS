using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Timers;

namespace HMS_Server
{
    public class FileReaderSetup : INotifyPropertyChanged
    {
        // Change notification
        public event PropertyChangedEventHandler PropertyChanged;

        // Katalog lokasjon
        private string _fileFolder { get; set; }
        public string fileFolder
        {
            get
            {
                return _fileFolder;
            }
            set
            {
                _fileFolder = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(filePath));
            }
        }

        // Filnavn
        private string _fileName { get; set; }
        public string fileName
        {
            get
            {
                return _fileName;
            }
            set
            {
                _fileName = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(filePath));
            }
        }

        // Path
        public string filePath
        {
            get
            {
                return string.Format("{0}\\{1}", fileFolder, fileName);
            }
        }

        // Lese frekvens
        public double readFrequency { get; set; }

        // Data Line delimiter
        public string delimiter { get; set; }

        // Fixed position data
        public bool fixedPosData { get; set; }
        public int fixedPosStart { get; set; }
        public int fixedPosTotal { get; set; }

        // Selected Data field
        public string dataField { get; set; }
        public DecimalSeparator decimalSeparator { get; set; }
        public bool autoExtractValue { get; set; }

        // Data Calculations Setup
        public List<CalculationSetup> calculationSetup { get; set; }

        // Reader timer
        private System.Timers.Timer timer;

        // Data linje lest fra fil
        private string _dataLine { get; set; }
        public string dataLine
        {
            get
            {
                return _dataLine;
            }
            set
            {
                _dataLine = value;
                OnPropertyChanged();
            }
        }

        // Time Stamp
        public DateTime _timestamp { get; set; }
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

        // Konstruktør
        public FileReaderSetup()
        {
            fileFolder = string.Empty;
            fileName = string.Empty;
            readFrequency = Constants.FileReadFreqDefault;
            delimiter = Constants.FileReaderDelimiterDefault;
            fixedPosData = false;
            fixedPosStart = 0;
            fixedPosTotal = 0;
            dataField = string.Empty;
            decimalSeparator = DecimalSeparator.Point;
            autoExtractValue = false;

            calculationSetup = new List<CalculationSetup>();
            for (int i = 0; i < Constants.DataCalculationSteps; i++)
                calculationSetup.Add(new CalculationSetup());
        }

        // Konstruktør
        public FileReaderSetup(FileReaderSetup frs)
        {
            fileFolder = frs.fileFolder;
            fileName = frs.fileName;
            readFrequency = frs.readFrequency;

            delimiter = frs.delimiter;
            fixedPosData = frs.fixedPosData;
            fixedPosStart = frs.fixedPosStart;
            fixedPosTotal = frs.fixedPosTotal;

            dataField = frs.dataField;
            decimalSeparator = frs.decimalSeparator;
            autoExtractValue = frs.autoExtractValue;

            calculationSetup = new List<CalculationSetup>();
            foreach (var item in frs.calculationSetup)
                calculationSetup.Add(new CalculationSetup(item));
        }

        public void StartReader(ErrorHandler errorHandler, FileReaderDataRetrieval.FileReaderCallback fileReaderCallback)
        {
            FileReaderSetup fileReaderData = new FileReaderSetup();

            // Overføre fil katalog og navn
            fileReaderData.fileFolder = fileFolder;
            fileReaderData.fileName = fileName;

            // Timer
            timer = new System.Timers.Timer(readFrequency);

            try
            {
                StreamReader fsReader = new StreamReader(filePath);

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
                        if (fsReader != null)
                        {
                            // Sjekke at vi ikke er kommet til end of file
                            if (!fsReader.EndOfStream)
                            {
                                // Lese en linje fra fil
                                dataLine = fsReader.ReadLine();
                                fileReaderData.dataLine = dataLine;

                                // Sette timestamp
                                timestamp = DateTime.UtcNow;
                                fileReaderData.timestamp = timestamp;

                                // Status
                                portStatus = PortStatus.Reading;
                            }
                            else
                            {
                                // Status
                                portStatus = PortStatus.EndOfFile;
                            }
                        }
                        else
                        {
                            // Status
                            portStatus = PortStatus.OpenError;
                        }

                        // Status
                        fileReaderData.portStatus = portStatus;

                        // Callback for å sende lest data linje tilbake for prosessering
                        if (fileReaderCallback != null)
                            fileReaderCallback(fileReaderData);
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

                // Status
                portStatus = PortStatus.OpenError;

                // Status
                fileReaderData.portStatus = portStatus;

                // Callback for å sende lest data linje tilbake for prosessering
                if (fileReaderCallback != null)
                    fileReaderCallback(fileReaderData);
            }
        }

        public void StopReader()
        {
            if (timer != null)
                timer.Stop();
        }

        // Variabel oppdatert
        // Dersom navn ikke settes brukes kallende medlem sitt navn
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
