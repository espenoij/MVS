using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Telerik.Windows.Controls;

namespace SensorMonitorClient
{
    /// <summary>
    /// Interaction logic for AdminSettings.xaml
    /// </summary>
    public partial class AdminSettings : UserControl
    {
        private AdminSettingsVM adminSettingsVM;
        private GeneralInformationVM generalInformationVM;
        private WindHeadingVM windHeadingVM;

        // Configuration settings
        private Config config;

        // TCP/IP Trafikk vindu
        private SocketConsoleWindow serverTrafficWindow;

        // TCP/IP Socket console for utskrift av trafikk
        private SocketConsole socketConsole;

        // Server Communications modul
        private ServerCom serverCom;

        private DispatcherTimer UIUpdateTimer = new DispatcherTimer();

        private bool clientIsMaster;

        public AdminSettings()
        {
            InitializeComponent();
        }

        public void Init(
            AdminSettingsVM viewModel,
            GeneralInformationVM generalInformationVM,
            WindHeadingVM windHeadingVM,
            Config config,
            SocketConsole socketConsole,
            ServerCom serverCom)
        {
            this.adminSettingsVM = viewModel;
            this.generalInformationVM = generalInformationVM;
            this.windHeadingVM = windHeadingVM;

            DataContext = viewModel;

            this.config = config;
            this.socketConsole = socketConsole;
            this.serverCom = serverCom;

            clientIsMaster = adminSettingsVM.clientIsMaster;

            // General Settings: Regulation Standard
            foreach (RegulationStandard value in Enum.GetValues(typeof(RegulationStandard)))
                cboRegulationStandard.Items.Add(value.ToString());

            viewModel.regulationStandard = (RegulationStandard)Enum.Parse(typeof(RegulationStandard), config.Read(ConfigKey.RegulationStandard));

            cboRegulationStandard.SelectedIndex = (int)viewModel.regulationStandard;
            cboRegulationStandard.Text = viewModel.regulationStandard.ToString();

            // Visualization: Vessel Image
            foreach (VesselImage value in Enum.GetValues(typeof(VesselImage)))
                cboVisVesselImage.Items.Add(value.ToString());

            viewModel.vesselImage = (VesselImage)Enum.Parse(typeof(VesselImage), config.Read(ConfigKey.VesselImage));

            cboVisVesselImage.Text = viewModel.vesselImage.ToString();
            cboVisVesselImage.SelectedIndex = (int)viewModel.vesselImage;

            cboVisVesselImage.Text = viewModel.vesselImage.ToString();

            // Visualization: Helideck Location
            //viewModel.helideckLocation = (HelideckLocation)Enum.Parse(typeof(HelideckLocation), config.Read(ConfigKey.HelideckLocation));
            //LoadHelideckLocationComboBox();
        }

        //public void LoadHelideckLocationComboBox()
        //{
        //    cboVisHelideckLocation.Items.Clear();

        //    switch (viewModel.vesselImage)
        //    {
        //        case VesselImage.None:
        //        case VesselImage.Triangle:
        //            cboVisHelideckLocation.Items.Add(HelideckLocation.Center.GetDescription());

        //            viewModel.helideckLocation = HelideckLocation.Center;
        //            break;

        //        case VesselImage.Rig:
        //            cboVisHelideckLocation.Items.Add(HelideckLocation.ForeCenter.GetDescription());
        //            cboVisHelideckLocation.Items.Add(HelideckLocation.ForePort.GetDescription());
        //            cboVisHelideckLocation.Items.Add(HelideckLocation.ForeStarboard.GetDescription());

        //            cboVisHelideckLocation.Items.Add(HelideckLocation.Center.GetDescription());
        //            cboVisHelideckLocation.Items.Add(HelideckLocation.CenterPort.GetDescription());
        //            cboVisHelideckLocation.Items.Add(HelideckLocation.CenterStarboard.GetDescription());

        //            cboVisHelideckLocation.Items.Add(HelideckLocation.AftCenter.GetDescription());
        //            cboVisHelideckLocation.Items.Add(HelideckLocation.AftPort.GetDescription());
        //            cboVisHelideckLocation.Items.Add(HelideckLocation.AftStarboard.GetDescription());

        //            if (viewModel.helideckLocation != HelideckLocation.ForeCenter &&
        //                viewModel.helideckLocation != HelideckLocation.ForePort &&
        //                viewModel.helideckLocation != HelideckLocation.ForeStarboard &&
        //                viewModel.helideckLocation != HelideckLocation.Center &&
        //                viewModel.helideckLocation != HelideckLocation.CenterPort &&
        //                viewModel.helideckLocation != HelideckLocation.CenterStarboard &&
        //                viewModel.helideckLocation != HelideckLocation.AftCenter &&
        //                viewModel.helideckLocation != HelideckLocation.AftPort &&
        //                viewModel.helideckLocation != HelideckLocation.AftStarboard)
        //            {
        //                viewModel.helideckLocation = HelideckLocation.ForeCenter;
        //            }
        //            break;

