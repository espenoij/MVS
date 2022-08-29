using NModbus;
using NModbus.Serial;
using System;
using System.IO.Ports;
using System.Linq;
using System.Net.Sockets;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Telerik.Windows.Controls;
using Telerik.Windows.Data;

namespace HMS_Server
{
    /// <summary>
    /// Interaction logic for ModbusSetupWindow.xaml
    /// </summary>
    public partial class ModbusSetupWindow : RadWindow
    {
        // Configuration settings
        private Config config;

        // Error Handler
        private ErrorHandler errorHandler;

        // Admin settings
        private AdminSettingsVM adminSettingsVM;

        // Sensor Data List
        private RadObservableCollection<SensorData> sensorDataList = new RadObservableCollection<SensorData>();

        // Valgt sensor data
        private SensorData sensorData;

        // Serie port objekt
        private SerialPort serialPort = new SerialPort();

        // MODBUS
        private ModbusFactory modbusFactory = new ModbusFactory();
        private IModbusSerialMaster modbusSerialMaster;
        private IModbusMaster modbusTCPMaster;
        private TcpClient modbusTCPClient;
        private ModbusHelper modbusHelper = new ModbusHelper();

        private DispatcherTimer modbusReader = new DispatcherTimer();

        // Data Processing
        private ModbusCalculations process = new ModbusCalculations();

        // Lister for visning
        private RadObservableCollection<ModbusData> rawDataItems = new RadObservableCollection<ModbusData>();
        private RadObservableCollection<ModbusData> selectedDataItems = new RadObservableCollection<ModbusData>();
        private RadObservableCollection<ModbusData> calculatedDataItems = new RadObservableCollection<ModbusData>();

        // View Model
        private ModbusSetupWindowVM modbusSetupWindowVM;

        public ModbusSetupWindow(SensorData sensorData, RadObservableCollection<SensorData> sensorDataList, Config config, ErrorHandler errorHandler, AdminSettingsVM adminSettingsVM)
        {
            InitializeComponent();

            modbusSetupWindowVM = new ModbusSetupWindowVM(config);
            DataContext = modbusSetupWindowVM;

            // Lagre valgt sensor data
            this.sensorData = sensorData;

            // Sensor Data List
            this.sensorDataList = sensorDataList;

            // Config
            this.config = config;

            // Error Handler
            this.errorHandler = errorHandler;

            // Admin Settings
            this.adminSettingsVM = adminSettingsVM;

            // Initialisere application settings
            InitializeApplicationSettings();
        }

        private void InitializeApplicationSettings()
        {
            // Initialiserer MODBUS leser
            InitializeModbusReader();

            InitBasicInformation();

            // Initialisere Serial Port Configuration settings
            InitializeConnectionSettings();

            // Initialisere Slave Selection
            InitializeModbusSettings();

            // Initialize Data Selection
            InitializeDataSelectionSettings();

            // Initialisere Data Processing
            InitializeDataCalculations();
        }

        private void InitializeModbusReader()
        {
            modbusReader.Tick += runModbusReader;

            void runModbusReader(object sender, EventArgs e)
            {
                // Åpne MODBUS port
                Modbus_Open();

                // Lese Data
                Modbus_Read();

                // Lukke MODBUS port
                Modbus_Close();
            }
        }

        private void InitBasicInformation()
        {
            // Basic Information
            lbSensorID.Content = sensorData.id.ToString();
            lbSensorName.Content = sensorData.name;
            lbSensorType.Content = sensorData.type.ToString();
        }

