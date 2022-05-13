using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO.Ports;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace SensorMonitor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        // Serie port objekt
        private SerialPort serialPort = new SerialPort();

        // Timer som begrenser hvor ofte vi viser data på skjerm
        // NB! Begrenser ikke lesing fra port, kun hvor ofte innkommende data blir vist
        // Dette for at ikke UI skal bli overbelastet med for mye oppdatering dersom det kommer inn mye data med høy frekvens
        Stopwatch dataTimer = new Stopwatch();

        // Noen globale variabler som brukes i og oppdateres av forskjellige tråder
        private string gPacketStart;
        private string gPacketEnd;
        private string gPacketDelimiter;
        private int gSelectedDataField;
        private DataProcessing gProcessing = new DataProcessing();
        private bool gSelectedDataAutoExtraction;
        private bool gShowControlChars;

        // Nummer stil og kultur-avhengig notasjon
        private static NumberStyles numberStyle = NumberStyles.Any;
        private static CultureInfo cultureInfo = CultureInfo.InvariantCulture;

        // Lister for visning
        private List<Packet> rawDataItems = new List<Packet>();
        private List<Packet> selectedPacketsItems = new List<Packet>();
        private List<PacketDataField> packetDataFieldsItems = new List<PacketDataField>();
        private List<SelectedData> selectedDataItems = new List<SelectedData>();
        private List<ProcessedData> processedDataItems = new List<ProcessedData>();

        public MainWindow()
        {
            InitializeComponent();

            // Initialisere application settings
            InitializeApplicationSettings();
        }

        private void btnSerialPortReaderOpen_Click(object sender, RoutedEventArgs e)
        {
            // Åpne serieporten
            OpenSerialPort(serialPort);

            // Stenge tilgang til å konfigurere porten
            if (serialPort.IsOpen)
            {
                cboCOMPort.IsEnabled = false;
                cboBaudRate.IsEnabled = false;
                cboDataBits.IsEnabled = false;
                cboStopBits.IsEnabled = false;
                cboDataParity.IsEnabled = false;
                cboHandShake.IsEnabled = false;

                btnSerialPortReaderStart.IsEnabled = false;
                btnSerialPortReaderStop.IsEnabled = true;
            }
        }

        private void btnSerialPortReaderClose_Click(object sender, RoutedEventArgs e)
        {
            //serialPortReaderProcess.CancelAsync();

            if (serialPort.IsOpen)
            {
                serialPort.Close();

                // Status
                sensorMonitorStatus.Text = "Serial Port closed.";

                if (!serialPort.IsOpen)
                {
                    cboCOMPort.IsEnabled = true;
                    cboBaudRate.IsEnabled = true;
                    cboDataBits.IsEnabled = true;
                    cboStopBits.IsEnabled = true;
                    cboDataParity.IsEnabled = true;
                    cboHandShake.IsEnabled = true;

                    btnSerialPortReaderStart.IsEnabled = true;
                    btnSerialPortReaderStop.IsEnabled = false;
                }
            }
        }

        private void ReadAvailableCOMPorts()
        {
            IniFile iniFile = new IniFile(INIFILE.Location);

            string[] arrayComPortsNames = null;
            int index = -1;
            string comPortName = String.Empty;

            arrayComPortsNames = SerialPort.GetPortNames();

            if (arrayComPortsNames.Length > 0)
            {
                // Sortere port-listen
                Array.Sort(arrayComPortsNames);

                // Legg porter inn i combo box
                do
                {
                    index += 1;
                    cboCOMPort.Items.Add(arrayComPortsNames[index]);
                }

                while (!((arrayComPortsNames[index] == comPortName) || (index == arrayComPortsNames.GetUpperBound(0))));

                // Skrive satt COM port i combobox tekst feltet
                string iniPort = iniFile.Read(INIFILE.PortName, INIFILE.SerialPortConfigurationSection);
                if (Array.Exists(arrayComPortsNames, element => element == iniPort))
                    cboCOMPort.Text = iniPort;
                else
                    cboCOMPort.Text = arrayComPortsNames[0];
            }
            else
            {
                // Fant ingen porter
                cboCOMPort.Text = "No COM ports found";
            }
        }

        private void InitializeApplicationSettings()
        {
            dataTimer.Start();

            // Initialisere Serial Port Configuration settings
            InitializeSerialPortConfiguration(serialPort);

            // Initialisere Packet Selection
            InitializePacketSelection();

            // Initialisere Packet Data Selection
            InitializePacketDataSelection();

            // Initialisere Data Selection
            InitializeDataSelection();

            // Initialisere Data Processing
            InitializeDataProcessing();

            // Lagrer config i tilfelle dette er første kjøring og ini fil ikke eksisterer
            SaveAllWindowSettings();
        }

        private void InitializeSerialPortConfiguration(SerialPort serialPort)
        {
            IniFile iniFile = new IniFile(INIFILE.Location);

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
            string iniBaudRate = iniFile.Read(INIFILE.BaudRate, INIFILE.SerialPortConfigurationSection);
            if (!string.IsNullOrEmpty(iniBaudRate))
                cboBaudRate.Text = iniBaudRate;
            else
                cboBaudRate.Text = cboBaudRate.Items[0].ToString();

            // Data Bits
            ///////////////////////////////////////////////////////////
            cboDataBits.Items.Add(7);
            cboDataBits.Items.Add(8);

            // Skrive satt data bit type i combobox feltet
            string iniDataBit = iniFile.Read(INIFILE.DataBits, INIFILE.SerialPortConfigurationSection);
            if (!string.IsNullOrEmpty(iniDataBit))
                cboDataBits.Text = iniDataBit;
            else
                cboDataBits.Text = cboDataBits.Items[1].ToString();

            // Stop Bits
            ///////////////////////////////////////////////////////////
            cboStopBits.Items.Add("One");
            cboStopBits.Items.Add("OnePointFive");
            cboStopBits.Items.Add("Two");

            // Skrive satt stop bit i combobox feltet
            string iniStopBit = iniFile.Read(INIFILE.StopBits, INIFILE.SerialPortConfigurationSection);
            if (!string.IsNullOrEmpty(iniStopBit))
                cboStopBits.Text = iniStopBit;
            else
                cboStopBits.Text = cboStopBits.Items[0].ToString();

            // Parity 
            ///////////////////////////////////////////////////////////
            cboDataParity.Items.Add("None");
            cboDataParity.Items.Add("Even");
            cboDataParity.Items.Add("Mark");
            cboDataParity.Items.Add("Odd");
            cboDataParity.Items.Add("Space");

            // Skrive satt parity i combobox feltet
            string iniDataParity = iniFile.Read(INIFILE.DataParity, INIFILE.SerialPortConfigurationSection);
            if (!string.IsNullOrEmpty(iniDataParity))
                cboDataParity.Text = iniDataParity;
            else
                cboDataParity.Text = cboDataParity.Items[0].ToString();

            // Handshake
            ///////////////////////////////////////////////////////////
            cboHandShake.Items.Add("None");
            cboHandShake.Items.Add("XOnXOff");
            cboHandShake.Items.Add("RequestToSend");
            cboHandShake.Items.Add("RequestToSendXOnXOff");

            // Skrive satt hand shake type i combobox feltet
            string iniHandShake = iniFile.Read(INIFILE.HandShake, INIFILE.SerialPortConfigurationSection);
            if (!string.IsNullOrEmpty(iniHandShake))
                cboHandShake.Text = iniHandShake;
            else
                cboHandShake.Text = cboHandShake.Items[0].ToString();

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
                    tbPortStatus.Fill = Brushes.SpringGreen;
                }
                else
                {
                    tbPortStatus.Fill = Brushes.Red;
                }
            }

            // Lese raw data view auto scroll
            if (iniFile.Read(INIFILE.RawDataAutoScroll, INIFILE.SerialPortConfigurationSection, "1") == "1")
                chkRawDataAutoScroll.IsChecked = true;
            else
                chkRawDataAutoScroll.IsChecked = false;

            // Lese kontroll chars option
            if (iniFile.Read(INIFILE.ShowControlChars, INIFILE.SerialPortConfigurationSection, "1") == "1")
            {
                chkShowControlChars.IsChecked = true;
                gShowControlChars = true;
            }
            else
            {
                chkShowControlChars.IsChecked = false;
                gShowControlChars = false;
            }
        }

        private void InitializePacketSelection()
        {
            IniFile iniFile = new IniFile(INIFILE.Location);

            // Binding for listviews
            lvSelectedPackets.ItemsSource = selectedPacketsItems;

            // Packet Start
            string iniSelectedPacketStart = StringOps.UnescapeSpace(iniFile.Read(INIFILE.SelectedPacketsStart, INIFILE.SerialPortConfigurationSection));
            if (!string.IsNullOrEmpty(iniSelectedPacketStart))
                tbSelectedPacketStart.Text = iniSelectedPacketStart;
            else
                tbSelectedPacketStart.Text = String.Empty;
            gPacketStart = tbSelectedPacketStart.Text;

            // Packet End
            string iniSelectedPacketEnd = StringOps.UnescapeSpace(iniFile.Read(INIFILE.SelectedPacketsEnd, INIFILE.SerialPortConfigurationSection, @"\r\n"));
            if (!string.IsNullOrEmpty(iniSelectedPacketEnd))
                tbSelectedPacketEnd.Text = iniSelectedPacketEnd;
            else
                tbSelectedPacketEnd.Text = String.Empty;
            gPacketEnd = tbSelectedPacketEnd.Text;

            // Lese packet selection view auto scroll
            if (iniFile.Read(INIFILE.SelectedPacketsAutoScroll, INIFILE.SerialPortConfigurationSection, "1") == "1")
                chkSelectedPacketsAutoScroll.IsChecked = true;
            else
                chkSelectedPacketsAutoScroll.IsChecked = false;
        }

        private void InitializePacketDataSelection()
        {
            IniFile iniFile = new IniFile(INIFILE.Location);

            // Binding for listviews
            lvPacketData.ItemsSource = packetDataFieldsItems;

            // Delimiter
            string iniPacketDataDelimiter = StringOps.UnescapeSpace(iniFile.Read(INIFILE.PacketDataDelimiter, INIFILE.SerialPortConfigurationSection));
            if (!string.IsNullOrEmpty(iniPacketDataDelimiter))
                tbPacketDataDelimiter.Text = iniPacketDataDelimiter;
            else
                tbPacketDataDelimiter.Text = String.Empty;
            gPacketDelimiter = tbPacketDataDelimiter.Text;

            // Lese packet data view auto scroll
            if (iniFile.Read(INIFILE.PacketDataAutoScroll, INIFILE.SerialPortConfigurationSection, "1") == "1")
                chkPacketDataAutoScroll.IsChecked = true;
            else
                chkPacketDataAutoScroll.IsChecked = false;
        }

        private void InitializeDataSelection()
        {
            IniFile iniFile = new IniFile(INIFILE.Location);

            // Binding for listviews
            lvSelectedData.ItemsSource = selectedDataItems;

            // Data Field 
            for (int i = 0; i < Constants.PacketDataFields; i++)
                cboSelectedDataField.Items.Add(String.Format("{0}", i));

            string iniSelectedDataField = iniFile.Read(INIFILE.SelectedDataField, INIFILE.SerialPortConfigurationSection);
            if (!string.IsNullOrEmpty(iniSelectedDataField))
                cboSelectedDataField.Text = iniSelectedDataField;
            else
                cboSelectedDataField.Text = cboSelectedDataField.Items[0].ToString();

            cboSelectedDataField.SelectedIndex = int.Parse(cboSelectedDataField.Text);

            // Lese selected data auto extraction setting
            if (iniFile.Read(INIFILE.SelectedDataAutoExtraction, INIFILE.SerialPortConfigurationSection, "0") == "1")
            {
                cboSelectedDataAutoExtract.IsChecked = true;
                gSelectedDataAutoExtraction = true;
            }
            else
            {
                cboSelectedDataAutoExtract.IsChecked = false;
                gSelectedDataAutoExtraction = false;
            }

            // Lese selected data view auto scroll
            if (iniFile.Read(INIFILE.SelectedDataAutoScroll, INIFILE.SerialPortConfigurationSection, "1") == "1")
                chkSelectedDataAutoScroll.IsChecked = true;
            else
                chkSelectedDataAutoScroll.IsChecked = false;
        }

        private void InitializeDataProcessing()
        {
            IniFile iniFile = new IniFile(INIFILE.Location);

            // Binding for listviews
            lvProcessedData.ItemsSource = processedDataItems;

            // Prosesseringstyper
            cboProcessingType.Items.Add(String.Format("None"));
            cboProcessingType.Items.Add(String.Format("Multiplication"));
            cboProcessingType.Items.Add(String.Format("Division"));

            string iniProcessingType = iniFile.Read(INIFILE.ProcessingType, INIFILE.SerialPortConfigurationSection);
            if (!string.IsNullOrEmpty(iniProcessingType))
                cboProcessingType.Text = iniProcessingType;
            else
                cboProcessingType.Text = cboProcessingType.Items[0].ToString();

            cboProcessingType.SelectedIndex = (int)Enum.Parse(typeof(ProcessingType), cboProcessingType.Text);

            // Parameter
            string iniProcessingParameter = iniFile.Read(INIFILE.ProcessingParameter, INIFILE.SerialPortConfigurationSection);
            if (!string.IsNullOrEmpty(iniProcessingParameter))
                tbProcessingParameter.Text = iniProcessingParameter;
            else
                tbProcessingParameter.Text = String.Empty;

            // Lese processed data view auto scroll
            if (iniFile.Read(INIFILE.ProcessingAutoScroll, INIFILE.SerialPortConfigurationSection, "1") == "1")
                chkProcessedDataAutoScroll.IsChecked = true;
            else
                chkProcessedDataAutoScroll.IsChecked = false;
        }

        private void SaveAllWindowSettings()
        {
            // Lagre alle settings til ini fil
            IniFile iniFile = new IniFile(INIFILE.Location);

            // Serial Port Configuration
            iniFile.Write(INIFILE.PortName, cboCOMPort.SelectedItem.ToString(), INIFILE.SerialPortConfigurationSection);
            iniFile.Write(INIFILE.BaudRate, cboBaudRate.SelectedItem.ToString(), INIFILE.SerialPortConfigurationSection);
            iniFile.Write(INIFILE.DataBits, cboDataBits.SelectedItem.ToString(), INIFILE.SerialPortConfigurationSection);
            iniFile.Write(INIFILE.StopBits, cboStopBits.SelectedItem.ToString(), INIFILE.SerialPortConfigurationSection);
            iniFile.Write(INIFILE.DataParity, cboDataParity.SelectedItem.ToString(), INIFILE.SerialPortConfigurationSection);
            iniFile.Write(INIFILE.HandShake, cboHandShake.SelectedItem.ToString(), INIFILE.SerialPortConfigurationSection);

            if (chkRawDataAutoScroll.IsChecked == true)
                iniFile.Write(INIFILE.RawDataAutoScroll, "1", INIFILE.SerialPortConfigurationSection);
            else
                iniFile.Write(INIFILE.RawDataAutoScroll, "0", INIFILE.SerialPortConfigurationSection);

            if (chkShowControlChars.IsChecked == true)
                iniFile.Write(INIFILE.ShowControlChars, "1", INIFILE.SerialPortConfigurationSection);
            else
                iniFile.Write(INIFILE.ShowControlChars, "0", INIFILE.SerialPortConfigurationSection);

            // Packet Selection
            iniFile.Write(INIFILE.SelectedPacketsStart, StringOps.EscapeSpace(tbSelectedPacketStart.Text), INIFILE.SerialPortConfigurationSection);
            iniFile.Write(INIFILE.SelectedPacketsEnd, StringOps.EscapeSpace(tbSelectedPacketEnd.Text), INIFILE.SerialPortConfigurationSection);

            if (chkSelectedPacketsAutoScroll.IsChecked == true)
                iniFile.Write(INIFILE.SelectedPacketsAutoScroll, "1", INIFILE.SerialPortConfigurationSection);
            else
                iniFile.Write(INIFILE.SelectedPacketsAutoScroll, "0", INIFILE.SerialPortConfigurationSection);

            // Packet Data
            iniFile.Write(INIFILE.PacketDataDelimiter, StringOps.EscapeSpace(tbPacketDataDelimiter.Text), INIFILE.SerialPortConfigurationSection);

            if (chkPacketDataAutoScroll.IsChecked == true)
                iniFile.Write(INIFILE.PacketDataAutoScroll, "1", INIFILE.SerialPortConfigurationSection);
            else
                iniFile.Write(INIFILE.PacketDataAutoScroll, "0", INIFILE.SerialPortConfigurationSection);

            // Data Selection
            iniFile.Write(INIFILE.SelectedDataField, cboSelectedDataField.SelectedItem.ToString(), INIFILE.SerialPortConfigurationSection);

            if (cboSelectedDataAutoExtract.IsChecked == true)
                iniFile.Write(INIFILE.SelectedDataAutoExtraction, "1", INIFILE.SerialPortConfigurationSection);
            else
                iniFile.Write(INIFILE.SelectedDataAutoExtraction, "0", INIFILE.SerialPortConfigurationSection);

            if (chkSelectedDataAutoScroll.IsChecked == true)
                iniFile.Write(INIFILE.SelectedDataAutoScroll, "1", INIFILE.SerialPortConfigurationSection);
            else
                iniFile.Write(INIFILE.SelectedDataAutoScroll, "0", INIFILE.SerialPortConfigurationSection);

            // Data Processing
            iniFile.Write(INIFILE.ProcessingType, cboProcessingType.SelectedItem.ToString(), INIFILE.SerialPortConfigurationSection);
            iniFile.Write(INIFILE.ProcessingParameter, tbProcessingParameter.Text, INIFILE.SerialPortConfigurationSection);

            if (chkProcessedDataAutoScroll.IsChecked == true)
                iniFile.Write(INIFILE.ProcessingAutoScroll, "1", INIFILE.SerialPortConfigurationSection);
            else
                iniFile.Write(INIFILE.ProcessingAutoScroll, "0", INIFILE.SerialPortConfigurationSection);
        }

        private void OpenSerialPort(SerialPort serialPort)
        {
            try
            {
                IniFile iniFile = new IniFile(INIFILE.Location);

                // Lese serie port settings fra ini fil og setter på SerialPort object 
                string iniPortName = iniFile.Read(INIFILE.PortName, INIFILE.SerialPortConfigurationSection);
                if (!string.IsNullOrEmpty(iniPortName))
                    serialPort.PortName = Convert.ToString(iniPortName);

                string iniBaudRate = iniFile.Read(INIFILE.BaudRate, INIFILE.SerialPortConfigurationSection);
                if (!string.IsNullOrEmpty(iniBaudRate))
                    serialPort.BaudRate = Convert.ToInt32(iniBaudRate);

                string iniDataBits = iniFile.Read(INIFILE.DataBits, INIFILE.SerialPortConfigurationSection);
                if (!string.IsNullOrEmpty(iniDataBits))
                    serialPort.DataBits = Convert.ToInt16(iniDataBits);

                string iniStopBits = iniFile.Read(INIFILE.StopBits, INIFILE.SerialPortConfigurationSection);
                if (!string.IsNullOrEmpty(iniStopBits))
                    serialPort.StopBits = (StopBits)Enum.Parse(typeof(StopBits), iniStopBits);

                string iniDataParity = iniFile.Read(INIFILE.DataParity, INIFILE.SerialPortConfigurationSection);
                if (!string.IsNullOrEmpty(iniDataParity))
                    serialPort.Parity = (Parity)Enum.Parse(typeof(Parity), iniDataParity);

                string iniHandShake = iniFile.Read(INIFILE.HandShake, INIFILE.SerialPortConfigurationSection);
                if (!string.IsNullOrEmpty(iniHandShake))
                    serialPort.Handshake = (Handshake)Enum.Parse(typeof(Handshake), iniHandShake);

                // Koble opp metode for å motta data
                serialPort.DataReceived += new SerialDataReceivedEventHandler(SerialPort_DataReceived);

                // Åpne port
                serialPort.Open();

                // Status
                sensorMonitorStatus.Text = "Serial port open for reading...";
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error opening serial port", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            // Lese fra port
            string inputData = serialPort.ReadExisting();

            //// Alternativ måte å lese fra port
            //int bytes = serialPort.BytesToRead;
            //byte[] array = new byte[bytes];
            //int i = 0;
            //while (serialPort.BytesToRead > 0)
            //{
            //    array[i] = Convert.ToByte(serialPort.ReadByte());
            //    i++;
            //}
            //string inputData = System.Text.Encoding.UTF8.GetString(array, 0, array.Length);

            if (dataTimer.ElapsedMilliseconds > Constants.DataTimerInterval)
            {
                // Sende leste data til skjermutskrift
                if (!String.IsNullOrEmpty(inputData))
                {
                    // Vise raw data
                    DisplayRawData(inputData);

                    // Prosessere raw data
                    FindSelectedPackets(inputData);
                }

                dataTimer.Restart();
            }
        }

        private void DisplayRawData(string dataString)
        {
            string dataStringMod;

            // Escape data string?
            if (gShowControlChars == true)
                dataStringMod = StringOps.EscapeControlChars(dataString);
            // ...eller bare rett ut
            else
                // Trimmer whitespace og linefeed
                dataStringMod = dataString.Trim();

            Application.Current.Dispatcher.BeginInvoke(
            DispatcherPriority.Normal,
            new Action(() => {

                // Legg ut data i raw data listview
                rawDataItems.Add(new Packet() { data = dataStringMod });
                lvRawData.Items.Refresh();

                // Raw Data Auto scroll
                if (chkRawDataAutoScroll.IsChecked == true && lvRawData.Items.Count > 0)
                {
                    lvRawData.ScrollIntoView(lvRawData.Items[lvRawData.Items.Count - 1]);
                }

                // Status
                sensorMonitorStatus.Text = "Receiving data from serial port...";
            }));
        }

        private void FindSelectedPackets(string dataString)
        {
            // Først må vi dele opp rå data i individuelle pakker i tilfelle vi får mer enn en pakke i en sending.
            // Så må disse pakkene behandles individuelt.

            // Først hente start og stop string
            string packetStartString = Regex.Unescape(gPacketStart); // gPacketStart testes når den settes, dropper exception handling på Regex her
            string packetEndString = Regex.Unescape(gPacketEnd);

            // Enten header eller end string må være satt, ellers kan vi ikke dele opp dataene eller verifisere at vi har fullstendig data pakke
            // Det burde egentlig være sånn at begge burde være satt, men det finnes sensorer som leverer data uten header og noen uten end...
            if (packetStartString != String.Empty || packetEndString != String.Empty)
            {
                int startPos = 0;
                int endPos = dataString.Length;
                int packetStartPos = 0;
                int packetEndPos;
                int totalCount;
                string packetString;

                while ((startPos <= endPos) && (packetStartPos > -1))
                {
                    totalCount = endPos - startPos;

                    // Sjekker om packet header er satt
                    if (!String.IsNullOrEmpty(packetStartString))
                        // Finne header pos
                        packetStartPos = dataString.IndexOf(packetStartString, startPos, totalCount, StringComparison.Ordinal); // Ytelse-/hastighets-forskjell er stor med/uten StringComparison.Ordinal
                    else
                        // Ingen header satt -> les fra første pos
                        packetStartPos = startPos;

                    // Fant ikke packetStartString -> finnes ikke i dataString -> avslutt
                    if (packetStartPos == -1)
                    {
                        break;
                    }
                    // startString funnet
                    else
                    {
                        // Så må vi søke etter endString
                        totalCount = endPos - packetStartPos;

                        // Sjekker om packet end er satt
                        if (!String.IsNullOrEmpty(packetEndString))
                            // Finne end pos
                            packetEndPos = dataString.IndexOf(packetEndString, packetStartPos, totalCount, StringComparison.Ordinal);
                        else
                            // Ingen end satt -> les til slutten
                            packetEndPos = totalCount;

                        // Fant ikke packetEndString -> finnes ikke i dataString -> avslutt
                        if (packetEndPos == -1)
                        {
                            break;
                        }
                        // packetEndString funnet
                        else
                        {
                            // Korrigerer packetEndString slik at vi får med packetEndString i resultatet
                            packetEndPos += packetEndString.Length;

                            // Plukke ut data mellom packetStartPos og packetEndPos for videre prosessering
                            packetString = dataString.Substring(packetStartPos, packetEndPos - packetStartPos);

                            // Vise de utvalgte telegrammene på skjerm
                            DisplaySelectedPackets(packetString);

                            // Finne data feltene i packet
                            FindPacketDataFields(packetString);

                            // Neste iterasjon
                            startPos = packetEndPos;
                        }
                    }
                }
            }
        }

        private void DisplaySelectedPackets(string packetString)
        {
            string dataStringMod;

            // Escape data string?
            if (gShowControlChars == true)
                dataStringMod = StringOps.EscapeControlChars(packetString);
            // ...eller bare rett ut
            else
                // Trimmer whitespace og linefeed
                dataStringMod = packetString.Trim();

            Application.Current.Dispatcher.BeginInvoke(
            DispatcherPriority.Normal,
            new Action(() => {

                // Legg ut data i selected packets listview
                selectedPacketsItems.Add(new Packet() { data = dataStringMod });
                lvSelectedPackets.Items.Refresh();

                // Selected Packets Auto scroll
                if (chkSelectedPacketsAutoScroll.IsChecked == true && lvSelectedPackets.Items.Count > 0)
                {
                    lvSelectedPackets.ScrollIntoView(lvSelectedPackets.Items[lvSelectedPackets.Items.Count - 1]);
                }
            }));
        }

        private void FindPacketDataFields(string dataString)
        {
            // Først hente start, stop og delimiter string
            string startString = Regex.Unescape(gPacketStart);
            string endString = Regex.Unescape(gPacketEnd);
            string[] delimiter = new string[] { Regex.Unescape(gPacketDelimiter) };

            // Sjekke start & end
            if ((dataString.StartsWith(startString, StringComparison.Ordinal) || startString == String.Empty) &&
                (dataString.EndsWith(endString, StringComparison.Ordinal) || endString == String.Empty))
            {
                string trimmedDataString = dataString;

                // Trimme bort start
                if (startString != String.Empty)
                    trimmedDataString = trimmedDataString.Substring(startString.Length);

                // Trimme bort evt delimiter etter start
                if (trimmedDataString.StartsWith(delimiter[0].ToString(), StringComparison.Ordinal))
                    trimmedDataString = trimmedDataString.Substring(delimiter[0].ToString().Length);

                // Trimme bort end
                if (endString != String.Empty)
                    trimmedDataString = trimmedDataString.Substring(0, trimmedDataString.IndexOf(endString, StringComparison.Ordinal));

                // Fordele data ut i datafelt før visning
                PacketDataField packetDataFields = new PacketDataField();

                string[] dataFields = trimmedDataString.Split(delimiter, StringSplitOptions.None);

                for (int i = 0; i < dataFields.Length && i < packetDataFields.Value.Length; i++)
                {
                    packetDataFields.Value[i] = dataFields[i];
                }

                // Vise data
                DisplayPacketDataFields(packetDataFields);

                // Prosessere selected data
                FindSelectedDataInPacket(packetDataFields);
            }
        }

        private void DisplayPacketDataFields(PacketDataField packetDataFields)
        {
            PacketDataField packetDataFieldsMod = new PacketDataField();

            //var sw = Stopwatch.StartNew();

            // Escape data string?
            if (gShowControlChars == true)
            {
                for (int i = 0; i < packetDataFields.Value.Length && i < packetDataFieldsMod.Value.Length; i++)
                    if (!String.IsNullOrEmpty(packetDataFields.Value[i]))
                        packetDataFieldsMod.Value[i] = StringOps.EscapeControlChars(packetDataFields.Value[i]);
            }
            // ...eller bare rett ut
            else
            {
                // Trimmer whitespace og linefeed
                for (int i = 0; i < packetDataFields.Value.Length && i < packetDataFieldsMod.Value.Length; i++)
                    if (!String.IsNullOrEmpty(packetDataFields.Value[i]))
                        packetDataFieldsMod.Value[i] = packetDataFields.Value[i].Trim();
            }
            //packetDataFieldsMod = packetDataFields;

            //long time = sw.ElapsedMilliseconds;

            Application.Current.Dispatcher.BeginInvoke(
            DispatcherPriority.Normal,
            new Action(() => {

                // Legg ut data i packet data listview
                packetDataFieldsItems.Add(packetDataFieldsMod);
                lvPacketData.Items.Refresh();

                // Packet Data Auto scroll
                if (chkPacketDataAutoScroll.IsChecked == true && lvPacketData.Items.Count > 0)
                {
                    lvPacketData.ScrollIntoView(lvPacketData.Items[lvPacketData.Items.Count - 1]);
                }
            }));
        }

        private void FindSelectedDataInPacket(PacketDataField packetDataFields)
        {
            SelectedData selectedData = new SelectedData();

            if (!String.IsNullOrEmpty(packetDataFields.Value[gSelectedDataField]))
                selectedData.selectedDataString = packetDataFields.Value[gSelectedDataField];

            // Vise selected data på skjerm
            if (!String.IsNullOrEmpty(selectedData.selectedDataString))
            {
                // Har vi automatisk number extraction?
                if (gSelectedDataAutoExtraction)
                {
                    double doubleValue;
                    int intValue;
                    string valueStr;
                    bool valueFound = false;

                    // NB! Viktig å bruke cultureInfo i konvertering til og fra string

                    // 1. Søker først etter desimal-tall verdier

                    // Søker etter substring med desimal-tall
                    valueStr = Regex.Match(selectedData.selectedDataString, @"-?\d*\.\d*").ToString();

                    // Fant vi substring med tall?
                    if (!String.IsNullOrEmpty(valueStr))
                    {
                        // Konvertere substring til double for å verifisere gyldig double
                        valueFound = double.TryParse(valueStr, numberStyle, cultureInfo, out doubleValue);

                        // Fant desimal tall
                        if (valueFound)
                        {
                            selectedData.selectedDataString = doubleValue.ToString(cultureInfo);
                        }
                    }

                    // ...dersom vi ikke fant desimal-tall verdier
                    if (!valueFound)
                    {
                        // 2. Søker etter integer verdier

                        // Søker etter substring med integer tall
                        valueStr = Regex.Match(selectedData.selectedDataString, @"-?\d+").ToString();

                        // Fant vi substring med tall?
                        if (!String.IsNullOrEmpty(valueStr))
                        {
                            // Konvertere substring til int for å verifisere gyldig int
                            valueFound = int.TryParse(valueStr, numberStyle, cultureInfo, out intValue);

                            // Fant integer tall
                            if (valueFound)
                            {
                                selectedData.selectedDataString = intValue.ToString(cultureInfo);
                            }
                        }
                    }
                }

                // Vise utvalgt datafelt på skjerm
                DisplaySelectedData(selectedData);

                // Prosessere utvalgt datafelt verdi
                ApplyProcessingToSelectedData(selectedData);
            }
        }

        private void DisplaySelectedData(SelectedData selectedData)
        {
            Application.Current.Dispatcher.BeginInvoke(
            DispatcherPriority.Normal,
            new Action(() => {

                // Legg ut data i packet data listview
                selectedDataItems.Add(selectedData);
                lvSelectedData.Items.Refresh();

                // Packet Data Auto scroll
                if (chkSelectedDataAutoScroll.IsChecked == true && lvSelectedData.Items.Count > 0)
                {
                    lvSelectedData.ScrollIntoView(lvSelectedData.Items[lvSelectedData.Items.Count - 1]);
                }
            }));
        }

        private void ApplyProcessingToSelectedData(SelectedData selectedData)
        {
            ProcessedData processedData = new ProcessedData();
            switch (gProcessing.type)
            {
                case ProcessingType.None:
                case ProcessingType.Multiplication:
                case ProcessingType.Division:
                    try
                    {
                        // NB! Viktig å bruke cultureInfo i konvertering til og fra string
                        double value;

                        // Sjekke om string er numerisk
                        if (double.TryParse(selectedData.selectedDataString, numberStyle, cultureInfo, out value))
                        {
                            // Utføre valgt prosessering
                            processedData.processedDataString = (gProcessing.DoProcessing(value)).ToString(cultureInfo);
                        }
                        else
                        {
                            processedData.processedDataString = "<Non-numeric data>";
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Data Selection", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    break;

                default:
                    processedData.processedDataString = "<Invalid Processing Type>";
                    break;
            }

            // Vise de prosesserte dataene
            DisplayProcessedData(processedData);
        }

        private void DisplayProcessedData(ProcessedData processedData)
        {
            Application.Current.Dispatcher.BeginInvoke(
            DispatcherPriority.Normal,
            new Action(() => {

                // Legg ut data i packet data listview
                processedDataItems.Add(processedData);
                lvProcessedData.Items.Refresh();

                // Packet Data Auto scroll
                if (chkProcessedDataAutoScroll.IsChecked == true && lvProcessedData.Items.Count > 0)
                {
                    lvProcessedData.ScrollIntoView(lvProcessedData.Items[lvProcessedData.Items.Count - 1]);
                }
            }));
        }

        private void cboCOMPort_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Lagre ny setting til ini fil
            IniFile iniFile = new IniFile(INIFILE.Location);

            iniFile.Write(INIFILE.PortName, cboCOMPort.SelectedItem.ToString(), INIFILE.SerialPortConfigurationSection);
        }

        private void cboBaudRate_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Lagre ny setting til ini fil
            IniFile iniFile = new IniFile(INIFILE.Location);
            iniFile.Write(INIFILE.BaudRate, cboBaudRate.SelectedItem.ToString(), INIFILE.SerialPortConfigurationSection);
        }

        private void cboDataBits_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Lagre ny setting til ini fil
            IniFile iniFile = new IniFile(INIFILE.Location);
            iniFile.Write(INIFILE.DataBits, cboDataBits.SelectedItem.ToString(), INIFILE.SerialPortConfigurationSection);
        }

        private void cboStopBits_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Lagre ny setting til ini fil
            IniFile iniFile = new IniFile(INIFILE.Location);
            iniFile.Write(INIFILE.StopBits, cboStopBits.SelectedItem.ToString(), INIFILE.SerialPortConfigurationSection);
        }

        private void cboDataParity_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Lagre ny setting til ini fil
            IniFile iniFile = new IniFile(INIFILE.Location);
            iniFile.Write(INIFILE.DataParity, cboDataParity.SelectedItem.ToString(), INIFILE.SerialPortConfigurationSection);
        }

        private void cboHandShake_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Lagre ny setting til ini fil
            IniFile iniFile = new IniFile(INIFILE.Location);
            iniFile.Write(INIFILE.HandShake, cboHandShake.SelectedItem.ToString(), INIFILE.SerialPortConfigurationSection);
        }

        private void chkRawDataAutoScroll_Checked(object sender, RoutedEventArgs e)
        {
            // Lagre ny setting til ini fil
            IniFile iniFile = new IniFile(INIFILE.Location);
            iniFile.Write(INIFILE.RawDataAutoScroll, "1", INIFILE.SerialPortConfigurationSection);

            // Scrolle til bunns
            if (lvRawData.Items.Count > 0)
                lvRawData.ScrollIntoView(lvRawData.Items[lvRawData.Items.Count - 1]);
        }

        private void chkRawDataAutoScroll_Unchecked(object sender, RoutedEventArgs e)
        {
            // Lagre ny setting til ini fil
            IniFile iniFile = new IniFile(INIFILE.Location);
            iniFile.Write(INIFILE.RawDataAutoScroll, "0", INIFILE.SerialPortConfigurationSection);
        }

        private void chkShowControlChars_Checked(object sender, RoutedEventArgs e)
        {
            gShowControlChars = true;

            // Lagre ny setting til ini fil
            IniFile iniFile = new IniFile(INIFILE.Location);
            iniFile.Write(INIFILE.ShowControlChars, "1", INIFILE.SerialPortConfigurationSection);
        }

        private void chkShowControlChars_Unchecked(object sender, RoutedEventArgs e)
        {
            gShowControlChars = false;

            // Lagre ny setting til ini fil
            IniFile iniFile = new IniFile(INIFILE.Location);
            iniFile.Write(INIFILE.ShowControlChars, "0", INIFILE.SerialPortConfigurationSection);
        }

        private void chkRawDataAllClear_Click(object sender, RoutedEventArgs e)
        {
            // Sletter lister
            rawDataItems.Clear();
            selectedPacketsItems.Clear();
            packetDataFieldsItems.Clear();
            selectedDataItems.Clear();
            processedDataItems.Clear();

            // Refresh på lister
            lvRawData.Items.Refresh();
            lvSelectedPackets.Items.Refresh();
            lvPacketData.Items.Refresh();
            lvSelectedData.Items.Refresh();
            lvProcessedData.Items.Refresh();
        }

        private void chkRawDataClear_Click(object sender, RoutedEventArgs e)
        {
            rawDataItems.Clear();
            lvRawData.Items.Refresh();
        }

        private void tbSelectedPacketStart_TextChanged(object sender, TextChangedEventArgs e)
        {
            gPacketStart = tbSelectedPacketStart.Text;

            // Test på om vi har gyldige escape characters
            try
            {
                string test = Regex.Unescape(gPacketStart); // Unescape gjør det mulig å skrive escape characters i input feltet
            }
            catch (Exception ex)
            {
                gPacketStart = String.Empty;

                //MessageBox.Show(ex.Message, "Input Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            // Lagre ny setting til ini fil
            IniFile iniFile = new IniFile(INIFILE.Location);
            iniFile.Write(INIFILE.SelectedPacketsStart, StringOps.EscapeSpace(gPacketStart), INIFILE.SerialPortConfigurationSection);
        }

        private void tbSelectedPacketEnd_TextChanged(object sender, TextChangedEventArgs e)
        {
            gPacketEnd = tbSelectedPacketEnd.Text;

            // Test på om vi har gyldige escape characters
            try
            {
                string test = Regex.Unescape(gPacketEnd); // Unescape gjør det mulig å skrive escape characters i input feltet
            }
            catch (Exception ex)
            {
                gPacketEnd = String.Empty;

                //MessageBox.Show(ex.Message, "Input Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            // Lagre ny setting til ini fil
            IniFile iniFile = new IniFile(INIFILE.Location);
            iniFile.Write(INIFILE.SelectedPacketsEnd, StringOps.EscapeSpace(gPacketEnd), INIFILE.SerialPortConfigurationSection);
        }

        private void chkSelectedPacketsAutoScroll_Checked(object sender, RoutedEventArgs e)
        {
            // Lagre ny setting til ini fil
            IniFile iniFile = new IniFile(INIFILE.Location);
            iniFile.Write(INIFILE.SelectedPacketsAutoScroll, "1", INIFILE.SerialPortConfigurationSection);

            // Scrolle til bunns
            if (lvSelectedPackets.Items.Count > 0)
                lvSelectedPackets.ScrollIntoView(lvSelectedPackets.Items[lvSelectedPackets.Items.Count - 1]);
        }

        private void chkSelectedPacketsAutoScroll_Unchecked(object sender, RoutedEventArgs e)
        {
            // Lagre ny setting til ini fil
            IniFile iniFile = new IniFile(INIFILE.Location);
            iniFile.Write(INIFILE.SelectedPacketsAutoScroll, "0", INIFILE.SerialPortConfigurationSection);
        }

        private void chkSelectedPacketsClear_Click(object sender, RoutedEventArgs e)
        {
            selectedPacketsItems.Clear();
            lvSelectedPackets.Items.Refresh();
        }

        private void chkPacketDataAutoScroll_Checked(object sender, RoutedEventArgs e)
        {
            // Lagre ny setting til ini fil
            IniFile iniFile = new IniFile(INIFILE.Location);
            iniFile.Write(INIFILE.PacketDataAutoScroll, "1", INIFILE.SerialPortConfigurationSection);

            // Scrolle til bunns
            if (lvPacketData.Items.Count > 0)
                lvPacketData.ScrollIntoView(lvPacketData.Items[lvPacketData.Items.Count - 1]);
        }

        private void chkPacketDataAutoScroll_Unchecked(object sender, RoutedEventArgs e)
        {
            // Lagre ny setting til ini fil
            IniFile iniFile = new IniFile(INIFILE.Location);
            iniFile.Write(INIFILE.PacketDataAutoScroll, "0", INIFILE.SerialPortConfigurationSection);
        }

        private void tbPacketDataDelimiter_TextChanged(object sender, TextChangedEventArgs e)
        {
            gPacketDelimiter = tbPacketDataDelimiter.Text;

            // Test på om vi har gyldige escape characters
            try
            {
                string test = Regex.Unescape(gPacketDelimiter); // Unescape gjør det mulig å skrive escape characters i input feltet
            }
            catch (Exception ex)
            {
                gPacketDelimiter = String.Empty;

                //MessageBox.Show(ex.Message, "Input Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            // Lagre ny setting til ini fil
            IniFile iniFile = new IniFile(INIFILE.Location);
            iniFile.Write(INIFILE.PacketDataDelimiter, StringOps.EscapeSpace(gPacketDelimiter), INIFILE.SerialPortConfigurationSection);
        }

        private void chkPacketDataClear_Click(object sender, RoutedEventArgs e)
        {
            packetDataFieldsItems.Clear();
            lvPacketData.Items.Refresh();
        }

        private void cboSelectedDataField_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            gSelectedDataField = cboSelectedDataField.SelectedIndex;

            // Lagre ny setting til ini fil
            IniFile iniFile = new IniFile(INIFILE.Location);
            iniFile.Write(INIFILE.SelectedDataField, cboSelectedDataField.SelectedItem.ToString(), INIFILE.SerialPortConfigurationSection);
        }

        private void cboSelectedDataAutoExtract_Checked(object sender, RoutedEventArgs e)
        {
            gSelectedDataAutoExtraction = true;

            // Lagre ny setting til ini fil
            IniFile iniFile = new IniFile(INIFILE.Location);
            iniFile.Write(INIFILE.SelectedDataAutoExtraction, "1", INIFILE.SerialPortConfigurationSection);
        }

        private void cboSelectedDataAutoExtract_Unchecked(object sender, RoutedEventArgs e)
        {
            gSelectedDataAutoExtraction = false;

            // Lagre ny setting til ini fil
            IniFile iniFile = new IniFile(INIFILE.Location);
            iniFile.Write(INIFILE.SelectedDataAutoExtraction, "0", INIFILE.SerialPortConfigurationSection);
        }

        private void chkSelectedDataAutoScroll_Checked(object sender, RoutedEventArgs e)
        {
            // Lagre ny setting til ini fil
            IniFile iniFile = new IniFile(INIFILE.Location);
            iniFile.Write(INIFILE.SelectedDataAutoScroll, "1", INIFILE.SerialPortConfigurationSection);

            // Scrolle til bunns
            if (lvSelectedData.Items.Count > 0)
                lvSelectedData.ScrollIntoView(lvSelectedData.Items[lvSelectedData.Items.Count - 1]);
        }
        private void chkSelectedDataAutoScroll_Unchecked(object sender, RoutedEventArgs e)
        {
            // Lagre ny setting til ini fil
            IniFile iniFile = new IniFile(INIFILE.Location);
            iniFile.Write(INIFILE.SelectedDataAutoScroll, "0", INIFILE.SerialPortConfigurationSection);
        }

        private void chkSelectedDataClear_Click(object sender, RoutedEventArgs e)
        {
            selectedDataItems.Clear();
            lvSelectedData.Items.Refresh();
        }

        private void cboProcessType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            gProcessing.type = (ProcessingType)cboProcessingType.SelectedIndex;

            // Lagre ny setting til ini fil
            IniFile iniFile = new IniFile(INIFILE.Location);
            iniFile.Write(INIFILE.ProcessingType, cboProcessingType.SelectedItem.ToString(), INIFILE.SerialPortConfigurationSection);
        }

        private void tbProcessingParameter_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!String.IsNullOrEmpty(tbProcessingParameter.Text))
                gProcessing.parameter = double.Parse(tbProcessingParameter.Text, cultureInfo);
            else
                gProcessing.parameter = 0;

            // Lagre ny setting til ini fil
            IniFile iniFile = new IniFile(INIFILE.Location);
            iniFile.Write(INIFILE.ProcessingParameter, gProcessing.parameter.ToString(), INIFILE.SerialPortConfigurationSection);
        }

        private void chkProcessedDataAutoScroll_Checked(object sender, RoutedEventArgs e)
        {
            // Lagre ny setting til ini fil
            IniFile iniFile = new IniFile(INIFILE.Location);
            iniFile.Write(INIFILE.ProcessingAutoScroll, "1", INIFILE.SerialPortConfigurationSection);

            // Scrolle til bunns
            if (lvProcessedData.Items.Count > 0)
                lvProcessedData.ScrollIntoView(lvProcessedData.Items[lvProcessedData.Items.Count - 1]);
        }

        private void chkProcessedDataAutoScroll_Unchecked(object sender, RoutedEventArgs e)
        {
            // Lagre ny setting til ini fil
            IniFile iniFile = new IniFile(INIFILE.Location);
            iniFile.Write(INIFILE.ProcessingAutoScroll, "0", INIFILE.SerialPortConfigurationSection);
        }

        private void chkProcessedDataClear_Click(object sender, RoutedEventArgs e)
        {
            processedDataItems.Clear();
            lvProcessedData.Items.Refresh();
        }

        private void tbSelectedPacketStart_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                gPacketStart = tbSelectedPacketStart.Text;

                // Test på om vi har gyldige escape characters
                try
                {
                    string test = Regex.Unescape(gPacketStart); // Unescape gjør det mulig å skrive escape characters i input feltet
                }
                catch (Exception ex)
                {
                    gPacketStart = String.Empty;

                    //MessageBox.Show(ex.Message, "Input Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }

                // Lagre ny setting til ini fil
                IniFile iniFile = new IniFile(INIFILE.Location);
                iniFile.Write(INIFILE.SelectedPacketsStart, StringOps.EscapeSpace(gPacketStart), INIFILE.SerialPortConfigurationSection);
            }
        }

        private void tbSelectedPacketEnd_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                gPacketEnd = tbSelectedPacketEnd.Text;

                // Test på om vi har gyldige escape characters
                try
                {
                    string test = Regex.Unescape(gPacketEnd); // Unescape gjør det mulig å skrive escape characters i input feltet
                }
                catch (Exception ex)
                {
                    gPacketEnd = String.Empty;

                    //MessageBox.Show(ex.Message, "Input Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }

                // Lagre ny setting til ini fil
                IniFile iniFile = new IniFile(INIFILE.Location);
                iniFile.Write(INIFILE.SelectedPacketsEnd, StringOps.EscapeSpace(gPacketEnd), INIFILE.SerialPortConfigurationSection);
            }
        }
    }

    public class Packet
    {
        public string data { get; set; }
    }

    public class PacketDataField
    {
        public string[] Value { get; set; } = new string[Constants.PacketDataFields];
    }

    public class SelectedData
    {
        public string selectedDataString { get; set; }
    }

    public class ProcessedData
    {
        public string processedDataString { get; set; }
    }
}