        //        case VesselImage.Ship:
        //            cboVisHelideckLocation.Items.Add(HelideckLocation.Fore.GetDescription());
        //            cboVisHelideckLocation.Items.Add(HelideckLocation.Center.GetDescription());
        //            cboVisHelideckLocation.Items.Add(HelideckLocation.Aft.GetDescription());

        //            if (viewModel.helideckLocation != HelideckLocation.Fore &&
        //                viewModel.helideckLocation != HelideckLocation.Center &&
        //                viewModel.helideckLocation != HelideckLocation.Aft)
        //            {
        //                viewModel.helideckLocation = HelideckLocation.Fore;
        //            }
        //            break;

        //        default:
        //            break;
        //    }

        //    cboVisHelideckLocation.Text = viewModel.helideckLocation.ToString();
        //    cboVisHelideckLocation.SelectedIndex = (int)viewModel.helideckLocation;
        //}

        public void UIUpdateServerRunning(bool state)
        {
            tbServerAddress.IsEnabled = !state;
            tbServerPort.IsEnabled = !state;

            btnDataRequestStart.IsEnabled = !state;
            btnDataRequestStop.IsEnabled = state;
            tbDataRequestFrequency.IsEnabled = !state;
            tbSensorStatusRequestFrequency.IsEnabled = !state;
        }

        private void btnTCPIPTest_Click(object sender, RoutedEventArgs e)
        {
            // Open window (men kun 1 instans av dette vinduet)
            if (serverTrafficWindow == null)
            {
                serverTrafficWindow = new SocketConsoleWindow(socketConsole);
                serverTrafficWindow.Closed += (a, b) => serverTrafficWindow = null;

                serverTrafficWindow.Show();

                // Vise ikon på taskbar
                var window = serverTrafficWindow.ParentOfType<Window>();
                window.ShowInTaskbar = true;
            }
            else
            {
                serverTrafficWindow.Show();
            }
        }

        private void tbDataRequestFrequency_LostFocus(object sender, RoutedEventArgs e)
        {
            tbDataRequestFrequency_Update(sender);
        }

