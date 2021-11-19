using Microsoft.Win32;
using System;
using System.IO;
using System.Windows;
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
        private ModbusCalculations process = new ModbusCalculations();

        // Lister for visning
        private RadObservableCollectionEx<ModbusData> rawDataItems = new RadObservableCollectionEx<ModbusData>();
        private RadObservableCollectionEx<ModbusData> selectedDataItems = new RadObservableCollectionEx<ModbusData>();
        private RadObservableCollectionEx<ModbusData> calculatedDataItems = new RadObservableCollectionEx<ModbusData>();

        // View Model
        private FileReaderWindowVM fileReaderWindowVM;

        public FileReaderSetupWindow(SensorData sensorData, RadObservableCollectionEx<SensorData> sensorDataList, Config config, ErrorHandler errorHandler)
        {
            InitializeComponent();

            fileReaderWindowVM = new FileReaderWindowVM();
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

        private void InitializeApplicationSettings()
        {
            // Initialiserer MODBUS leser
            InitializeFileReader();

            InitBasicInformation();

            // Initialisere Serial Port Configuration settings
            InitializeConnectionSettings();

            // Initialisere Slave Selection
            InitializeFileReaderSettings();

            // Initialize Data Selection
            InitializeDataSelectionSettings();

            // Initialisere Data Processing
            InitializeDataCalculations();
        }

        private void InitializeFileReader()
        {
            fileReaderTimer.Interval = TimeSpan.FromMilliseconds(sensorData.GetSaveFrequency(config));
            fileReaderTimer.Tick += runFileReaderTimer;

            void runFileReaderTimer(object sender, EventArgs e)
            {
            }
        }

        private void InitBasicInformation()
        {
            // Basic Information
            lbSensorID.Content = sensorData.id.ToString();
            lbSensorName.Content = sensorData.name;
            lbSensorType.Content = sensorData.type.ToString();
        }

        private void InitializeConnectionSettings()
        {
            // Binding for sample data listviews
            //lvModbusRawData.ItemsSource = rawDataItems;

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

        private void InitializeFileReaderSettings()
        {
        }

        private void InitializeDataSelectionSettings()
        {
            // Binding for sample data listviews
            lvSelectedData.ItemsSource = selectedDataItems;
            lvSelectedData2.ItemsSource = selectedDataItems;
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
            fileReaderTimer.Start();

            // Status
            sensorMonitorStatus.Text = "MODBUS connection open for reading...";
        }

        private void FileReader_Stop()
        {
            // Starter reader
            fileReaderTimer.Stop();

            // Status
            sensorMonitorStatus.Text = "MODBUS connection closed.";
        }

        private void btnFileReaderStart_Click(object sender, RoutedEventArgs e)
        {
            FileReader_Start();
        }

        private void btnFileReaderStop_Click(object sender, RoutedEventArgs e)
        {
            FileReader_Stop();
        }

        private void tbFileFolder_LostFocus(object sender, RoutedEventArgs e)
        {

        }

        private void tbFileFolder_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {

        }

        private void tbFileName_LostFocus(object sender, RoutedEventArgs e)
        {

        }

        private void tbFileName_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {

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
            }
        }
    }
}