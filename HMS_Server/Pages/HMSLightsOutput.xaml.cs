using NModbus;
using NModbus.Serial;
using System;
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

        // Error Handler
        private ErrorHandler errorHandler;

        private SensorData lightsOutputData;
        private HMSLightsOutputVM hmsLightsOutputVM;
        private AdminSettingsVM adminSettingsVM;

        // Serie port objekt
        private SerialPort serialPort = new SerialPort();

        // MODBUS
        private ModbusFactory modbusFactory = new ModbusFactory();
        private ModbusHelper modbusHelper = new ModbusHelper();

        private DispatcherTimer modbusReader = new DispatcherTimer();

        public HMSLightsOutput()
        {
            InitializeComponent();
        }

        public void Init(SensorData lightsOutputData, HMSLightsOutputVM hmsLightsOutputVM, Config config, AdminSettingsVM adminSettingsVM, ErrorHandler errorHandler)
        {
            DataContext = hmsLightsOutputVM;

            this.lightsOutputData = lightsOutputData;
            this.hmsLightsOutputVM = hmsLightsOutputVM;
            this.config = config;
            this.adminSettingsVM = adminSettingsVM;
            this.errorHandler = errorHandler;

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
            cboBaudRate.Text = lightsOutputData.modbus.baudRate.ToString();

            // Data Bits
            ///////////////////////////////////////////////////////////
            cboDataBits.Items.Add(7);
            cboDataBits.Items.Add(8);

            // Skrive satt data bit type i combobox feltet
            cboDataBits.Text = lightsOutputData.modbus.dataBits.ToString();

            // Stop Bits
            ///////////////////////////////////////////////////////////
            cboStopBits.Items.Add("One");
            cboStopBits.Items.Add("OnePointFive");
            cboStopBits.Items.Add("Two");

            // Skrive satt stop bit i combobox feltet
            cboStopBits.Text = lightsOutputData.modbus.stopBits.ToString();

            // Parity 
            ///////////////////////////////////////////////////////////
            cboParity.Items.Add("None");
            cboParity.Items.Add("Even");
            cboParity.Items.Add("Mark");
            cboParity.Items.Add("Odd");
            cboParity.Items.Add("Space");

            // Skrive satt parity i combobox feltet
            cboParity.Text = lightsOutputData.modbus.parity.ToString();

            // Handshake
            ///////////////////////////////////////////////////////////
            cboHandShake.Items.Add("None");
            cboHandShake.Items.Add("XOnXOff");
            cboHandShake.Items.Add("RequestToSend");
            cboHandShake.Items.Add("RequestToSendXOnXOff");

            // Slave ID
            ///////////////////////////////////////////////////////////
            tbSlaveID.Text = lightsOutputData.modbus.slaveID.ToString();

            // Sette serie port settings på SerialPort object 
            ///////////////////////////////////////////////////////////
            serialPort.PortName = lightsOutputData.modbus.portName;
            serialPort.BaudRate = lightsOutputData.modbus.baudRate;
            serialPort.DataBits = lightsOutputData.modbus.dataBits;
            serialPort.StopBits = lightsOutputData.modbus.stopBits;
            serialPort.Parity = lightsOutputData.modbus.parity;
            serialPort.Handshake = lightsOutputData.modbus.handshake;

            // Timeout
            serialPort.ReadTimeout = Constants.ModbusTimeout;       // NB! Brukes av ReadHoldingRegisters. Uten disse vil programmet fryse dersom ReadHoldingRegisters ikke får svar.
            serialPort.WriteTimeout = Constants.ModbusTimeout;

            // Skrive satt hand shake type i combobox feltet
            cboHandShake.Text = lightsOutputData.modbus.handshake.ToString();

            // MODBUS RTU Reader
            modbusReader.Tick += runModbusWriter;

            void runModbusWriter(object sender, EventArgs e)
            {
                ModbusSendLightsOutputCommand();
                //Thread thread = new Thread(() => ModbusWrite());
                //thread.IsBackground = true;
                //thread.Start();

                //void ModbusWrite()
                //{
                //    // Sende Data via MODBUS
                //    ModbusSendLightsOutputCommand();
                //}
            }
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

            Modbus_Start();
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

            Modbus_Stop();
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
                cboPortName.Text = lightsOutputData.modbus.portName;
            }
            else
            {
                // Fant ingen porter
                cboPortName.Text = "No COM ports found";
            }
        }

        private void ModbusSendLightsOutputCommand()
        {
            if (lightsOutputData.type != SensorType.ModbusTCP)
            {
                try
                {
                    // Sjekke om porten er åpen
                    if (!serialPort.IsOpen)
                        serialPort.Open();

                    if (serialPort.IsOpen)
                    {
                        // Starte MODBUS
                        IModbusSerialMaster modbusSerialMaster = modbusFactory.CreateRtuMaster(new SerialPortAdapter(serialPort));

                        int i = 1;
                        foreach (var outputAddress in hmsLightsOutputVM.outputAddressList)
                        {
                            // Finne ut hvor det skal skrives
                            ModbusObjectType modbusObjectType = modbusHelper.AddressToObjectType(outputAddress);

                            // Skrive til registers (kun til coil)
                            switch (modbusObjectType)
                            {
                                case ModbusObjectType.Coil:
                                    {
                                        modbusSerialMaster.WriteSingleCoil(
                                            lightsOutputData.modbus.slaveID,
                                            outputAddress,
                                            LightOutputToWriteValue(hmsLightsOutputVM.HMSLightsOutput, i++));
                                    }
                                    break;
                            }
                        }

                        // Lukke port
                        serialPort.Close();
                    }
                }
                catch (Exception ex)
                {
                    errorHandler.Insert(
                        new ErrorMessage(
                            DateTime.UtcNow,
                            ErrorMessageType.MODBUS,
                            ErrorMessageCategory.AdminUser,
                            string.Format("Modbus_Write\n\nSystem Message:\n{0}", ex.Message)));

                    // Lukke port
                    serialPort.Close();
                }
            }
        }

        private void Modbus_Start()
        {
            // Gjør dette trikset med Interval her for å få dispatchertimer til å kjøre med en gang.
            // Ellers venter den til intervallet er gått før den kjører første gang.
            modbusReader.Interval = TimeSpan.FromMilliseconds(0);
            modbusReader.Start();
            modbusReader.Interval = TimeSpan.FromMilliseconds(lightsOutputData.GetSaveFrequency(config));
        }

        private void Modbus_Stop()
        {
            modbusReader.Stop();
        }

        private bool LightOutputToWriteValue(LightsOutputType lightsOutput, int output)
        {
            // Q - Aviation: Q40RI03L HMS Repeater Light System
            switch (output)
            {
                case 1:
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

                case 2:
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

                case 3:
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

        private void cboCOMPort_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Lagre ny setting
            lightsOutputData.modbus.portName = cboPortName.SelectedItem.ToString();
            config.SetLightsOutputData(lightsOutputData);
        }

        private void cboBaudRate_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                // Lagre ny setting
                lightsOutputData.modbus.baudRate = Convert.ToInt32(cboBaudRate.SelectedItem.ToString());
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
                lightsOutputData.modbus.dataBits = Convert.ToInt16(cboDataBits.SelectedItem.ToString());
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
                lightsOutputData.modbus.stopBits = (StopBits)Enum.Parse(typeof(StopBits), cboStopBits.SelectedItem.ToString());
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
                lightsOutputData.modbus.parity = (Parity)Enum.Parse(typeof(Parity), cboParity.SelectedItem.ToString());
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
                lightsOutputData.modbus.handshake = (Handshake)Enum.Parse(typeof(Handshake), cboHandShake.SelectedItem.ToString());
                config.SetLightsOutputData(lightsOutputData);
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
            // Sjekk av input
            DataValidation.Double(
                (sender as TextBox).Text,
                Constants.MODBUSSlaveIDMin,
                Constants.MODBUSSlaveIDMax,
                Constants.MODBUSSlaveIDDefault,
                out double validatedInput);

            lightsOutputData.modbus.slaveID = (byte)validatedInput;
            (sender as TextBox).Text = validatedInput.ToString();
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
                // Lagre ny setting
                if (!string.IsNullOrEmpty((sender as TextBox).Text))
                {
                    int address = 0;
                    if (int.TryParse((sender as TextBox).Text, out address))
                    {
                        if (modbusHelper.ValidAddressSpaceCoil(address))
                        {
                            hmsLightsOutputVM.outputAddress1 = (UInt16)address;
                        }
                        else
                        {
                            RadWindow.Alert(
                                string.Format("Input Error\n\nValid address space:\n{0} - {1}",
                                    Constants.ModbusCoilMin,
                                    Constants.ModbusCoilMax));

                            hmsLightsOutputVM.outputAddress1 = Constants.ModbusDefaultAddress;
                        }
                    }
                }
                else
                {
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
                // Lagre ny setting
                if (!string.IsNullOrEmpty((sender as TextBox).Text))
                {
                    int address = 0;
                    if (int.TryParse((sender as TextBox).Text, out address))
                    {
                        if (modbusHelper.ValidAddressSpaceCoil(address))
                        {
                            hmsLightsOutputVM.outputAddress2 = (UInt16)address;
                        }
                        else
                        {
                            RadWindow.Alert(
                                string.Format("Input Error\n\nValid address space:\n{0} - {1}",
                                    Constants.ModbusCoilMin,
                                    Constants.ModbusCoilMax));

                            hmsLightsOutputVM.outputAddress2 = Constants.ModbusDefaultAddress;
                        }
                    }
                }
                else
                {
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
                // Lagre ny setting
                if (!string.IsNullOrEmpty((sender as TextBox).Text))
                {
                    int address = 0;
                    if (int.TryParse((sender as TextBox).Text, out address))
                    {
                        if (modbusHelper.ValidAddressSpaceCoil(address))
                        {
                            hmsLightsOutputVM.outputAddress3 = (UInt16)address;
                        }
                        else
                        {
                            RadWindow.Alert(
                                string.Format("Input Error\n\nValid address space:\n{0} - {1}",
                                    Constants.ModbusCoilMin,
                                    Constants.ModbusCoilMax));

                            hmsLightsOutputVM.outputAddress3 = Constants.ModbusDefaultAddress;
                        }
                    }
                }
                else
                {
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