using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Telerik.Windows.Controls;
using Telerik.Windows.Data;

namespace HMS_Server
{
    /// <summary>
    /// Interaction logic for FileReaderSetupWindow.xaml
    /// </summary>
    public partial class FileReaderSetupWindow : RadWindow
    {
        // Error Handler
        private ErrorHandler errorHandler;

        // Admin Settings
        private AdminSettingsVM adminSettingsVM;

        // Valgt sensor data
        private SensorData sensorData;

        private DispatcherTimer fileReaderTimer = new DispatcherTimer();

        // Data Processing
        private FileReaderProcessing process = new FileReaderProcessing();
        private List<DataCalculations> dataCalculations = new List<DataCalculations>();

        // Lister for visning
        private RadObservableCollection<FileDataLine> fileDataLines = new RadObservableCollection<FileDataLine>();
        private RadObservableCollection<FileDataFields> fileDataFields = new RadObservableCollection<FileDataFields>();
        private RadObservableCollection<SelectedDataField> selectedDataList = new RadObservableCollection<SelectedDataField>();
        private RadObservableCollection<CalculatedData> calculatedDataList = new RadObservableCollection<CalculatedData>();

        // File stream reader
        private StreamReader fsReader;

        // View Model
        private FileReaderWindowVM fileReaderWindowVM;

        public FileReaderSetupWindow(SensorData sensorData, Config config, ErrorHandler errorHandler, AdminSettingsVM adminSettingsVM)
        {
            InitializeComponent();

            fileReaderWindowVM = new FileReaderWindowVM(config);
            DataContext = fileReaderWindowVM;

            // Lagre valgt sensor data
            this.sensorData = sensorData;

            // Error Handler
            this.errorHandler = errorHandler;

            // Admin Settings
            this.adminSettingsVM = adminSettingsVM;

            // Initialisere application settings
            InitializeApplicationSettings();
        }

        // Initialiserer
        private void InitializeApplicationSettings()
        {
            InitFileReader();

            InitBasicInformation();
            InitSettings();
            InitFileReaderControls();

            InitDataFieldSplit();
            InitDataSelection();
            InitDataCalculations();
        }