        private void InitializeConnectionSettings()
        {
            // Binding for sample data listviews
            lvModbusRawData.ItemsSource = rawDataItems;

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
            cboBaudRate.Text = sensorData.modbus.baudRate.ToString();

            // Data Bits
            ///////////////////////////////////////////////////////////
            cboDataBits.Items.Add(7);
            cboDataBits.Items.Add(8);

            // Skrive satt data bit type i combobox feltet
            cboDataBits.Text = sensorData.modbus.dataBits.ToString();

            // Stop Bits
            ///////////////////////////////////////////////////////////
            cboStopBits.Items.Add("One");
            cboStopBits.Items.Add("OnePointFive");
            cboStopBits.Items.Add("Two");

            // Skrive satt stop bit i combobox feltet
            cboStopBits.Text = sensorData.modbus.stopBits.ToString();

            // Parity 
            ///////////////////////////////////////////////////////////
            cboParity.Items.Add("None");
            cboParity.Items.Add("Even");
            cboParity.Items.Add("Mark");
            cboParity.Items.Add("Odd");
            cboParity.Items.Add("Space");

            // Skrive satt parity i combobox feltet
            cboParity.Text = sensorData.modbus.parity.ToString();

            // Handshake
            ///////////////////////////////////////////////////////////
            cboHandShake.Items.Add("None");
            cboHandShake.Items.Add("XOnXOff");
            cboHandShake.Items.Add("RequestToSend");
            cboHandShake.Items.Add("RequestToSendXOnXOff");

            // Skrive satt hand shake type i combobox feltet
            cboHandShake.Text = sensorData.modbus.handshake.ToString();

            // TCP Address
            ///////////////////////////////////////////////////////////
            tbModbusTCPAddress.Text = sensorData.modbus.tcpAddress;

            // TCP Port
            ///////////////////////////////////////////////////////////
            tbModbusTCPPort.Text = sensorData.modbus.tcpPort.ToString();

            // Avhengig av MODBUS type, sette hvilke connection parametre som skal være åpne
            ///////////////////////////////////////////////////////////
            if (sensorData.type == SensorType.ModbusRTU ||
                sensorData.type == SensorType.ModbusASCII)
            {
                cboPortName.IsEnabled = true;
                cboBaudRate.IsEnabled = true;
                cboDataBits.IsEnabled = true;
                cboStopBits.IsEnabled = true;
                cboParity.IsEnabled = true;
                cboHandShake.IsEnabled = true;

                tbModbusTCPAddress.IsEnabled = false;
                tbModbusTCPPort.IsEnabled = false;
            }
            // TCP
            else
            {
                cboPortName.IsEnabled = false;
                cboBaudRate.IsEnabled = false;
                cboDataBits.IsEnabled = false;
                cboStopBits.IsEnabled = false;
                cboParity.IsEnabled = false;
                cboHandShake.IsEnabled = false;

                tbModbusTCPAddress.IsEnabled = true;
                tbModbusTCPPort.IsEnabled = true;
            }

            // Status Felt
            ///////////////////////////////////////////////////////////
            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(200);
            timer.Tick += timer_Tick;
            timer.Start();

            void timer_Tick(object sender, EventArgs e)
            {
                if (modbusReader.IsEnabled)
                {
                    bModbusPortStatus.Background = (Brush)this.FindResource("ColorGreen");
                }
                else
                {
                    bModbusPortStatus.Background = (Brush)this.FindResource("ColorRed");
                }
            }
        }

        private void InitializeModbusSettings()
        {
            // Slave ID
            tbModbusSlaveID.Text = sensorData.modbus.slaveID.ToString();

            // Start Address
            tbSampleDataStartAddress.Text = sensorData.modbus.startAddress.ToString();

            // Total Address
            tbSampleDataTotalAddresses.Text = sensorData.modbus.totalAddresses.ToString();
        }

        private void InitializeDataSelectionSettings()
        {
            // Binding for sample data listviews
            lvSelectedData.ItemsSource = selectedDataItems;
            lvSelectedData2.ItemsSource = selectedDataItems;

            // Total Address
            tbSelectDataAddress.Text = sensorData.modbus.dataAddress.ToString();

            // Data Lines
            modbusSetupWindowVM.totalDataLinesString = config.ReadWithDefault(ConfigKey.TotalDataLines, ConfigSection.ModbusConfig, Constants.GUIDataLinesDefault).ToString();
        }

        private void InitializeDataCalculations()
        {
            try
            {
                // Binding for listviews
                lvCalculatedData.ItemsSource = calculatedDataItems;

                // Data Calculations 1
                // Prosesseringstyper
                foreach (CalculationType value in Enum.GetValues(typeof(CalculationType)))
                    cboCalculationType1.Items.Add(value.GetDescription());

                cboCalculationType1.Text = sensorData.modbus.calculationSetup[0].type.GetDescription();
                cboCalculationType1.SelectedIndex = (int)sensorData.modbus.calculationSetup[0].type;

                // Parameter
                tbCalculationParameter1.Text = sensorData.modbus.calculationSetup[0].parameter.ToString();

                // Data Calculations 2
                // Prosesseringstyper
                foreach (CalculationType value in Enum.GetValues(typeof(CalculationType)))
                    cboCalculationType2.Items.Add(value.GetDescription());

                cboCalculationType2.Text = sensorData.modbus.calculationSetup[1].type.GetDescription();
                cboCalculationType2.SelectedIndex = (int)sensorData.modbus.calculationSetup[1].type;

                // Parameter
                tbCalculationParameter2.Text = sensorData.modbus.calculationSetup[1].parameter.ToString();

                // Data Calculations 3
                // Prosesseringstyper
                foreach (CalculationType value in Enum.GetValues(typeof(CalculationType)))
                    cboCalculationType3.Items.Add(value.GetDescription());

                cboCalculationType3.Text = sensorData.modbus.calculationSetup[2].type.GetDescription();
                cboCalculationType3.SelectedIndex = (int)sensorData.modbus.calculationSetup[2].type;

                // Parameter
                tbCalculationParameter3.Text = sensorData.modbus.calculationSetup[2].parameter.ToString();

                // Data Calculations 4
                // Prosesseringstyper
                foreach (CalculationType value in Enum.GetValues(typeof(CalculationType)))
                    cboCalculationType4.Items.Add(value.GetDescription());

                cboCalculationType4.Text = sensorData.modbus.calculationSetup[3].type.GetDescription();
                cboCalculationType4.SelectedIndex = (int)sensorData.modbus.calculationSetup[3].type;

                // Parameter
                tbCalculationParameter4.Text = sensorData.modbus.calculationSetup[3].parameter.ToString();
            }
            catch (Exception ex)
            {
                RadWindow.Alert(string.Format("InitializeDataProcessing\n\n{0}", TextHelper.Wrap(ex.Message)));
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
                {
                    // Sjekke om porten er brukt til Serie Port kommunikasjon -> Da skal den ikke brukes til MODBUS
                    if (sensorDataList.Where(x => x.type == SensorType.SerialPort && x.serialPort.portName == comPortName).Count() == 0)
                    {
                        cboPortName.Items.Add(comPortName);
                    }
                }

                // Skrive satt COM port i combobox tekst feltet
                cboPortName.Text = sensorData.modbus.portName;
            }
            else
            {
                // Fant ingen porter
                cboPortName.Text = "No COM ports found";
            }
        }

