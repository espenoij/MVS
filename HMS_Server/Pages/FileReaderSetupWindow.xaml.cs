using Microsoft.Win32;
using System;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Telerik.Windows.Controls;

namespace HMS_Server
{
    /// <summary>
    /// Interaction logic for FileReaderSetupWindow.xaml
    /// </summary>
    public partial class FileReaderSetupWindow : RadWindow
    {
        // Configuration settings
        private Config config;

        // Error Handler
        ErrorHandler errorHandler;

        // Sensor Data List
        private RadObservableCollectionEx<SensorData> sensorDataList = new RadObservableCollectionEx<SensorData>();

        // Valgt sensor data
        private SensorData sensorData;

        private DispatcherTimer fileReaderTimer = new DispatcherTimer();

        // Data Processing
        private FileReaderProcessing process = new FileReaderProcessing();

        // Lister for visning
        private RadObservableCollectionEx<FileDataLine> fileDataLines = new RadObservableCollectionEx<FileDataLine>();
        private RadObservableCollectionEx<FileDataFields> fileDataFields = new RadObservableCollectionEx<FileDataFields>();
        private RadObservableCollectionEx<FileReaderData> calculatedDataItems = new RadObservableCollectionEx<FileReaderData>();

        // File stream reader
        private StreamReader fsReader;

        // View Model
        private FileReaderWindowVM fileReaderWindowVM;

        public FileReaderSetupWindow(SensorData sensorData, RadObservableCollectionEx<SensorData> sensorDataList, Config config, ErrorHandler errorHandler)
        {
            InitializeComponent();

            fileReaderWindowVM = new FileReaderWindowVM(config);
            DataContext = fileReaderWindowVM;

            // Lagre valgt sensor data
            this.sensorData = sensorData;

            // Sensor Data List
            this.sensorDataList = sensorDataList;

            // Config
            this.config = config;

            // Error Handler
            this.errorHandler = errorHandler;

            // Initialisere application settings
            InitializeApplicationSettings();
        }

        // Initialiserer
        private void InitializeApplicationSettings()
        {
            InitializeFileReader();

            InitBasicInformation();
            InitFilePath();
            InitializeFileReaderControls();

            InitializeDataFieldSplit();
            InitializeDataCalculations();
        }

        private void InitializeFileReader()
        {
            // Binding for data listviews
            lvDataLine.ItemsSource = fileDataLines;
            lvDataField.ItemsSource = fileDataFields;

            fileReaderTimer.Tick += runFileReaderTimer;

            void runFileReaderTimer(object sender, EventArgs e)
            {
                // Sjekke at vi ikke er kommet til end of file
                if (!fsReader.EndOfStream)
                {
                    // Lese en linje fra fil
                    string dataLine = fsReader.ReadLine();

                    // Sende leste data til skjermutskrift
                    if (!string.IsNullOrEmpty(dataLine))
                    {
                        // Vise data linje på skjerm
                        DisplayDataLines(dataLine);

                        // Dele data linje opp i individuelle data felt
                        FileDataFields dataFields =  process.SplitDataLine(dataLine);

                        // Vise data felt på skjerm
                        DisplayDataFields(dataFields);
                    }
                }
            }
        }

        private void InitBasicInformation()
        {
            // Basic Information
            lbSensorID.Content = sensorData.id.ToString();
            lbSensorName.Content = sensorData.name;
            lbSensorType.Content = sensorData.type.ToString();
        }

        private void InitFilePath()
        {
            // File Folder
            fileReaderWindowVM.fileFolder = sensorData.fileReader.fileFolder;
            fileReaderWindowVM.fileName = sensorData.fileReader.fileName;
        }

        private void InitializeFileReaderControls()
        {
            // Status Felt
            ///////////////////////////////////////////////////////////
            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(200);
            timer.Tick += timer_Tick;
            timer.Start();

            void timer_Tick(object sender, EventArgs e)
            {
                if (fileReaderTimer.IsEnabled)
                {
                    bFileReaderStatus.Background = (Brush)this.FindResource("ColorGreen");
                }
                else
                {
                    bFileReaderStatus.Background = (Brush)this.FindResource("ColorRed");
                }
            }
        }

        private void InitializeDataFieldSplit()
        {
            // Delimiter
            tbFileReaderDelimiter.Text = sensorData.fileReader.delimiter;
            process.delimiter = sensorData.fileReader.delimiter;

            // Fixed Position Data
            if (sensorData.fileReader.fixedPosData)
            {
                cboFileReaderFixedPositionData.IsChecked = true;
                process.fixedPosData = true;
            }
            else
            {
                cboFileReaderFixedPositionData.IsChecked = false;
                process.fixedPosData = false;
            }

            tbFileReaderFixedPositionDataStart.Text = sensorData.fileReader.fixedPosStart.ToString();
            process.fixedPosStart = sensorData.fileReader.fixedPosStart;

            tbFileReaderFixedPositionDataTotal.Text = sensorData.fileReader.fixedPosTotal.ToString();
            process.fixedPosTotal = sensorData.fileReader.fixedPosTotal;
        }