        private void InitFileReader()
        {
            fileReaderTimer.Tick += runFileReaderTimer;

            void runFileReaderTimer(object sender, EventArgs e)
            {
                // Sjekke at vi ikke er kommet til end of file
                if (!fsReader.EndOfStream)
                {
                    // Trinn 1: Lese en linje fra fil
                    //////////////////////////////////////////////////////////////////////////////
                    string dataLine = fsReader.ReadLine();

                    // Sende leste data til skjermutskrift
                    if (!string.IsNullOrEmpty(dataLine))
                    {
                        // Vise data linje på skjerm
                        DisplayDataLines(dataLine);

                        // Trinn 2: Dele data linje opp i individuelle data felt
                        //////////////////////////////////////////////////////////////////////////////
                        FileDataFields dataFields =  process.SplitDataLine(dataLine);

                        // Vise data felt på skjerm
                        DisplayDataFields(dataFields);

                        // Trinn 3: Finne valgt datafelt
                        //////////////////////////////////////////////////////////////////////////////
                        SelectedDataField selectedData = process.FindSelectedData(dataFields);

                        // Vise valgt data på skjerm
                        DisplaySelectedDataField(selectedData);

                        // Trinn 4: Utføre kalkulasjoner på utvalgt data
                        //////////////////////////////////////////////////////////////////////////////
                        CalculatedData calculatedData = process.ApplyCalculationsToSelectedData(selectedData, dataCalculations, DateTime.UtcNow, errorHandler, ErrorMessageCategory.Admin, adminSettingsVM);

                        // Vise de prosesserte dataene
                        DisplayProcessedData(calculatedData);
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

        private void InitSettings()
        {
            // File Folder
            fileReaderWindowVM.fileFolder = sensorData.fileReader.fileFolder;
            fileReaderWindowVM.fileName = sensorData.fileReader.fileName;
            fileReaderWindowVM.readFrequency = sensorData.fileReader.readFrequency;
        }

        private void InitFileReaderControls()
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

        private void InitDataFieldSplit()
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

        private void InitDataSelection()
        {
            // Binding for data listviews
            lvDataLine.ItemsSource = fileDataLines;
            lvDataField.ItemsSource = fileDataFields;
            lvSelectedData.ItemsSource = selectedDataList;

            // Data Field 
            for (int i = 0; i < Constants.PacketDataFields; i++)
                cboSelectedDataField.Items.Add(i.ToString());

            cboSelectedDataField.Text = TextHelper.UnescapeSpace(sensorData.fileReader.dataField);

            if (cboSelectedDataField.Text != string.Empty)
                cboSelectedDataField.SelectedIndex = int.Parse(cboSelectedDataField.Text);
            else
                cboSelectedDataField.SelectedIndex = 0;

            // Decimal Separator
            foreach (var value in Enum.GetValues(typeof(DecimalSeparator)))
                cboDecimalSeparator.Items.Add(value.ToString());

            cboDecimalSeparator.Text = sensorData.fileReader.decimalSeparator.ToString();
            process.decimalSeparator = sensorData.fileReader.decimalSeparator;

            // Lese selected data auto extraction setting
            cboSelectedDataAutoExtract.IsChecked = sensorData.fileReader.autoExtractValue;
            process.autoExtractValue = sensorData.fileReader.autoExtractValue;
        }

        private void InitDataCalculations()
        {
            try
            {
                // Binding for listviews
                lvSelectedData2.ItemsSource = selectedDataList;
                lvCalculatedData.ItemsSource = calculatedDataList;

                for (int i = 0; i < Constants.DataCalculationSteps; i++)
                {
                    dataCalculations.Add(new DataCalculations()
                    {
                        type = sensorData.fileReader.calculationSetup[i].type,
                        parameter = sensorData.fileReader.calculationSetup[i].parameter,
                    });
                }

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

            // Starter reader dispatcher
            FileReaderDispatcher_Start();

            // Status
            fileReadingStatus.Text = "File open for reading...";

            // Stenge tilgang til settings
            tbFileFolder.IsEnabled = false;
            tbFileName.IsEnabled = false;
            btnFilePath.IsEnabled = false;
            tbFileReadFrequency.IsEnabled = false;

            btnFileReaderStart.IsEnabled = false;
            btnFileReaderStop.IsEnabled = true;
        }

        private void FileReader_Stop()
        {
            // Lukke file stream
            fsReader?.Close();

            // Stopper reader dispatcher
            FileReaderDispatcher_Stop();

            // Status
            fileReadingStatus.Text = "File closed.";

            // Resette dataCalculations
            foreach (var item in dataCalculations)
                item.Reset();

            // Åpne tilgang til settings
            tbFileFolder.IsEnabled = true;
            tbFileName.IsEnabled = true;
            btnFilePath.IsEnabled = true;
            tbFileReadFrequency.IsEnabled = true;

            btnFileReaderStart.IsEnabled = true;
            btnFileReaderStop.IsEnabled = false;
        }

        private void FileReaderDispatcher_Start()
        {
            // Gjør dette trikset med Interval her for å få dispatchertimer til å kjøre med en gang.
            // Ellers venter den til intervallet er gått før den kjører første gang.
            fileReaderTimer.Interval = TimeSpan.FromMilliseconds(0);
            fileReaderTimer.Start();
            fileReaderTimer.Interval = TimeSpan.FromMilliseconds(fileReaderWindowVM.readFrequency);
        }

        private void FileReaderDispatcher_Stop()
        {
            fileReaderTimer.Stop();
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
            // Legg ut data
            fileDataFields.Add(dataFields);

            // Begrense data på skjerm
            while (fileDataFields.Count > fileReaderWindowVM.totalDataLines)
                fileDataFields.RemoveAt(0);
        }

        private void DisplaySelectedDataField(SelectedDataField selectedData)
        {
            // Legg ut data
            selectedDataList.Add(selectedData);

            // Begrense data på skjerm
            while (selectedDataList.Count > fileReaderWindowVM.totalDataLines)
                selectedDataList.RemoveAt(0);
        }

        private void DisplayProcessedData(CalculatedData calculatedData)
        {
            // Legg ut data
            calculatedDataList.Add(calculatedData);

            // Begrense data på skjerm
            while (calculatedDataList.Count > fileReaderWindowVM.totalDataLines)
                calculatedDataList.RemoveAt(0);
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
            fileDataFields.Clear();
            selectedDataList.Clear();
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

            fileReaderWindowVM.readFrequency = validatedInput;
            sensorData.fileReader.readFrequency = validatedInput;
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

        private void cboSelectedDataField_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            process.dataField = cboSelectedDataField.SelectedIndex;

            // Lagre ny setting
            sensorData.fileReader.dataField = process.dataField.ToString();
        }

        private void cboDecimalSeparator_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            process.decimalSeparator = (DecimalSeparator)Enum.Parse(typeof(DecimalSeparator), cboDecimalSeparator.SelectedItem.ToString());

            // Lagre ny setting
            sensorData.fileReader.decimalSeparator = process.decimalSeparator;
        }

        private void cboSelectedDataAutoExtract_Checked(object sender, RoutedEventArgs e)
        {
            process.autoExtractValue = true;

            // Lagre ny setting
            sensorData.fileReader.autoExtractValue = true;
        }

        private void cboSelectedDataAutoExtract_Unchecked(object sender, RoutedEventArgs e)
        {
            process.autoExtractValue = false;

            // Lagre ny setting
            sensorData.fileReader.autoExtractValue = false;
        }

        private void cboCalculationType1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Lagre ny setting
            sensorData.fileReader.calculationSetup[0].type = EnumExtension.GetEnumValueFromDescription<CalculationType>(cboCalculationType1.Text);
            dataCalculations[0].type = sensorData.fileReader.calculationSetup[0].type;
        }

        private void tbCalculationParameter1_LostFocus(object sender, RoutedEventArgs e)
        {
            tbCalculationParameter1_Update();
        }

        private void tbCalculationParameter1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                tbCalculationParameter1_Update();
                Keyboard.ClearFocus();
            }
        }

        private void tbCalculationParameter1_Update()
        {
            // Lagre ny setting
            if (double.TryParse(tbCalculationParameter1.Text, Constants.numberStyle, Constants.cultureInfo, out double param))
                sensorData.fileReader.calculationSetup[0].parameter = param;
            else
                sensorData.fileReader.calculationSetup[0].parameter = 0;

            dataCalculations[0].parameter = sensorData.fileReader.calculationSetup[0].parameter;

            // Skrive data (som kan være korrigert) tilbake til tekstfeltet
            tbCalculationParameter1.Text = sensorData.fileReader.calculationSetup[0].parameter.ToString();
        }

        private void cboCalculationType2_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Lagre ny setting
            sensorData.fileReader.calculationSetup[1].type = EnumExtension.GetEnumValueFromDescription<CalculationType>(cboCalculationType2.Text);
            dataCalculations[1].type = sensorData.fileReader.calculationSetup[1].type;
        }

