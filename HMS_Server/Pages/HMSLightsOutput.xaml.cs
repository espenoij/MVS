using System;
using System.IO.Ports;
using System.Windows;
using System.Windows.Controls;
using Telerik.Windows.Controls;

namespace HMS_Server
{
    /// <summary>
    /// Interaction logic for HMSLightsOutput.xaml
    /// </summary>
    public partial class HMSLightsOutput : UserControl
    {
        // Configuration settings
        private Config config;

        private SensorData lightsOutputData;
        private HMSLightsOutputVM hmsLightsOutputVM;
        private AdminSettingsVM adminSettingsVM;

        public HMSLightsOutput()
        {
            InitializeComponent();
        }

        public void Init(SensorData lightsOutputData, HMSLightsOutputVM hmsLightsOutputVM, Config config, AdminSettingsVM adminSettingsVM)
        {
            DataContext = hmsLightsOutputVM;

            this.lightsOutputData = lightsOutputData;
            this.hmsLightsOutputVM = hmsLightsOutputVM;
            this.config = config;
            this.adminSettingsVM = adminSettingsVM;

            // CAP eller NOROG
            if (adminSettingsVM.regulationStandard == RegulationStandard.CAP)
            {
                gridLights_CAP.Visibility = Visibility.Visible;
                gridLights_NOROG.Visibility = Visibility.Collapsed;

                // Test Mode: Helideck Status
                cboTestHelideckStatus.Items.Add(HelideckStatusType.OFF.ToString());
                cboTestHelideckStatus.Items.Add(HelideckStatusType.BLUE.ToString());
                cboTestHelideckStatus.Items.Add(HelideckStatusType.AMBER.ToString());
                cboTestHelideckStatus.Items.Add(HelideckStatusType.RED.ToString());

                cboTestHelideckStatus.SelectedIndex = (int)HelideckStatusType.OFF;
                cboTestHelideckStatus.Text = HelideckStatusType.OFF.ToString();

                // Test Mode: Helideck Display Mode
                cboTestDisplayMode.Items.Add(DisplayMode.PreLanding.ToString());
                cboTestDisplayMode.Items.Add(DisplayMode.OnDeck.ToString());

                cboTestDisplayMode.SelectedIndex = (int)DisplayMode.PreLanding;
                cboTestDisplayMode.Text = DisplayMode.PreLanding.ToString();
            }
            else
            {
                gridLights_CAP.Visibility = Visibility.Collapsed;
                gridLights_NOROG.Visibility = Visibility.Visible;

                // Test Mode: Helideck Status
                cboTestHelideckStatus.Items.Add(HelideckStatusType.OFF.ToString());
                cboTestHelideckStatus.Items.Add(HelideckStatusType.GREEN.ToString());
                cboTestHelideckStatus.Items.Add(HelideckStatusType.RED.ToString());

                cboTestHelideckStatus.SelectedIndex = (int)HelideckStatusType.OFF;
                cboTestHelideckStatus.Text = HelideckStatusType.OFF.ToString();

                cboTestDisplayMode.IsEnabled = false;
            }

            // COM Port Names
            ///////////////////////////////////////////////////////////
            // Lese tilgjengelig serie porter
            ReadAvailableCOMPorts();

            // Baud Rate
            ///////////////////////////////////////////////////////////
            cboBaudRate.Items.Add(110);
            cboBaudRate.Items.Add(300);
            cboBaudRate.Items.Add(600);
            cboBaudRate.Items.Add(1200);
            cboBaudRate.Items.Add(2400);
            cboBaudRate.Items.Add(4800);
            cboBaudRate.Items.Add(9600);
            cboBaudRate.Items.Add(14400);
            cboBaudRate.Items.Add(19200);
            cboBaudRate.Items.Add(38400);
            cboBaudRate.Items.Add(57600);
            cboBaudRate.Items.Add(115200);
            cboBaudRate.Items.Add(256000);

            // Skrive satt baud rate i combobox feltet
            cboBaudRate.Text = lightsOutputData.serialPort.baudRate.ToString();

            // Data Bits
            ///////////////////////////////////////////////////////////
            cboDataBits.Items.Add(7);
            cboDataBits.Items.Add(8);

            // Skrive satt data bit type i combobox feltet
            cboDataBits.Text = lightsOutputData.serialPort.dataBits.ToString();

            // Stop Bits
            ///////////////////////////////////////////////////////////
            cboStopBits.Items.Add("One");
            cboStopBits.Items.Add("OnePointFive");
            cboStopBits.Items.Add("Two");

            // Skrive satt stop bit i combobox feltet
            cboStopBits.Text = lightsOutputData.serialPort.stopBits.ToString();

            // Parity 
            ///////////////////////////////////////////////////////////
            cboParity.Items.Add("None");
            cboParity.Items.Add("Even");
            cboParity.Items.Add("Mark");
            cboParity.Items.Add("Odd");
            cboParity.Items.Add("Space");

            // Skrive satt parity i combobox feltet
            cboParity.Text = lightsOutputData.serialPort.parity.ToString();

            // Handshake
            ///////////////////////////////////////////////////////////
            cboHandShake.Items.Add("None");
            cboHandShake.Items.Add("XOnXOff");
            cboHandShake.Items.Add("RequestToSend");
            cboHandShake.Items.Add("RequestToSendXOnXOff");

            // Skrive satt hand shake type i combobox feltet
            cboHandShake.Text = lightsOutputData.serialPort.handshake.ToString();
        }