        private void InitializeDataCalculations()
        {
            try
            {
                // Binding for listviews
                lvCalculatedData.ItemsSource = calculatedDataItems;

                // Data Calculations 1
                // Prosesseringstyper
                foreach (CalculationType value in Enum.GetValues(typeof(CalculationType)))
                    cboCalculationType1.Items.Add(value.GetDescription());

                cboCalculationType1.Text = sensorData.fileReader.calculationSetup[0].type.GetDescription();
                cboCalculationType1.SelectedIndex = (int)sensorData.fileReader.calculationSetup[0].type;

                // Parameter
                tbCalculationParameter1.Text = sensorData.fileReader.calculationSetup[0].parameter.ToString();

                // Data Calculations 2
                // Prosesseringstyper
                foreach (CalculationType value in Enum.GetValues(typeof(CalculationType)))
                    cboCalculationType2.Items.Add(value.GetDescription());

                cboCalculationType2.Text = sensorData.fileReader.calculationSetup[1].type.GetDescription();
                cboCalculationType2.SelectedIndex = (int)sensorData.fileReader.calculationSetup[1].type;

                // Parameter
                tbCalculationParameter2.Text = sensorData.fileReader.calculationSetup[1].parameter.ToString();

                // Data Calculations 3
                // Prosesseringstyper
                foreach (CalculationType value in Enum.GetValues(typeof(CalculationType)))
                    cboCalculationType3.Items.Add(value.GetDescription());

                cboCalculationType3.Text = sensorData.fileReader.calculationSetup[2].type.GetDescription();
                cboCalculationType3.SelectedIndex = (int)sensorData.fileReader.calculationSetup[2].type;

                // Parameter
                tbCalculationParameter3.Text = sensorData.fileReader.calculationSetup[2].parameter.ToString();

                // Data Calculations 4
                // Prosesseringstyper
                foreach (CalculationType value in Enum.GetValues(typeof(CalculationType)))
                    cboCalculationType4.Items.Add(value.GetDescription());

                cboCalculationType4.Text = sensorData.fileReader.calculationSetup[3].type.GetDescription();
                cboCalculationType4.SelectedIndex = (int)sensorData.fileReader.calculationSetup[3].type;

                // Parameter
                tbCalculationParameter4.Text = sensorData.fileReader.calculationSetup[3].parameter.ToString();
            }
            catch (Exception ex)
            {
                RadWindow.Alert(string.Format("InitializeDataProcessing\n\n{0}", TextHelper.Wrap(ex.Message)));
            }
        }

        private void FileReader_Start()
        {
            // Filen vi skal lese: Opprette streamreader objekt basert på file path
            fsReader = new StreamReader(sensorData.fileReader.filePath);

            // Starter reader
            fileReaderTimer.Interval = TimeSpan.FromMilliseconds(fileReaderWindowVM.fileReadFrequency);
            fileReaderTimer.Start();

            // Status
            fileReadingStatus.Text = "File open for reading...";

            // Stenge tilgang til settings
            tbFileFolder.IsEnabled = false;
            btnFilePath.IsEnabled = false;
            tbFileReadFrequency.IsEnabled = false;

            btnFileReaderStart.IsEnabled = false;
            btnFileReaderStop.IsEnabled = true;
        }

        private void FileReader_Stop()
        {
            // Lukke file stream
            fsReader?.Close();

            // Stopper reader
            fileReaderTimer.Stop();

            // Status
            fileReadingStatus.Text = "File closed.";

            // Åpne tilgang til settings
            tbFileFolder.IsEnabled = true;
            btnFilePath.IsEnabled = true;
            tbFileReadFrequency.IsEnabled = true;

            btnFileReaderStart.IsEnabled = true;
            btnFileReaderStop.IsEnabled = false;
        }

        private void DisplayDataLines(string dataString)
        {
            // Legg ut data i raw data listview
            fileDataLines.Add(new FileDataLine() { data = dataString });

            // Begrense data på skjerm
            while (fileDataLines.Count > fileReaderWindowVM.totalDataLines)
                fileDataLines.RemoveAt(0);

            // Status
            fileReadingStatus.Text = "Reading data from file...";
        }

        private void DisplayDataFields(FileDataFields dataFields)
        {
            // Legg ut data i raw data listview
            fileDataFields.Add(dataFields);

            // Begrense data på skjerm
            while (fileDataFields.Count > fileReaderWindowVM.totalDataLines)
                fileDataFields.RemoveAt(0);
        }

        private void btnFileReaderStart_Click(object sender, RoutedEventArgs e)
        {
            FileReader_Start();
        }

        private void btnFileReaderStop_Click(object sender, RoutedEventArgs e)
        {
            FileReader_Stop();
        }

