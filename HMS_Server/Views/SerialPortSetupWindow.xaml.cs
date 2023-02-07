using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
    /// Interaction logic for SerialPortSetupWindow.xaml
    /// </summary>
    public partial class SerialPortSetupWindow : RadWindow
    {
        // Configuration settings
        private Config config;

        // Error Handler
        private ErrorHandler errorHandler;

        // Admin Settings
        private AdminSettingsVM adminSettingsVM;

        // Sensor Data List
        private RadObservableCollection<SensorData> sensorDataList = new RadObservableCollection<SensorData>();

        // Valgt sensor data
        private SensorData sensorData;

        // Serie port objekt
        private SerialPort serialPort = new SerialPort();

        // Første lesing dumpes - gjøres fordi den kan være mye buffrede data som venter, og disse kan overbelaste grensesnittet.
        private bool firstRead = true;

        // Time stamp for sist leste data
        private DateTime dataTimeStamp;

        // Data Processing
        private SerialPortProcessing process = new SerialPortProcessing();
        private List<DataCalculations> dataCalculations = new List<DataCalculations>();

        // Timer som begrenser hvor ofte vi viser data på skjerm.
        // NB! Begrenser ikke lesing fra selve porten, kun hvor ofte innkommende data blir prosessert og vist på skjerm.
        // Dette for at ikke UI skal bli overbelastet med for mye oppdatering dersom det kommer inn mye data med høy frekvens.
        private Stopwatch dataLimitTimer = new Stopwatch();

        // Noen globale variabler som brukes i og oppdateres av forskjellige tråder
        private bool showControlChars;

        // Lister for visning
        private RadObservableCollection<Packet> rawDataItems = new RadObservableCollection<Packet>();
        private RadObservableCollection<Packet> selectedPacketsItems = new RadObservableCollection<Packet>();
        private RadObservableCollection<PacketDataFields> packetDataFieldsItems = new RadObservableCollection<PacketDataFields>();
        private RadObservableCollection<SelectedDataField> selectedDataFieldItems = new RadObservableCollection<SelectedDataField>();
        private RadObservableCollection<CalculatedData> calculatedDataItems = new RadObservableCollection<CalculatedData>();

        // View Model
        private SerialPortSetupWindowVM serialPortSetupWindowVM;

        // Input data
        private string inputDataBuffer = string.Empty;

        public SerialPortSetupWindow(SensorData sensorDataItem, RadObservableCollection<SensorData> sensorDataList, Config config, ErrorHandler errorHandler, AdminSettingsVM adminSettingsVM)
        {
            InitializeComponent();

            serialPortSetupWindowVM = new SerialPortSetupWindowVM(config);
            DataContext = serialPortSetupWindowVM;

            // Lagre valgt sensor data
            sensorData = sensorDataItem;

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

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            // Lukke serie porten dersom den er åpen
            if (serialPort != null)
                if (serialPort.IsOpen)
                    serialPort.Close();
        }

        private void InitializeApplicationSettings()
        {
            dataLimitTimer.Start();

            InitBasicInformation();

            InitSerialPortConfiguration(serialPort);

            InitSensorInput();

            InitDataDisplayOptions();

            InitPacketSelection();

            InitPacketDataSelection();

            InitDataSelection();

            InitDataCalculations();

            // Lagrer config i tilfelle dette er første kjøring og config fil ikke eksisterer
            SaveAllWindowSettings();

            // Port status sjekk
            PortStatusCheck();
        }

        private void PortStatusCheck()
        {
            // Sjekker om det kommer data på porten
            // Restarte porten dersom det ikke kommer data

            // Lese data timeout fra config
            double dataTimeout = config.ReadWithDefault(ConfigKey.DataTimeout, Constants.DataTimeoutDefault);

            DispatcherTimer statusTimer = new DispatcherTimer();
            statusTimer.Interval = TimeSpan.FromMilliseconds(config.ReadWithDefault(ConfigKey.ServerUIUpdateFrequency, Constants.ServerUIUpdateFrequencyDefault));
            statusTimer.Tick += runPortStatusCheck;
            statusTimer.Start();

            void runPortStatusCheck(object sender, EventArgs e)
            {
                try
                {
                    if (serialPort != null)
                    {
                        if (serialPort.IsOpen)
                        {
                            if (dataTimeStamp.AddMilliseconds(dataTimeout) < DateTime.UtcNow)
                            {
                                serialPort.Close();
                                serialPort.Open();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Sette feilmelding
                    errorHandler.Insert(
                        new ErrorMessage(
                            DateTime.UtcNow,
                            ErrorMessageType.SerialPort,
                            ErrorMessageCategory.AdminUser,
                            string.Format("Port Restart Failed ({0}) (runPortStatusCheck): System Message: {1}", serialPort.PortName, ex.Message)));
                }
            }
        }

        private void InitBasicInformation()
        {
            // Basic Information
            lbSensorID.Content = sensorData.id.ToString();
            lbSensorName.Content = sensorData.name;
            lbSensorType.Content = sensorData.type.ToString();
        }

        private void InitSerialPortConfiguration(SerialPort serialPort)
        {
            // Binding for listview
            lvRawData.ItemsSource = rawDataItems;

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
            cboBaudRate.Text = sensorData.serialPort.baudRate.ToString();

            // Data Bits
            ///////////////////////////////////////////////////////////
            cboDataBits.Items.Add(7);
            cboDataBits.Items.Add(8);

            // Skrive satt data bit type i combobox feltet
            cboDataBits.Text = sensorData.serialPort.dataBits.ToString();

            // Stop Bits
            ///////////////////////////////////////////////////////////
            cboStopBits.Items.Add("One");
            cboStopBits.Items.Add("OnePointFive");
            cboStopBits.Items.Add("Two");

            // Skrive satt stop bit i combobox feltet
            cboStopBits.Text = sensorData.serialPort.stopBits.ToString();

            // Parity 
            ///////////////////////////////////////////////////////////
            cboParity.Items.Add("None");
            cboParity.Items.Add("Even");
            cboParity.Items.Add("Mark");
            cboParity.Items.Add("Odd");
            cboParity.Items.Add("Space");

            // Skrive satt parity i combobox feltet
            cboParity.Text = sensorData.serialPort.parity.ToString();

            // Handshake
            ///////////////////////////////////////////////////////////
            cboHandShake.Items.Add("None");
            cboHandShake.Items.Add("XOnXOff");
            cboHandShake.Items.Add("RequestToSend");
            cboHandShake.Items.Add("RequestToSendXOnXOff");

            // Skrive satt hand shake type i combobox feltet
            cboHandShake.Text = sensorData.serialPort.handshake.ToString();

            // Status Felt
            ///////////////////////////////////////////////////////////
            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(200);
            timer.Tick += timer_Tick;
            timer.Start();

            void timer_Tick(object sender, EventArgs e)
            {
                if (serialPort != null && serialPort.IsOpen)
                {
                    bSerialPortStatus.Background = (Brush)this.FindResource("ColorGreen");
                }
                else
                {
                    bSerialPortStatus.Background = (Brush)this.FindResource("ColorRed");
                }
            }
        }

        private void InitSensorInput()
        {
            // Data Type
            foreach (InputDataType value in Enum.GetValues(typeof(InputDataType)))
                cboSensorDataType.Items.Add(value.ToString());
            cboSensorDataType.Text = sensorData.serialPort.inputType.ToString();
            process.inputType = sensorData.serialPort.inputType;

            // Data Bytes in value
            foreach (BinaryType value in Enum.GetValues(typeof(BinaryType)))
                cboSelectedDataBytesInValue.Items.Add(value.GetDescription());
            cboSelectedDataBytesInValue.Text = sensorData.serialPort.binaryType.GetDescription();
            process.binaryType = sensorData.serialPort.binaryType;
        }

        private void InitDataDisplayOptions()
        {
            // Lese kontroll chars option
            if (config.Read(ConfigKey.ShowControlChars, ConfigSection.SerialPortConfig) == "1")
            {
                chkShowControlChars.IsChecked = true;
                showControlChars = true;
            }
            else
            {
                chkShowControlChars.IsChecked = false;
                showControlChars = false;
            }

            // Data Lines
            serialPortSetupWindowVM.totalDataLinesString = config.ReadWithDefault(ConfigKey.TotalDataLines, ConfigSection.SerialPortConfig, Constants.GUIDataLinesDefault).ToString();
        }

        private void InitPacketSelection()
        {
            // Binding for listviews
            lvSelectedPackets.ItemsSource = selectedPacketsItems;
            lvSelectedPackets2.ItemsSource = selectedPacketsItems;

            // Packet Header
            tbSelectedPacketHeader.Text = TextHelper.UnescapeSpace(sensorData.serialPort.packetHeader);

            try
            {
                process.packetHeader = Regex.Unescape(tbSelectedPacketHeader.Text); // Unescape gjør det mulig å skrive escape characters i input feltet
            }
            catch (Exception)
            {
                process.packetHeader = string.Empty;
            }

            // Packet End
            tbSelectedPacketEnd.Text = TextHelper.UnescapeSpace(sensorData.serialPort.packetEnd);

            try
            {
                process.packetEnd = Regex.Unescape(tbSelectedPacketEnd.Text); // Unescape gjør det mulig å skrive escape characters i input feltet
            }
            catch (Exception)
            {
                process.packetEnd = string.Empty;
            }
        }

        private void InitPacketDataSelection()
        {
            // Binding for listviews
            lvPacketData.ItemsSource = packetDataFieldsItems;

            // Delimiter
            tbPacketDataDelimiter.Text = TextHelper.UnescapeSpace(sensorData.serialPort.packetDelimiter);
            process.packetDelimiter = tbPacketDataDelimiter.Text;
            SetDataFieldSplitDisplay();

            // Combine Fields
            foreach (var value in Enum.GetValues(typeof(SerialPortSetup.CombineFields)))
                cboCombineFields.Items.Add(value.ToString());

            cboCombineFields.Text = sensorData.serialPort.packetCombineFields.ToString();
            process.packetCombineFields = sensorData.serialPort.packetCombineFields;

            // Fixed Position Data
            if (sensorData.serialPort.fixedPosData)
            {
                cboFixedPositionData.IsChecked = true;
                process.fixedPosData = true;
            }
            else
            {
                cboFixedPositionData.IsChecked = false;
                process.fixedPosData = false;
            }

            // Enable/Disable fixed position input
            SetDataSelectionDisplay();

            tbFixedPositionDataStart.Text = sensorData.serialPort.fixedPosStart.ToString();
            tbFixedPositionDataTotal.Text = sensorData.serialPort.fixedPosTotal.ToString();
            process.fixedPosStart = sensorData.serialPort.fixedPosStart;
            process.fixedPosTotal = sensorData.serialPort.fixedPosTotal;
        }

        private void InitDataSelection()
        {
            // Binding for listviews
            lvSelectedData.ItemsSource = selectedDataFieldItems;
            lvSelectedData2.ItemsSource = selectedDataFieldItems;

            // Data Field 
            for (int i = 0; i < Constants.PacketDataFields; i++)
                cboSelectedDataField.Items.Add(i.ToString());

            cboSelectedDataField.Text = TextHelper.UnescapeSpace(sensorData.serialPort.dataField);

            if (cboSelectedDataField.Text != string.Empty)
                cboSelectedDataField.SelectedIndex = int.Parse(cboSelectedDataField.Text);
            else
                cboSelectedDataField.SelectedIndex = 0;

            // Decimal Separator
            foreach (var value in Enum.GetValues(typeof(DecimalSeparator)))
                cboDecimalSeparator.Items.Add(value.ToString());

            cboDecimalSeparator.Text = sensorData.serialPort.decimalSeparator.ToString();
            process.decimalSeparator = sensorData.serialPort.decimalSeparator;

            // Lese selected data auto extraction setting
            cboSelectedDataAutoExtract.IsChecked = sensorData.serialPort.autoExtractValue;
            process.autoExtractValue = sensorData.serialPort.autoExtractValue;
        }

        private void InitDataCalculations()
        {
            try
            {
                for (int i = 0; i < Constants.DataCalculationSteps; i++)
                {
                    dataCalculations.Add(new DataCalculations());
                    //dataCalculations.Add(new DataCalculations()
                    //{
                    //    type = sensorData.fileReader.calculationSetup[i].type,
                    //    parameter = sensorData.serialPort.calculationSetup[i].parameter,
                    //});
                }

                // Binding for listviews
                lvCalculatedData.ItemsSource = calculatedDataItems;

                // Data Calculations
                // Prosesseringstyper
                foreach (CalculationType value in Enum.GetValues(typeof(CalculationType)))
                    cboCalculationType1.Items.Add(value.GetDescription());

                cboCalculationType1.Text = sensorData.serialPort.calculationSetup[0].type.GetDescription();
                cboCalculationType1.SelectedIndex = (int)sensorData.serialPort.calculationSetup[0].type;

                // Parameter
                tbCalculationParameter1.Text = sensorData.serialPort.calculationSetup[0].parameter.ToString();

                // Data Calculations 2
                // Prosesseringstyper
                foreach (CalculationType value in Enum.GetValues(typeof(CalculationType)))
                    cboCalculationType2.Items.Add(value.GetDescription());

                cboCalculationType2.Text = sensorData.serialPort.calculationSetup[1].type.GetDescription();
                cboCalculationType2.SelectedIndex = (int)sensorData.serialPort.calculationSetup[1].type;

                // Parameter
                tbCalculationParameter2.Text = sensorData.serialPort.calculationSetup[1].parameter.ToString();

                // Data Calculations 3
                // Prosesseringstyper
                foreach (CalculationType value in Enum.GetValues(typeof(CalculationType)))
                    cboCalculationType3.Items.Add(value.GetDescription());

                cboCalculationType3.Text = sensorData.serialPort.calculationSetup[2].type.GetDescription();
                cboCalculationType3.SelectedIndex = (int)sensorData.serialPort.calculationSetup[2].type;

                // Parameter
                tbCalculationParameter3.Text = sensorData.serialPort.calculationSetup[2].parameter.ToString();

                // Data Calculations 4
                // Prosesseringstyper
                foreach (CalculationType value in Enum.GetValues(typeof(CalculationType)))
                    cboCalculationType4.Items.Add(value.GetDescription());

                cboCalculationType4.Text = sensorData.serialPort.calculationSetup[3].type.GetDescription();
                cboCalculationType4.SelectedIndex = (int)sensorData.serialPort.calculationSetup[3].type;

                // Parameter
                tbCalculationParameter4.Text = sensorData.serialPort.calculationSetup[3].parameter.ToString();


                // Overføre calculation parametre
                for (int i = 0; i < Constants.DataCalculationSteps; i++)
                {
                    dataCalculations[i].type = sensorData.serialPort.calculationSetup[i].type;
                    dataCalculations[i].parameter = sensorData.serialPort.calculationSetup[i].parameter;
                }
            }
            catch (Exception ex)
            {
                RadWindow.Alert(string.Format("InitializeDataProcessing\n\n{0}", TextHelper.Wrap(TextHelper.Wrap(ex.Message))));
            }
        }

        private void SaveAllWindowSettings()
        {
            if (chkShowControlChars.IsChecked == true)
                config.Write(ConfigKey.ShowControlChars, "1", ConfigSection.SerialPortConfig);
            else
                config.Write(ConfigKey.ShowControlChars, "0", ConfigSection.SerialPortConfig);
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
                    // Sjekke om porten er brukt til MODBUS kommunikasjon -> Da skal den ikke brukes til serie port
                    if (sensorDataList.Where(x => (x.type == SensorType.ModbusRTU ||
                                                  x.type == SensorType.ModbusASCII ||
                                                  x.type == SensorType.ModbusTCP) &&
                                                        x.modbus.portName == comPortName).Count() == 0)
                    {
                        cboPortName.Items.Add(comPortName);
                    }
                }

                // Skrive satt COM port i combobox tekst feltet
                cboPortName.Text = sensorData.serialPort.portName;
            }
            else
            {
                // Fant ingen porter
                cboPortName.Text = "No COM ports found";
            }
        }

        private void OpenSerialPort(SerialPort serialPort)
        {
            try
            {
                // Sette serie port settings på SerialPort object 
                serialPort.PortName = sensorData.serialPort.portName;
                serialPort.BaudRate = sensorData.serialPort.baudRate;
                serialPort.DataBits = sensorData.serialPort.dataBits;
                serialPort.StopBits = sensorData.serialPort.stopBits;
                serialPort.Parity = sensorData.serialPort.parity;
                serialPort.Handshake = sensorData.serialPort.handshake;

                // Koble opp metode for å motta data
                serialPort.DataReceived += new SerialDataReceivedEventHandler(SerialPort_DataReceived);

                // First Read
                firstRead = true;

                // Åpne port
                serialPort.Open();

                // Status
                sensorMonitorStatus.Text = "Serial port open for reading...";
            }
            catch (Exception ex)
            {
                RadWindow.Alert(string.Format("Error opening serial port\n\n{0}", TextHelper.Wrap(ex.Message)));
            }
        }

        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            // Trinn 1: Lese fra port
            //////////////////////////////////////////////////////////////////////////////
            //inputDataBuffer += serialPort.ReadExisting();

            int bytes = serialPort.BytesToRead;
            byte[] array = new byte[bytes];
            for (int i = 0; serialPort.BytesToRead > 0 && i < bytes; i++)
            {
                array[i] = Convert.ToByte(serialPort.ReadByte());
            }

            switch (sensorData.serialPort.inputType)
            {
                case InputDataType.Text:
                    inputDataBuffer += Encoding.UTF8.GetString(array, 0, array.Length);
                    break;

                case InputDataType.Binary:
                    inputDataBuffer += BitConverter.ToString(array);
                    break;

                default:
                    break;
            }

            //switch (sensorData.serialPort.inputType)
            //{
            //    case InputDataType.Text:
            //        {
            //            inputData += serialPort.ReadExisting();

            //            //// Alternativ metode
            //            //int bytes = serialPort.BytesToRead;
            //            //byte[] array = new byte[bytes];
            //            //int i = 0;
            //            //while (serialPort.BytesToRead > 0)
            //            //{
            //            //    array[i++] = Convert.ToByte(serialPort.ReadByte());
            //            //}
            //            //inputData = Encoding.UTF8.GetString(array, 0, array.Length);
            //        }
            //        break;

            //    case InputDataType.Binary:
            //        {
            //            try
            //            {
            //                // Av en eller annen grunn leverer ikke denne koden samme resulat som koden under
            //                //inputData = serialPort.ReadExisting();
            //                //byte[] byteArray = Encoding.Default.GetBytes(inputData);
            //                //inputData = BitConverter.ToString(byteArray);

            //                //int bytes = serialPort.BytesToRead;
            //                //byte[] array = new byte[bytes];
            //                //for (int i = 0; serialPort.BytesToRead > 0 && i < bytes; i++)
            //                //{
            //                //    array[i] = Convert.ToByte(serialPort.ReadByte());
            //                //}
            //                //inputData = BitConverter.ToString(array);

            //                inputData += serialPort.ReadExisting();
            //            }
            //            catch (Exception ex)
            //            {
            //                // Sette feilmelding
            //                errorHandler?.Insert(
            //                    new ErrorMessage(
            //                        DateTime.UtcNow,
            //                        ErrorMessageType.SerialPort,
            //                        ErrorMessageCategory.AdminUser,
            //                        string.Format("SerialPort_DataReceived (Binary): Binary conversion failed. {0}", ex.Message)));

            //                inputData = string.Empty;
            //            }
            //        }
            //        break;
            //}

            // Sette time stamp
            dataTimeStamp = DateTime.UtcNow;

            // Første lesing etter at porten er åpnet vil bli dumpet.
            // Det kan ligge mye buffrede data som dumper inn ved første lesing. Droppes slik at ikke grensesnittet overbelastes.
            if (!firstRead)
            {
                // Timer som sørger for at vi ikke sender for mye data til UI thread
                // Vi trenger ikke lese og behandle hver eneste sending på porten her
                if (dataLimitTimer.ElapsedMilliseconds > config.ReadWithDefault(ConfigKey.SetupGUIDataLimit, Constants.GUIDataLimitDefault))
                {
                    // Sende leste data til skjermutskrift
                    if (!string.IsNullOrEmpty(inputDataBuffer))
                    {
                        // Hente data string fra inputBuffer
                        string inputData = inputDataBuffer;

                        // Vise raw data
                        DisplayRawData(inputData);

                        // Trinn 2: Prosessere raw data, finne pakker
                        //////////////////////////////////////////////////////////////////////////////
                        List<string> incomingPackets = process.FindSelectedPackets(inputData);

                        // Sjekk om vi skal ta vare på noe data eller om buffer skal slettes
                        int lastHeaderPos = inputDataBuffer.LastIndexOf(process.packetHeader);
                        int lastEndPos = inputDataBuffer.LastIndexOf(process.packetEnd);

                        // Dersom vi har en HEADER etter en END
                        if (lastHeaderPos > lastEndPos &&
                            lastHeaderPos != -1 &&
                            lastEndPos != -1 &&
                            !string.IsNullOrEmpty(process.packetHeader) &&
                            !string.IsNullOrEmpty(process.packetEnd))
                            // Lagre fra header (i tilfelle resten av packet kommer i neste sending fra serieport)
                            inputDataBuffer = inputDataBuffer.Substring(lastHeaderPos);
                        else
                            // Ingen HEADER etter END -> Slette buffer
                            inputDataBuffer = String.Empty;

                        // Må også begrense hvor my data som skal leses i input buffer når vi ikke finner packets
                        // slik at den ikke fylles i det uendelige.
                        if (inputDataBuffer.Count() > 4096) // 4KB limit per packet
                            inputDataBuffer = String.Empty;

                        // Prosessere pakkene som ble funnet
                        foreach (var packet in incomingPackets)
                        {
                            // Vise de utvalgte pakkene på skjerm
                            DisplaySelectedPackets(packet);

                            // Trinn 3: Finne datafeltene i pakke
                            //////////////////////////////////////////////////////////////////////////////
                            PacketDataFields packetDataFields = process.FindPacketDataFields(packet);

                            // Vise data
                            DisplayPacketDataFields(packetDataFields);

                            // Trinn 4: Finne valgt datafelt
                            //////////////////////////////////////////////////////////////////////////////
                            SelectedDataField selectedData = process.FindSelectedDataInPacket(packetDataFields);

                            // Vise utvalgt datafelt på skjerm
                            DisplaySelectedData(selectedData);

                            // Trinn 5: Utføre kalkulasjoner på utvalgt datafelt
                            //////////////////////////////////////////////////////////////////////////////
                            CalculatedData calculatedData = process.ApplyCalculationsToSelectedData(selectedData, dataCalculations, dataTimeStamp, errorHandler, ErrorMessageCategory.Admin, adminSettingsVM);

                            // Vise de prosesserte dataene
                            DisplayProcessedData(calculatedData);
                        }
                    }

                    dataLimitTimer.Restart();
                }
            }
            else
            {
                firstRead = false;
            }
        }

        private void DisplayRawData(string dataString)
        {
            string dataStringMod;

            // Escape data string?
            if (showControlChars == true)
                dataStringMod = TextHelper.EscapeControlChars(dataString);
            // ...eller bare rett ut
            else
                // Trimmer whitespace og linefeed
                dataStringMod = dataString.Trim();

            Application.Current.Dispatcher.BeginInvoke(
            DispatcherPriority.Normal,
            new Action(() =>
            {
                // Legg ut data i raw data listview
                rawDataItems.Add(new Packet() { data = dataStringMod });

                // Begrense data på skjerm
                while (rawDataItems.Count() > serialPortSetupWindowVM.totalDataLines)
                    rawDataItems.RemoveAt(0);

                // Hvorfor ikke bare automatisk scrolle data til siste i listen?
                // Fordi scroll funksjonen til telerik (under) er så inn i helsike treig. UI blir ikke-responsiv.
                //lvRawData.ScrollIntoViewAsync(lvRawData.Items[lvRawData.Items.Count - 1], lvRawData.Columns[0], null);

                // Status
                sensorMonitorStatus.Text = "Receiving data from serial port...";
            }));
        }

        private void DisplaySelectedPackets(string packetString)
        {
            string dataStringMod;

            // Escape data string?
            if (showControlChars == true)
                dataStringMod = TextHelper.EscapeControlChars(packetString);
            // ...eller bare rett ut
            else
                // Trimmer whitespace og linefeed
                dataStringMod = packetString.Trim();

            Application.Current.Dispatcher.BeginInvoke(
            DispatcherPriority.Normal,
            new Action(() =>
            {
                // Legg ut data i selected packets listview
                selectedPacketsItems.Add(new Packet() { data = dataStringMod });

                // Begrense data på skjerm
                while (selectedPacketsItems.Count() > serialPortSetupWindowVM.totalDataLines)
                    selectedPacketsItems.RemoveAt(0);
            }));
        }

        private void DisplayPacketDataFields(PacketDataFields packetDataFields)
        {
            PacketDataFields packetDataFieldsMod = new PacketDataFields();

            // Escape data string?
            if (showControlChars == true)
            {
                for (int i = 0; i < packetDataFields.dataField.Length && i < packetDataFieldsMod.dataField.Length; i++)
                    if (!string.IsNullOrEmpty(packetDataFields.dataField[i]))
                        packetDataFieldsMod.dataField[i] = TextHelper.EscapeControlChars(packetDataFields.dataField[i]);
            }
            // ...eller bare rett ut
            else
            {
                // Trimmer whitespace og linefeed
                for (int i = 0; i < packetDataFields.dataField.Length && i < packetDataFieldsMod.dataField.Length; i++)
                    if (!string.IsNullOrEmpty(packetDataFields.dataField[i]))
                        packetDataFieldsMod.dataField[i] = packetDataFields.dataField[i].Trim();
            }

            Application.Current.Dispatcher.BeginInvoke(
            DispatcherPriority.Normal,
            new Action(() =>
            {
                // Legg ut data i packet data listview
                packetDataFieldsItems.Add(packetDataFieldsMod);

                // Begrense data på skjerm
                while (packetDataFieldsItems.Count() > serialPortSetupWindowVM.totalDataLines)
                    packetDataFieldsItems.RemoveAt(0);
            }));
        }

        private void DisplaySelectedData(SelectedDataField selectedData)
        {
            Application.Current.Dispatcher.BeginInvoke(
            DispatcherPriority.Normal,
            new Action(() =>
            {
                // Legg ut data i packet data listview
                selectedDataFieldItems.Add(selectedData);

                // Begrense data på skjerm
                while (selectedDataFieldItems.Count() > serialPortSetupWindowVM.totalDataLines)
                    selectedDataFieldItems.RemoveAt(0);
            }));
        }

        private void DisplayProcessedData(CalculatedData calculatedData)
        {
            Application.Current.Dispatcher.BeginInvoke(
            DispatcherPriority.Normal,
            new Action(() =>
            {
                // Legg ut data i packet data listview
                calculatedDataItems.Add(calculatedData);

                // Begrense data på skjerm
                while (calculatedDataItems.Count() > serialPortSetupWindowVM.totalDataLines)
                    calculatedDataItems.RemoveAt(0);
            }));
        }

        private void SetDataFieldSplitDisplay()
        {
            if (sensorData.serialPort.inputType == InputDataType.Binary)
            {
                lbPacketDataDelimiter.IsEnabled = false;
                tbPacketDataDelimiter.IsEnabled = false;
                lbCombineFields.IsEnabled = false;
                cboCombineFields.IsEnabled = false;
                cboFixedPositionData.IsEnabled = false;
            }
            else
            {
                lbPacketDataDelimiter.IsEnabled = true;
                tbPacketDataDelimiter.IsEnabled = true;
                lbCombineFields.IsEnabled = true;
                cboCombineFields.IsEnabled = true;
                cboFixedPositionData.IsEnabled = true;
            }
        }

        private void SetDataSelectionDisplay()
        {
            if (sensorData.serialPort.inputType == InputDataType.Binary)
            {
                cboFixedPositionData.IsEnabled = false;
                lblFixedPositionDataStart.IsEnabled = false;
                tbFixedPositionDataStart.IsEnabled = false;
                lblFixedPositionDataTotal.IsEnabled = false;
                tbFixedPositionDataTotal.IsEnabled = false;

                lbDecimalSeparator.IsEnabled = false;
                cboDecimalSeparator.IsEnabled = false;
                cboSelectedDataAutoExtract.IsEnabled = false;

                lbSelectedDataBytesInValue.IsEnabled = true;
                cboSelectedDataBytesInValue.IsEnabled = true;
            }
            else
            {
                cboFixedPositionData.IsEnabled = true;
                lblFixedPositionDataStart.IsEnabled = process.fixedPosData;
                tbFixedPositionDataStart.IsEnabled = process.fixedPosData;
                lblFixedPositionDataTotal.IsEnabled = process.fixedPosData;
                tbFixedPositionDataTotal.IsEnabled = process.fixedPosData;

                lbDecimalSeparator.IsEnabled = true;
                cboDecimalSeparator.IsEnabled = true;
                cboSelectedDataAutoExtract.IsEnabled = true;

                lbSelectedDataBytesInValue.IsEnabled = false;
                cboSelectedDataBytesInValue.IsEnabled = false;
            }
        }

        private void cboCOMPort_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Lagre ny setting
            sensorData.serialPort.portName = cboPortName.SelectedItem.ToString();
        }

        private void cboBaudRate_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                // Lagre ny setting
                sensorData.serialPort.baudRate = Convert.ToInt32(cboBaudRate.SelectedItem.ToString());
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
                sensorData.serialPort.dataBits = Convert.ToInt16(cboDataBits.SelectedItem.ToString());
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
                sensorData.serialPort.stopBits = (StopBits)Enum.Parse(typeof(StopBits), cboStopBits.SelectedItem.ToString());
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
                sensorData.serialPort.parity = (Parity)Enum.Parse(typeof(Parity), cboParity.SelectedItem.ToString());
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
                sensorData.serialPort.handshake = (Handshake)Enum.Parse(typeof(Handshake), cboHandShake.SelectedItem.ToString());
            }
            catch (Exception ex)
            {
                RadWindow.Alert(string.Format("cboHandShake_SelectionChanged\n\n{0}", TextHelper.Wrap(ex.Message)));
            }
        }

        private void btnSerialPortReaderOpen_Click(object sender, RoutedEventArgs e)
        {
            // Åpne serieporten
            OpenSerialPort(serialPort);

            // Stenge tilgang til å konfigurere porten
            if (serialPort.IsOpen)
            {
                cboPortName.IsEnabled = false;
                cboBaudRate.IsEnabled = false;
                cboDataBits.IsEnabled = false;
                cboStopBits.IsEnabled = false;
                cboParity.IsEnabled = false;
                cboHandShake.IsEnabled = false;

                btnSerialPortReaderStart.IsEnabled = false;
                btnSerialPortReaderStop.IsEnabled = true;
            }
        }

        private void btnSerialPortReaderClose_Click(object sender, RoutedEventArgs e)
        {
            if (serialPort != null)
            {
                if (serialPort.IsOpen)
                {
                    serialPort.Close();

                    // Status
                    sensorMonitorStatus.Text = "Serial Port closed.";

                    if (!serialPort.IsOpen)
                    {
                        cboPortName.IsEnabled = true;
                        cboBaudRate.IsEnabled = true;
                        cboDataBits.IsEnabled = true;
                        cboStopBits.IsEnabled = true;
                        cboParity.IsEnabled = true;
                        cboHandShake.IsEnabled = true;

                        btnSerialPortReaderStart.IsEnabled = true;
                        btnSerialPortReaderStop.IsEnabled = false;
                    }
                }
            }

            // Resette dataCalculations
            foreach (var item in dataCalculations)
                item.Reset();
        }

        private void chkShowControlChars_Checked(object sender, RoutedEventArgs e)
        {
            showControlChars = true;

            // Lagre ny setting til config fil
            config.Write(ConfigKey.ShowControlChars, "1", ConfigSection.SerialPortConfig);
        }

        private void chkShowControlChars_Unchecked(object sender, RoutedEventArgs e)
        {
            showControlChars = false;

            // Lagre ny setting til config fil
            config.Write(ConfigKey.ShowControlChars, "0", ConfigSection.SerialPortConfig);
        }

        private void chkRawDataAllClear_Click(object sender, RoutedEventArgs e)
        {
            // Sletter lister
            rawDataItems.Clear();
            selectedPacketsItems.Clear();
            packetDataFieldsItems.Clear();
            selectedDataFieldItems.Clear();
            calculatedDataItems.Clear();
        }

        private void tbSelectedPacketHeader_LostFocus(object sender, RoutedEventArgs e)
        {
            tbSelectedPacketHeader_Update();
        }

        private void tbSelectedPacketHeader_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                tbSelectedPacketHeader_Update();
                Keyboard.ClearFocus();
            }
        }

        private void tbSelectedPacketHeader_Update()
        {
            // Test på om vi har gyldige escape characters
            try
            {
                process.packetHeader = Regex.Unescape(tbSelectedPacketHeader.Text); // Unescape gjør det mulig å skrive escape characters i input feltet
            }
            catch (Exception)
            {
                process.packetHeader = string.Empty;
            }

            // Lagre ny setting
            sensorData.serialPort.packetHeader = TextHelper.EscapeSpace(tbSelectedPacketHeader.Text);
        }

        private void tbSelectedPacketEnd_LostFocus(object sender, RoutedEventArgs e)
        {
            tbSelectedPacketEnd_Update();
        }

        private void tbSelectedPacketEnd_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                tbSelectedPacketEnd_Update();
                Keyboard.ClearFocus();
            }
        }

        private void tbSelectedPacketEnd_Update()
        {
            // Test på om vi har gyldige escape characters
            try
            {
                process.packetEnd = Regex.Unescape(tbSelectedPacketEnd.Text); // Unescape gjør det mulig å skrive escape characters i input feltet
            }
            catch (Exception)
            {
                process.packetEnd = string.Empty;
            }

            // Lagre ny setting
            sensorData.serialPort.packetEnd = TextHelper.EscapeSpace(tbSelectedPacketEnd.Text);
        }

        private void tbPacketDataDelimiter_LostFocus(object sender, RoutedEventArgs e)
        {
            tbPacketDataDelimiter_Update();
        }

        private void tbPacketDataDelimiter_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                tbPacketDataDelimiter_Update();
                Keyboard.ClearFocus();
            }
        }

        private void tbPacketDataDelimiter_Update()
        {
            // Test på om vi har gyldige escape characters
            try
            {
                process.packetDelimiter = Regex.Unescape(tbPacketDataDelimiter.Text); // Unescape gjør det mulig å skrive escape characters i input feltet
            }
            catch (Exception)
            {
                process.packetDelimiter = string.Empty;
            }

            // Lagre ny setting
            sensorData.serialPort.packetDelimiter = TextHelper.EscapeSpace(tbPacketDataDelimiter.Text);
        }

        private void cboCombineFields_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                process.packetCombineFields = (SerialPortSetup.CombineFields)Enum.Parse(typeof(SerialPortSetup.CombineFields), cboCombineFields.SelectedItem.ToString());
            }
            catch (Exception)
            {
                process.packetCombineFields = SerialPortSetup.CombineFields.None;
            }

            // Lagre ny setting
            sensorData.serialPort.packetCombineFields = process.packetCombineFields;
        }

        private void cboSelectedDataField_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            process.dataField = cboSelectedDataField.SelectedIndex;

            // Lagre ny setting
            sensorData.serialPort.dataField = process.dataField.ToString();
        }

        private void cboDecimalSeparator_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            process.decimalSeparator = (DecimalSeparator)Enum.Parse(typeof(DecimalSeparator), cboDecimalSeparator.SelectedItem.ToString());

            // Lagre ny setting
            sensorData.serialPort.decimalSeparator = process.decimalSeparator;
        }

        private void cboSelectedDataAutoExtract_Checked(object sender, RoutedEventArgs e)
        {
            process.autoExtractValue = true;

            // Lagre ny setting
            sensorData.serialPort.autoExtractValue = true;
        }

        private void cboSelectedDataAutoExtract_Unchecked(object sender, RoutedEventArgs e)
        {
            process.autoExtractValue = false;

            // Lagre ny setting
            sensorData.serialPort.autoExtractValue = false;
        }

        private void cboCalculationType1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Lagre ny setting
            sensorData.serialPort.calculationSetup[0].type = EnumExtension.GetEnumValueFromDescription<CalculationType>(cboCalculationType1.Text);
            dataCalculations[0].type = sensorData.serialPort.calculationSetup[0].type;
        }

        private void tbCalculationParameter1_LostFocus(object sender, RoutedEventArgs e)
        {
            tbCalculationParameter1_Update();
        }

        private void tbCalculationParameter1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                tbCalculationParameter1_Update();
                Keyboard.ClearFocus();
            }
        }

        private void tbCalculationParameter1_Update()
        {
            // Lagre ny setting
            if (double.TryParse(tbCalculationParameter1.Text, Constants.numberStyle, Constants.cultureInfo, out double param))
                sensorData.serialPort.calculationSetup[0].parameter = param;
            else
                sensorData.serialPort.calculationSetup[0].parameter = 0;

            dataCalculations[0].parameter = sensorData.serialPort.calculationSetup[0].parameter;

            // Skrive data (som kan være korrigert) tilbake til tekstfeltet
            tbCalculationParameter1.Text = sensorData.serialPort.calculationSetup[0].parameter.ToString();
        }

        private void cboCalculationType2_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Lagre ny setting
            sensorData.serialPort.calculationSetup[1].type = EnumExtension.GetEnumValueFromDescription<CalculationType>(cboCalculationType2.Text);
            dataCalculations[1].type = sensorData.serialPort.calculationSetup[1].type;
        }

        private void tbCalculationParameter2_LostFocus(object sender, RoutedEventArgs e)
        {
            tbCalculationParameter2_Update();
        }

        private void tbCalculationParameter2_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                tbCalculationParameter2_Update();
                Keyboard.ClearFocus();
            }
        }

        private void tbCalculationParameter2_Update()
        {
            // Lagre ny setting
            if (double.TryParse(tbCalculationParameter2.Text, Constants.numberStyle, Constants.cultureInfo, out double param))
                sensorData.serialPort.calculationSetup[1].parameter = param;
            else
                sensorData.serialPort.calculationSetup[1].parameter = 0;

            dataCalculations[1].parameter = sensorData.serialPort.calculationSetup[1].parameter;

            // Skrive data (som kan være korrigert) tilbake til tekstfeltet
            tbCalculationParameter2.Text = sensorData.serialPort.calculationSetup[1].parameter.ToString();

        }

        private void cboCalculationType3_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Lagre ny setting
            sensorData.serialPort.calculationSetup[2].type = EnumExtension.GetEnumValueFromDescription<CalculationType>(cboCalculationType3.Text);
            dataCalculations[2].type = sensorData.serialPort.calculationSetup[2].type;
        }

        private void tbCalculationParameter3_LostFocus(object sender, RoutedEventArgs e)
        {
            tbCalculationParameter3_Update();
        }

        private void tbCalculationParameter3_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                tbCalculationParameter3_Update();
                Keyboard.ClearFocus();
            }
        }

        private void tbCalculationParameter3_Update()
        {
            // Lagre ny setting
            if (double.TryParse(tbCalculationParameter3.Text, Constants.numberStyle, Constants.cultureInfo, out double param))
                sensorData.serialPort.calculationSetup[2].parameter = param;
            else
                sensorData.serialPort.calculationSetup[2].parameter = 0;

            dataCalculations[2].parameter = sensorData.serialPort.calculationSetup[2].parameter;

            // Skrive data (som kan være korrigert) tilbake til tekstfeltet
            tbCalculationParameter3.Text = sensorData.serialPort.calculationSetup[2].parameter.ToString();
        }

        private void cboCalculationType4_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Lagre ny setting
            sensorData.serialPort.calculationSetup[3].type = EnumExtension.GetEnumValueFromDescription<CalculationType>(cboCalculationType4.Text);
            dataCalculations[3].type = sensorData.serialPort.calculationSetup[3].type;
        }

        private void tbCalculationParameter4_LostFocus(object sender, RoutedEventArgs e)
        {
            tbCalculationParameter4_Update();
        }

        private void tbCalculationParameter4_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                tbCalculationParameter4_Update();
                Keyboard.ClearFocus();
            }
        }

        private void tbCalculationParameter4_Update()
        {
            // Lagre ny setting
            if (double.TryParse(tbCalculationParameter4.Text, Constants.numberStyle, Constants.cultureInfo, out double param))
                sensorData.serialPort.calculationSetup[3].parameter = param;
            else
                sensorData.serialPort.calculationSetup[3].parameter = 0;

            dataCalculations[3].parameter = sensorData.serialPort.calculationSetup[3].parameter;

            // Skrive data (som kan være korrigert) tilbake til tekstfeltet
            tbCalculationParameter4.Text = sensorData.serialPort.calculationSetup[3].parameter.ToString();
        }

        private void cboFixedPositionData_Checked(object sender, RoutedEventArgs e)
        {
            process.fixedPosData = true;

            // Lagre ny setting
            sensorData.serialPort.fixedPosData = process.fixedPosData;

            // Enable position input
            SetDataSelectionDisplay();
        }

        private void cboFixedPositionData_Unchecked(object sender, RoutedEventArgs e)
        {
            process.fixedPosData = false;

            // Lagre ny setting
            sensorData.serialPort.fixedPosData = process.fixedPosData;

            // Disable position input
            SetDataSelectionDisplay();
        }

        private void tbFixedPositionDataStart_LostFocus(object sender, RoutedEventArgs e)
        {
            tbFixedPositionDataStart_Update();
        }

        private void tbFixedPositionDataStart_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                tbFixedPositionDataStart_Update();
                Keyboard.ClearFocus();
            }
        }

        private void tbFixedPositionDataStart_Update()
        {
            try
            {
                if (string.IsNullOrEmpty(tbFixedPositionDataStart.Text))
                    process.fixedPosStart = 0;
                else
                    process.fixedPosStart = Convert.ToInt32(tbFixedPositionDataStart.Text);

                // Lagre ny setting
                sensorData.serialPort.fixedPosStart = process.fixedPosStart;
            }
            catch (Exception ex)
            {
                RadWindow.Alert(string.Format("Input must be numeric.\n\n{0}", TextHelper.Wrap(ex.Message)));
            }
        }

        private void tbFixedPositionDataTotal_LostFocus(object sender, RoutedEventArgs e)
        {
            tbFixedPositionDataTotal_Update();
        }

        private void tbFixedPositionDataTotal_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                tbFixedPositionDataTotal_Update();
                Keyboard.ClearFocus();
            }
        }

        private void tbFixedPositionDataTotal_Update()
        {
            try
            {
                if (string.IsNullOrEmpty(tbFixedPositionDataTotal.Text))
                    process.fixedPosTotal = 0;
                else
                    process.fixedPosTotal = Convert.ToInt32(tbFixedPositionDataTotal.Text);

                // Lagre ny setting
                sensorData.serialPort.fixedPosTotal = process.fixedPosTotal;
            }
            catch (Exception ex)
            {
                RadWindow.Alert(string.Format("Input must be numeric.\n\n{0}", TextHelper.Wrap(ex.Message)));
            }
        }

        private void RadWindow_Closed(object sender, WindowClosedEventArgs e)
        {
            // Når vinduet lukkes sjekker vi om port fortsatt er åpen
            if (serialPort != null)
                if (serialPort.IsOpen)
                    // Lukke port
                    serialPort.Close();
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
            serialPortSetupWindowVM.totalDataLinesString = (sender as TextBox).Text;
        }

        private void cboSensorDataType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                // Lagre ny setting
                sensorData.serialPort.inputType = (InputDataType)Enum.Parse(typeof(InputDataType), cboSensorDataType.SelectedItem.ToString());

                if (sensorData.serialPort.inputType == InputDataType.Binary)
                {
                    process.packetDelimiter = Constants.BinaryDataDelimiter;
                    sensorData.serialPort.packetDelimiter = Constants.BinaryDataDelimiter;
                    tbPacketDataDelimiter.Text = Constants.BinaryDataDelimiter;
                }
                
                process.inputType = sensorData.serialPort.inputType;

                SetDataFieldSplitDisplay();
                SetDataSelectionDisplay();
            }
            catch (Exception ex)
            {
                RadWindow.Alert(string.Format("cboSensorDataType_SelectionChanged\n\n{0}", TextHelper.Wrap(ex.Message)));
            }
        }

        private void cboSelectedDataBytesInValue_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            process.binaryType = EnumExtension.GetEnumValueFromDescription<BinaryType>(cboSelectedDataBytesInValue.Text);

            // Lagre ny setting
            sensorData.serialPort.binaryType = process.binaryType;

            SetDataSelectionDisplay();
        }
    }

    public class Packet : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private string _data { get; set; }
        public string data
        {
            get
            {
                return _data;
            }
            set
            {
                _data = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(data)));
            }
        }
    }

    public class PacketDataFields : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private string[] _dataField { get; set; } = new string[Constants.PacketDataFields];
        public string[] dataField
        {
            get
            {
                return _dataField;
            }
            set
            {
                _dataField = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(dataField)));
            }
        }
    }

    public class SelectedDataField : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private string _selectedDataFieldString { get; set; }
        public string selectedDataFieldString
        {
            get
            {
                return _selectedDataFieldString;
            }
            set
            {
                _selectedDataFieldString = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(selectedDataFieldString)));
            }
        }
    }

    public class CalculatedData : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public double _data { get; set; }
        public double data
        {
            get
            {
                return _data;
            }
            set
            {
                _data = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(data)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(dataString)));
            }
        }

        public string dataString
        {
            get
            {
                if (!double.IsNaN(data))
                    return data.ToString();
                else
                    return "<non-numeric value>";
            }
        }
    }
}