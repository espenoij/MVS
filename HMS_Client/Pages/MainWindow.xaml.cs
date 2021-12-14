using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Telerik.Windows.Controls;

namespace HMS_Client
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : RadWindow
    {
        // Configuration settings
        private Config config;

        // TCP/IP Socket console for utskrift av trafikk
        private SocketConsole socketConsole;

        // Server communications modul
        private ServerCom serverCom = new ServerCom();

        // Data Request callback
        public delegate void DataRequestCallback();

        // User Inputs have been set callback
        public delegate void UserInputsSetCallback();

        // Liste/samling med sensor data: Data som skal presenteres i HMS klienten
        private HMSDataCollection hmsDataCollection;

        // Sensor Status
        private SensorGroupStatus sensorStatus;

        // Application Restart Required callback
        public delegate void RestartRequiredCallback(bool showMessage);

        // Sette vind visning til 2-min mean callback
        public delegate void ResetWindDisplayCallback();

        // View Model
        private MainWindowVM mainWindowVM = new MainWindowVM();
        private GeneralInformationVM generalInformationVM = new GeneralInformationVM();
        private UserInputsVM userInputsVM = new UserInputsVM();
        private HelideckMotionLimitsVM helideckMotionLimitsVM = new HelideckMotionLimitsVM();
        private HelideckMotionTrendVM helideckMotionTrendVM = new HelideckMotionTrendVM();
        private HelideckStabilityLimitsVM helideckStabilityLimitsVM = new HelideckStabilityLimitsVM();
        private HelideckRelativeWindLimitsVM helideckRelativeWindLimitsVM = new HelideckRelativeWindLimitsVM();
        private WindHeadingTrendVM helideckWindHeadingTrendVM = new WindHeadingTrendVM();
        private HelideckStatusVM helideckStatusVM = new HelideckStatusVM();
        private HelideckStatusTrendVM helideckStatusTrendVM = new HelideckStatusTrendVM();
        private MeteorologicalVM meteorologicalVM = new MeteorologicalVM();
        private SensorStatusDisplayVM sensorStatusVM = new SensorStatusDisplayVM();
        private WindHeadingVM windHeadingVM = new WindHeadingVM();
        private AdminSettingsVM adminSettingsVM = new AdminSettingsVM();
        private HelideckReportVM helideckReportVM = new HelideckReportVM();

        private RegulationStandard regulationStandard;

        public MainWindow()
        {
            DataContext = mainWindowVM;

            // Config
            config = new Config();

            // Socket Console
            socketConsole = new SocketConsole();

            // HMS Data Collection
            hmsDataCollection = new HMSDataCollection();

            // Sensor Status
            sensorStatus = new SensorGroupStatus(config, hmsDataCollection);

            // Regulation Standard
            regulationStandard = (RegulationStandard)Enum.Parse(typeof(RegulationStandard), config.ReadWithDefault(ConfigKey.RegulationStandard, RegulationStandard.NOROG.ToString()));

            // Callback funksjon som kalles når application restart er påkrevd
            RestartRequiredCallback restartRequired = new RestartRequiredCallback(ShowRestartRequiredMessage);

            // Sette vind visning til 2-min mean callback
            ResetWindDisplayCallback resetWindDisplayCallback = new ResetWindDisplayCallback(ResetWindDisplay);

            // View Model Init
            mainWindowVM.Init();
            adminSettingsVM.Init(config, serverCom, restartRequired);
            helideckReportVM.Init(config, sensorStatus);
            generalInformationVM.Init(config, adminSettingsVM, sensorStatus);

            userInputsVM.Init(
                adminSettingsVM,
                helideckMotionLimitsVM,
                config,
                mainWindowVM,
                helideckRelativeWindLimitsVM,
                helideckStabilityLimitsVM,
                helideckWindHeadingTrendVM,
                helideckRelativeWindLimitsVM,
                windHeadingVM,
                resetWindDisplayCallback,
                serverCom);

            helideckMotionLimitsVM.Init(config, sensorStatus);
            helideckMotionTrendVM.Init(config, sensorStatus);
            helideckStabilityLimitsVM.Init(config, sensorStatus);
            helideckRelativeWindLimitsVM.Init(config, sensorStatus);
            helideckWindHeadingTrendVM.Init(config, sensorStatus, userInputsVM);
            helideckStatusVM.Init(config, sensorStatus, userInputsVM);
            helideckStatusTrendVM.Init(helideckStatusVM, helideckStabilityLimitsVM, config);
            meteorologicalVM.Init(config, sensorStatus);
            sensorStatusVM.Init(sensorStatus, config);
            windHeadingVM.Init(config, sensorStatus, userInputsVM, adminSettingsVM);

            // XAML
            InitializeComponent();

            // Init av UI
            InitUI();

            // Admin Modus
            InitAdminMode();

            // Data Request modul
            InitDataRequest();

            // Helideck Report

            // Admin grensesnittet
            AdminMode.IsActive = false;
            tabAdminSettings.Visibility = Visibility.Collapsed;
            tabAdminDataFlow.Visibility = Visibility.Collapsed;

            ucAdminSettings.UIUpdateServerRunning(true);

            // TODO: DEBUG DEBUG DEBUG !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!! Fjerne det under før release
            AdminMode.IsActive = true;
            tabAdminSettings.Visibility = Visibility.Visible;
            tabAdminDataFlow.Visibility = Visibility.Visible;
        }

        private void InitUI()
        {
            // Server stop/start
            ucAdminSettings.UIUpdateServerRunning(false);

            // Gi data tilgang til de forskjellige sub-delene av UI
            //////////////////////////////////////////////////////////
            ///
            if (regulationStandard == RegulationStandard.NOROG)
            {
                // Justere grid litt slik at det ser bra ut
                // Grid i XAML er tilpasset CAP layout
                GridLengthConverter gridLengthConverter = new GridLengthConverter();
                col1RowDef1.Height = (GridLength)gridLengthConverter.ConvertFrom("15*");
                col1RowDef2.Height = (GridLength)gridLengthConverter.ConvertFrom("12*");
                col1RowDef3.Height = (GridLength)gridLengthConverter.ConvertFrom("9*");

                // General Information
                capDisplayMode.Visibility = Visibility.Collapsed;
                ucGeneralInformation_NOROG.Visibility = Visibility.Visible;
                ucGeneralInformation_CAP.Visibility = Visibility.Collapsed;

                ucGeneralInformation_NOROG.Init(generalInformationVM);

                // User Inputs
                ucUserInputs_NOROG.Visibility = Visibility.Visible;
                ucUserInputs_CAP.Visibility = Visibility.Collapsed;

                ucUserInputs_NOROG.Init(userInputsVM, config, adminSettingsVM);

                // Helideck Motion Limits
                ucHelideckMotionLimits_NOROG.Visibility = Visibility.Visible;
                ucHelideckMotionLimits_CAP.Visibility = Visibility.Collapsed;
                ucHelideckMotionLimits_NOROG.Init(helideckMotionLimitsVM, config, tabHelicopterOps);

                // Helideck Status
                ucHelideckStatus_NOROG.Visibility = Visibility.Visible;
                ucHelideckStatus_CAP.Visibility = Visibility.Collapsed;
                ucHelideckStatus_NOROG.Init(helideckStatusVM);

                // Helideck Motion Trend
                ucHelideckMotionTrend_NOROG.Visibility = Visibility.Visible;
                ucHelideckMotionTrend_NOROG.Init(helideckMotionTrendVM, config);

                // Helideck Stability Limits
                ucHelideckStabilityLimits_CAP.Visibility = Visibility.Collapsed;

                // Relative Wind Direction Limits
                ucRelativeWindDirectionLimits_CAP.Visibility = Visibility.Collapsed;

                // Helideck Wind & Heading Trend
                ucHelideckWindHeadingTrend_CAP.Visibility = Visibility.Collapsed;
                // Helideck Status Trend
                ucHelideckStatusTrend_CAP.Visibility = Visibility.Collapsed;

                // Helideck Report
                tabHelideckReport_NOROG.Visibility = Visibility.Visible;
                tabHelideckReport_CAP.Visibility = Visibility.Collapsed;
                ucHelideckReport_NOROG.Init(helideckReportVM, config);
            }
            else
            if (regulationStandard == RegulationStandard.CAP)
            {
                // Justere grid litt slik at det ser bra ut
                GridLengthConverter gridLengthConverter = new GridLengthConverter();
                col1RowDef1.Height = (GridLength)gridLengthConverter.ConvertFrom("14*");
                col1RowDef2.Height = (GridLength)gridLengthConverter.ConvertFrom("13*");
                col1RowDef3.Height = (GridLength)gridLengthConverter.ConvertFrom("9*");

                // General Information
                capDisplayMode.Visibility = Visibility.Visible;
                ucGeneralInformation_NOROG.Visibility = Visibility.Collapsed;
                ucGeneralInformation_CAP.Visibility = Visibility.Visible;

                ucGeneralInformation_CAP.Init(generalInformationVM, config);

                // User Inputs
                ucUserInputs_NOROG.Visibility = Visibility.Collapsed;
                ucUserInputs_CAP.Visibility = Visibility.Visible;

                ucUserInputs_CAP.Init(userInputsVM, config, adminSettingsVM, helideckStabilityLimitsVM, helideckStatusTrendVM);

                // Helideck Motion Limits
                ucHelideckMotionLimits_NOROG.Visibility = Visibility.Collapsed;
                ucHelideckMotionLimits_CAP.Visibility = Visibility.Visible;
                ucHelideckMotionLimits_CAP.Init(helideckMotionLimitsVM, config, tabHelicopterOps);

                // Helideck Status
                ucHelideckStatus_NOROG.Visibility = Visibility.Collapsed;
                ucHelideckStatus_CAP.Visibility = Visibility.Visible;
                ucHelideckStatus_CAP.Init(helideckStatusVM);

                // Helideck Motion Trend
                ucHelideckMotionTrend_NOROG.Visibility = Visibility.Collapsed;

                // Helideck Stability Limits
                ucHelideckStabilityLimits_CAP.Visibility = Visibility.Visible;
                ucHelideckStabilityLimits_CAP.Init(helideckStabilityLimitsVM, helideckStatusTrendVM);

                // Relative Wind Direction Limits
                ucRelativeWindDirectionLimits_CAP.Visibility = Visibility.Visible;
                ucRelativeWindDirectionLimits_CAP.Init(helideckRelativeWindLimitsVM);

                // Helideck Wind & Heading Trend
                ucHelideckWindHeadingTrend_CAP.Visibility = Visibility.Visible;
                ucHelideckWindHeadingTrend_CAP.Init(helideckWindHeadingTrendVM);

                // Helideck Status Trend
                ucHelideckStatusTrend_CAP.Visibility = Visibility.Visible;
                ucHelideckStatusTrend_CAP.Init(helideckStatusTrendVM, config);

                // Helideck Report
                tabHelideckReport_CAP.Visibility = Visibility.Visible;
                tabHelideckReport_NOROG.Visibility = Visibility.Collapsed;
                ucHelideckReport_CAP.Init(helideckReportVM, config);
            }

            // Helideck Motion Trend Zoom
            ucHelideckMotionHistory.Init(helideckMotionTrendVM);

            // Sensor Status Display
            ucSensorStatus.Init(sensorStatusVM);

            // Wind & Heading
            ucWindHeading.Init(windHeadingVM, config);

            // Meteorological
            ucMeteorological.Init(meteorologicalVM);

            // Admin Settings
            ucAdminSettings.Init(
                adminSettingsVM,
                generalInformationVM,
                windHeadingVM,
                config,
                socketConsole,
                serverCom);

            // Admin Data Flow
            ucAdminDataFlow.Init(
                hmsDataCollection,
                sensorStatus);
        }

        private void InitDataRequest()
        {
            // Callback funksjon som kalles når DataRequest er ferdig med å hente data
            DataRequestCallback processCallback = new DataRequestCallback(DataRequestComplete);

            serverCom.Init(
                socketConsole,
                hmsDataCollection,
                adminSettingsVM,
                sensorStatus,
                sensorStatusVM,
                userInputsVM,
                processCallback,
                config);
        }

        public void ShowRestartRequiredMessage(bool showMessage)
        {
            if (showMessage)
                dpApplicationRestartRequired.Visibility = Visibility.Visible;
            else
                dpApplicationRestartRequired.Visibility = Visibility.Collapsed;
        }

        public void ResetWindDisplay()
        {
            ucWindHeading.ResetWindDisplay();
        }

        public void DataRequestComplete()
        {
            UpdateHMSData(hmsDataCollection);
        }

        private void InitAdminMode()
        {
            // Admin mode key event
            this.KeyDown += new KeyEventHandler(OnAdminKeyCommand);
        }

        private void OnAdminKeyCommand(object sender, KeyEventArgs e)
        {
            if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift &&
                (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                if (Keyboard.IsKeyDown(Key.H))
                {
                    if (!tabAdminSettings.IsVisible)
                    {
                        // Åpne admin passord vindu
                        DialogAdminMode adminMode = new DialogAdminMode();
                        adminMode.Owner = App.Current.MainWindow;
                        adminMode.ShowDialog();

                        if (AdminMode.IsActive)
                        {
                            // Vise admin grensesnittet
                            tabAdminSettings.Visibility = Visibility.Visible;
                            tabAdminDataFlow.Visibility = Visibility.Visible;
                        }
                    }
                    else
                    {
                        // Skjule admin grensesnittet
                        tabAdminSettings.Visibility = Visibility.Collapsed;
                        tabAdminDataFlow.Visibility = Visibility.Collapsed;
                    }
                }
            }
        }

        private void UpdateHMSData(HMSDataCollection hmsDataCollection)
        {
            // Fordele data videre ut i UI

            // General Information
            generalInformationVM.UpdateData(hmsDataCollection);

            // User Inputs
            userInputsVM.UpdateData(hmsDataCollection);

            // Helideck Motion
            helideckMotionLimitsVM.UpdateData(hmsDataCollection);

            // Helideck Motion Trend
            helideckMotionTrendVM.UpdateData(hmsDataCollection);

            if (regulationStandard == RegulationStandard.NOROG)
            {
            }
            else
            if (regulationStandard == RegulationStandard.CAP)
            {
                // Helideck Stability Limits
                helideckStabilityLimitsVM.UpdateData(hmsDataCollection);

                // Helideck Relative Wind Limits
                helideckRelativeWindLimitsVM.UpdateData(hmsDataCollection);

                // Helideck Heading Trend
                helideckWindHeadingTrendVM.UpdateData(hmsDataCollection);
            }

            // Helideck Status
            helideckStatusVM.UpdateData(hmsDataCollection);

            // Meteorological
            meteorologicalVM.UpdateData(hmsDataCollection);

            // Wind & Heading
            windHeadingVM.UpdateData(hmsDataCollection);

            // Helideck Report
            helideckReportVM.UpdateData(hmsDataCollection);
        }

        private void btnScreenCapture_Click(object sender, RoutedEventArgs e)
        {
            // Ta et screenshot
            string screenCaptureFolder = Path.Combine(Environment.CurrentDirectory, Constants.HelideckReportFolder);
            string screenCaptureFile = string.Format("{0}_{1}.jpg", Constants.ScreenCaptureFilename, DateTime.UtcNow.ToString("yyyy-MM-dd_hh-mm-ss"));

            ScreenCapture screenCapture = new ScreenCapture();
            screenCapture.Capture(screenCaptureFolder, screenCaptureFile);

            // Lagrer filnavnet til bruk i helideck report
            helideckReportVM.screenCaptureFile = screenCaptureFile;
        }

        private void tcMainMenu_SelectionChanged(object sender, RadSelectionChangedEventArgs e)
        {
            // Viser screen capture knappen kun på hovedsiden og når NOROG standard er valgt
            if (tabHelicopterOps.IsSelected && adminSettingsVM.regulationStandard == RegulationStandard.NOROG)
                btnScreenCapture.Visibility = Visibility.Visible;
            else
                btnScreenCapture.Visibility = Visibility.Collapsed;

            // Fjerne fokus fra alle elementer slik at ikke knapper og felt lyser opp i rødt når vi bytter tab
            Dispatcher.BeginInvoke((Action)(() =>
            {
                (sender as RadTabControl).Focus();
            }), DispatcherPriority.ApplicationIdle);
        }
    }
}
