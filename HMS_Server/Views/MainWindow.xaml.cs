using System;
using System.IO.Ports;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Telerik.Windows.Controls;
using Telerik.Windows.Data;

namespace HMS_Server
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : RadWindow
    {
        private RadObservableCollection<SensorData> statusDisplayList = new RadObservableCollection<SensorData>();
        private RadObservableCollection<SensorData> sensorDataDisplayList = new RadObservableCollection<SensorData>();
        private RadObservableCollection<SerialPortData> serialPortDataDisplayList = new RadObservableCollection<SerialPortData>();
        private RadObservableCollection<FileReaderSetup> fileReaderDataDisplayList = new RadObservableCollection<FileReaderSetup>();

        //private SensorData sensorDataSelected = new SensorData();

        // Database
        private DatabaseHandler database = new DatabaseHandler();

        // Configuration settings
        private Config config;

        // Sensor data service for behandling av data
        private SensorDataRetrieval sensorDataRetrieval;

        // Database Maintenance Dispatcher
        private System.Timers.Timer maintenanceTimer;

        // Error Handler
        private ErrorHandler errorHandler;

        // Socket Listener
        private SocketListener socketListener;

        // Socket console hvor vi mottar meldinger fra socket listener
        private SocketConsole socketConsole = new SocketConsole();

        public delegate void UserInputsCallback(UserInputs userInputs);

        // Liste med HMS sensor data: Data som skal gjennom HMS prosessen og sendes til HMS klient
        private HMSDataCollection hmsInputData;

        // Verification data
        private Verfication verfication;

        // Sensor Status
        private HMSSensorGroupStatus sensorStatus;

        // HMS
        private HMSProcessing hmsProcessing;
        private HMSDataCollection hmsOutputData;
        private HMSDatabase hmsDatabase;
        private DispatcherTimer hmsTimer = new DispatcherTimer();
        private DispatcherTimer hmsDatabaseTimer = new DispatcherTimer();

        // Lights Output
        private DispatcherTimer lightsOutputTimer = new DispatcherTimer();
        private LightsOutputConnection lightsOutputConnection;

        // Data verification
        private DispatcherTimer verificationTimer = new DispatcherTimer();

        // View Models
        private AdminSettingsVM adminSettingsVM = new AdminSettingsVM();
        private HMSLightsOutputVM hmsLightsOutputVM = new HMSLightsOutputVM();

        // Motion Limits
        private HelideckMotionLimits motionLimits;

        // User Inputs
        private UserInputs userInputs = new UserInputs();

        // Application Restart Required callback
        public delegate void RestartRequiredCallback(bool showMessage);

        // Kjører serveren?
        private bool serverStarted = false;

        // Database tabeller opprettet
        private bool databaseTablesCreated = false;

        // License/Activation
        private ActivationVM activationVM;

        // Stop server callback
        public delegate void StopServerCallback();

        // Kommer fra sensor input edit
        private bool sensorInputEdited = false;

        public MainWindow()
        {
            InitializeComponent();

            // Error Handler
            errorHandler = new ErrorHandler(database);

            // Config
            config = new Config();

            // Init
            sensorDataRetrieval = new SensorDataRetrieval(config, database, errorHandler, adminSettingsVM);

            // HMS Output data
            hmsOutputData = new HMSDataCollection();

            // HMS Database
            hmsDatabase = new HMSDatabase(database, errorHandler);

            // Init view model
            InitViewModel();

            // Admin Settings
            ucAdminSettings.Init(
                adminSettingsVM,
                config,
                database,
                errorHandler,
                sensorDataRetrieval.GetSensorDataList());

            // Sensor Input Edit
            StopServerCallback stopServerCallback = new StopServerCallback(StopServer);
            ucSensorSetupPage.Init(config, errorHandler, adminSettingsVM, stopServerCallback);

            // Error Message
            ucErrorMessagesPage.Init(config, errorHandler);

            // HMS Lights Output
            InitLightsOutput();

            // Sensor Status
            sensorStatus = new HMSSensorGroupStatus(config, database, errorHandler);

            // Laste sensor data setups fra fil
            sensorDataRetrieval.LoadSensors();

            // Starter kjøring av status oppdatering
            sensorDataRetrieval.UpdateSensorStatus();

            // Init UI
            InitUI();
            InitUIHMS();
            InitUIDataVerification();

            // Socket Init
            InitSocketConsole();

            // Init User Inputs
            InitUserInput();

            // Init UI Input
            InitUIInputUpdate();

            // Init HMS Data Flow
            InitHMSUpdate();

            // Init verification data prosessering
            InitVerificationUpdate();

            // Opprette database tabeller
            // Sjekker først om bruker og passord for databasen er satt (ikke satt rett etter installasjon)
            if (!string.IsNullOrEmpty(config.Read(ConfigKey.DatabaseUserID)) &&
                !string.IsNullOrEmpty(config.Read(ConfigKey.DatabasePassword)))
            {
                try
                {
                    // CreateTables gjør selv sjekk på om tabell finnes fra før eller må opprettes
                    database.CreateTables(sensorDataRetrieval.GetSensorDataList());

                    errorHandler.ResetDatabaseError(ErrorHandler.DatabaseErrorType.CreateTablesSensorData2);
                    errorHandler.ResetDatabaseError(ErrorHandler.DatabaseErrorType.CreateTablesSensorData1);
                }
                catch (Exception ex)
                {
                    errorHandler.Insert(
                         new ErrorMessage(
                             DateTime.UtcNow,
                             ErrorMessageType.Database,
                             ErrorMessageCategory.None,
                             string.Format("Database Error (CreateTables 1)\n\nSystem Message:\n{0}", ex.Message)));

                    errorHandler.SetDatabaseError(ErrorHandler.DatabaseErrorType.CreateTablesSensorData2);
                }

                // Database Maintenance Init
                DatabaseMaintenanceInit();
            }

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

            InitLicense();

#if DEBUG
            // Vise admin grensesnittet
            AdminMode.IsActive = true;

            tabInputEdit.Visibility = Visibility.Visible;
            //tabOutput.Visibility = Visibility.Visible;
            tabHMS.Visibility = Visibility.Visible;
            tabSettings.Visibility = Visibility.Visible;
            tabErrorMessages.Visibility = Visibility.Visible;

            if (adminSettingsVM.dataVerificationEnabled)
                tabDataVerification.Visibility = Visibility.Visible;

            tabInput_SensorData.Visibility = Visibility.Visible;
            tabInput_SerialData.Visibility = Visibility.Visible;
            tabInput_FileReader.Visibility = Visibility.Visible;
            tabInput_SocketConsole.Visibility = Visibility.Visible;
#else
            // Starte server automatisk ved oppstart
            if (adminSettingsVM.autoStartHMS && activationVM.isActivated)
                StartServer();
#endif
        }

        private void InitViewModel()
        {
            // Callback funksjon som kalles når application restart er påkrevd
            RestartRequiredCallback restartRequired = new RestartRequiredCallback(ShowRestartRequiredMessage);

            adminSettingsVM.Init(config, restartRequired);
            hmsLightsOutputVM.Init(hmsOutputData, config, userInputs, adminSettingsVM);
        }

        private void InitUI()
        {
            // Init Admin Mode
            InitAdminMode();

            // Activation
            activationVM = new ActivationVM(config, Application.ResourceAssembly.GetName().Version);

            // Liste med sensor status
            gvStatusDisplay.ItemsSource = statusDisplayList;

            // Liste med sensor verdier
            gvSensorDataDisplay.ItemsSource = sensorDataDisplayList;

            // Liste med serieport verdier
            gvSerialPortDataDisplay.ItemsSource = serialPortDataDisplayList;

            // Liste med file reader verdier
            gvFileReaderDataDisplay.ItemsSource = fileReaderDataDisplayList;

            // Div UI init
            SetStartStopButtons(true);
        }

        private void SetStartStopButtons(bool state)
        {
            if (activationVM.isActivated || AdminMode.IsActive)
            {
                btnStart1.IsEnabled = state;
                btnStart2.IsEnabled = state;
                btnStart3.IsEnabled = state;
                btnStop1.IsEnabled = !state;
                btnStop2.IsEnabled = !state;
                btnStop3.IsEnabled = !state;
            }
            else
            {
                btnStart1.IsEnabled = false;
                btnStart2.IsEnabled = false;
                btnStart3.IsEnabled = false;
                btnStop1.IsEnabled = false;
                btnStop2.IsEnabled = false;
                btnStop3.IsEnabled = false;
            }
        }

        private void InitUIHMS()
        {
            // Motion Limits
            motionLimits = new HelideckMotionLimits(userInputs, config, adminSettingsVM);

            // HMS data list
            hmsInputData = new HMSDataCollection();
            hmsInputData.LoadHMSInput(config);

            // HMS data: Prosessering av input data til output data
            hmsProcessing = new HMSProcessing(
                motionLimits,
                hmsOutputData,
                adminSettingsVM,
                userInputs,
                errorHandler,
                adminSettingsVM.dataVerificationEnabled);

            // Main Menu HMS: Input Setup
            ucHMSInputSetup.Init(
                sensorDataRetrieval.GetSensorDataList(),
                hmsInputData,
                config);

            // Main Menu HMS: HMS Output
            ucHMSOutput.Init(
                hmsOutputData,
                config);

            // Main Menu HMS: Sensor Groups
            ucHMSSensorGroups.Init(
                hmsInputData,
                sensorStatus,
                config);
        }

        private void InitUIDataVerification()
        {
            // Data verification
            verfication = new Verfication(config, userInputs, errorHandler);

            // Setup
            ucDataVerificationSetupTestData.Init(
                hmsOutputData,
                verfication.GetTestData(),
                config);

            ucDataVerificationSetupRefData.Init(
                sensorDataRetrieval,
                verfication.GetRefData(),
                config);

            // Results
            ucDataVerificationResult.Init(
                verfication,
                config);
        }

        private void InitUserInput()
        {
            try
            {
                // Lese sist brukte user inputs fra fil
                if (adminSettingsVM.regulationStandard == RegulationStandard.NOROG)
                    userInputs.helicopterType = (HelicopterType)Enum.Parse(typeof(HelicopterType), config.ReadWithDefault(ConfigKey.HelicopterType, HelicopterType.Default.ToString()));
                else
                    userInputs.helicopterType = HelicopterType.Default;

                userInputs.helideckCategory = (HelideckCategory)Enum.Parse(typeof(HelideckCategory), config.ReadWithDefault(ConfigKey.HelideckCategory, HelideckCategory.Category1.ToString()));
                userInputs.dayNight = (DayNight)Enum.Parse(typeof(DayNight), config.ReadWithDefault(ConfigKey.DayNight, DayNight.Day.ToString()));
                userInputs.displayMode = (DisplayMode)Enum.Parse(typeof(DisplayMode), config.ReadWithDefault(ConfigKey.DisplayMode, DisplayMode.PreLanding.ToString()));

                if (DateTime.TryParse(config.Read(ConfigKey.OnDeckTime), out DateTime onDeckTime))
                    userInputs.onDeckTime = onDeckTime;
                else
                    userInputs.onDeckTime = DateTime.UtcNow;

                userInputs.onDeckHelicopterHeading = config.ReadWithDefault(ConfigKey.OnDeckHelicopterHeading, Constants.HeadingDefault);
                userInputs.onDeckVesselHeading = config.ReadWithDefault(ConfigKey.OnDeckVesselHeading, Constants.HeadingDefault);
                userInputs.onDeckWindDirection = config.ReadWithDefault(ConfigKey.OnDeckWindDirection, Constants.HeadingDefault);
            }
            catch (Exception ex)
            {
                errorHandler.Insert(
                    new ErrorMessage(
                        DateTime.UtcNow,
                        ErrorMessageType.All,
                        ErrorMessageCategory.AdminUser,
                        string.Format("Failed to read User Inputs from file.\n\nSystem Message:\n{0}", ex.Message)));
            }
        }

        private void InitSocketConsole()
        {
            // Listview source binding
            gvServerSocketConsole.ItemsSource = socketConsole.Messages();

            // Callback funksjon som kalles når socketClient er ferdig med å hente data
            UserInputsCallback userInputsCallback = new UserInputsCallback(UpdateUserInputs);

            // Socket listener som lytter til og kommuniserer med klientene
            socketListener = new SocketListener(
                                    hmsOutputData, 
                                    sensorStatus, 
                                    socketConsole, 
                                    activationVM,
                                    userInputsCallback);
        }

        private void InitAdminMode()
        {
            // Admin mode key event
            KeyDown += new KeyEventHandler(OnAdminKeyCommand);
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
            StopServer();
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

                if (AdminMode.IsActive)
                {
                    DisplayList.Transfer(sensorDataRetrieval.GetSensorDataList(), sensorDataDisplayList);
                    DisplayList.Transfer(sensorDataRetrieval.GetSerialPortDataReceivedList(), serialPortDataDisplayList);
                    DisplayList.Transfer(sensorDataRetrieval.GetFileReaderList(), fileReaderDataDisplayList);
                }
            }
        }

        private void InitHMSUpdate()
        {
            // Dispatcher som oppdatere HMS prosessering
            hmsTimer.Interval = TimeSpan.FromMilliseconds(config.ReadWithDefault(ConfigKey.HMSProcessingFrequency, Constants.HMSProcessingFrequencyDefault));
            hmsTimer.Tick += runHMSUpdate;

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
                        hmsInputData.TransferData(sensorDataRetrieval.GetSensorDataList());

                        // HMS: HMS Output Data
                        // Prosesserer sensor data om til data som kan sendes til HMS klient
                        hmsProcessing.Update(hmsInputData, adminSettingsVM.dataVerificationEnabled);

                        // Sette database status
                        SetDatabaseStatus(hmsInputData);


                        // HMS: Lagre data i databasen
                        /////////////////////////////////////////////////////////////////////////////////////////////////////

                        // Utfører første en sjekk på om database tabellene er opprettet.
                        // Grunnen til at dette gjøres her, så sent etter init, er at output listen (med dbColumnNames) fylles av 
                        // inidividuelle sub-rutiner rundt omkring.
                        // Output listen er derfor ikke komplett før en update (hmsProcessing.Update ovenfor) er utført.
                        if (!databaseTablesCreated)
                        {
                            hmsDatabase.CreateDataTables(hmsOutputData);
                            sensorStatus.CreateDataTables();

                            databaseTablesCreated = true;

                            // Kjør vedlikehold en gang ved oppstart
                            DoDatabaseMaintenance(DatabaseMaintenanceType.HMS);
                            DoDatabaseMaintenance(DatabaseMaintenanceType.STATUS);
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

                    errorHandler.SetDatabaseError(ErrorHandler.DatabaseErrorType.DatabaseMaintenance);
                }
            }

            // Dispatcher som lagrerer HMS data til databasen
            hmsDatabaseTimer.Interval = TimeSpan.FromMilliseconds(config.ReadWithDefault(ConfigKey.DatabaseSaveFrequency, Constants.DatabaseSaveFreqDefault));
            hmsDatabaseTimer.Tick += runSaveHMSData;

            void runSaveHMSData(object sender, EventArgs e)
            {
                try
                {
                    Thread thread = new Thread(() => SaveHMSData_Thread());
                    thread.IsBackground = true;
                    thread.Start();

                    void SaveHMSData_Thread()
                    {
                        // Lagre data i databasen
                        hmsDatabase.InsertData(hmsOutputData);
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

                    errorHandler.SetDatabaseError(ErrorHandler.DatabaseErrorType.DatabaseMaintenance);
                }
            }
        }

        private void SetDatabaseStatus(HMSDataCollection hmsInputData)
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

        private void InitLightsOutput()
        {
            // CAP
            if (adminSettingsVM.regulationStandard == RegulationStandard.CAP)
            {
                // Opprette lights output connection
                lightsOutputConnection = new LightsOutputConnection(config);

                // Init
                ucHMSLightsOutput.Init(lightsOutputConnection, hmsLightsOutputVM, config, adminSettingsVM, errorHandler);
            }
            // NOROG
            else
            {
                // TODO: Modulen for output til NOROG lys system er ikke ferdig implementert.
                //ucHMSLightsOutput.Init(new SensorData(SensorType.ModbusRTU), hmsLightsOutputVM, config, adminSettingsVM, errorHandler);
            }

            // Enable/disable helideck lights output
            if (!adminSettingsVM.helideckLightsOutput)
            {
                tabHMS_LightsOutput.Visibility = Visibility.Collapsed;
            }
        }

        private void InitVerificationUpdate()
        {
            if (adminSettingsVM.dataVerificationEnabled)
            {
                // Dispatcher som oppdaterer verification data (test og referanse data)
                verificationTimer.Interval = TimeSpan.FromMilliseconds(config.ReadWithDefault(ConfigKey.HMSProcessingFrequency, Constants.HMSProcessingFrequencyDefault));
                verificationTimer.Tick += runLightsOutputUpdate;

                void runLightsOutputUpdate(object sender, EventArgs e)
                {
                    verfication.Update(hmsOutputData, sensorDataRetrieval.GetSensorDataList(), database);
                }
            }
        }

        private void InitLicense()
        {
            // Sette start/stop knappene
            SetStartStopButtons(true);
        }

        public void UpdateUserInputs(UserInputs userInputs)
        {
            if (userInputs != null)
            {
                // Setter ikke user data fra klient dersom vi holder på med data verifikasjon
                if (!adminSettingsVM.dataVerificationEnabled)
                {
                    // Overføre data
                    this.userInputs.Set(userInputs);

                    // Lagre til fil
                    config.Write(ConfigKey.HelicopterType, userInputs.helicopterType.ToString());
                    config.Write(ConfigKey.HelideckCategory, userInputs.helideckCategory.ToString());
                    config.Write(ConfigKey.DayNight, userInputs.dayNight.ToString());
                    config.Write(ConfigKey.DisplayMode, userInputs.displayMode.ToString());

                    config.Write(ConfigKey.OnDeckTime, userInputs.onDeckTime.ToString());
                    config.Write(ConfigKey.OnDeckHelicopterHeading, userInputs.onDeckHelicopterHeading.ToString());
                    config.Write(ConfigKey.OnDeckVesselHeading, userInputs.onDeckVesselHeading.ToString());
                    config.Write(ConfigKey.OnDeckWindDirection, userInputs.onDeckWindDirection.ToString());
                }
            }
        }

        private void btnStart_Click(object sender, RoutedEventArgs e)
        {
            StartServer();
        }

        private void btnStop_Click(object sender, RoutedEventArgs e)
        {
            StopServer();
        }

        private void StartServer()
        {
            if (activationVM.isActivated)
            {
                try
                {
                    // Oppretter tabellene dersom de ikke eksisterer
                    database.CreateTables(sensorDataRetrieval.GetSensorDataList());

                    errorHandler.ResetDatabaseError(ErrorHandler.DatabaseErrorType.CreateTablesStartServer);
                }
                catch (Exception ex)
                {
                    errorHandler.Insert(
                        new ErrorMessage(
                            DateTime.UtcNow,
                            ErrorMessageType.Database,
                            ErrorMessageCategory.None,
                            string.Format("Database Error (CreateTables 2)\n\nSystem Message:\n{0}", ex.Message)));

                    errorHandler.SetDatabaseError(ErrorHandler.DatabaseErrorType.CreateTablesStartServer);
                }

                // Resette sammenligningsdata
                if (adminSettingsVM.dataVerificationEnabled)
                    verfication.Reset();

                // Resetter data listene i dataCalculations
                hmsProcessing.ResetDataCalculations();

                // Starter data-innhenting 
                sensorDataRetrieval.SensorDataRetrieval_Start();

                // Sette knappe status
                SetStartStopButtons(false);

                // HMS prosessering updater
                hmsTimer.Start();
                hmsDatabaseTimer.Start();

                // Database Maintenance
                DoDatabaseMaintenance(DatabaseMaintenanceType.SENSOR);  // Kjør vedlikehold en gang ved oppstart
                maintenanceTimer?.Start();                               // Og så hver X timer

                // Socket Listener
                socketListener.Start();

                // Sensor Status
                sensorStatus.Start(hmsOutputData);

                // HMS
                ucHMSInputSetup.Start();
                ucHMSOutput.Start();
                ucHMSSensorGroups.Start();

                // Lights Output
                if (adminSettingsVM.regulationStandard == RegulationStandard.CAP &&
                    adminSettingsVM.helideckLightsOutput &&
                    !adminSettingsVM.dataVerificationEnabled)
                {
                    lightsOutputTimer.Start();
                    ucHMSLightsOutput.Start();
                }

                // Data verification
                verificationTimer.Start();

                // Server startet
                serverStarted = true;
            }
            // Server er ikke aktivert
            else
            {
                // Åpne not activated dialog
                DialogNotActivated notActivatedDlg = new DialogNotActivated();
                notActivatedDlg.Owner = App.Current.MainWindow;
                notActivatedDlg.ShowDialog();

            }
        }

        private void StopServer()
        {
            sensorDataRetrieval.SensorDataRetrieval_Stop();

            SetStartStopButtons(true);

            // HMS prosessering updater
            hmsTimer.Stop();
            hmsDatabaseTimer.Stop();

            // Database Maintenance
            maintenanceTimer?.Stop();

            // Socket Listener
            socketListener.Stop();

            // Sensor Status
            sensorStatus.Stop();

            // HMS
            ucHMSInputSetup.Stop();
            ucHMSOutput.Stop();
            ucHMSSensorGroups.Stop();

            // Lights Output
            if (adminSettingsVM.regulationStandard == RegulationStandard.CAP)
            {
                lightsOutputTimer.Stop();
                ucHMSLightsOutput.Stop();
            }

            // Data verification
            verificationTimer.Stop();

            // Server startet
            serverStarted = false;

            // 
            databaseTablesCreated = false;
        }

        private void ExitServer()
        {
            socketListener.Exit();
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

        private void DatabaseMaintenanceInit()
        {
            // Timer som kjører vedlikehold på databasen
            // 1. Slette gamle data
            // 2. Sletter gamle feilmeldinger

            maintenanceTimer = new System.Timers.Timer(Constants.DBMaintenanceFrequency);
            maintenanceTimer.AutoReset = true;
            maintenanceTimer.Elapsed += runDatabaseMaintenance;

            void runDatabaseMaintenance(object sender, EventArgs e)
            {
                DoDatabaseMaintenance(DatabaseMaintenanceType.ALL);
            }
        }

        private void DoDatabaseMaintenance(DatabaseMaintenanceType type)
        {
            Thread thread = new Thread(() => runDatabaseMaintenance_Thread());
            thread.IsBackground = true;
            thread.Start();

            void runDatabaseMaintenance_Thread()
            {
                try
                {
                    switch (type)
                    {
                        case DatabaseMaintenanceType.SENSOR:
                            database.DatabaseMaintenance(sensorDataRetrieval.GetSensorDataList());
                            break;
                        case DatabaseMaintenanceType.HMS:
                            database.DatabaseMaintenanceHMSData();
                            break;
                        case DatabaseMaintenanceType.STATUS:
                            database.DatabaseMaintenanceSensorStatus();
                            break;
                        case DatabaseMaintenanceType.ALL:
                            database.DatabaseMaintenance(sensorDataRetrieval.GetSensorDataList());
                            database.DatabaseMaintenanceHMSData();
                            database.DatabaseMaintenanceSensorStatus();
                            break;
                    }

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

        private void OnAdminKeyCommand(object sender, KeyEventArgs e)
        {
            if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift &&
                (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                // Admin Mode grensesnitt
                if (Keyboard.IsKeyDown(Key.H))
                {
                    if (!AdminMode.IsActive)
                    {
                        // Åpne admin passord vindu
                        DialogAdminMode adminMode = new DialogAdminMode();
                        adminMode.Owner = App.Current.MainWindow;
                        adminMode.ShowDialog();

                        if (AdminMode.IsActive)
                        {
                            // Vise admin grensesnittet
                            tabInputEdit.Visibility = Visibility.Visible;
                            //tabOutput.Visibility = Visibility.Visible;
                            tabHMS.Visibility = Visibility.Visible;
                            tabSettings.Visibility = Visibility.Visible;
                            tabErrorMessages.Visibility = Visibility.Visible;

                            if (adminSettingsVM.dataVerificationEnabled)
                                tabDataVerification.Visibility = Visibility.Visible;

                            tabInput_SensorData.Visibility = Visibility.Visible;
                            tabInput_SerialData.Visibility = Visibility.Visible;
                            tabInput_FileReader.Visibility = Visibility.Visible;
                            tabInput_SocketConsole.Visibility = Visibility.Visible;
                        }
                    }
                    else
                    {
                        // Skjule admin grensesnittet

                        // Sette Input tab som valgt tab og skjule resten
                        tabInput.IsSelected = true;

                        tabInputEdit.Visibility = Visibility.Collapsed;
                        //tabOutput.Visibility = Visibility.Collapsed;
                        tabHMS.Visibility = Visibility.Collapsed;
                        tabDataVerification.Visibility = Visibility.Collapsed;
                        tabSettings.Visibility = Visibility.Collapsed;
                        tabErrorMessages.Visibility = Visibility.Collapsed;

                        // Sette Status tab som valgt sub tab
                        tabInput_SensorData.Visibility = Visibility.Collapsed;
                        tabInput_SerialData.Visibility = Visibility.Collapsed;
                        tabInput_FileReader.Visibility = Visibility.Collapsed;
                        tabInput_SocketConsole.Visibility = Visibility.Collapsed;

                        tabInput_Status.IsSelected = true;
                        tabInput_SensorData.IsSelected = false;
                        tabInput_SerialData.IsSelected = false;
                        tabInput_FileReader.IsSelected = false;
                        tabInput_SocketConsole.IsSelected = false;

                        AdminMode.IsActive = false;
                    }

                    // Setter start/stop status
                    SetStartStopButtons(!serverStarted);
                }
                else
                // Aktiverings grensesnitt
                if (Keyboard.IsKeyDown(Key.A))
                {
                    // Åpne activation passord vindu
                    DialogActivationPassword activationPwDlg = new DialogActivationPassword();
                    activationPwDlg.Init(activationVM);
                    activationPwDlg.Owner = App.Current.MainWindow;
                    activationPwDlg.ShowDialog();
                }
            }
        }

        private void chkSocketComClear_Click(object sender, RoutedEventArgs e)
        {
            socketConsole.Clear();
        }

        private void tcMainMenu_SelectionChanged(object sender, RadSelectionChangedEventArgs e)
        {
            // Fjerne fokus fra alle elementer slik at ikke knapper og felt lyser opp i rødt når vi bytter tab
            Dispatcher.BeginInvoke((Action)(() =>
            {
                (sender as RadTabControl).Focus();
            }), DispatcherPriority.ApplicationIdle);

            // Dersom vi har vært innom input edit, og forlatt siden igjen, må vi anta at sensor input er endret
            // -> laste display lister på nytt i tilfelle sensor input er redigert
            if (!tabInputEdit.IsSelected && sensorInputEdited)
            {
                // Laste sensor data setups fra fil
                sensorDataRetrieval.LoadSensors();

                // Resette display lister
                statusDisplayList.Clear();
                sensorDataDisplayList.Clear();
                serialPortDataDisplayList.Clear();
                fileReaderDataDisplayList.Clear();
            }

            // Sensor Input Edit Tab
            if (tabInputEdit.IsSelected)
            {
                ucSensorSetupPage.ServerStartedCheck(serverStarted);
                sensorInputEdited = true;
            }
            else
            {
                sensorInputEdited = false;
            }

            // HMS Tab
            if (tabHMS.IsSelected)
            {
                ucHMSInputSetup.UpdateSensorDataList();
            }
        }

        private void btnAbout_Click(object sender, RoutedEventArgs e)
        {
            // Åpne about dialog vindu
            DialogAbout aboutDlg = new DialogAbout();
            aboutDlg.Owner = App.Current.MainWindow;
            aboutDlg.Init(activationVM);
            aboutDlg.ShowDialog();
        }
    }
}
