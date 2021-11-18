using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
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
        private RadObservableCollectionEx<SensorData> statusDisplayList = new RadObservableCollectionEx<SensorData>();
        private RadObservableCollectionEx<SensorData> sensorDataDisplayList = new RadObservableCollectionEx<SensorData>();
        private RadObservableCollectionEx<SerialPortData> serialPortDataDisplayList = new RadObservableCollectionEx<SerialPortData>();

        //private SensorData sensorDataSelected = new SensorData();

        // Database
        private DatabaseHandler database = new DatabaseHandler();

        // Configuration settings
        private Config config;

        // Sensor data service for behandling av data
        private SensorDataRetrieval sensorDataRetrieval;

        // Database Maintenance Dispatcher
        private System.Timers.Timer maintenanceTimer;

        // Error Messages Window
        private ErrorMessagesWindow errorMessagesWindow;

        // Error Handler
        private ErrorHandler errorHandler;

        // Socket Listener
        private SocketListener socketListener;
        // Socket console hvor vi mottar meldinger fra socket listener
        private SocketConsole socketConsole = new SocketConsole();

        public delegate void UserInputsCallback(UserInputs userInputs);

        // Liste med HMS sensor data: Data som skal gjennom HMS prosessen og sendes til HMS klient
        private HMSDataCollection hmsInputData;

        // Sensor Status
        private HMSSensorGroupStatus sensorStatus;

        // HMS
        private HMSProcessing hmsProcessing;
        private HMSDataCollection hmsOutputData;
        private HMSDatabase hmsDatabase;

        // View Models
        private AdminSettingsVM adminSettingsVM = new AdminSettingsVM();

        // Motion Limits
        private HelideckMotionLimits motionLimits;

        // User Inputs
        private UserInputs userInputs = new UserInputs();

        // Application Restart Required callback
        public delegate void RestartRequiredCallback(bool showMessage);

        public MainWindow()
        {
            InitializeComponent();

            // Config
            config = new Config();

            // Error Handler
            errorHandler = new ErrorHandler(database);

            // Init view model
            InitViewModel();

            // Init
            sensorDataRetrieval = new SensorDataRetrieval(config, database, errorHandler);

            // HMS Output data
            hmsOutputData = new HMSDataCollection();

            // HMS Database
            hmsDatabase = new HMSDatabase(database, errorHandler);

            // Admin Settings
            ucAdminSettings.Init(
                adminSettingsVM,
                config,
                database,
                errorHandler,
                sensorDataDisplayList,
                hmsOutputData.GetDataList());

            // Sensor Status
            sensorStatus = new HMSSensorGroupStatus(config, hmsOutputData);

            // Laste sensor data setups fra fil
            sensorDataRetrieval.LoadSensors();

            // Starter kjøring av status oppdatering
            sensorDataRetrieval.UpdateSensorStatus();

            // Socket Init
            InitSocketConsole();

            // Init UI
            InitUI();
            InitUIHMS();

            // Init User Inputs
            InitUserInput();

            // Init UI Input
            InitUIInputUpdate();

            // Init HMS Data Flow
            InitHMSUpdate();

            // Init HMS Database operations
            InitHMSDatabaseOps();

            // Opprette database tabeller
            // CreateTables gjør selv sjekk på om tabell finnes fra før eller må opprettes
            try
            {
                database.CreateTables(sensorDataRetrieval.GetSensorDataList());

                errorHandler.ResetDatabaseError(ErrorHandler.DatabaseErrorType.CreateTables);
            }
            catch (Exception ex)
            {
                errorHandler.Insert(
                     new ErrorMessage(
                         DateTime.UtcNow,
                         ErrorMessageType.Database,
                         ErrorMessageCategory.None,
                         string.Format("Database Error (CreateTables 1)\n\nSystem Message:\n{0}", ex.Message)));

                errorHandler.SetDatabaseError(ErrorHandler.DatabaseErrorType.CreateTables);
            }

            // Database Maintenance Init
            DatabaseMaintenanceInit();

            // Database Status Check
            DispatcherTimer databaseStatusCheck = new DispatcherTimer();
            databaseStatusCheck.Interval = TimeSpan.FromMilliseconds(2000);
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
                }
                catch (Exception ex)
                {
                    errorHandler.Insert(
                         new ErrorMessage(
                             DateTime.UtcNow,
                             ErrorMessageType.Database,
                             ErrorMessageCategory.Admin,
                             string.Format("Database Status Check Error (checkDatabaseStatus)\n\nSystem Message:\n{0}", ex.Message)));

                    errorHandler.SetDatabaseError(ErrorHandler.DatabaseErrorType.CreateTables);
                }
            }

            // TODO: DEBUG DEBUG DEBUG !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!! Fjerne det under før release
            // Vise admin grensesnittet
            AdminMode.IsActive = true;
            btnErrorMessages.Visibility = Visibility.Visible;
            btnSetup.Visibility = Visibility.Visible;
            tabInput_SensorData.Visibility = Visibility.Visible;
            tabInput_SerialData.Visibility = Visibility.Visible;
            tabInput_SocketConsole.Visibility = Visibility.Visible;

            // Starte server automatisk ved oppstart
            StartServer();
        }

        private void InitViewModel()
        {
            // Callback funksjon som kalles når application restart er påkrevd
            RestartRequiredCallback restartRequired = new RestartRequiredCallback(ShowRestartRequiredMessage);

            adminSettingsVM.Init(config, restartRequired);
        }

        private void InitUI()
        {
            // Init Admin Mode
            InitAdminMode();

            // Liste med sensor status
            gvStatusDisplay.ItemsSource = statusDisplayList;

            // Liste med sensor verdier
            gvSensorDataDisplay.ItemsSource = sensorDataDisplayList;

            // Liste med serieport verdier
            gvSerialPortDataDisplay.ItemsSource = serialPortDataDisplayList;

            // Div UI init
            btnStart1.IsEnabled = true;
            btnStart2.IsEnabled = true;
            btnStop1.IsEnabled = false;
            btnStop2.IsEnabled = false;
        }

        private void InitUIHMS()
        {
            // Motion Limits
            motionLimits = new HelideckMotionLimits(userInputs, config, adminSettingsVM);

            // HMS data list
            hmsInputData = new HMSDataCollection();
            hmsInputData.LoadHMSInputDefinitions(config);

            // HMS data: Prosessering av input data til output data
            hmsProcessing = new HMSProcessing(motionLimits, hmsOutputData, adminSettingsVM, userInputs, errorHandler);

            // Data Flow
            ucHMSDataSetup.Init(
                sensorDataDisplayList,
                hmsInputData,
                hmsOutputData,
                config);

            // Sensor Groups
            ucHMSSensorGroups.Init(
                sensorDataDisplayList,
                hmsInputData,
                config,
                sensorStatus);
        }

        private void InitUserInput()
        {
            try
            {
                // Lese sist brukte user inputs fra fil
                userInputs.helicopterType = (HelicopterType)Enum.Parse(typeof(HelicopterType), config.Read(ConfigKey.HelicopterType));
                userInputs.helideckCategory = (HelideckCategory)Enum.Parse(typeof(HelideckCategory), config.Read(ConfigKey.HelideckCategory));
                userInputs.dayNight = (DayNight)Enum.Parse(typeof(DayNight), config.Read(ConfigKey.DayNight));
                userInputs.displayMode = (DisplayMode)Enum.Parse(typeof(DisplayMode), config.Read(ConfigKey.DisplayMode));

                if (DateTime.TryParse(config.Read(ConfigKey.OnDeckTime), out DateTime onDeckTime))
                    userInputs.onDeckTime = onDeckTime;
                else
                    userInputs.onDeckTime = DateTime.UtcNow;

                userInputs.onDeckHelicopterHeading = config.Read(ConfigKey.OnDeckHelicopterHeading, Constants.HeadingDefault);
                userInputs.onDeckVesselHeading = config.Read(ConfigKey.OnDeckVesselHeading, Constants.HeadingDefault);
                userInputs.onDeckWindDirection = config.Read(ConfigKey.OnDeckWindDirection, Constants.HeadingDefault);
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
            socketListener = new SocketListener(hmsOutputData, sensorStatus, socketConsole, userInputs, userInputsCallback);
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
            // Lukke alle serie porter
            sensorDataRetrieval.SensorDataRetrieval_Stop();
        }

        private void InitUIInputUpdate()
        {
            // Dispatcher som oppdatere UI
            DispatcherTimer uiTimer = new DispatcherTimer();
            uiTimer.Interval = TimeSpan.FromMilliseconds(Constants.ServerUpdateFrequencyUI);
            uiTimer.Tick += runUpdateUI;
            uiTimer.Start();

            void runUpdateUI(object sender, EventArgs e)
            {
                try
                {
                    Thread thread = new Thread(() => UpdateUI_Thread());
                    thread.Start();

                    void UpdateUI_Thread()
                    {
                        // Input: Status
                        /////////////////////////////////////////////////////////////////////////////////////////////////////

                        // Hente listen med prosesserte sensor data
                        foreach (var item in sensorDataRetrieval.GetSensorDataList().ToList())
                        {
                            // Finne igjen sensor i display listen
                            var sensorDataList = statusDisplayList.Where(x => x.id == item.id);

                            // Dersom vi fant sensor
                            if (sensorDataList.Count() > 0)
                            {
                                // Oppdater data
                                sensorDataList.First().timestamp = item.timestamp;
                                sensorDataList.First().data = item.data;
                            }
                            // ...fant ikke sensor
                            else
                            {
                                // Legg den inn i listen
                                statusDisplayList.Add(item);
                            }
                        }

                        if (AdminMode.IsActive)
                        {
                            // Input: Sensor Data
                            /////////////////////////////////////////////////////////////////////////////////////////////////////

                            // Hente listen med prosesserte sensor data
                            foreach (var item in sensorDataRetrieval.GetSensorDataList().ToList())
                            {
                                // Finne igjen sensor i display listen
                                var sensorDataList = sensorDataDisplayList.Where(x => x.id == item.id);

                                // Dersom vi fant sensor
                                if (sensorDataList.Count() > 0)
                                {
                                    // Oppdater data
                                    sensorDataList.First().timestamp = item.timestamp;
                                    sensorDataList.First().data = item.data;
                                }
                                // ...fant ikke sensor
                                else
                                {
                                    // Legg den inn i listen
                                    sensorDataDisplayList.Add(item);
                                }
                            }

                            // TAB: Serial Ports
                            /////////////////////////////////////////////////////////////////////////////////////////////////////

                            // Gå gjennom listen med mottatte data fra serie port
                            foreach (var item in sensorDataRetrieval.GetSerialPortDataReceivedList().ToList())
                            {
                                // Finne korrekt serie port
                                var serialPortDataList = serialPortDataDisplayList.Where(x => x.portName == item.portName);

                                // Port eksisterer -> Oppdater data
                                if (serialPortDataList.Count() > 0)
                                {
                                    // Oppdatere data hentet fra serie port
                                    serialPortDataList.First().data = item.data;
                                    serialPortDataList.First().timestamp = item.timestamp;

                                    // Oppdatere serie port status
                                    serialPortDataList.First().portStatus = item.portStatus;
                                }
                                // Dersom den ikke eksisterer -> legge inn ny i listen for serie port data display
                                else
                                {
                                    // Lagre i listen
                                    serialPortDataDisplayList.Add(item);
                                }
                            }
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
                            string.Format("UI Update Error (runUpdateUI)\n\nSystem Message:\n{0}", ex.Message)));

                    errorHandler.SetDatabaseError(ErrorHandler.DatabaseErrorType.DatabaseMaintenance);
                }
            }
        }

        private void InitHMSUpdate()
        {
            // Dispatcher som oppdatere HMS prosessering
            DispatcherTimer hmsTimer = new DispatcherTimer();
            hmsTimer.Interval = TimeSpan.FromMilliseconds(Constants.ServerUpdateFrequencyHMS);
            hmsTimer.Tick += runUpdateHMS;
            hmsTimer.Start();

            void runUpdateHMS(object sender, EventArgs e)
            {
                try
                { 
                    Thread thread = new Thread(() => UpdateHMS_Thread());
                    thread.Start();

                    void UpdateHMS_Thread()
                    {
                        // HMS: HMS Input Data
                        /////////////////////////////////////////////////////////////////////////////////////////////////////
                        hmsInputData.Transfer(sensorDataRetrieval.GetSensorDataList());

                        // HMS: HMS Output Data
                        /////////////////////////////////////////////////////////////////////////////////////////////////////
                        // Prosesserer sensor data om til data som kan sendes til HMS klient
                        hmsProcessing.Update(hmsInputData);
                    }
                }
                catch (Exception ex)
                {
                    errorHandler.Insert(
                        new ErrorMessage(
                            DateTime.UtcNow,
                            ErrorMessageType.Database,
                            ErrorMessageCategory.Admin,
                            string.Format("UI Update Error (runUpdateHMS)\n\nSystem Message:\n{0}", ex.Message)));

                    errorHandler.SetDatabaseError(ErrorHandler.DatabaseErrorType.DatabaseMaintenance);
                }
            }
        }

        private void InitHMSDatabaseOps()
        {
            bool databaseTablesCreated = false;

            // Dispatcher som oppdatere HMS prosessering
            DispatcherTimer hmsTimer = new DispatcherTimer();
            hmsTimer.Interval = TimeSpan.FromMilliseconds(Constants.DBHMSSaveFrequency);
            hmsTimer.Tick += runHMSDatabaseOps;
            hmsTimer.Start();

            void runHMSDatabaseOps(object sender, EventArgs e)
            {
                try
                { 
                    Thread thread = new Thread(() => UpdateHMS_Thread());
                    thread.Start();

                    void UpdateHMS_Thread()
                    {
                        // HMS: Lagre data i databasen
                        /////////////////////////////////////////////////////////////////////////////////////////////////////

                        // Utfører første en sjekk på om database tabellene er opprettet.
                        // Grunnen til at dette gjøres her, så sent etter init, er at output listen fylles av 
                        // inidividuelle sub-rutiner rundt omkring.
                        // Output listen er derfor ikke komplett før en update (hmsProcessing.Update ovenfor) er utført.
                        if (!databaseTablesCreated)
                        {
                            hmsDatabase.CreateHMSDataTables(hmsOutputData);
                            databaseTablesCreated = true;

                            // Kjør vedlikehold en gang ved oppstart
                            DoDatabaseMaintenance(DatabaseMaintenanceType.HMS);
                        }

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
                            string.Format("Database Ops Error (runHMSDatabaseOps)\n\nSystem Message:\n{0}", ex.Message)));

                    errorHandler.SetDatabaseError(ErrorHandler.DatabaseErrorType.DatabaseMaintenance);
                }
            }
        }

        public void UpdateUserInputs(UserInputs userInputs)
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
            try
            {
                // Oppretter tabellene dersom de ikke eksisterer
                database.CreateTables(sensorDataRetrieval.GetSensorDataList());

                errorHandler.ResetDatabaseError(ErrorHandler.DatabaseErrorType.CreateTables);
            }
            catch (Exception ex)
            {
                errorHandler.Insert(
                    new ErrorMessage(
                        DateTime.UtcNow,
                        ErrorMessageType.Database,
                        ErrorMessageCategory.None,
                        string.Format("Database Error (CreateTables 2)\n\nSystem Message:\n{0}", ex.Message)));

                errorHandler.SetDatabaseError(ErrorHandler.DatabaseErrorType.CreateTables);
            }

            // Starter data-innhenting 
            sensorDataRetrieval.SensorDataRetrieval_Start();

            // Sette knappe status
            btnStart1.IsEnabled = false;
            btnStart2.IsEnabled = false;
            btnStop1.IsEnabled = true;
            btnStop2.IsEnabled = true;
            btnSetup.IsEnabled = false;

            // Database Maintenance            
            DoDatabaseMaintenance(DatabaseMaintenanceType.SENSOR); // Kjør vedlikehold en gang ved oppstart
            maintenanceTimer.Start();   // Og så hver X timer

            // Socket Listener
            socketListener.Start();

            // Socket console updater
            //socketConsoleUpdater.Start();
        }

        private void StopServer()
        {
            sensorDataRetrieval.SensorDataRetrieval_Stop();
            
            btnStart1.IsEnabled = true;
            btnStart2.IsEnabled = true;
            btnStop1.IsEnabled = false;
            btnStop2.IsEnabled = false;
            btnSetup.IsEnabled = true;

            // Database Maintenance
            maintenanceTimer.Stop();

            // Socket Listener
            socketListener.Stop();

            // Socket console updater
            //socketConsoleUpdater.Stop();
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
        }

        private void btnErrorMessages_Click(object sender, RoutedEventArgs e)
        {
            // Open window (men kun 1 instans av dette vinduet)
            if (errorMessagesWindow == null)
            {
                errorMessagesWindow = new ErrorMessagesWindow(config, errorHandler);
                errorMessagesWindow.Closed += (a, b) => errorMessagesWindow = null;
                
                errorMessagesWindow.Show();

                // Vise ikon på taskbar
                var window = errorMessagesWindow.ParentOfType<Window>();
                window.ShowInTaskbar = true;
            }
            else
            {

                errorMessagesWindow.Show();
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
            thread.Start();

            void runDatabaseMaintenance_Thread()
            {
                try
                {
                    switch (type)
                    {
                        case DatabaseMaintenanceType.SENSOR:
                            database.DatabaseMaintenance(statusDisplayList);
                            break;
                        case DatabaseMaintenanceType.HMS:
                            database.DatabaseMaintenance(hmsOutputData.GetDataList());
                            break;
                        case DatabaseMaintenanceType.ALL:
                            database.DatabaseMaintenance(statusDisplayList);
                            database.DatabaseMaintenance(hmsOutputData.GetDataList());
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
                if (Keyboard.IsKeyDown(Key.H))
                {
                    if (!btnErrorMessages.IsVisible)
                    {
                        // Åpne admin passord vindu
                        DialogAdminMode adminMode = new DialogAdminMode();
                        adminMode.Owner = App.Current.MainWindow;
                        adminMode.ShowDialog();

                        if (AdminMode.IsActive)
                        {
                            // Vise admin grensesnittet
                            btnErrorMessages.Visibility = Visibility.Visible;
                            btnSetup.Visibility = Visibility.Visible;

                            tabHMS.Visibility = Visibility.Visible;
                            tabOutput.Visibility = Visibility.Visible;
                            tabSettings.Visibility = Visibility.Visible;

                            tabInput_SensorData.Visibility = Visibility.Visible;
                            tabInput_SerialData.Visibility = Visibility.Visible;
                            tabInput_SocketConsole.Visibility = Visibility.Visible;
                        }
                    }
                    else
                    {
                        // Skjule admin grensesnittet
                        btnErrorMessages.Visibility = Visibility.Collapsed;
                        btnSetup.Visibility = Visibility.Collapsed;

                        // Sette Input tab som valgt tab og skjule resten
                        tabInput.IsSelected = true;
                        tabHMS.Visibility = Visibility.Collapsed;
                        tabOutput.Visibility = Visibility.Collapsed;
                        tabSettings.Visibility = Visibility.Collapsed;

                        // Sette Status tab som valgt sub tab
                        tabInput_SensorData.Visibility = Visibility.Collapsed;
                        tabInput_SerialData.Visibility = Visibility.Collapsed;
                        tabInput_SocketConsole.Visibility = Visibility.Collapsed;

                        tabInput_Status.IsSelected = true;
                        tabInput_SensorData.IsSelected = false;
                        tabInput_SerialData.IsSelected = false;
                        tabInput_SocketConsole.IsSelected = false;
                    }
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
            Dispatcher.BeginInvoke((Action)(() => {
                (sender as RadTabControl).Focus();
            }), DispatcherPriority.ApplicationIdle);
        }
    }
}
