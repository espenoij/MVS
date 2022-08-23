﻿using System;
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
        public delegate void WarningBarMessage(WarningBarMessageType message);

        // Sette vind visning til 2-min mean callback
        public delegate void SetDefaultWindMeasurementCallback();

        // Client denied callback
        public delegate void ClientDeniedCallback();
        private bool clientDeniedReceived = false;

        // View Model
        private MainWindowVM mainWindowVM = new MainWindowVM();
        private GeneralInformationVM generalInformationVM = new GeneralInformationVM();
        private UserInputsVM userInputsVM = new UserInputsVM();
        private HelideckMotionLimitsVM helideckMotionLimitsVM = new HelideckMotionLimitsVM();
        private HelideckMotionTrendVM helideckMotionTrendVM = new HelideckMotionTrendVM();
        private OnDeckStabilityLimitsVM onDeckStabilityLimitsVM = new OnDeckStabilityLimitsVM();
        private RelativeWindLimitsVM relativeWindLimitsVM = new RelativeWindLimitsVM();
        private LandingStatusTrendVM landingStatusTrendVM = new LandingStatusTrendVM();
        private WindHeadingChangeVM windHeadingChangeVM = new WindHeadingChangeVM();
        private HelideckStatusVM helideckStatusVM = new HelideckStatusVM();
        private MeteorologicalVM meteorologicalVM = new MeteorologicalVM();
        private SensorStatusDisplayVM sensorStatusVM = new SensorStatusDisplayVM();
        private WindHeadingVM windHeadingVM = new WindHeadingVM();
        private AdminSettingsVM adminSettingsVM = new AdminSettingsVM();
        private HelideckReportVM helideckReportVM = new HelideckReportVM();
        private EMSWaveDataVM emsWaveDataVM = new EMSWaveDataVM();

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

            // Callback funksjon som kalles når application restart er påkrevd
            WarningBarMessage warningBarMessage = new WarningBarMessage(ShowWarningBarMessage);

            // Sette vind visning til 2-min mean callback
            SetDefaultWindMeasurementCallback setDefaultWindMeasurementCallback = new SetDefaultWindMeasurementCallback(SetDefaultWindMeasurement);

            // View Model Init
            mainWindowVM.Init();
            adminSettingsVM.Init(config, serverCom, warningBarMessage);
            helideckReportVM.Init(config, adminSettingsVM, sensorStatus);
            generalInformationVM.Init(config, adminSettingsVM, sensorStatus, Application.ResourceAssembly.GetName().Version);

            userInputsVM.Init(
                adminSettingsVM,
                helideckMotionLimitsVM,
                config,
                mainWindowVM,
                onDeckStabilityLimitsVM,
                windHeadingChangeVM,
                relativeWindLimitsVM,
                windHeadingVM,
                setDefaultWindMeasurementCallback,
                serverCom);

            helideckMotionLimitsVM.Init(config, sensorStatus);
            helideckMotionTrendVM.Init(adminSettingsVM, config, sensorStatus);
            onDeckStabilityLimitsVM.Init(config, sensorStatus);
            relativeWindLimitsVM.Init(config, sensorStatus, onDeckStabilityLimitsVM);
            landingStatusTrendVM.Init(config, onDeckStabilityLimitsVM, helideckStatusVM);
            
            windHeadingChangeVM.Init(config, sensorStatus, relativeWindLimitsVM);

            helideckStatusVM.Init(config, sensorStatus, userInputsVM, adminSettingsVM);
            meteorologicalVM.Init(config, sensorStatus);
            sensorStatusVM.Init(sensorStatus, config, adminSettingsVM);
            windHeadingVM.Init(helideckStatusVM, config, sensorStatus, userInputsVM, adminSettingsVM);

            emsWaveDataVM.Init(adminSettingsVM, config, sensorStatus);

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

            // Information (CAP)
            if (adminSettingsVM.regulationStandard == RegulationStandard.CAP)
                tabInformation.Visibility = Visibility.Visible;
            else
                tabInformation.Visibility = Visibility.Collapsed;

            // EMS Page
            if (adminSettingsVM.enableEMS)
                tabEMS.Visibility = Visibility.Visible;
            else
                tabEMS.Visibility = Visibility.Collapsed;

            //// TODO: DEBUG DEBUG DEBUG !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!! Fjerne det under før release
            //AdminMode.IsActive = true;
            //tabAdminSettings.Visibility = Visibility.Visible;
            //tabAdminDataFlow.Visibility = Visibility.Visible;
        }

        private void InitUI()
        {
            // Server stop/start
            ucAdminSettings.UIUpdateServerRunning(false);

            // Gi data tilgang til de forskjellige sub-delene av UI
            //////////////////////////////////////////////////////////
            ///
            if (adminSettingsVM.regulationStandard == RegulationStandard.NOROG)
            {
                // General Information
                capDisplayMode.Visibility = Visibility.Collapsed;
                ucGeneralInformation_NOROG.Visibility = Visibility.Visible;
                ucGeneralInformation_CAP.Visibility = Visibility.Collapsed;

                ucGeneralInformation_NOROG.Init(generalInformationVM, Application.ResourceAssembly.GetName().Version);

                // User Inputs
                ucUserInputs_NOROG.Visibility = Visibility.Visible;
                ucUserInputs_CAP.Visibility = Visibility.Collapsed;

                ucUserInputs_NOROG.Init(userInputsVM, config, adminSettingsVM);

                // Helideck Motion Limits
                ucHelideckMotionLimits_NOROG.Visibility = Visibility.Visible;
                ucTouchdownLimits_CAP.Visibility = Visibility.Collapsed;
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

                // Wind & Heading Change
                ucWindHeadingChange_CAP.Visibility = Visibility.Collapsed;

                // Helideck Status Trend
                ucHelideckStatusTrend_CAP.Visibility = Visibility.Collapsed;

                // Motion History
                tabHelideckMotionHistory_NOROG.Visibility = Visibility.Visible;
                tabHelideckMotionHistory_CAP.Visibility = Visibility.Collapsed;
                ucHelideckMotionHistory_NOROG.Init(helideckMotionTrendVM);

                // Helideck Report
                tabHelideckReport_NOROG.Visibility = Visibility.Visible;
                tabHelideckReport_CAP.Visibility = Visibility.Collapsed;
                ucHelideckReport_NOROG.Init(helideckReportVM, config, adminSettingsVM);

                // Sensor Status Display
                ucSensorStatus_NOROG.Init(sensorStatusVM);
            }
            else
            if (adminSettingsVM.regulationStandard == RegulationStandard.CAP)
            {
                // General Information
                capDisplayMode.Visibility = Visibility.Visible;
                ucGeneralInformation_NOROG.Visibility = Visibility.Collapsed;
                ucGeneralInformation_CAP.Visibility = Visibility.Visible;

                ucGeneralInformation_CAP.Init(generalInformationVM);

                // User Inputs
                ucUserInputs_NOROG.Visibility = Visibility.Collapsed;
                ucUserInputs_CAP.Visibility = Visibility.Visible;

                ucUserInputs_CAP.Init(userInputsVM, config, adminSettingsVM, onDeckStabilityLimitsVM, landingStatusTrendVM);

                // Helideck Motion Limits / Touchdown Limits
                ucHelideckMotionLimits_NOROG.Visibility = Visibility.Collapsed;
                ucTouchdownLimits_CAP.Visibility = Visibility.Visible;
                ucTouchdownLimits_CAP.Init(helideckMotionLimitsVM, config, tabHelicopterOps, tcMainMenu);

                // Helideck Status
                ucHelideckStatus_NOROG.Visibility = Visibility.Collapsed;
                ucHelideckStatus_CAP.Visibility = Visibility.Visible;
                ucHelideckStatus_CAP.Init(helideckStatusVM);

                // Helideck Motion Trend
                ucHelideckMotionTrend_NOROG.Visibility = Visibility.Collapsed;

                // Helideck Stability Limits
                ucHelideckStabilityLimits_CAP.Visibility = Visibility.Visible;
                ucHelideckStabilityLimits_CAP.Init(onDeckStabilityLimitsVM, landingStatusTrendVM);

                // Relative Wind Direction Limits
                ucRelativeWindDirectionLimits_CAP.Visibility = Visibility.Visible;
                ucRelativeWindDirectionLimits_CAP.Init(relativeWindLimitsVM);

                // Helideck Wind & Heading Trend
                ucWindHeadingChange_CAP.Visibility = Visibility.Visible;
                ucWindHeadingChange_CAP.Init(windHeadingChangeVM, config);

                // Helideck Status Trend
                ucHelideckStatusTrend_CAP.Visibility = Visibility.Visible;
                ucHelideckStatusTrend_CAP.Init(landingStatusTrendVM, config);

                // Helideck Motion history
                tabHelideckMotionHistory_CAP.Visibility = Visibility.Visible;
                tabHelideckMotionHistory_NOROG.Visibility = Visibility.Collapsed;
                ucHelideckMotionHistory_CAP.Init(helideckMotionTrendVM, landingStatusTrendVM, config, tabHelideckMotionHistory_CAP);

                // Helideck Report
                tabHelideckReport_CAP.Visibility = Visibility.Visible;
                tabHelideckReport_NOROG.Visibility = Visibility.Collapsed;
                ucHelideckReport_CAP.Init(helideckReportVM, config, adminSettingsVM);

                // Sensor Status Display
                ucSensorStatus_CAP.Init(sensorStatusVM);
            }

            // Wind & Heading
            ucWindHeading.Init(windHeadingVM, userInputsVM, adminSettingsVM);

            // Meteorological
            ucMeteorological.Init(meteorologicalVM);

            // EMS Page
            if (adminSettingsVM.enableEMS)
            {
                // EMS Wave Data
                ucEMSWaveData.Init(emsWaveDataVM);

                // EMS Wind & Heading
                ucEMSWindHeading.Init(windHeadingVM, userInputsVM, adminSettingsVM);

                // EMS Meteorological (samme som HMS met)
                ucEMSMeteorological.Init(meteorologicalVM);

                // EMS Weather
                ucEMSWeather.Init(meteorologicalVM);
            }

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
            DataRequestCallback dataRequestCallback = new DataRequestCallback(DataRequestComplete);

            // Callback funksjon i tilfelle klienten blir avvist
            ClientDeniedCallback clientDeniedCallback = new ClientDeniedCallback(ClientDenied);

            serverCom.Init(
                socketConsole,
                hmsDataCollection,
                sensorStatus,
                config,
                dataRequestCallback,
                clientDeniedCallback);
        }

        public void ShowWarningBarMessage(WarningBarMessageType message)
        {
            Application.Current.Dispatcher.BeginInvoke(
            DispatcherPriority.Normal,
            new Action(() =>
            {

                if (message == WarningBarMessageType.RestartRequired)
                    dpApplicationRestartRequired.Visibility = Visibility.Visible;
                else
                    dpApplicationRestartRequired.Visibility = Visibility.Collapsed;

                if (message == WarningBarMessageType.DataVerification)
                    dpDataVerificationActiveWarning.Visibility = Visibility.Visible;
                else
                    dpDataVerificationActiveWarning.Visibility = Visibility.Collapsed;
            }));
        }

        public void SetDefaultWindMeasurement()
        {
            ucWindHeading.SetDefaultWindMeasurement();
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

        public void ClientDenied()
        {
            // Sørge for at denne koden kun kjøres en gang
            if (!clientDeniedReceived)
            {
                clientDeniedReceived = true;

                // Stopp sending av data request
                serverCom.Stop();

                // Kjøre på UI thread
                Application.Current.Dispatcher.BeginInvoke(
                DispatcherPriority.Normal,
                new Action(() =>
                {
                    // Vise client denied dialog
                    DialogClientDenied clientDeniedDialog = new DialogClientDenied();
                    clientDeniedDialog.Owner = App.Current.MainWindow;
                    clientDeniedDialog.ShowDialog();

                    // Lukke programmet
                    Close();
                }));
            }
        }

        private void OnAdminKeyCommand(object sender, KeyEventArgs e)
        {
            if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift &&
                (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                // Admin grensesnitt
                if (Keyboard.IsKeyDown(Key.H))
                {
                    if (!tabAdminSettings.IsVisible)
                    {
                        // Åpne admin passord vindu
                        DialogAdminPassword adminMode = new DialogAdminPassword();
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

            // Admin Settings
            adminSettingsVM.UpdateData(hmsDataCollection);

            // General Information
            generalInformationVM.UpdateData(hmsDataCollection);

            // User Inputs
            userInputsVM.UpdateData(hmsDataCollection);

            // Helideck Motion
            helideckMotionLimitsVM.UpdateData(hmsDataCollection);

            // Helideck Motion Trend
            helideckMotionTrendVM.UpdateData(hmsDataCollection);

            if (adminSettingsVM.regulationStandard == RegulationStandard.NOROG)
            {
            }
            else
            if (adminSettingsVM.regulationStandard == RegulationStandard.CAP)
            {
                // Helideck Stability Limits
                onDeckStabilityLimitsVM.UpdateData(hmsDataCollection);

                // Helideck Relative Wind Limits
                relativeWindLimitsVM.UpdateData(hmsDataCollection);

                // Helideck Heading Trend
                windHeadingChangeVM.UpdateData(hmsDataCollection);
            }

            // Helideck Status
            helideckStatusVM.UpdateData(hmsDataCollection);

            // Meteorological
            meteorologicalVM.UpdateData(hmsDataCollection);

            // Wind & Heading
            windHeadingVM.UpdateData(hmsDataCollection);

            // Helideck Report
            helideckReportVM.UpdateData(hmsDataCollection);

            // EMS
            if (adminSettingsVM.enableEMS)
                emsWaveDataVM.UpdateData(hmsDataCollection);
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
            //// Viser screen capture knappen kun på hovedsiden og når NOROG standard er valgt
            //if (tabHelicopterOps.IsSelected && adminSettingsVM.regulationStandard == RegulationStandard.NOROG)
            //    btnScreenCapture.Visibility = Visibility.Visible;
            //else
            //    btnScreenCapture.Visibility = Visibility.Collapsed;

            // Fjerne fokus fra alle elementer slik at ikke knapper og felt lyser opp i rødt når vi bytter tab
            Dispatcher.BeginInvoke((Action)(() =>
            {
                (sender as RadTabControl).Focus();
            }), DispatcherPriority.ApplicationIdle);
        }
    }
}