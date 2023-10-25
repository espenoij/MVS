﻿using System;
using System.Diagnostics.Metrics;
using System.IO.Ports;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Telerik.Windows.Controls;
using Telerik.Windows.Data;

namespace MVS
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : RadWindow
    {
        //// TEST
        //int counter1 = 0;
        //int counter2 = 0;

        private RadObservableCollection<SensorData> statusDisplayList = new RadObservableCollection<SensorData>();
        private RadObservableCollection<SensorData> sensorDataDisplayList = new RadObservableCollection<SensorData>();
        private RadObservableCollection<SerialPortData> serialPortDataDisplayList = new RadObservableCollection<SerialPortData>();
        private RadObservableCollection<FileReaderSetup> fileReaderDataDisplayList = new RadObservableCollection<FileReaderSetup>();
        private RadObservableCollection<FixedValueSetup> fixedValueDataDisplayList = new RadObservableCollection<FixedValueSetup>();

        //private SensorData sensorDataSelected = new SensorData();

        // Database
        private DatabaseHandler database = new DatabaseHandler();

        // Configuration settings
        private Config config;

        // Sensor data service for behandling av data
        private SensorDataRetrieval sensorDataRetrieval;

        // Error Handler
        private ErrorHandler errorHandler;

        // Liste med HMS sensor data: Data som skal gjennom HMS prosessen og sendes til HMS klient
        private MVSDataCollection mvsInputData;

        // HMS
        private MVSProcessing mvsProcessing;
        private MVSDataCollection mvsOutputData;
        private MVSDatabase mvsDatabase;
        private DispatcherTimer mvsTimer = new DispatcherTimer();
        private DispatcherTimer mvsDatabaseTimer = new DispatcherTimer();

        // View Models
        private AdminSettingsVM adminSettingsVM = new AdminSettingsVM();

        // Application Restart Required callback
        public delegate void RestartRequiredCallback(bool showMessage);

        // Kjører serveren?
        private bool serverStarted = false;

        // Database tabeller opprettet
        private bool databaseTablesCreated = false;

        // Stop server callback
        public delegate void StopServerCallback();

        // Kommer fra sensor input edit
        private bool sensorInputEdited = false;

        // Operations mode
        private OperationsMode operationsMode;

        // About
        private MainWindowVM mainWindowVM;

        public MainWindow()
        {
            InitializeComponent();

            // Error Handler
            errorHandler = new ErrorHandler(database);

            // Config
            config = new Config();

            // Init
            sensorDataRetrieval = new SensorDataRetrieval(config, database, errorHandler, adminSettingsVM);

            // Main Window VM
            mainWindowVM = new MainWindowVM(Application.ResourceAssembly.GetName().Version);

            // MVS Output data
            mvsOutputData = new MVSDataCollection();

            // HMS Database
            mvsDatabase = new MVSDatabase(database, errorHandler);

            // Sørge for at databasen er opprettet
            mvsDatabase.CreateDataSetTables();
            mvsDatabase.CreateErrorMessagesTables();

            // Operations mode
            operationsMode = OperationsMode.Stop;

            // Init view model
            InitViewModel();

            // Admin Settings
            ucAdminSettings.Init(
                adminSettingsVM,
                config,
                database,
                errorHandler,
                sensorDataRetrieval.GetSensorDataList());

            // Stop Recording Callback
            StopServerCallback stopServerCallback = new StopServerCallback(StopRecording);

            // Motion Data Sets page
            ucMotionDataSets.Init(mainWindowVM, mvsDatabase, stopServerCallback);

            // Sensor Input Setup
            ucSensorSetupPage.Init(config, errorHandler, adminSettingsVM, stopServerCallback);

            // Error Message
            ucErrorMessagesPage.Init(config, errorHandler);

            // Laste sensor data setups fra fil
            sensorDataRetrieval.LoadSensors();

            // Starter kjøring av status oppdatering
            sensorDataRetrieval.UpdateSensorStatus();

            // Init UI
            InitUI();
            InitUIHMS();

            // Init UI Input
            InitUIInputUpdate();

            // Init HMS Data Flow
            InitHMSUpdate();

            //// Opprette database tabeller
            //// Sjekker først om bruker og passord for databasen er satt (ikke satt rett etter installasjon)
            //if (!string.IsNullOrEmpty(config.Read(ConfigKey.DatabaseUserID)) &&
            //    !string.IsNullOrEmpty(config.Read(ConfigKey.DatabasePassword)))
            //{
            //    try
            //    {
            //        // CreateTables gjør selv sjekk på om tabell finnes fra før eller må opprettes
            //        database.CreateTables(sensorDataRetrieval.GetSensorDataList());

            //        errorHandler.ResetDatabaseError(ErrorHandler.DatabaseErrorType.CreateTablesSensorData2);
            //        errorHandler.ResetDatabaseError(ErrorHandler.DatabaseErrorType.CreateTablesSensorData1);
            //    }
            //    catch (Exception ex)
            //    {
            //        errorHandler.Insert(
            //             new ErrorMessage(
            //                 DateTime.UtcNow,
            //                 ErrorMessageType.Database,
            //                 ErrorMessageCategory.None,
            //                 string.Format("Database Error (CreateTables 1)\n\nSystem Message:\n{0}", ex.Message)));

            //        errorHandler.SetDatabaseError(ErrorHandler.DatabaseErrorType.CreateTablesSensorData2);
            //    }
            //}

            // Database Status Check
            DispatcherTimer databaseStatusCheck = new DispatcherTimer();
            databaseStatusCheck.Interval = TimeSpan.FromMilliseconds(config.ReadWithDefault(ConfigKey.ServerUIUpdateFrequency, Constants.ServerUIUpdateFrequencyDefault));
            databaseStatusCheck.Tick += checkDatabaseStatus;
            databaseStatusCheck.Start();

            void checkDatabaseStatus(object sender, EventArgs e)
            {
                try
                {
                    if (errorHandler.IsDatabaseError())
                    {
                        tbDatabaseStatus.Visibility = Visibility.Visible;

                        tbDatabaseStatusText.Text = string.Format("Database Error: {0}", errorHandler.GetDatabaseErrorType().ToString());
                    }
                    else
                    {
                        tbDatabaseStatus.Visibility = Visibility.Collapsed;
                    }

                    errorHandler.ResetDatabaseError(ErrorHandler.DatabaseErrorType.StatusCheck);
                }
                catch (Exception ex)
                {
                    errorHandler.Insert(
                         new ErrorMessage(
                             DateTime.UtcNow,
                             ErrorMessageType.Database,
                             ErrorMessageCategory.Admin,
                             string.Format("Database Status Check Error (checkDatabaseStatus)\n\nSystem Message:\n{0}", ex.Message)));

                    errorHandler.SetDatabaseError(ErrorHandler.DatabaseErrorType.StatusCheck);
                }
            }

            // Sette start/stop knappene
            SetOperationsMode(OperationsMode.Stop);

            tabSensorSetup.Visibility = Visibility.Visible;
            tabInputSetup.Visibility = Visibility.Visible;
            tabAdminSettings.Visibility = Visibility.Visible;
            tabErrorMessages.Visibility = Visibility.Visible;

            tabInput_SensorData.Visibility = Visibility.Visible;
            tabInput_SerialData.Visibility = Visibility.Visible;
            tabInput_FileReader.Visibility = Visibility.Visible;
            tabInput_FixedValue.Visibility = Visibility.Visible;
        }

        private void InitViewModel()
        {
            // Callback funksjon som kalles når application restart er påkrevd
            RestartRequiredCallback restartRequired = new RestartRequiredCallback(ShowRestartRequiredMessage);

            adminSettingsVM.Init(config, restartRequired);
        }

        private void InitUI()
        {
            // Liste med sensor status
            gvStatusDisplay.ItemsSource = statusDisplayList;

            // Liste med sensor verdier
            gvSensorDataDisplay.ItemsSource = sensorDataDisplayList;

            // Liste med serieport verdier
            gvSerialPortDataDisplay.ItemsSource = serialPortDataDisplayList;

            // Liste med file reader verdier
            gvFileReaderDataDisplay.ItemsSource = fileReaderDataDisplayList;

            // Liste med fixed value verdier
            gvFixedValueDataDisplay.ItemsSource = fixedValueDataDisplayList;

            // Div UI init
            SetOperationsMode(OperationsMode.Stop);

            // Oppdatering av UI elementer
            DispatcherTimer uiTimer = new DispatcherTimer();
            uiTimer.Interval = TimeSpan.FromMilliseconds(config.ReadWithDefault(ConfigKey.ServerUIUpdateFrequency, Constants.ServerUIUpdateFrequencyDefault));
            uiTimer.Tick += UpdateUI;
            uiTimer.Start();
        }

        private void UpdateUI(object sender, EventArgs e)
        {
        }

        private void SetOperationsMode(OperationsMode mode)
        {
            operationsMode = mode;

            switch (operationsMode)
            {
                case OperationsMode.Recording:
                case OperationsMode.Test:
                    btnStart1.IsEnabled = false;
                    btnStart2.IsEnabled = false;
                    btnStart3.IsEnabled = false;

                    btnTest1.IsEnabled = false;
                    btnTest2.IsEnabled = false;
                    btnTest3.IsEnabled = false;

                    btnStop1.IsEnabled = true;
                    btnStop2.IsEnabled = true;
                    btnStop3.IsEnabled = true;
                    break;

                case OperationsMode.Stop:
                    btnStart1.IsEnabled = true;
                    btnStart2.IsEnabled = true;
                    btnStart3.IsEnabled = true;

                    btnTest1.IsEnabled = true;
                    btnTest2.IsEnabled = true;
                    btnTest3.IsEnabled = true;

                    btnStop1.IsEnabled = false;
                    btnStop2.IsEnabled = false;
                    btnStop3.IsEnabled = false;
                    break;

                default:
                    break;
            }
        }

        private void InitUIHMS()
        {
            // HMS data list
            mvsInputData = new MVSDataCollection();
            mvsInputData.LoadMVSInput(config);

            // HMS data: Prosessering av input data til output data
            mvsProcessing = new MVSProcessing(
                mvsOutputData,
                adminSettingsVM,
                errorHandler);

            // Main Menu MVS: Input Setup
            ucMVSInputSetup.Init(
                sensorDataRetrieval.GetSensorDataList(),
                mvsInputData,
                config);

            // Main Menu HMS: MVS Output
            ucMVSOutput.Init(
                mvsOutputData,
                config);
        }

        public void ShowRestartRequiredMessage(bool showMessage)
        {
            if (showMessage)
                dpApplicationRestartRequired.Visibility = Visibility.Visible;
            else
                dpApplicationRestartRequired.Visibility = Visibility.Collapsed;
        }

        private void Window_Closing(object sender, Telerik.Windows.Controls.WindowClosedEventArgs e)
        {
            StopRecording();
            ExitServer();

            Application.Current.Shutdown();
        }

        private void InitUIInputUpdate()
        {
            // Dispatcher som oppdatere UI
            DispatcherTimer uiTimer = new DispatcherTimer();
            uiTimer.Interval = TimeSpan.FromMilliseconds(config.ReadWithDefault(ConfigKey.ServerUIUpdateFrequency, Constants.ServerUIUpdateFrequencyDefault));
            uiTimer.Tick += runUIInputUpdate;
            uiTimer.Start();

            void runUIInputUpdate(object sender, EventArgs e)
            {
                // Overføre fra data lister til display lister
                DisplayList.Transfer(sensorDataRetrieval.GetSensorDataList(), statusDisplayList);

                DisplayList.Transfer(sensorDataRetrieval.GetSensorDataList(), sensorDataDisplayList);
                DisplayList.Transfer(sensorDataRetrieval.GetSerialPortDataReceivedList(), serialPortDataDisplayList);
                DisplayList.Transfer(sensorDataRetrieval.GetFileReaderList(), fileReaderDataDisplayList);
                DisplayList.Transfer(sensorDataRetrieval.GetFixedValueList(), fixedValueDataDisplayList);

                    //gvSerialPortDataDisplay.Items.Refresh();
            }
        }

        private void InitHMSUpdate()
        {
            // Dispatcher som oppdatere HMS prosessering
            mvsTimer.Interval = TimeSpan.FromMilliseconds(config.ReadWithDefault(ConfigKey.HMSProcessingFrequency, Constants.HMSProcessingFrequencyDefault));
            mvsTimer.Tick += runHMSUpdate;

            void runHMSUpdate(object sender, EventArgs e)
            {
                try
                {
                    Thread thread = new Thread(() => UpdateHMS_Thread());
                    thread.IsBackground = true;
                    thread.Start();

                    void UpdateHMS_Thread()
                    {
                        // HMS Update
                        /////////////////////////////////////////////////////////////////////////////////////////////////////

                        // HMS: HMS Input Data
                        mvsInputData.TransferData(sensorDataRetrieval.GetSensorDataList());

                        // HMS: HMS Output Data
                        // Prosesserer sensor data om til data som kan sendes til HMS klient
                        mvsProcessing.Update(mvsInputData);

                        // Sette database status
                        SetDatabaseStatus(mvsInputData);

                        // HMS: Lagre data i databasen
                        /////////////////////////////////////////////////////////////////////////////////////////////////////

                        // Utfører første en sjekk på om database tabellene er opprettet.
                        // Grunnen til at dette gjøres her, så sent etter init, er at output listen (med dbColumnNames) fylles av 
                        // inidividuelle sub-rutiner rundt omkring.
                        // Output listen er derfor ikke komplett før en update (hmsProcessing.Update ovenfor) er utført.
                        if (!databaseTablesCreated)
                        {
                            mvsDatabase.CreateDataTables(mainWindowVM.SelectedMotionDataSet, mvsOutputData);

                            databaseTablesCreated = true;

                            // Kjør vedlikehold en gang ved oppstart
                            DoDatabaseMaintenance();
                        }
                    }
                }
                catch (Exception ex)
                {
                    errorHandler.Insert(
                        new ErrorMessage(
                            DateTime.UtcNow,
                            ErrorMessageType.Database,
                            ErrorMessageCategory.Admin,
                            string.Format("Server Update Error (runHMSUpdate)\n\nSystem Message:\n{0}", ex.Message)));
                }
            }

            // Dispatcher som lagrerer MVS data til databasen
            mvsDatabaseTimer.Interval = TimeSpan.FromMilliseconds(config.ReadWithDefault(ConfigKey.DatabaseSaveFrequency, Constants.DatabaseSaveFreqDefault));
            mvsDatabaseTimer.Tick += runSaveMVSData;

            void runSaveMVSData(object sender, EventArgs e)
            {
                try
                {
                    Thread thread = new Thread(() => SaveMVSData_Thread());
                    thread.IsBackground = true;
                    thread.Start();

                    void SaveMVSData_Thread()
                    {
                        // Lagre data i databasen
                        mvsDatabase.Insert(mainWindowVM.SelectedMotionDataSet, mvsOutputData);
                    }
                }
                catch (Exception ex)
                {
                    errorHandler.Insert(
                        new ErrorMessage(
                            DateTime.UtcNow,
                            ErrorMessageType.Database,
                            ErrorMessageCategory.Admin,
                            string.Format("Database Insert Error (runSaveHMSData)\n\nSystem Message:\n{0}", ex.Message)));
                }
            }
        }

        private void SetDatabaseStatus(MVSDataCollection hmsInputData)
        {
            // Finne match i mottaker data listen
            var dbStatus = hmsInputData.GetData(ValueType.Database);

            if (dbStatus != null)
            {
                if (errorHandler.IsDatabaseError())
                {
                    dbStatus.status = DataStatus.TIMEOUT_ERROR;
                    dbStatus.timestamp = DateTime.MinValue;
                }
                else
                {
                    dbStatus.status = DataStatus.OK;
                    dbStatus.timestamp = DateTime.UtcNow;
                }
            }
        }

        private void btnStart_Click(object sender, RoutedEventArgs e)
        {
            StartRecording();
        }

        private void btnStop_Click(object sender, RoutedEventArgs e)
        {
            StopRecording();
        }

        private void btnTest_Click(object sender, RoutedEventArgs e)
        {
            StartTest();
        }

        private void StartRecording()
        {
            // Sjekker at et data set er valgt
            if (mainWindowVM.SelectedMotionDataSet == null)
            {
                DialogHandler.Warning("No data set selected.", "Please select a data set to record data into before starting a recording session.");
            }
            else
            {
                // Resetter data listene i dataCalculations
                mvsProcessing.ResetDataCalculations();

                // Starter data-innhenting 
                sensorDataRetrieval.SensorDataRetrieval_Start();

                // HMS prosessering updater
                mvsTimer.Start();
                mvsDatabaseTimer.Start();

                // Sensor Setup
                ucSensorSetupPage.ServerStartedCheck(true);
                ucMVSInputSetup.Start();
                ucMVSOutput.Start();

                // Sette operasjonsmodus
                SetOperationsMode(OperationsMode.Recording);

                // Server startet
                serverStarted = true;
            }
        }

        private void StartTest()
        {
            // Resetter data listene i dataCalculations
            mvsProcessing.ResetDataCalculations();

            // Starter data-innhenting 
            sensorDataRetrieval.SensorDataRetrieval_Start();

            // HMS prosessering updater
            mvsTimer.Start();
            mvsDatabaseTimer.Start();

            // Sensor Setup
            ucSensorSetupPage.ServerStartedCheck(true);
            ucMVSInputSetup.Start();
            ucMVSOutput.Start();

            // Sette operasjonsmodus
            SetOperationsMode(OperationsMode.Test);

            // Server startet
            serverStarted = true;
        }

        private void StopRecording()
        {
            sensorDataRetrieval.SensorDataRetrieval_Stop();

            // MVS prosessering updater
            mvsTimer.Stop();
            mvsDatabaseTimer.Stop();

            ucSensorSetupPage.ServerStartedCheck(false);
            ucMVSInputSetup.Stop();
            ucMVSOutput.Stop();
            ucMotionDataSets.Stop(operationsMode);

            // Sette operasjonsmodus
            SetOperationsMode(OperationsMode.Stop);

            // Server startet
            serverStarted = false;

            // Resette sjekk på om database tabeller er opprettet
            databaseTablesCreated = false;
        }

        private void ExitServer()
        {
        }

        private void DoDatabaseMaintenance()
        {
            Thread thread = new Thread(() => runDatabaseMaintenance_Thread());
            thread.IsBackground = true;
            thread.Start();

            void runDatabaseMaintenance_Thread()
            {
                try
                {
                    database.DatabaseMaintenance();

                    errorHandler.ResetDatabaseError(ErrorHandler.DatabaseErrorType.DatabaseMaintenance);
                }
                catch (Exception ex)
                {
                    errorHandler.Insert(
                        new ErrorMessage(
                            DateTime.UtcNow,
                            ErrorMessageType.Database,
                            ErrorMessageCategory.None,
                            string.Format("Database Error (DatabaseMaintenance)\n\nSystem Message:\n{0}", ex.Message)));

                    errorHandler.SetDatabaseError(ErrorHandler.DatabaseErrorType.DatabaseMaintenance);
                }
            }
        }

        private void gvStatusDisplay_SelectionChanged(object sender, SelectionChangeEventArgs e)
        {
            // Tillater ikke valg av linjer på status tab (kundens område)
            (sender as RadGridView).UnselectAll();
        }

        private void gvSensorDataDisplay_SelectionChanged(object sender, SelectionChangeEventArgs e)
        {
            //sensorDataSelected = (sender as RadGridView).SelectedItem as SensorData;
        }

        private void tcMainMenu_SelectionChanged(object sender, RadSelectionChangedEventArgs e)
        {
            // Fjerne fokus fra alle elementer slik at ikke knapper og felt lyser opp i rødt når vi bytter tab
            Dispatcher.BeginInvoke((Action)(() =>
            {
                (sender as RadTabControl).Focus();
            }), DispatcherPriority.ApplicationIdle);

            // Dersom vi har vært innom input edit, og forlatt siden igjen, og server ikke kjører, må vi anta at sensor input er endret
            // -> laste display lister på nytt i tilfelle sensor input er redigert
            if (!tabSensorSetup.IsSelected && sensorInputEdited && !serverStarted)
            {
                // Laste sensor data setups fra fil
                sensorDataRetrieval.LoadSensors();

                // Resette display lister
                statusDisplayList.Clear();
                sensorDataDisplayList.Clear();
                serialPortDataDisplayList.Clear();
                fileReaderDataDisplayList.Clear();
                fixedValueDataDisplayList.Clear();
            }

            // Sensor Input Edit Tab
            if (tabSensorSetup.IsSelected)
            {
                ucSensorSetupPage.ServerStartedCheck(serverStarted);
                sensorInputEdited = true;
            }
            else
            {
                sensorInputEdited = false;
            }

            // Input Setup Tab
            if (tabInputSetup.IsSelected)
            {
                ucMVSInputSetup.UpdateSensorDataList();
            }
        }

        private void btnAbout_Click(object sender, RoutedEventArgs e)
        {
            // Åpne about dialog vindu
            DialogAbout aboutDlg = new DialogAbout();
            aboutDlg.Owner = App.Current.MainWindow;
            aboutDlg.Init(mainWindowVM);
            aboutDlg.ShowDialog();
        }
    }
}