        public void Start()
        {
            // Stenge tilgang til å konfigurere porten
            cboPortName.IsEnabled = false;
            cboBaudRate.IsEnabled = false;
            cboDataBits.IsEnabled = false;
            cboStopBits.IsEnabled = false;
            cboParity.IsEnabled = false;
            cboHandShake.IsEnabled = false;
        }

        public void Stop()
        {
            // Åpne tilgang til å konfigurere porten
            cboPortName.IsEnabled = true;
            cboBaudRate.IsEnabled = true;
            cboDataBits.IsEnabled = true;
            cboStopBits.IsEnabled = true;
            cboParity.IsEnabled = true;
            cboHandShake.IsEnabled = true;
        }

        private void ReadAvailableCOMPorts()
        {
            string[] comPortsNameArray;

            comPortsNameArray = SerialPort.GetPortNames();

            if (comPortsNameArray.Length > 0)
            {
                // Sortere port-listen
                Array.Sort(comPortsNameArray);

                // Legg porter inn i combo box
                foreach (var comPortName in comPortsNameArray)
                    cboPortName.Items.Add(comPortName);

                // Skrive satt COM port i combobox tekst feltet
                cboPortName.Text = lightsOutputData.serialPort.portName;
            }
            else
            {
                // Fant ingen porter
                cboPortName.Text = "No COM ports found";
            }
        }

        private void cboCOMPort_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Lagre ny setting
            lightsOutputData.serialPort.portName = cboPortName.SelectedItem.ToString();
            config.SetLightsOutputData(lightsOutputData);
        }

        private void cboBaudRate_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                // Lagre ny setting
                lightsOutputData.serialPort.baudRate = Convert.ToInt32(cboBaudRate.SelectedItem.ToString());
                config.SetLightsOutputData(lightsOutputData);
            }
            catch (Exception ex)
            {
                RadWindow.Alert(string.Format("cboBaudRate_SelectionChanged\n\n{0}", TextHelper.Wrap(ex.Message)));
            }
        }

        private void cboDataBits_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                // Lagre ny setting
                lightsOutputData.serialPort.dataBits = Convert.ToInt16(cboDataBits.SelectedItem.ToString());
                config.SetLightsOutputData(lightsOutputData);
            }
            catch (Exception ex)
            {
                RadWindow.Alert(string.Format("cboDataBits_SelectionChanged\n\n{0}", TextHelper.Wrap(ex.Message)));
            }
        }

        private void cboStopBits_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                // Lagre ny setting
                lightsOutputData.serialPort.stopBits = (StopBits)Enum.Parse(typeof(StopBits), cboStopBits.SelectedItem.ToString());
                config.SetLightsOutputData(lightsOutputData);
            }
            catch (Exception ex)
            {
                RadWindow.Alert(string.Format("cboStopBits_SelectionChanged\n\n{0}", TextHelper.Wrap(ex.Message)));
            }
        }

        private void cboDataParity_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                // Lagre ny setting
                lightsOutputData.serialPort.parity = (Parity)Enum.Parse(typeof(Parity), cboParity.SelectedItem.ToString());
                config.SetLightsOutputData(lightsOutputData);
            }
            catch (Exception ex)
            {
                RadWindow.Alert(string.Format("cboDataParity_SelectionChanged\n\n{0}", TextHelper.Wrap(ex.Message)));
            }
        }

        private void cboHandShake_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                // Lagre ny setting
                lightsOutputData.serialPort.handshake = (Handshake)Enum.Parse(typeof(Handshake), cboHandShake.SelectedItem.ToString());
                config.SetLightsOutputData(lightsOutputData);
            }
            catch (Exception ex)
            {
                RadWindow.Alert(string.Format("cboHandShake_SelectionChanged\n\n{0}", TextHelper.Wrap(ex.Message)));
            }
        }

        private void cboTestHelideckStatus_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            hmsLightsOutputVM.testModeStatus = (HelideckStatusType)Enum.Parse(typeof(HelideckStatusType), cboTestHelideckStatus.SelectedItem.ToString());
        }

        private void cboTestDisplayMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            hmsLightsOutputVM.testModeDisplayMode = (DisplayMode)Enum.Parse(typeof(DisplayMode), cboTestDisplayMode.SelectedItem.ToString());
        }
    }
}