        private void tbCalculationParameter2_LostFocus(object sender, RoutedEventArgs e)
        {
            tbCalculationParameter2_Update();
        }

        private void tbCalculationParameter2_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                tbCalculationParameter2_Update();
                Keyboard.ClearFocus();
            }
        }

        private void tbCalculationParameter2_Update()
        {
            // Lagre ny setting
            if (double.TryParse(tbCalculationParameter2.Text, Constants.numberStyle, Constants.cultureInfo, out double param))
                sensorData.fileReader.calculationSetup[1].parameter = param;
            else
                sensorData.fileReader.calculationSetup[1].parameter = 0;

            dataCalculations[1].parameter = sensorData.fileReader.calculationSetup[1].parameter;

            // Skrive data (som kan være korrigert) tilbake til tekstfeltet
            tbCalculationParameter2.Text = sensorData.fileReader.calculationSetup[1].parameter.ToString();

        }

        private void cboCalculationType3_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Lagre ny setting
            sensorData.fileReader.calculationSetup[2].type = EnumExtension.GetEnumValueFromDescription<CalculationType>(cboCalculationType3.Text);
            dataCalculations[2].type = sensorData.fileReader.calculationSetup[2].type;
        }

        private void tbCalculationParameter3_LostFocus(object sender, RoutedEventArgs e)
        {
            tbCalculationParameter3_Update();
        }

        private void tbCalculationParameter3_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                tbCalculationParameter3_Update();
                Keyboard.ClearFocus();
            }
        }

        private void tbCalculationParameter3_Update()
        {
            // Lagre ny setting
            if (double.TryParse(tbCalculationParameter3.Text, Constants.numberStyle, Constants.cultureInfo, out double param))
                sensorData.fileReader.calculationSetup[2].parameter = param;
            else
                sensorData.fileReader.calculationSetup[2].parameter = 0;

            dataCalculations[2].parameter = sensorData.fileReader.calculationSetup[2].parameter;

            // Skrive data (som kan være korrigert) tilbake til tekstfeltet
            tbCalculationParameter3.Text = sensorData.fileReader.calculationSetup[2].parameter.ToString();
        }

        private void cboCalculationType4_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Lagre ny setting
            sensorData.fileReader.calculationSetup[3].type = EnumExtension.GetEnumValueFromDescription<CalculationType>(cboCalculationType4.Text);
            dataCalculations[3].type = sensorData.fileReader.calculationSetup[3].type;
        }

        private void tbCalculationParameter4_LostFocus(object sender, RoutedEventArgs e)
        {
            tbCalculationParameter4_Update();
        }

        private void tbCalculationParameter4_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                tbCalculationParameter4_Update();
                Keyboard.ClearFocus();
            }
        }

        private void tbCalculationParameter4_Update()
        {
            // Lagre ny setting
            if (double.TryParse(tbCalculationParameter4.Text, Constants.numberStyle, Constants.cultureInfo, out double param))
                sensorData.fileReader.calculationSetup[3].parameter = param;
            else
                sensorData.fileReader.calculationSetup[3].parameter = 0;

            dataCalculations[3].parameter = sensorData.fileReader.calculationSetup[3].parameter;

            // Skrive data (som kan være korrigert) tilbake til tekstfeltet
            tbCalculationParameter4.Text = sensorData.fileReader.calculationSetup[3].parameter.ToString();
        }
    }
}
