using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows.Threading;

namespace MVS
{
    public class FileReaderSetup : INotifyPropertyChanged
    {
        //// TEST
        //private int counter1 = 0;
        //private int counter2 = 0;
        //private int counter3 = 0;

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
                return string.Format("{0}\\{1}", _fileFolder, _fileName);
            }
        }

        // Lese frekvens
        public double readFrequency { get; set; }

        // Start-tidspunkt for lesing
        private DateTime readStartTime;

        // Totalt antall leste linjer
        private double linesRead;

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
        private DispatcherTimer timer;

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
                if (_timestamp.Ticks != 0)
                    return _timestamp.ToString(Constants.TimestampFormat, Constants.cultureInfo);
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
                return _portStatus.ToString();
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

        public void StartReader(Config config, ErrorHandler errorHandler, FileReaderDataRetrieval.FileReaderCallback fileReaderCallback)
        {
            FileReaderSetup fileReaderData = new FileReaderSetup();

            // Tidspunktet vi starter lesing fra fil
            readStartTime = DateTime.UtcNow;

            // Resette lines read
            linesRead = 0;

            // Overføre fil katalog og navn
            fileReaderData.fileFolder = fileFolder;
            fileReaderData.fileName = fileName;

            // Timer
            timer = new DispatcherTimer();

            try
            {
                FileStream fs = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                BufferedStream bs = new BufferedStream(fs);
                StreamReader sr = new StreamReader(bs);

                // Timer parametre
                timer.Interval = TimeSpan.FromMilliseconds(config.ReadWithDefault(ConfigKey.HMSProcessingFrequency, Constants.HMSProcessingFrequencyDefault));
                timer.Tick += runReader;

                // Starte timer
                timer.Start();

                void runReader(Object source, EventArgs e)
                {
                    Thread thread = new Thread(() => runReaderTask());
                    thread.IsBackground = true;
                    thread.Start();

                    void runReaderTask()
                    {
                        bool lineReadComplete = false;

                        // Finne ut om vi har kommet så langt i tid at vi må lese en ny linje fra fil
                        double expectedLinesRead = (DateTime.UtcNow - readStartTime).TotalMilliseconds / readFrequency;
                        while (linesRead < expectedLinesRead && !lineReadComplete)
                        {
                            if (sr != null)
                            {
                                // Sjekke at vi ikke er kommet til end of file
                                if (!sr.EndOfStream)
                                {
                                    // Lese en linje fra fil
                                    dataLine = sr.ReadLine();
                                    if (dataLine != null)
                                    {
                                        fileReaderData.dataLine = dataLine;

                                        // Sette timestamp
                                        timestamp = readStartTime.AddMilliseconds(linesRead * readFrequency);

                                        // NB! Linjen over er det som ble brukt under CAP godkjenning.
                                        // Den setter timestamp til det tidspunktet linje/data skulle ha vært lest.
                                        // Dette gir større nøyaktighet ifht innlesing av data til korrekt tidspunkt.
                                        // Problemet er at i noen tilfeller fyrer ikke timer så often som den er satt til.
                                        // Da blir data satt med for gammle timestamp og som så blir merket med timeout
                                        // og dermed blir forkastet.
                                        // Løsning: Problemet ble løst ved å lage en while loop her istedet for if.

                                        fileReaderData.timestamp = timestamp;

                                        //// Test
                                        //Debug.WriteLine(string.Format("timestamp: {0} - Now: {1}", timestamp, DateTime.UtcNow));

                                        // Øke antall leste linjer
                                        linesRead += 1;

                                        // Status
                                        portStatus = PortStatus.Reading;
                                    }
                                    else
                                    {
                                        // Status
                                        portStatus = PortStatus.NoData;

                                        lineReadComplete = true;
                                    }
                                }
                                else
                                {
                                    // Status
                                    portStatus = PortStatus.EndOfFile;

                                    // Stoppe lesing av fil
                                    lineReadComplete = true;
                                    timer.Stop();
                                }
                            }
                            else
                            {
                                // Status
                                portStatus = PortStatus.OpenError;

                                lineReadComplete = true;
                            }

                            // Status
                            fileReaderData.portStatus = portStatus;

                            //// TEST
                            //counter1++;

                            // Callback for å sende lest data linje tilbake for prosessering
                            if (fileReaderCallback != null)
                            {
                                //// TEST
                                //counter2++;

                                fileReaderCallback(fileReaderData);
                            }

                            //// TEST
                            //errorHandler.Insert(
                            //    new ErrorMessage(
                            //        DateTime.UtcNow,
                            //        ErrorMessageType.FileReader,
                            //        ErrorMessageCategory.AdminUser,
                            //        string.Format("1 expectedLinesRead: {0}, linesRead : {1} --- Elapsed Time: {2}", expectedLinesRead, linesRead, (DateTime.UtcNow - readStartTime).TotalSeconds)));
                            //Debug.WriteLine(string.Format("1 expectedLinesRead: {0}, linesRead : {1} --- Elapsed Time: {2}", (int)expectedLinesRead, linesRead, (DateTime.UtcNow - readStartTime).TotalSeconds));
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
        protected virtual void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