        private void Modbus_Open()
        {
            try
            {
                // Åpne MODBUS protokoll
                switch (sensorData.type)
                {
                    case SensorType.ModbusRTU:
                    case SensorType.ModbusASCII:
                        // Sette serie port settings på SerialPort object 
                        serialPort.PortName = sensorData.modbus.portName;
                        serialPort.BaudRate = sensorData.modbus.baudRate;
                        serialPort.DataBits = sensorData.modbus.dataBits;
                        serialPort.StopBits = sensorData.modbus.stopBits;
                        serialPort.Parity = sensorData.modbus.parity;
                        serialPort.Handshake = sensorData.modbus.handshake;

                        // Timeout
                        serialPort.ReadTimeout = Constants.ModbusTimeout;       // NB! Brukes av ReadHoldingRegisters. Uten disse vil programmet fryse dersom ReadHoldingRegisters ikke får svar.
                        serialPort.WriteTimeout = Constants.ModbusTimeout;

                        // Åpne port
                        serialPort.Open();

                        if (sensorData.type == SensorType.ModbusRTU)
                            modbusSerialMaster = modbusFactory.CreateRtuMaster(new SerialPortAdapter(serialPort));
                        else
                            modbusSerialMaster = modbusFactory.CreateAsciiMaster(new SerialPortAdapter(serialPort));
                        break;

                    case SensorType.ModbusTCP:
                        modbusTCPClient = new TcpClient(sensorData.modbus.tcpAddress, sensorData.modbus.tcpPort);

                        modbusTCPClient.ReceiveTimeout = Constants.ModbusTimeout;
                        modbusTCPClient.SendTimeout = Constants.ModbusTimeout;

                        modbusTCPMaster = modbusFactory.CreateMaster(modbusTCPClient);
                        break;
                }
            }
            catch (Exception ex)
            {
                RadWindow.Alert(string.Format("Error opening MODBUS connection\n\n{0}", TextHelper.Wrap(ex.Message)));

                // Stoppe lesing av data dersom vi ikke får åpnet serie port
                Modbus_Stop();
            }
        }