        private void tbDataRequestFrequency_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                tbDataRequestFrequency_Update(sender);
                Keyboard.ClearFocus();
            }
        }

        private void tbDataRequestFrequency_Update(object sender)
        {
            // Sjekk av input
            DataValidation.Double(
                (sender as TextBox).Text,
                Constants.DataRequestFrequencyMin,
                Constants.DataRequestFrequencyMax,
                Constants.DataRequestFrequencyDefault,
                out double validatedInput);

            adminSettingsVM.dataRequestFrequency = validatedInput;
        }

        private void tbSensorStatusRequestFrequency_LostFocus(object sender, RoutedEventArgs e)
        {
            tbSensorStatusRequestFrequency_Update(sender);
        }

        private void tbSensorStatusRequestFrequency_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                tbSensorStatusRequestFrequency_Update(sender);
                Keyboard.ClearFocus();
            }
        }

        private void tbSensorStatusRequestFrequency_Update(object sender)
        {
            // Sjekk av input
            DataValidation.Double(
                (sender as TextBox).Text,
                Constants.SensorStatusRequestFrequencyMin,
                Constants.SensorStatusRequestFrequencyMax,
                Constants.SensorStatusRequestFrequencyDefault,
                out double validatedInput);

            adminSettingsVM.sensorStatusRequestFrequency = validatedInput;
        }

        private void btnDataRequestStart_Click(object sender, RoutedEventArgs e)
        {
            serverCom.Start();
            UIUpdateTimer.Start();

            UIUpdateServerRunning(true);
        }

        private void btnDataRequestStop_Click(object sender, RoutedEventArgs e)
        {
            serverCom.Stop();
            UIUpdateTimer.Stop();

            UIUpdateServerRunning(false);
        }

        private void tbServerAddress_LostFocus(object sender, RoutedEventArgs e)
        {
            tbServerAddress_Update(sender);
        }

        private void tbServerAddress_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                tbServerAddress_Update(sender);
                Keyboard.ClearFocus();
            }
        }

        private void tbServerAddress_Update(object sender)
        {
            DataValidation.IPAddress(
                (sender as TextBox).Text,
                Constants.DefaultServerAddress,
                out string validatedInput);

            adminSettingsVM.serverAddress = validatedInput;
        }

        private void tbServerPort_LostFocus(object sender, RoutedEventArgs e)
        {
            tbServerPort_Update(sender);
        }

        private void tbServerPort_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                tbServerPort_Update(sender);
                Keyboard.ClearFocus();
            }
        }

        private void tbServerPort_Update(object sender)
        {
            // Sjekk av input
            DataValidation.Double(
                (sender as TextBox).Text,
                Constants.PortMin,
                Constants.PortMax,
                Constants.ServerPortDefault,
                out double validatedInput);

            adminSettingsVM.serverPort = validatedInput;
        }

        private void tbUIUpdateFrequency_LostFocus(object sender, RoutedEventArgs e)
        {
            tbUIUpdateFrequency_Update(sender);
        }

        private void tbUIUpdateFrequency_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                tbUIUpdateFrequency_Update(sender);
                Keyboard.ClearFocus();
            }
        }

        private void tbUIUpdateFrequency_Update(object sender)
        {
            // Sjekk av input
            DataValidation.Double(
                (sender as TextBox).Text,
                Constants.ClientUpdateFrequencyUIMin,
                Constants.ClientUpdateFrequencyUIMax,
                Constants.ClientUpdateFrequencyUIDefault,
                out double validatedInput);

            if (adminSettingsVM.uiUpdateFrequency != validatedInput)
                adminSettingsVM.ApplicationRestartRequired();

            adminSettingsVM.uiUpdateFrequency = validatedInput;
        }

        private void tbDataTimeout_LostFocus(object sender, RoutedEventArgs e)
        {
            tbDataTimeout_Update(sender);
        }

        private void tbDataTimeout_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                tbDataTimeout_Update(sender);
                Keyboard.ClearFocus();
            }
        }

        private void tbDataTimeout_Update(object sender)
        {
            // Sjekk av input
            DataValidation.Double(
                (sender as TextBox).Text,
                Constants.DataTimeoutMin,
                Constants.DataTimeoutMax,
                Constants.DataTimeoutDefault,
                out double validatedInput);

            if (adminSettingsVM.dataTimeout != validatedInput)
                adminSettingsVM.ApplicationRestartRequired();

            adminSettingsVM.dataTimeout = validatedInput;
        }

        private void tbChartDataUpdateFrequency20m_LostFocus(object sender, RoutedEventArgs e)
        {
            tbChartDataUpdateFrequency20m_Update(sender);
        }

        private void tbChartDataUpdateFrequency20m_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                tbChartDataUpdateFrequency20m_Update(sender);
                Keyboard.ClearFocus();
            }
        }

        private void tbChartDataUpdateFrequency20m_Update(object sender)
        {
            // Sjekk av input
            DataValidation.Double(
                (sender as TextBox).Text,
                Constants.ChartUpdateFrequencyUIMin,
                Constants.ChartUpdateFrequencyUIMax,
                Constants.ChartUpdateFrequencyUI20mDefault,
                out double validatedInput);

            if (adminSettingsVM.chartDataUpdateFrequency20m != validatedInput)
                adminSettingsVM.ApplicationRestartRequired();

            adminSettingsVM.chartDataUpdateFrequency20m = validatedInput;
        }

        private void tbChartDataUpdateFrequency3h_LostFocus(object sender, RoutedEventArgs e)
        {
            tbChartDataUpdateFrequency3h_Update(sender);
        }

        private void tbChartDataUpdateFrequency3h_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                tbChartDataUpdateFrequency3h_Update(sender);
                Keyboard.ClearFocus();
            }
        }

        private void tbChartDataUpdateFrequency3h_Update(object sender)
        {
            // Sjekk av input
            DataValidation.Double(
                (sender as TextBox).Text,
                Constants.ChartUpdateFrequencyUIMin,
                Constants.ChartUpdateFrequencyUIMax,
                Constants.ChartUpdateFrequencyUI3hDefault,
                out double validatedInput);

            if (adminSettingsVM.chartDataUpdateFrequency3h != validatedInput)
                adminSettingsVM.ApplicationRestartRequired();

            adminSettingsVM.chartDataUpdateFrequency3h = validatedInput;
        }

        private void cboVisVesselImage_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            adminSettingsVM.vesselImage = (VesselImage)cboVisVesselImage.SelectedIndex;
            windHeadingVM.vesselImage = (VesselImage)cboVisVesselImage.SelectedIndex;
            generalInformationVM.vesselImage = (VesselImage)cboVisVesselImage.SelectedIndex;
        }

        private void cboRegulationStandard_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (adminSettingsVM.regulationStandard != (RegulationStandard)cboRegulationStandard.SelectedIndex)
            {
                adminSettingsVM.regulationStandard = (RegulationStandard)cboRegulationStandard.SelectedIndex;
                adminSettingsVM.ApplicationRestartRequired();
            }
        }

        private void chkClientIsMaster_Checked(object sender, RoutedEventArgs e)
        {
            if (clientIsMaster != adminSettingsVM.clientIsMaster)
                adminSettingsVM.ApplicationRestartRequired();

            clientIsMaster = adminSettingsVM.clientIsMaster;
        }

        private void chkClientIsMaster_Unchecked(object sender, RoutedEventArgs e)
        {
            if (clientIsMaster != adminSettingsVM.clientIsMaster)
                adminSettingsVM.ApplicationRestartRequired();

            clientIsMaster = adminSettingsVM.clientIsMaster;
        }
    }
}