        private void btnFilePath_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                DefaultExt = "csv",
                Filter = "CSV files|*.csv|All Files (*.*)|*.*",
            };
            if (dialog.ShowDialog() == true)
            {
                fileReaderWindowVM.fileFolder = Path.GetDirectoryName(dialog.FileName);
                fileReaderWindowVM.fileName = Path.GetFileName(dialog.FileName);

                sensorData.fileReader.fileFolder = fileReaderWindowVM.fileFolder;
                sensorData.fileReader.fileName = fileReaderWindowVM.fileName;
            }
        }

        private void chkClearAllData_Click(object sender, RoutedEventArgs e)
        {
            // Sletter lister
            fileDataLines.Clear();
        }

        private void tbTotalDataLines_LostFocus(object sender, RoutedEventArgs e)
        {
            tbTotalDataLines_Update(sender);
        }

        private void tbTotalDataLines_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                tbTotalDataLines_Update(sender);
                Keyboard.ClearFocus();
            }
        }

        private void tbTotalDataLines_Update(object sender)
        {
            fileReaderWindowVM.totalDataLinesString = (sender as TextBox).Text;
        }

        private void tbFileReadFrequency_LostFocus(object sender, RoutedEventArgs e)
        {
            tbFileReadFrequency_Update(sender);
        }

        private void tbFileReadFrequency_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                tbFileReadFrequency_Update(sender);
                Keyboard.ClearFocus();
            }
        }

        private void tbFileReadFrequency_Update(object sender)
        {
            // Sjekk av input
            DataValidation.Double(
                    (sender as TextBox).Text,
                    Constants.FileReadFreqMin,
                    Constants.FileReadFreqMax,
                    Constants.FileReadFreqDefault,
                    out double validatedInput);

            fileReaderWindowVM.fileReadFrequency = validatedInput;
        }


        private void cboFixedPositionData_Checked(object sender, RoutedEventArgs e)
        {
            process.fixedPosData = true;

            // Lagre ny setting
            sensorData.fileReader.fixedPosData = true;
        }

        private void cboFixedPositionData_Unchecked(object sender, RoutedEventArgs e)
        {
            process.fixedPosData = false;

            // Lagre ny setting
            sensorData.fileReader.fixedPosData = false;
        }

        private void tbFixedPositionDataStart_LostFocus(object sender, RoutedEventArgs e)
        {
            tbFixedPositionDataStart_Update(sender);
        }

        private void tbFixedPositionDataStart_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                tbFixedPositionDataStart_Update(sender);
                Keyboard.ClearFocus();
            }
        }

        private void tbFixedPositionDataStart_Update(object sender)
        {
            try
            {
                if (string.IsNullOrEmpty((sender as TextBox).Text))
                    process.fixedPosStart = 0;
                else
                    process.fixedPosStart = Convert.ToInt32((sender as TextBox).Text);

                // Lagre ny setting
                sensorData.fileReader.fixedPosStart = process.fixedPosStart;
            }
            catch (Exception ex)
            {
                RadWindow.Alert(string.Format("Input must be numeric.\n\n{0}", TextHelper.Wrap(ex.Message)));
            }
        }

        private void tbFixedPositionDataTotal_LostFocus(object sender, RoutedEventArgs e)
        {
            tbFixedPositionDataTotal_Update(sender);
        }

        private void tbFixedPositionDataTotal_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                tbFixedPositionDataTotal_Update(sender);
                Keyboard.ClearFocus();
            }
        }

        private void tbFixedPositionDataTotal_Update(object sender)
        {
            try
            {
                if (string.IsNullOrEmpty((sender as TextBox).Text))
                    process.fixedPosTotal = 0;
                else
                    process.fixedPosTotal = Convert.ToInt32((sender as TextBox).Text);

                // Lagre ny setting
                sensorData.fileReader.fixedPosTotal = process.fixedPosTotal;
            }
            catch (Exception ex)
            {
                RadWindow.Alert(string.Format("Input must be numeric.\n\n{0}", TextHelper.Wrap(ex.Message)));
            }
        }

        private void tbFileReaderDelimiter_LostFocus(object sender, RoutedEventArgs e)
        {
            tbFileReaderDelimiter_Update(sender);
        }

        private void tbFileReaderDelimiter_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                tbFileReaderDelimiter_Update(sender);
                Keyboard.ClearFocus();
            }
        }

        private void tbFileReaderDelimiter_Update(object sender)
        {
            DataValidation.StringLength(
                (sender as TextBox).Text,
                1,
                Constants.FileReaderDelimiterDefault,
                out string validatedInput);

            (sender as TextBox).Text = validatedInput;

            // Lagre ny setting
            sensorData.fileReader.delimiter = validatedInput;

            process.delimiter = validatedInput;
        }

        private void RadWindow_Closed(object sender, WindowClosedEventArgs e)
        {
            FileReader_Stop();
        }
    }
}