        private void Modbus_Read()
        {
            if ((sensorData.type == SensorType.ModbusTCP && modbusTCPMaster != null) ||
                (sensorData.type != SensorType.ModbusTCP && modbusSerialMaster != null))
            {
                try
                {
                    // Data lagres her
                    ModbusData modbusData = new ModbusData();

                    // Finne ut hvor det skal leses
                    ModbusObjectType modbusObjectType = modbusHelper.AddressToObjectType(sensorData.modbus.dataAddress);

                    // Read registers
                    switch (modbusObjectType)
                    {
                        case ModbusObjectType.Coil:
                            {
                                bool[] registers = new bool[0];

                                if (sensorData.type == SensorType.ModbusTCP)
                                    registers = modbusTCPMaster.ReadCoils(sensorData.modbus.slaveID, modbusHelper.AddressToOffset(sensorData.modbus.startAddress), (ushort)sensorData.modbus.totalAddresses);
                                else
                                    registers = modbusSerialMaster.ReadCoils(sensorData.modbus.slaveID, modbusHelper.AddressToOffset(sensorData.modbus.startAddress), (ushort)sensorData.modbus.totalAddresses);

                                // Vise sample data
                                DisplayRegisters(registers, sensorData.modbus);

                                // Selected Data
                                process.GetSelectedData(sensorData, registers, modbusData);
                            }
                            break;

                        case ModbusObjectType.DiscreteInput:
                            {
                                bool[] registers = new bool[0]; ;

                                if (sensorData.type == SensorType.ModbusTCP)
                                    registers = modbusTCPMaster.ReadInputs(sensorData.modbus.slaveID, modbusHelper.AddressToOffset(sensorData.modbus.startAddress), (ushort)sensorData.modbus.totalAddresses);
                                else
                                    registers = modbusSerialMaster.ReadInputs(sensorData.modbus.slaveID, modbusHelper.AddressToOffset(sensorData.modbus.startAddress), (ushort)sensorData.modbus.totalAddresses);

                                // Vise sample data
                                DisplayRegisters(registers, sensorData.modbus);

                                // Selected Data
                                process.GetSelectedData(sensorData, registers, modbusData);
                            }
                            break;

                        case ModbusObjectType.InputRegister:
                            {
                                ushort[] registers = new ushort[0];

                                if (sensorData.type == SensorType.ModbusTCP)
                                    registers = modbusTCPMaster.ReadInputRegisters(sensorData.modbus.slaveID, modbusHelper.AddressToOffset(sensorData.modbus.startAddress), (ushort)sensorData.modbus.totalAddresses);
                                else
                                    registers = modbusSerialMaster.ReadInputRegisters(sensorData.modbus.slaveID, modbusHelper.AddressToOffset(sensorData.modbus.startAddress), (ushort)sensorData.modbus.totalAddresses);

                                // Vise sample data
                                DisplayRegisters(registers, sensorData.modbus);

                                // Selected Data
                                process.GetSelectedData(sensorData, registers, modbusData);
                            }
                            break;

                        case ModbusObjectType.HoldingRegister:
                            {
                                ushort[] registers = new ushort[0];

                                if (sensorData.type == SensorType.ModbusTCP)
                                    registers = modbusTCPMaster.ReadHoldingRegisters(sensorData.modbus.slaveID, modbusHelper.AddressToOffset(sensorData.modbus.startAddress), (ushort)sensorData.modbus.totalAddresses);
                                else
                                    registers = modbusSerialMaster.ReadHoldingRegisters(sensorData.modbus.slaveID, modbusHelper.AddressToOffset(sensorData.modbus.startAddress), (ushort)sensorData.modbus.totalAddresses);

                                // Vise sample data
                                DisplayRegisters(registers, sensorData.modbus);

                                // Selected Data
                                process.GetSelectedData(sensorData, registers, modbusData);
                            }
                            break;
                    }

                    // Vise selected data
                    DisplaySelectedData(modbusData);

                    // Utføre kalkulasjoner på data
                    process.ApplyCalculationsToSelectedData(sensorData, DateTime.UtcNow, modbusData, errorHandler, ErrorMessageCategory.AdminUser, adminSettingsVM);

                    // Vise processed data
                    DisplayProcessedData(modbusData);
                }
                catch (Exception ex)
                {
                    RadWindow.Alert(string.Format("Modbus_Read\n\n{0}", TextHelper.Wrap(ex.Message)));

                    Modbus_Stop();
                }
            }
        }

        private void Modbus_Close()
        {
            switch (sensorData.type)
            {
                case SensorType.ModbusRTU:
                case SensorType.ModbusASCII:
                    if (serialPort != null)
                        serialPort.Close();
                    break;

                case SensorType.ModbusTCP:
                    if (modbusTCPClient != null)
                        modbusTCPClient.Close();
                    break;
            }
        }

        private void Modbus_Start()
        {
            ModbusDispatcher_Start();

            // Status
            sensorMonitorStatus.Text = "MODBUS connection open for reading...";

            // Stenge tilgang til å konfigurere porten
            cboPortName.IsEnabled = false;
            cboBaudRate.IsEnabled = false;
            cboDataBits.IsEnabled = false;
            cboStopBits.IsEnabled = false;
            cboParity.IsEnabled = false;
            cboHandShake.IsEnabled = false;

            // Stenge tilgang til TCP
            tbModbusTCPAddress.IsEnabled = false;
            tbModbusTCPPort.IsEnabled = false;

            tbModbusSlaveID.IsEnabled = false;
            tbSampleDataStartAddress.IsEnabled = false;
            tbSampleDataTotalAddresses.IsEnabled = false;

            btnModbusReaderStart.IsEnabled = false;
            btnModbusReaderStop.IsEnabled = true;
        }

