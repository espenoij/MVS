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
        private SensorData lightsOutputData;

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

        // Lights output feilmelding
        private bool lightsOutputError = false;

        public MainWindow()
        {
            InitializeComponent();

            // Config
            config = new Config();

            // Error Handler
            errorHandler = new ErrorHandler(database);

            // Init
            sensorDataRetrieval = new SensorDataRetrieval(config, database, errorHandler);

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
            // CreateTables gjør selv sjekk på om tabell finnes fra før eller må opprettes
            try
            {
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

            // TODO: DEBUG DEBUG DEBUG !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!! Fjerne det under før release
            // Vise admin grensesnittet
            //AdminMode.IsActive = true;
            //btnSetup.Visibility = Visibility.Visible;
            //tabInput_SensorData.Visibility = Visibility.Visible;
            //tabInput_SerialData.Visibility = Visibility.Visible;
            //tabInput_FileReader.Visibility = Visibility.Visible;
            //tabInput_SocketConsole.Visibility = Visibility.Visible;

            // Starte server automatisk ved oppstart
            //StartServer();
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
            activationVM = new ActivationVM(config);

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

            // HMS Input Setup
            ucHMSInputSetup.Init(
                sensorDataRetrieval.GetSensorDataList(),
                hmsInputData,
                config);

            // HMS Output
            ucHMSOutput.Init(
                hmsOutputData,
                config);

            // Sensor Groups
            ucHMSSensorGroups.Init(
                hmsInputData,
                config,
                sensorStatus);
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
            var dbStatus = hmsInputData.GetDataList().Where(x => x?.id == Constants.DatabaseStatusID);

            if (dbStatus.Count() > 0)
            {
                if (errorHandler.IsDatabaseError())
                    dbStatus.First().status = DataStatus.TIMEOUT_ERROR;
                else
                    dbStatus.First().status = DataStatus.OK;

                dbStatus.First().timestamp = DateTime.UtcNow;
            }
        }

        private void InitLightsOutput()
        {
            // CAP
            if (adminSettingsVM.regulationStandard == RegulationStandard.CAP)
            {
                // Hente lights output data fra fil
                lightsOutputData = new SensorData(config.GetLightsOutputData());

                ucHMSLightsOutput.Init(lightsOutputData, hmsLightsOutputVM, config, adminSettingsVM, errorHandler);

                // Dispatcher som oppdaterer verification data (test og referanse data)
                lightsOutputTimer.Interval = TimeSpan.FromMilliseconds(config.ReadWithDefault(ConfigKey.LightsOutputFrequency, Constants.LightsOutputFrequencyDefault));
                lightsOutputTimer.Tick += runLightsOutputUpdate;

                void runLightsOutputUpdate(object sender, EventArgs e)
                {
                    Thread thread = new Thread(() => SendLightsOutput_Thread());
                    thread.IsBackground = true;
                    thread.Start();

                    void SendLightsOutput_Thread()
                    {
                        try
                        {
                            // Sende lys signal
                            SerialPort serialPort = new SerialPort();

                            // Serie port parametre
                            serialPort.PortName = lightsOutputData.modbus.portName;
                            serialPort.BaudRate = lightsOutputData.modbus.baudRate;
                            serialPort.DataBits = lightsOutputData.modbus.dataBits;
                            serialPort.StopBits = lightsOutputData.modbus.stopBits;
                            serialPort.Parity = lightsOutputData.modbus.parity;
                            serialPort.Handshake = lightsOutputData.modbus.handshake;

                            // Åpne serie port
                            serialPort.Open();

                            if (serialPort.IsOpen)
                            {
                                // Sende lys signal
                                serialPort.Write(hmsLightsOutputVM.HMSLightsOutput.ToString());
                            }
                            else
                            {
                                errorHandler.Insert(
                                    new ErrorMessage(
                                        DateTime.UtcNow,
                                        ErrorMessageType.SerialPort,
                                        ErrorMessageCategory.None,
                                        string.Format("Lights Output Error\n\nUnable to open port.")));
                            }

                            // Lukke serie port
                            serialPort.Close();
                        }
                        catch (Exception ex)
                        {
                            if (!lightsOutputError)
                            {
                                lightsOutputError = true;

                                errorHandler.Insert(
                                    new ErrorMessage(
                                        DateTime.UtcNow,
                                        ErrorMessageType.SerialPort,
                                        ErrorMessageCategory.AdminUser,
                                            string.Format("Lights Output Error\n\nCheck lights output setup.\n\n{0}", ex.Message)));
                            }
                        }
                    }
                }
            }
            else
            {
                tabHMS_LightsOutput.Visibility = Visibility.Collapsed;

                ucHMSLightsOutput.Init(new SensorData(SensorType.ModbusRTU), hmsLightsOutputVM, config, adminSettingsVM, errorHandler);
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

            // Reset
            lightsOutputError = false;
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

                // Resette dataCalculations
                if (adminSettingsVM.dataVerificationEnabled)
                {
                    // Resetter data listene i dataCalculations kun ved data verifikasjons-testing
                    // Under normal kjøring kan "gamle" data i listene være fullt brukelig ettersom
                    // de uansett blir slettet automatisk når de faktisk er for gamle.
                    hmsProcessing.ResetDataCalculations();

                    // Resette sammenligningsdata
                    verfication.Reset();
                }

                // Starter data-innhenting 
                sensorDataRetrieval.SensorDataRetrieval_Start();

                // Sette knappe status
                SetStartStopButtons(false);
                btnSetup.IsEnabled = false;

                // HMS prosessering updater
                hmsTimer.Start();
                hmsDatabaseTimer.Start();

                // Database Maintenance
                DoDatabaseMaintenance(DatabaseMaintenanceType.SENSOR);  // Kjør vedlikehold en gang ved oppstart
                maintenanceTimer.Start();                               // Og så hver X timer

                // Socket Listener
                socketListener.Start();

                // Sensor Status
                sensorStatus.Start(hmsInputData);

                // Lights Output
                if (adminSettingsVM.regulationStandard == RegulationStandard.CAP)
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
            btnSetup.IsEnabled = true;

            // HMS prosessering updater
            hmsTimer.Stop();
            hmsDatabaseTimer.Stop();

            // Database Maintenance
            maintenanceTimer.Stop();

            // Socket Listener
            socketListener.Stop();

            // Sensor Status
            sensorStatus.Stop();

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

        private void btnSetup_Click(object sender, RoutedEventArgs e)
        {
            // Forhåndsvalgt sensor data
            //int id;
            //if (sensorDataSelected != null)
            //    id = sensorDataSelected.id;
            //else
            //    id = -1;

            // Open new modal window 
            SensorSetupWindow sensorSetupWindow = new SensorSetupWindow(config, errorHandler/*, id*/);
            sensorSetupWindow.Owner = Application.Current.MainWindow;
            sensorSetupWindow.ShowDialog();

            // Laste sensor data setups fra fil
            sensorDataRetrieval.LoadSensors();

            // Resette display lister
            statusDisplayList.Clear();
            sensorDataDisplayList.Clear();
            serialPortDataDisplayList.Clear();
            fileReaderDataDisplayList.Clear();
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
                            btnSetup.Visibility = Visibility.Visible;

                            tabOutput.Visibility = Visibility.Visible;
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
                        btnSetup.Visibility = Visibility.Collapsed;

                        // Sette Input tab som valgt tab og skjule resten
                        tabInput.IsSelected = true;

                        tabOutput.Visibility = Visibility.Collapsed;
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
        }
    }
}
