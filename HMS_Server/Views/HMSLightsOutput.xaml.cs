using NModbus;
using NModbus.Serial;
using System;
using System.Diagnostics;
using System.IO.Ports;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
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

        // Connection Type
        private LightsOutputConnection outputConnection;

        private HMSLightsOutputVM hmsLightsOutputVM;
        private AdminSettingsVM adminSettingsVM;

        // Serie port objekt
        private SerialPort serialPort = new SerialPort();

        // MODBUS
        private ModbusFactory modbusFactory = new ModbusFactory();
        private IModbusSerialMaster modbusSerialMaster = null;
        private ModbusHelper modbusHelper = new ModbusHelper();

        private DispatcherTimer sendLightsOutput = new DispatcherTimer();

        public HMSLightsOutput()
        {
            InitializeComponent();
        }

        public void Init(LightsOutputConnection outputConnection, HMSLightsOutputVM hmsLightsOutputVM, Config config, AdminSettingsVM adminSettingsVM, ErrorHandler errorHandler)
        {
            DataContext = hmsLightsOutputVM;

            this.outputConnection = outputConnection;
            this.hmsLightsOutputVM = hmsLightsOutputVM;
            this.config = config;
            this.adminSettingsVM = adminSettingsVM;

            if (adminSettingsVM.regulationStandard == RegulationStandard.CAP)
            {
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

                // Connection Types
                ///////////////////////////////////////////////////////////
                foreach (OutputConnectionType value in Enum.GetValues(typeof(OutputConnectionType)))
                    cboConnectionType.Items.Add(value.GetDescription());

                cboConnectionType.Text = outputConnection.type.GetDescription();

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
                cboBaudRate.Text = outputConnection.baudRate.ToString();

                // Data Bits
                ///////////////////////////////////////////////////////////
                cboDataBits.Items.Add(7);
                cboDataBits.Items.Add(8);

                // Skrive satt data bit type i combobox feltet
                cboDataBits.Text = outputConnection.dataBits.ToString();

                // Stop Bits
                ///////////////////////////////////////////////////////////
                cboStopBits.Items.Add("One");
                cboStopBits.Items.Add("OnePointFive");
                cboStopBits.Items.Add("Two");

                // Skrive satt stop bit i combobox feltet
                cboStopBits.Text = outputConnection.stopBits.ToString();

                // Parity 
                ///////////////////////////////////////////////////////////
                cboParity.Items.Add("None");
                cboParity.Items.Add("Even");
                cboParity.Items.Add("Mark");
                cboParity.Items.Add("Odd");
                cboParity.Items.Add("Space");

                // Skrive satt parity i combobox feltet
                cboParity.Text = outputConnection.parity.ToString();

                // Handshake
                ///////////////////////////////////////////////////////////
                cboHandShake.Items.Add("None");
                cboHandShake.Items.Add("XOnXOff");
                cboHandShake.Items.Add("RequestToSend");
                cboHandShake.Items.Add("RequestToSendXOnXOff");

                // Skrive satt hand shake type i combobox feltet
                cboHandShake.Text = outputConnection.handshake.ToString();

                // Slave ID
                ///////////////////////////////////////////////////////////
                if (outputConnection.type == OutputConnectionType.MODBUS_RTU)
                {
                    if (outputConnection.data.modbus != null)
                        tbSlaveID.Text = outputConnection.data.modbus.slaveID.ToString();
                }

                // ADAM Address
                ///////////////////////////////////////////////////////////
                if (outputConnection.type == OutputConnectionType.ADAM_4060)
                {
                    if (outputConnection.data.serialPort != null)
                        tbAdamAddress.Text = outputConnection.adamAddress;
                }

                // Sette serie port settings på SerialPort object 
                ///////////////////////////////////////////////////////////
                serialPort.PortName = outputConnection.portName;
                serialPort.BaudRate = outputConnection.baudRate;
                serialPort.DataBits = outputConnection.dataBits;
                serialPort.StopBits = outputConnection.stopBits;
                serialPort.Parity = outputConnection.parity;
                serialPort.Handshake = outputConnection.handshake;

                // Sette UI elementer
                UpdateUI();

                // Timeout
                serialPort.ReadTimeout = Constants.ModbusTimeout;       // NB! Brukes av ReadHoldingRegisters. Uten disse vil programmet fryse dersom ReadHoldingRegisters ikke får svar.
                serialPort.WriteTimeout = Constants.ModbusTimeout;

                // Sende lys output
                sendLightsOutput.Interval = TimeSpan.FromMilliseconds(config.ReadWithDefault(ConfigKey.LightsOutputFrequency, Constants.LightsOutputFrequencyDefault));
                sendLightsOutput.Tick += runLightsOutput;

                void runLightsOutput(object sender, EventArgs e)
                {
                    Thread thread = new Thread(() => LightsOutputCommand());
                    thread.IsBackground = true;
                    thread.Start();

                    void LightsOutputCommand()
                    {
                        switch (outputConnection.type)
                        {
                            // MODBUS RTU
                            case OutputConnectionType.MODBUS_RTU:
                                if (outputConnection.data.modbus != null)
                                {
                                    try
                                    {
                                        // Sjekke om porten er åpen
                                        if (!serialPort.IsOpen)
                                            serialPort.Open();

                                        if (serialPort.IsOpen)
                                        {
                                            // Starte MODBUS
                                            if (modbusSerialMaster == null)
                                                modbusSerialMaster = modbusFactory.CreateRtuMaster(new SerialPortAdapter(serialPort));

                                            // Løpe gjennom adressene det skal sendes på
                                            for (int i = 0; i < hmsLightsOutputVM.outputAddressList.Count; i++)
                                            {
                                                // Ok master?
                                                if (modbusSerialMaster?.Transport != null)
                                                {
                                                    // Sende data (true/false)
                                                    modbusSerialMaster?.WriteSingleCoil(
                                                       outputConnection.data.modbus.slaveID,
                                                       hmsLightsOutputVM.outputAddressList[i],
                                                       LightOutputToWriteValue(hmsLightsOutputVM.HMSLightsOutput, i));
                                                }
                                            }
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        // WinModbus må settes opp med RTU og autocreate datamap må være på, ellers får vi feilmeldinger
                                        errorHandler.Insert(
                                            new ErrorMessage(
                                                DateTime.UtcNow,
                                                ErrorMessageType.MODBUS,
                                                ErrorMessageCategory.AdminUser,
                                                string.Format("Modbus Write (Lights Output): {0}", ex.Message)));
                                    }
                                }
                                break;

                            // ADAM-4060
                            case OutputConnectionType.ADAM_4060:
                                if (outputConnection.data.serialPort != null)
                                {
                                    try
                                    {
                                        // Sjekke om porten er åpen
                                        if (!serialPort.IsOpen)
                                            serialPort.Open();

                                        if (serialPort.IsOpen)
                                        {
                                            int channelOutput = 0;

                                            if (LightOutputToWriteValue(hmsLightsOutputVM.HMSLightsOutput, 0))
                                                channelOutput += 1;

                                            if (LightOutputToWriteValue(hmsLightsOutputVM.HMSLightsOutput, 1))
                                                channelOutput += 2;

                                            if (LightOutputToWriteValue(hmsLightsOutputVM.HMSLightsOutput, 2))
                                                channelOutput += 4;

                                            int channelOutputValue;
                                            Int32.TryParse(outputConnection.adamAddress, out channelOutputValue);

                                            // Generere ADAM-4060 kommando for å sette addresser
                                            string command = String.Format("#{0}00{1}\r\n",
                                                channelOutputValue.ToString("X2"),
                                                channelOutput.ToString("X2"));

                                            // Skrive commando til serie port
                                            serialPort.Write(command);
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        // WinModbus må settes opp med RTU og autocreate datamap må være på, ellers får vi feilmeldinger
                                        errorHandler.Insert(
                                            new ErrorMessage(
                                                DateTime.UtcNow,
                                                ErrorMessageType.SerialPort,
                                                ErrorMessageCategory.AdminUser,
                                                string.Format("Serial Port Write (Lights Output): {0}", ex.Message)));
                                    }
                                }
                                break;
                        }
                    }
                }
            }
        }

        public void Start()
        {
            // Stenge tilgang til å konfigurere porten
            if (adminSettingsVM?.regulationStandard == RegulationStandard.CAP)
            {
                EnableModbusUI(false);

                Modbus_Start();
            }
        }

        public void Stop()
        {
            // Åpne tilgang til å konfigurere porten
            if (adminSettingsVM?.regulationStandard == RegulationStandard.CAP)
            {
                EnableModbusUI(true);

                Modbus_Stop();
            }
        }

        private void EnableModbusUI(bool state)
        {
            lbConnectionType.IsEnabled = state;
            cboConnectionType.IsEnabled = state;
            lbPortName.IsEnabled = state;
            cboPortName.IsEnabled = state;
            lbBaudRate.IsEnabled = state;
            cboBaudRate.IsEnabled = state;
            lbDataBits.IsEnabled = state;
            cboDataBits.IsEnabled = state;
            lbStopBits.IsEnabled = state;
            cboStopBits.IsEnabled = state;
            lbParity.IsEnabled = state;
            cboParity.IsEnabled = state;
            lbHandShake.IsEnabled = state;
            cboHandShake.IsEnabled = state;
            lbSlaveID.IsEnabled = state;
            tbSlaveID.IsEnabled = state;
            lbAdamAddress.IsEnabled = state;
            tbAdamAddress.IsEnabled = state;
            lbOutputAddress1.IsEnabled = state;
            tbOutputAddress1.IsEnabled = state;
            lbOutputAddress2.IsEnabled = state;
            tbOutputAddress2.IsEnabled = state;
            lbOutputAddress3.IsEnabled = state;
            tbOutputAddress3.IsEnabled = state;
        }

        private void UpdateUI()
        {
            switch (outputConnection.type)
            {
                case OutputConnectionType.MODBUS_RTU:
                    lbSlaveID.Visibility = Visibility.Visible;
                    tbSlaveID.Visibility = Visibility.Visible;

                    lbAdamAddress.Visibility = Visibility.Collapsed;
                    tbAdamAddress.Visibility = Visibility.Collapsed;

                    lbOutputAddress1.Visibility = Visibility.Visible;
                    tbOutputAddress1.Visibility = Visibility.Visible;

                    lbOutputAddress2.Visibility = Visibility.Visible;
                    tbOutputAddress2.Visibility = Visibility.Visible;

                    lbOutputAddress3.Visibility = Visibility.Visible;
                    tbOutputAddress3.Visibility = Visibility.Visible;

                    break;

                case OutputConnectionType.ADAM_4060:
                    lbSlaveID.Visibility = Visibility.Collapsed;
                    tbSlaveID.Visibility = Visibility.Collapsed;

                    lbAdamAddress.Visibility = Visibility.Visible;
                    tbAdamAddress.Visibility = Visibility.Visible;

                    lbOutputAddress1.Visibility = Visibility.Collapsed;
                    tbOutputAddress1.Visibility = Visibility.Collapsed;

                    lbOutputAddress2.Visibility = Visibility.Collapsed;
                    tbOutputAddress2.Visibility = Visibility.Collapsed;

                    lbOutputAddress3.Visibility = Visibility.Collapsed;
                    tbOutputAddress3.Visibility = Visibility.Collapsed;

                    break;
            }
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
                cboPortName.Text = outputConnection.portName;
            }
            else
            {
                // Fant ingen porter
                cboPortName.Text = "No COM ports found";
            }
        }

        private void Modbus_Start()
        {
            sendLightsOutput?.Start();
        }

        private void Modbus_Stop()
        {
            sendLightsOutput?.Stop();

            // Lukke port
            serialPort?.Close();

            // Stenge MODBUS grensesnitt
            //modbusSerialMaster?.Dispose();
        }

        private bool LightOutputToWriteValue(LightsOutputType lightsOutput, int output)
        {
            // Q - Aviation: Q40RI03L HMS Repeater Light System
            //
            // Lys output:      Kanal 0:    Kanal 1:    Kanal 2:
            // Red              1           1           0
            // Amber            1           0           0
            // Blue             0           1           0
            // Red Flash        1           1           1
            // Amber Flash      1           0           1
            // Blue Flash       0           1           1

            switch (output)
            {
                case 0:
                    switch (lightsOutput)
                    {
                        case LightsOutputType.Amber:
                        case LightsOutputType.Red:
                        case LightsOutputType.AmberFlash:
                        case LightsOutputType.RedFlash:
                            return true;
                        default:
                            return false;
                    }

                case 1:
                    switch (lightsOutput)
                    {
                        case LightsOutputType.Blue:
                        case LightsOutputType.Red:
                        case LightsOutputType.BlueFlash:
                        case LightsOutputType.RedFlash:
                            return true;
                        default:
                            return false;
                    }

                case 2:
                    switch (lightsOutput)
                    {
                        case LightsOutputType.BlueFlash:
                        case LightsOutputType.AmberFlash:
                        case LightsOutputType.RedFlash:
                            return true;
                        default:
                            return false;
                    }

                default:
                    return false;
            }
        }

        private void cboConnectionType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (outputConnection != null)
            {
                outputConnection.SetType(EnumExtension.GetEnumValueFromDescription<OutputConnectionType>(cboConnectionType.Text));

                UpdateUI();
            }
        }

        private void cboCOMPort_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Lagre ny setting
            outputConnection.portName = cboPortName.SelectedItem.ToString();
            config.SetLightsOutputData(outputConnection.data);
        }

        private void cboBaudRate_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                // Lagre ny setting
                outputConnection.baudRate = Convert.ToInt32(cboBaudRate.SelectedItem.ToString());
                config.SetLightsOutputData(outputConnection.data);
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
                outputConnection.dataBits = Convert.ToInt16(cboDataBits.SelectedItem.ToString());
                config.SetLightsOutputData(outputConnection.data);
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
                outputConnection.stopBits = (StopBits)Enum.Parse(typeof(StopBits), cboStopBits.SelectedItem.ToString());
                config.SetLightsOutputData(outputConnection.data);
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
                outputConnection.parity = (Parity)Enum.Parse(typeof(Parity), cboParity.SelectedItem.ToString());
                config.SetLightsOutputData(outputConnection.data);
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
                outputConnection.handshake = (Handshake)Enum.Parse(typeof(Handshake), cboHandShake.SelectedItem.ToString());
                config.SetLightsOutputData(outputConnection.data);
            }
            catch (Exception ex)
            {
                RadWindow.Alert(string.Format("cboHandShake_SelectionChanged\n\n{0}", TextHelper.Wrap(ex.Message)));
            }
        }

        private void tbSlaveID_LostFocus(object sender, RoutedEventArgs e)
        {
            tbSlaveID_Update(sender);
        }

        private void tbSlaveID_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                tbSlaveID_Update(sender);
                Keyboard.ClearFocus();
            }
        }

        private void tbSlaveID_Update(object sender)
        {
            if (outputConnection.data.modbus != null)
            {
                // Sjekk av input
                DataValidation.Double(
                (sender as TextBox).Text,
                Constants.MODBUSSlaveIDMin,
                Constants.MODBUSSlaveIDMax,
                Constants.MODBUSSlaveIDDefault,
                out double validatedInput);

                outputConnection.data.modbus.slaveID = (byte)validatedInput;
                (sender as TextBox).Text = validatedInput.ToString();

                config.SetLightsOutputData(outputConnection.data);
            }
        }

        private void tbAdamAddress_LostFocus(object sender, RoutedEventArgs e)
        {
            tbAdamAddress_Update(sender);
        }

        private void tbAdamAddress_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                tbAdamAddress_Update(sender);
                Keyboard.ClearFocus();
            }
        }

        private void tbAdamAddress_Update(object sender)
        {
            // Sjekk av input
            DataValidation.Double(
            (sender as TextBox).Text,
            Constants.AdamAddressMin,
            Constants.AdamAddressMax,
            Constants.AdamAddressDefault,
            out double validatedInput);

            outputConnection.adamAddress = validatedInput.ToString();
            (sender as TextBox).Text = validatedInput.ToString();

            config.SetLightsOutputData(outputConnection.data);
        }

        private void cboTestHelideckStatus_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            hmsLightsOutputVM.testModeStatus = (HelideckStatusType)Enum.Parse(typeof(HelideckStatusType), cboTestHelideckStatus.SelectedItem.ToString());
        }

        private void cboTestDisplayMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            hmsLightsOutputVM.testModeDisplayMode = (DisplayMode)Enum.Parse(typeof(DisplayMode), cboTestDisplayMode.SelectedItem.ToString());
        }

        private void tbOutputAddress1_LostFocus(object sender, RoutedEventArgs e)
        {
            tbOutputAddress1_Update(sender);
        }

        private void tbOutputAddress1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                tbOutputAddress1_Update(sender);
                Keyboard.ClearFocus();
            }
        }

        private void tbOutputAddress1_Update(object sender)
        {
            try
            {
                bool inputOK = false;

                // Lagre ny setting
                if (!string.IsNullOrEmpty((sender as TextBox).Text))
                {
                    int address = 0;
                    if (int.TryParse((sender as TextBox).Text, out address))
                    {
                        if (modbusHelper.ValidAddressSpaceCoil(address))
                        {
                            hmsLightsOutputVM.outputAddress1 = (UInt16)address;
                            inputOK = true;
                        }
                    }
                }

                if (!inputOK)
                {
                    RadWindow.Alert(
                       string.Format("Input Error\n\nValid address space:\n{0} - {1}",
                       Constants.ModbusCoilMin,
                       Constants.ModbusCoilMax));

                    hmsLightsOutputVM.outputAddress1 = Constants.ModbusDefaultAddress;
                }

                (sender as TextBox).Text = hmsLightsOutputVM.outputAddress1.ToString();
            }
            catch (Exception ex)
            {
                RadWindow.Alert(string.Format("Input Error\n\n{0}", TextHelper.Wrap(ex.Message)));
            }
        }

        private void tbOutputAddress2_LostFocus(object sender, RoutedEventArgs e)
        {
            tbOutputAddress2_Update(sender);
        }

        private void tbOutputAddress2_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                tbOutputAddress2_Update(sender);
                Keyboard.ClearFocus();
            }
        }

        private void tbOutputAddress2_Update(object sender)
        {
            try
            {
                bool inputOK = false;

                // Lagre ny setting
                if (!string.IsNullOrEmpty((sender as TextBox).Text))
                {
                    int address = 0;
                    if (int.TryParse((sender as TextBox).Text, out address))
                    {
                        if (modbusHelper.ValidAddressSpaceCoil(address))
                        {
                            hmsLightsOutputVM.outputAddress2 = (UInt16)address;
                            inputOK = true;
                        }
                    }
                }

                if (!inputOK)
                {
                    RadWindow.Alert(
                       string.Format("Input Error\n\nValid address space:\n{0} - {1}",
                       Constants.ModbusCoilMin,
                       Constants.ModbusCoilMax));

                    hmsLightsOutputVM.outputAddress2 = Constants.ModbusDefaultAddress;
                }

                (sender as TextBox).Text = hmsLightsOutputVM.outputAddress2.ToString();
            }
            catch (Exception ex)
            {
                RadWindow.Alert(string.Format("Input Error\n\n{0}", TextHelper.Wrap(ex.Message)));
            }
        }


        private void tbOutputAddress3_LostFocus(object sender, RoutedEventArgs e)
        {
            tbOutputAddress3_Update(sender);
        }

        private void tbOutputAddress3_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                tbOutputAddress3_Update(sender);
                Keyboard.ClearFocus();
            }
        }

        private void tbOutputAddress3_Update(object sender)
        {
            try
            {
                bool inputOK = false;

                // Lagre ny setting
                if (!string.IsNullOrEmpty((sender as TextBox).Text))
                {
                    int address = 0;
                    if (int.TryParse((sender as TextBox).Text, out address))
                    {
                        if (modbusHelper.ValidAddressSpaceCoil(address))
                        {
                            hmsLightsOutputVM.outputAddress3 = (UInt16)address;
                            inputOK = true;
                        }
                    }
                }

                if (!inputOK)
                {
                    RadWindow.Alert(
                       string.Format("Input Error\n\nValid address space:\n{0} - {1}",
                       Constants.ModbusCoilMin,
                       Constants.ModbusCoilMax));

                    hmsLightsOutputVM.outputAddress3 = Constants.ModbusDefaultAddress;
                }

                (sender as TextBox).Text = hmsLightsOutputVM.outputAddress3.ToString();
            }
            catch (Exception ex)
            {
                RadWindow.Alert(string.Format("Input Error\n\n{0}", TextHelper.Wrap(ex.Message)));
            }
        }
    }
}