        private void Modbus_Stop()
        {
            Modbus_Close();

            // Starter reader
            ModbusDispatcher_Stop();

            // Status
            sensorMonitorStatus.Text = "MODBUS connection closed.";

            if (sensorData.type == SensorType.ModbusRTU ||
                sensorData.type == SensorType.ModbusASCII)
            {
                cboPortName.IsEnabled = true;
                cboBaudRate.IsEnabled = true;
                cboDataBits.IsEnabled = true;
                cboStopBits.IsEnabled = true;
                cboParity.IsEnabled = true;
                cboHandShake.IsEnabled = true;

                tbModbusTCPAddress.IsEnabled = false;
                tbModbusTCPPort.IsEnabled = false;
            }
            else
            {
                cboPortName.IsEnabled = false;
                cboBaudRate.IsEnabled = false;
                cboDataBits.IsEnabled = false;
                cboStopBits.IsEnabled = false;
                cboParity.IsEnabled = false;
                cboHandShake.IsEnabled = false;

                tbModbusTCPAddress.IsEnabled = true;
                tbModbusTCPPort.IsEnabled = true;
            }

            tbModbusSlaveID.IsEnabled = true;
            tbSampleDataStartAddress.IsEnabled = true;
            tbSampleDataTotalAddresses.IsEnabled = true;

            btnModbusReaderStart.IsEnabled = true;
            btnModbusReaderStop.IsEnabled = false;
        }

        private void ModbusDispatcher_Start()
        {
            // Gjør dette trikset med Interval her for å få dispatchertimer til å kjøre med en gang.
            // Ellers venter den til intervallet er gått før den kjører første gang.
            modbusReader.Interval = TimeSpan.FromMilliseconds(0);
            modbusReader.Start();
            modbusReader.Interval = TimeSpan.FromMilliseconds(sensorData.GetSaveFrequency(config));
        }

        private void ModbusDispatcher_Stop()
        {
            modbusReader.Stop();
        }

        private void DisplayRegisters(bool[] registers, ModbusSetup modbusSetup)
        {
            Application.Current.Dispatcher.BeginInvoke(
            DispatcherPriority.Normal,
            new Action(() =>
            {

                try
                {
                    // Fjerne alle gamle data
                    rawDataItems.Clear();

                    // Legg ut data i sample data listview
                    for (int i = 0; i < modbusSetup.totalAddresses && i < registers.Length; i++)
                    {
                        rawDataItems.Add(new ModbusData()
                        {
                            address = modbusSetup.startAddress + i,
                            data = Convert.ToInt32(registers[i])
                        });
                    }

                    // Status
                    sensorMonitorStatus.Text = "Receiving data from MODBUS...";
                }
                catch (Exception ex)
                {
                    RadWindow.Alert(string.Format("Register Data Error (1)\n\n{0}", TextHelper.Wrap(ex.Message)));
                }
            }));
        }

        private void DisplayRegisters(ushort[] registers, ModbusSetup modbusSetup)
        {
            Application.Current.Dispatcher.BeginInvoke(
            DispatcherPriority.Normal,
            new Action(() =>
            {

                try
                {
                    // Fjerne alle gamle data
                    rawDataItems.Clear();

                    // Legg ut data i sample data listview
                    for (int i = 0; i < modbusSetup.totalAddresses && i < registers.Length; i++)
                    {
                        rawDataItems.Add(new ModbusData()
                        {
                            address = modbusSetup.startAddress + i,
                            data = Convert.ToInt32(registers[i])
                        });
                    }

                    // Status
                    sensorMonitorStatus.Text = "Receiving data from MODBUS...";
                }
                catch (Exception ex)
                {
                    RadWindow.Alert(string.Format("Register Data Error (2)\n\n{0}", TextHelper.Wrap(ex.Message)));
                }
            }));
        }

        private void DisplaySelectedData(ModbusData modbusData)
        {
            Application.Current.Dispatcher.BeginInvoke(
            DispatcherPriority.Normal,
            new Action(() =>
            {
                // Legg ut data i packet data listview
                selectedDataItems.Add(modbusData);

                // Begrense data på skjerm
                while (selectedDataItems.Count() > modbusSetupWindowVM.totalDataLines)
                    selectedDataItems.RemoveAt(0);
            }));
        }

        private void DisplayProcessedData(ModbusData modbusData)
        {
            Application.Current.Dispatcher.BeginInvoke(
            DispatcherPriority.Normal,
            new Action(() =>
            {

                // Legg ut data i packet data listview
                calculatedDataItems.Add(modbusData);

                // Begrense data på skjerm
                while (calculatedDataItems.Count() > modbusSetupWindowVM.totalDataLines)
                    calculatedDataItems.RemoveAt(0);
            }));
        }

        private void cboCOMPort_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Lagre ny setting
            sensorData.modbus.portName = cboPortName.SelectedItem.ToString();
        }

        private void cboBaudRate_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                // Lagre ny setting
                sensorData.modbus.baudRate = Convert.ToInt32(cboBaudRate.SelectedItem.ToString());
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
                sensorData.modbus.dataBits = Convert.ToInt16(cboDataBits.SelectedItem.ToString());
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
                sensorData.modbus.stopBits = (StopBits)Enum.Parse(typeof(StopBits), cboStopBits.SelectedItem.ToString());
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
                sensorData.modbus.parity = (Parity)Enum.Parse(typeof(Parity), cboParity.SelectedItem.ToString());
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
                sensorData.modbus.handshake = (Handshake)Enum.Parse(typeof(Handshake), cboHandShake.SelectedItem.ToString());
            }
            catch (Exception ex)
            {
                RadWindow.Alert(string.Format("cboHandShake_SelectionChanged\n\n{0}", TextHelper.Wrap(ex.Message)));
            }
        }

        private void btnModbusReaderOpen_Click(object sender, RoutedEventArgs e)
        {
            Modbus_Start();
        }

        private void btnModbusReaderClose_Click(object sender, RoutedEventArgs e)
        {
            Modbus_Stop();
        }

        private void chkSelectedDataClear_Click(object sender, RoutedEventArgs e)
        {
            rawDataItems.Clear();
            selectedDataItems.Clear();
            calculatedDataItems.Clear();
        }

        private void cboCalculationType1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Lagre ny setting
            sensorData.modbus.calculationSetup[0].type = (CalculationType)cboCalculationType1.SelectedIndex;
            sensorData.dataCalculations[0].type = sensorData.modbus.calculationSetup[0].type;
        }

        private void tbCalculationParameter1_LostFocus(object sender, RoutedEventArgs e)
        {
            tbCalculationParameter1_Update(sender);
        }

        private void tbCalculationParameter1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                tbCalculationParameter1_Update(sender);
                Keyboard.ClearFocus();
            }
        }

        private void tbCalculationParameter1_Update(object sender)
        {
            double param;

            // Lagre ny setting
            if (double.TryParse((sender as TextBox).Text, Constants.numberStyle, Constants.cultureInfo, out param))
                sensorData.modbus.calculationSetup[0].parameter = param;
            else
                sensorData.modbus.calculationSetup[0].parameter = 0;

            sensorData.dataCalculations[0].parameter = sensorData.modbus.calculationSetup[0].parameter;

            // Skrive data (som kan være korrigert) tilbake til tekstfeltet
            (sender as TextBox).Text = sensorData.modbus.calculationSetup[0].parameter.ToString();
        }

        private void cboCalculationType2_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Lagre ny setting
            sensorData.modbus.calculationSetup[1].type = (CalculationType)cboCalculationType2.SelectedIndex;
            sensorData.dataCalculations[1].type = sensorData.modbus.calculationSetup[1].type;
        }

        private void tbCalculationParameter2_LostFocus(object sender, RoutedEventArgs e)
        {
            tbCalculationParameter2_Update(sender);
        }

        private void tbCalculationParameter2_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                tbCalculationParameter2_Update(sender);
                Keyboard.ClearFocus();
            }
        }

        private void tbCalculationParameter2_Update(object sender)
        {
            // Lagre ny setting
            if (double.TryParse((sender as TextBox).Text, Constants.numberStyle, Constants.cultureInfo, out double param))
                sensorData.modbus.calculationSetup[1].parameter = param;
            else
                sensorData.modbus.calculationSetup[1].parameter = 0;

            sensorData.dataCalculations[1].parameter = sensorData.modbus.calculationSetup[1].parameter;

            // Skrive data (som kan være korrigert) tilbake til tekstfeltet
            (sender as TextBox).Text = sensorData.modbus.calculationSetup[1].parameter.ToString();
        }

        private void cboCalculationType3_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Lagre ny setting
            sensorData.modbus.calculationSetup[2].type = (CalculationType)cboCalculationType3.SelectedIndex;
            sensorData.dataCalculations[2].type = sensorData.modbus.calculationSetup[2].type;
        }

        private void tbCalculationParameter3_LostFocus(object sender, RoutedEventArgs e)
        {
            tbCalculationParameter3_Update(sender);
        }

        private void tbCalculationParameter3_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                tbCalculationParameter3_Update(sender);
                Keyboard.ClearFocus();
            }
        }

        private void tbCalculationParameter3_Update(object sender)
        {
            // Lagre ny setting
            if (double.TryParse((sender as TextBox).Text, Constants.numberStyle, Constants.cultureInfo, out double param))
                sensorData.modbus.calculationSetup[2].parameter = param;
            else
                sensorData.modbus.calculationSetup[2].parameter = 0;

            sensorData.dataCalculations[2].parameter = sensorData.modbus.calculationSetup[2].parameter;

            // Skrive data (som kan være korrigert) tilbake til tekstfeltet
            (sender as TextBox).Text = sensorData.modbus.calculationSetup[2].parameter.ToString();
        }

        private void cboCalculationType4_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Lagre ny setting
            sensorData.modbus.calculationSetup[3].type = (CalculationType)cboCalculationType4.SelectedIndex;
            sensorData.dataCalculations[3].type = sensorData.modbus.calculationSetup[3].type;
        }

        private void tbCalculationParameter4_LostFocus(object sender, RoutedEventArgs e)
        {
            tbCalculationParameter4_Update(sender);
        }

        private void tbCalculationParameter4_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                tbCalculationParameter4_Update(sender);
                Keyboard.ClearFocus();
            }
        }

        private void tbCalculationParameter4_Update(object sender)
        {
            // Lagre ny setting
            if (double.TryParse((sender as TextBox).Text, Constants.numberStyle, Constants.cultureInfo, out double param))
                sensorData.modbus.calculationSetup[3].parameter = param;
            else
                sensorData.modbus.calculationSetup[3].parameter = 0;

            sensorData.dataCalculations[3].parameter = sensorData.modbus.calculationSetup[3].parameter;

            // Skrive data (som kan være korrigert) tilbake til tekstfeltet
            (sender as TextBox).Text = sensorData.modbus.calculationSetup[3].parameter.ToString();
        }

        private void tbModbusSlaveID_LostFocus(object sender, RoutedEventArgs e)
        {
            tbModbusSlaveID_Update(sender);
        }

        private void tbModbusSlaveID_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                tbModbusSlaveID_Update(sender);
                Keyboard.ClearFocus();
            }
        }

        private void tbModbusSlaveID_Update(object sender)
        {
            // Sjekk av input
            DataValidation.Double(
                (sender as TextBox).Text,
                Constants.MODBUSSlaveIDMin,
                Constants.MODBUSSlaveIDMax,
                Constants.MODBUSSlaveIDDefault,
                out double validatedInput);

            sensorData.modbus.slaveID = (byte)validatedInput;
            (sender as TextBox).Text = validatedInput.ToString();
        }

        private void tbSampleDataRangeAddress_LostFocus(object sender, RoutedEventArgs e)
        {
            tbSampleDataStartAddress_Update(sender);
        }

        private void tbSampleDataStartAddress_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                tbSampleDataStartAddress_Update(sender);
                Keyboard.ClearFocus();
            }
        }

        private void tbSampleDataStartAddress_Update(object sender)
        {
            try
            {
                // Lagre ny setting
                if (!string.IsNullOrEmpty((sender as TextBox).Text))
                {
                    int startAddress = 0;
                    if (int.TryParse((sender as TextBox).Text, out startAddress))
                    {
                        if (modbusHelper.ValidAddressSpace(startAddress))
                        {
                            sensorData.modbus.startAddress = (ushort)startAddress;
                            CheckDataAddress();
                        }
                        else
                        {
                            RadWindow.Alert(
                                string.Format("Input Error\n\nValid address space:\n{0} - {1}\n{2} - {3}\n{4} - {5}\n{6} - {7} (a)",
                                    Constants.ModbusCoilMin,
                                    Constants.ModbusCoilMax,
                                    Constants.ModbusDiscreteInputMin,
                                    Constants.ModbusDiscreteInputMax,
                                    Constants.ModbusInputRegisterMin,
                                    Constants.ModbusInputRegisterMax,
                                    Constants.ModbusHoldingRegisterMin,
                                    Constants.ModbusHoldingRegisterMax));

                            sensorData.modbus.startAddress = Constants.ModbusDefaultAddress;
                        }
                    }
                }
                else
                {
                    sensorData.modbus.startAddress = Constants.ModbusDefaultAddress;
                }

                (sender as TextBox).Text = sensorData.modbus.startAddress.ToString();
            }
            catch (Exception ex)
            {
                RadWindow.Alert(string.Format("Input Error\n\n{0}", TextHelper.Wrap(ex.Message)));
            }
        }

        private void tbDataRangeTotalAddresses_LostFocus(object sender, RoutedEventArgs e)
        {
            tbSampleDataTotalAddresses_Update(sender);
        }

        private void tbSampleDataTotalAddresses_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                tbSampleDataTotalAddresses_Update(sender);
                Keyboard.ClearFocus();
            }
        }

        private void tbSampleDataTotalAddresses_Update(object sender)
        {
            try
            {
                // Lagre ny setting
                if (!string.IsNullOrEmpty((sender as TextBox).Text))
                {
                    int totalAddresses = 0;
                    if (int.TryParse((sender as TextBox).Text, out totalAddresses))
                    {
                        if (totalAddresses >= 0 && totalAddresses <= Constants.ModbusRegistersMax)
                        {
                            if (modbusHelper.ValidAddressSpace(sensorData.modbus.startAddress + totalAddresses - 1))
                            {
                                sensorData.modbus.totalAddresses = totalAddresses;
                                CheckDataAddress();
                            }
                            else
                            {
                                RadWindow.Alert(
                                    string.Format("Input Error\n\nValid address space (Start + Total):\n{0} - {1}\n{2} - {3}\n{4} - {5}\n{6} - {7} (b)",
                                        Constants.ModbusCoilMin,
                                        Constants.ModbusCoilMax,
                                        Constants.ModbusDiscreteInputMin,
                                        Constants.ModbusDiscreteInputMax,
                                        Constants.ModbusInputRegisterMin,
                                        Constants.ModbusInputRegisterMax,
                                        Constants.ModbusHoldingRegisterMin,
                                        Constants.ModbusHoldingRegisterMax));

                                sensorData.modbus.totalAddresses = 0;
                            }
                        }
                        else
                        {
                            RadWindow.Alert(string.Format("Valid address range: 0 - {0} (b)", Constants.ModbusRegistersMax));
                            sensorData.modbus.totalAddresses = 0;
                        }
                    }
                }
                else
                {
                    sensorData.modbus.startAddress = 0;
                }

                (sender as TextBox).Text = sensorData.modbus.totalAddresses.ToString();
            }
            catch (Exception ex)
            {
                RadWindow.Alert(string.Format("Input Error\n\n{0}", TextHelper.Wrap(ex.Message)));
            }
        }

        private void tbSelectDataAddress_LostFocus(object sender, RoutedEventArgs e)
        {
            tbSelectDataAddress_Update(sender);
        }

        private void tbSelectDataAddress_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                tbSelectDataAddress_Update(sender);
                Keyboard.ClearFocus();
            }
        }

        private void tbSelectDataAddress_Update(object sender)
        {
            try
            {
                // Lagre ny setting
                if (!string.IsNullOrEmpty((sender as TextBox).Text))
                {
                    int dataAddress = 0;
                    if (int.TryParse((sender as TextBox).Text, out dataAddress))
                    {
                        if (dataAddress >= sensorData.modbus.startAddress && dataAddress <= sensorData.modbus.startAddress + sensorData.modbus.totalAddresses - 1)
                        {
                            sensorData.modbus.dataAddress = (ushort)dataAddress;
                        }
                        else
                        {
                            RadWindow.Alert(
                                string.Format("Input Error\n\nData Address must be within the\nsample data address space:\n\n{0} - {1}",
                                    sensorData.modbus.startAddress,
                                    sensorData.modbus.startAddress + sensorData.modbus.totalAddresses - 1));

                            sensorData.modbus.dataAddress = sensorData.modbus.startAddress;
                        }
                    }
                }
                else
                {
                    sensorData.modbus.dataAddress = sensorData.modbus.startAddress;
                }

                (sender as TextBox).Text = sensorData.modbus.dataAddress.ToString();
            }
            catch (Exception ex)
            {
                RadWindow.Alert(string.Format("Input Error\n\n{0}", TextHelper.Wrap(ex.Message)));
            }
        }

        private void CheckDataAddress()
        {
            if (sensorData.modbus.dataAddress < sensorData.modbus.startAddress ||
                sensorData.modbus.dataAddress > sensorData.modbus.startAddress + sensorData.modbus.totalAddresses - 1)
            {
                sensorData.modbus.dataAddress = sensorData.modbus.startAddress;
                tbSelectDataAddress.Text = sensorData.modbus.dataAddress.ToString();
            }
        }

        private void tbModbusTCPAddress_LostFocus(object sender, RoutedEventArgs e)
        {
            tbModbusTCPAddress_Update(sender);
        }

        private void tbModbusTCPAddress_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                tbModbusTCPAddress_Update(sender);
                Keyboard.ClearFocus();
            }
        }

        private void tbModbusTCPAddress_Update(object sender)
        {
            // Sjekk av input
            DataValidation.IPAddress(
                (sender as TextBox).Text,
                Constants.DefaultDatabaseAddress,
                out string validatedInput);

            sensorData.modbus.tcpAddress = validatedInput;
            (sender as TextBox).Text = validatedInput;
        }

        private void tbModbusTCPPort_LostFocus(object sender, RoutedEventArgs e)
        {
            tbModbusTCPPort_Update(sender);
        }

        private void tbModbusTCPPort_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                tbModbusTCPPort_Update(sender);
                Keyboard.ClearFocus();
            }
        }

        private void tbModbusTCPPort_Update(object sender)
        {
            // Sjekk av input
            DataValidation.Double(
                (sender as TextBox).Text,
                Constants.PortMin,
                Constants.PortMax,
                Constants.MODBUSPortDefault,
                out double validatedInput);

            sensorData.modbus.tcpPort = (int)validatedInput;
            (sender as TextBox).Text = validatedInput.ToString();
        }

        private void tbDataLines_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                tbDataLines_Update(sender);
                Keyboard.ClearFocus();
            }
        }

        private void tbDataLines_LostFocus(object sender, RoutedEventArgs e)
        {
            tbDataLines_Update(sender);
        }

        private void tbDataLines_Update(object sender)
        {
            modbusSetupWindowVM.totalDataLinesString = (sender as TextBox).Text;
        }
    }
}