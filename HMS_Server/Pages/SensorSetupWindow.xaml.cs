using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Telerik.Windows.Controls;

namespace HMS_Server
{
    /// <summary>
    /// Interaction logic for SensorSetupWindow.xaml
    /// </summary>
    public partial class SensorSetupWindow : RadWindow
    {
        // Configuration settings
        Config config;

        // Error Handler
        private ErrorHandler errorHandler;

        // Sensor Data List
        private RadObservableCollectionEx<SensorData> sensorDataList = new RadObservableCollectionEx<SensorData>();
        private SensorData sensorDataSelected = new SensorData();

        // List view sorting
        Sorter sorter = new Sorter();

        public SensorSetupWindow(Config config, ErrorHandler errorHandler/*, int preSelectedSensorDataID*/)
        {
            InitializeComponent();

            // Config
            this.config = config;

            // Error Handler
            this.errorHandler = errorHandler;

            InitUI(/*preSelectedSensorDataID*/);
        }

        public void InitUI(/*int preSelectedSensorDataID*/)
        {
            // Liste med sensor verdier
            gvSensorSetupItems.ItemsSource = sensorDataList;

            // Fylle sensor type combobox
            foreach (var value in Enum.GetValues(typeof(SensorType)))
                cboSensorType.Items.Add(value.ToString());

            // Sette default verdi
            cboSensorType.Text = cboSensorType.Items[0].ToString();

            // Fylle database option save frequency combobox
            foreach (var value in Enum.GetValues(typeof(DatabaseSaveFrequency)))
                cboDBStorageFrequencyType.Items.Add(value.ToString());

            // Laste sensor data
            LoadSensorData();

            // Forhåndsvalgt sensor data
            // NB! Denne koden fungerer ikke etter implementasjon av telerik grensesnittet.
            // gvSensorSetupItems.Items.Count oppdateres ikke umiddelbart og står med 0 når vi kommer her.
            //if (preSelectedSensorDataID != -1)
            //{
            //    //gvSensorSetupItems.Items.Refresh();

            //    var items = sensorDataList.Where(x => x.id == preSelectedSensorDataID);
            //    int index = sensorDataList.IndexOf(items.First());
            //    if (index >= 0 && index < gvSensorSetupItems.Items.Count)
            //    {
            //        gvSensorSetupItems.SelectedItem = gvSensorSetupItems.Items[index];
            //        gvSensorSetupItems.UpdateLayout();
            //    }
            //}
        }

        void LoadSensorData()
        {
            // Hente liste med data fra fil
            SensorConfigCollection sensorConfigCollection = config.GetAllSensorData();

            if (sensorConfigCollection != null)
            {
                // Legge alle data inn i listview for visning på skjerm
                foreach (SensorConfig item in sensorConfigCollection)
                {
                    SensorData sensorData = new SensorData(item);

                    sensorDataList.Add(sensorData);
                }
            }
        }

        private void btnNew_Click(object sender, RoutedEventArgs e)
        {
            // Legge inne nytt tomt sensor verdi objekt
            SensorData newSensor = new SensorData();

            // Sette data
            newSensor.id = Constants.SensorIDNotSet;    // Settes automatisk når nye data lagres til fil i config.NewData
            newSensor.name = "New Sensor Value";
            newSensor.description = "(description)";
            newSensor.type = SensorType.None;
            newSensor.saveToDatabase = false;
            newSensor.saveFreq = DatabaseSaveFrequency.Sensor;

            // Legge i listen
            sensorDataList.Add(newSensor);

            // Skrive til fil
            config.NewData(sensorDataList[sensorDataList.Count - 1]);

            // Sette ny item som selected (vil også laste data ettersom SelectionChange under kalles automatisk når vi gjør dette)
            int index = gvSensorSetupItems.Items.IndexOf(newSensor);
            gvSensorSetupItems.SelectedItem = gvSensorSetupItems.Items[index];
        }

        private void btnCopy_Click(object sender, RoutedEventArgs e)
        {
            // Nytt sensor verdi objekt
            SensorData newSensor = new SensorData();

            // Sette data fra selected sensor value objekt
            newSensor.id = Constants.SensorIDNotSet;    // Settes automatisk når nye data lagres til fil i config.NewData
            newSensor.name = string.Format("{0} (copy)", sensorDataSelected.name);
            newSensor.type = sensorDataSelected.type;
            newSensor.description = sensorDataSelected.description;
            newSensor.saveToDatabase = sensorDataSelected.saveToDatabase;
            newSensor.saveFreq = sensorDataSelected.saveFreq;

            switch (sensorDataSelected.type)
            {
                case SensorType.None:
                    break;

                case SensorType.SerialPort:
                    newSensor.serialPort = new SerialPortSetup(sensorDataSelected.serialPort);
                    break;

                case SensorType.ModbusRTU:
                case SensorType.ModbusASCII:
                case SensorType.ModbusTCP:
                    newSensor.modbus = new ModbusSetup(sensorDataSelected.modbus);
                    break;

                case SensorType.FileReader:
                    newSensor.fileReader = new FileReaderSetup(sensorDataSelected.fileReader);
                    break;
            }

            // Legge i listen
            sensorDataList.Add(newSensor);

            // Skrive til fil
            config.NewData(sensorDataList[sensorDataList.Count - 1]);

            // Sette ny item som selected (vil også laste data ettersom SelectionChange under kalles automatisk når vi gjør dette)
            int index = gvSensorSetupItems.Items.IndexOf(newSensor);
            gvSensorSetupItems.SelectedItem = gvSensorSetupItems.Items[index];
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            RadWindow.Confirm("Delete the selected item?", OnClosed);

            void OnClosed(object sendero, WindowClosedEventArgs ea)
            {
                if ((bool)ea.DialogResult == true)
                {
                    // Slette fra fil
                    config.DeleteData(sensorDataSelected);

                    // Slette fra listen
                    sensorDataList.Remove(sensorDataSelected);
                    //((List<SensorData>)gvSensorSetupsItems.ItemsSource).Remove(sensorDataSelected);

                    // Sette item 0 som selected (vil også laste data ettersom SelectionChange under kalles automatisk når vi gjør dette)
                    gvSensorSetupItems.SelectedItem = gvSensorSetupItems.Items[0];
                }
            }
        }

        private void gvSensorSetupItems_SelectionChanged(object sender, SelectionChangeEventArgs e)
        {
            sensorDataSelected = (sender as RadGridView).SelectedItem as SensorData; // <------------- Elegant kode

            if (sensorDataSelected != null)
            {
                // Basic Information
                lbSensorID.Content = sensorDataSelected.id.ToString();
                tbSensorName.Text = sensorDataSelected.name;
                tbSensorDescription.Text = sensorDataSelected.description;
                cboSensorType.Text = sensorDataSelected.type.ToString();

                // Laste Sensor Setup UI for valgt sensor type
                UISensorSetup_Load(sensorDataSelected.type);
            }
            else
            {
                lbSensorID.Content = string.Empty;
                tbSensorName.Text = string.Empty;
                cboSensorType.Text = SensorType.None.ToString();
                tbSensorDescription.Text = string.Empty;
                cboDBStorageFrequencyType.Text = string.Empty;

                UISensorSetup_Load(SensorType.None);
            }
        }

        private void tbSensorName_LostFocus(object sender, RoutedEventArgs e)
        {
            tbSensorName_Update(sender);
        }

        private void tbSensorName_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                tbSensorName_Update(sender);
                Keyboard.ClearFocus();
            }
        }

        private void tbSensorName_Update(object sender)
        {
            if (sensorDataSelected != null)
            {
                sensorDataSelected.name = (sender as TextBox).Text;

                // Oppdatere config fil
                config.SetData(sensorDataSelected);
            }
        }

        private void cboSensorType_DropDownClosed(object sender, EventArgs e)
        {
            // Ny valgt sensor type
            SensorType newSensorType = (SensorType)(sender as RadComboBox).SelectedIndex;

            // Er sensor data satt fra listen?
            if (sensorDataSelected != null)
            {
                // Dersom samme sensor blir valgt gjør vi ingenting
                // Trenger helle ikke gjøre noe dersom vi bytter mellom MODBUS RTU og ASCII
                switch (newSensorType)
                {
                    case SensorType.None:
                    case SensorType.SerialPort:
                    case SensorType.FileReader:
                        if (newSensorType != sensorDataSelected.type)
                        {
                            ChangeSensorTypeWithWarning(newSensorType);
                        }
                        break;

                    case SensorType.ModbusRTU:
                    case SensorType.ModbusASCII:
                    case SensorType.ModbusTCP:
                        if (sensorDataSelected.type == SensorType.ModbusRTU ||
                            sensorDataSelected.type == SensorType.ModbusASCII ||
                            sensorDataSelected.type == SensorType.ModbusTCP)
                        {
                            ChangeSensorType(newSensorType);
                        }
                        else
                        {
                            ChangeSensorTypeWithWarning(newSensorType);
                        }
                        break;
                }
            }
        }

        private void ChangeSensorTypeWithWarning(SensorType newSensorType)
        {
            MessageBoxResult result = MessageBoxResult.OK;

            // Varsel - men ikke dersom None var satt og vi velger noe annet
            if (sensorDataSelected.type != SensorType.None)
            {
                RadWindow.Confirm("Changing the sensor type will remove all\nsettings for the currently selected sensor.", OnClosed);

                void OnClosed(object sender, WindowClosedEventArgs e)
                {
                    if ((bool)e.DialogResult == true)
                        result = MessageBoxResult.OK;
                    else
                        result = MessageBoxResult.Cancel;
                }
            }

            switch (result)
            {
                case MessageBoxResult.OK:
                    // Sette ny sensor type
                    sensorDataSelected.type = newSensorType;

                    switch (newSensorType)
                    {
                        case SensorType.SerialPort:
                            // Sensor Data Options
                            sensorDataSelected.saveFreq = DatabaseSaveFrequency.Sensor;
                            break;

                        case SensorType.ModbusRTU:
                        case SensorType.ModbusASCII:
                        case SensorType.ModbusTCP:
                            // Sensor Data Options
                            sensorDataSelected.saveFreq = DatabaseSaveFrequency.Program;

                            //// Default settings
                            //sensorDataSelected.modbus.slaveID = 1;
                            //sensorDataSelected.modbus.startAddress = Constants.ModbusInputRegisterMin;
                            //sensorDataSelected.modbus.totalAddresses = 10;
                            //sensorDataSelected.modbus.dataAddress = Constants.ModbusInputRegisterMin;
                            break;

                        case SensorType.FileReader:
                            // Sensor Data Options
                            sensorDataSelected.saveFreq = DatabaseSaveFrequency.Program;
                            break;
                    }

                    // Lagre til fil
                    config.SetData(sensorDataSelected);

                    // Laste UI for valgt sensor type
                    UISensorSetup_Load(sensorDataSelected.type);

                    break;

                case MessageBoxResult.Cancel:
                    // Tilbakestill tekst
                    cboSensorType.Text = sensorDataSelected.type.ToString();
                    break;
            }
        }

        private void ChangeSensorType(SensorType newSensorType)
        {
            // Sette ny sensor type
            sensorDataSelected.type = newSensorType;

            // Lagre til fil
            config.SetData(sensorDataSelected);

            // Laste UI for valgt sensor type
            UISensorSetup_Load(sensorDataSelected.type);
        }

        private void tbSensorDescription_LostFocus(object sender, RoutedEventArgs e)
        {
            tbSensorDescription_Update(sender);
        }

        private void tbSensorDescription_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                tbSensorDescription_Update(sender);
                Keyboard.ClearFocus();
            }
        }

        private void tbSensorDescription_Update(object sender)
        {
            if (sensorDataSelected != null)
            {
                sensorDataSelected.description = (sender as TextBox).Text;

                // Lagre til fil
                config.SetData(sensorDataSelected);
            }
        }

        private void chkSaveToDatabase_Checked(object sender, RoutedEventArgs e)
        {
            sensorDataSelected.saveToDatabase = true;

            // Lagre til fil
            config.SetData(sensorDataSelected);
        }

        private void chkSaveToDatabase_Unchecked(object sender, RoutedEventArgs e)
        {
            sensorDataSelected.saveToDatabase = false;

            // Lagre til fil
            config.SetData(sensorDataSelected);
        }

        private void cboDBStorageFrequencyType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sensorDataSelected != null &&
                (sender as RadComboBox).SelectedItem != null)
            {
                sensorDataSelected.saveFreq = (DatabaseSaveFrequency)Enum.Parse(typeof(DatabaseSaveFrequency), (sender as RadComboBox).SelectedItem.ToString());

                // Lagre til fil
                config.SetData(sensorDataSelected);
            }
        }

        private void btnSerialPortSetup_Click(object sender, RoutedEventArgs e)
        {
            // Open new modal window 
            SerialPortSetupWindow newWindow = new SerialPortSetupWindow(sensorDataSelected, sensorDataList, config, errorHandler);
            newWindow.Owner = App.Current.MainWindow;
            newWindow.Closed += SerialPortSetupWindow_Closed;
            newWindow.ShowDialog();

            // Lagre til disk når vi går ut av vinduet
            config.SetData(sensorDataSelected);

            // Serial Port settings sync
            SyncSerialPortSettings(sensorDataSelected);
        }

        private void btnModbusSetup_Click(object sender, RoutedEventArgs e)
        {
            // Open new modal window 
            ModbusSetupWindow newWindow = new ModbusSetupWindow(sensorDataSelected, sensorDataList, config, errorHandler);
            newWindow.Owner = App.Current.MainWindow;
            newWindow.Closed += ModbusSetupWindow_Closed;
            newWindow.ShowDialog();

            // Lagre til disk når vi går ut av vinduet
            config.SetData(sensorDataSelected);

            // Serial Port settings sync
            SyncSerialPortSettings(sensorDataSelected);
        }

        private void btnFileReaderSetup_Click(object sender, RoutedEventArgs e)
        {
            // Open new modal window 
            FileReaderSetupWindow newWindow = new FileReaderSetupWindow(sensorDataSelected, config, errorHandler);
            newWindow.Owner = App.Current.MainWindow;
            newWindow.Closed += FileReaderSetupWindow_Closed;
            newWindow.ShowDialog();

            // Lagre til disk når vi går ut av vinduet
            config.SetData(sensorDataSelected);
        }

        private void SyncSerialPortSettings(SensorData sensorData)
        {
            // Går igjennom alle sensor data og finne de som er satt på samme COM port -> oppdatere settings slik at alle er samkjørt
            foreach (var item in sensorDataList)
            {
                // Same type?
                if (item.type == sensorData.type)
                {
                    // Hvilken type?
                    switch (item.type)
                    {
                        case SensorType.SerialPort:
                            if (item.serialPort.portName == sensorData.serialPort.portName)
                            {
                                // Kopiere settings 
                                item.serialPort.portName = sensorData.serialPort.portName;
                                item.serialPort.baudRate = sensorData.serialPort.baudRate;
                                item.serialPort.dataBits = sensorData.serialPort.dataBits;
                                item.serialPort.stopBits = sensorData.serialPort.stopBits;
                                item.serialPort.parity = sensorData.serialPort.parity;
                                item.serialPort.handshake = sensorData.serialPort.handshake;

                                // Lagre til fil
                                config.SetData(item);
                            }
                            break;

                        case SensorType.ModbusRTU:
                        case SensorType.ModbusASCII:
                            if (item.modbus.portName == sensorData.modbus.portName)
                            {
                                // Kopiere settings 
                                item.modbus.portName = sensorData.modbus.portName;
                                item.modbus.baudRate = sensorData.modbus.baudRate;
                                item.modbus.dataBits = sensorData.modbus.dataBits;
                                item.modbus.stopBits = sensorData.modbus.stopBits;
                                item.modbus.parity = sensorData.modbus.parity;
                                item.modbus.handshake = sensorData.modbus.handshake;

                                // Lagre til fil
                                config.SetData(item);
                            }
                            break;

                        default:
                            break;
                    }
                }
            }
        }

        private void SyncFileReaderSettings(SensorData sensorData)
        {
            // Går igjennom alle sensor data og finne de som er satt til å lese fra samme file
            // -> oppdatere settings slik at alle er samkjørt
            foreach (var item in sensorDataList)
            {
                // Same type?
                if (item.type == sensorData.type)
                {
                    if (item.fileReader.fileFolder == sensorData.fileReader.fileFolder &&
                        item.fileReader.fileName == sensorData.fileReader.fileName)
                    {
                        // Kopiere settings 
                        item.serialPort.portName = sensorData.serialPort.portName;
                        item.serialPort.baudRate = sensorData.serialPort.baudRate;
                        item.serialPort.dataBits = sensorData.serialPort.dataBits;
                        item.serialPort.stopBits = sensorData.serialPort.stopBits;
                        item.serialPort.parity = sensorData.serialPort.parity;
                        item.serialPort.handshake = sensorData.serialPort.handshake;

                        // Lagre til fil
                        config.SetData(item);
                    }
                }
            }
        }

        void SerialPortSetupWindow_Closed(object sender, EventArgs e)
        {
            // Laste inn sensor setup data på nytt
            UILoadData_SerialPort();
        }

        void ModbusSetupWindow_Closed(object sender, EventArgs e)
        {
            // Laste inn sensor setup data på nytt
            UILoadData_MODBUS();
        }

        void FileReaderSetupWindow_Closed(object sender, EventArgs e)
        {
            // Laste inn sensor setup data på nytt
            UILoadData_FileReader();
        }

        private void UISensorSetup_Load(SensorType type)
        {
            switch (type)
            {
                case SensorType.None:
                    // Vise UI
                    gbSetupSummaryNone.Visibility = Visibility.Visible;
                    gbSetupSummarySerialPort.Visibility = Visibility.Collapsed;
                    gbSetupSummaryModbus.Visibility = Visibility.Collapsed;
                    gbSetupSummaryFileReader.Visibility = Visibility.Collapsed;

                    btnSerialPortSetup.Visibility = Visibility.Collapsed;
                    btnModbusSetup.Visibility = Visibility.Collapsed;
                    btnFileReaderSetup.Visibility = Visibility.Collapsed;
                    break;

                case SensorType.SerialPort:
                    // Vise UI
                    gbSetupSummaryNone.Visibility = Visibility.Collapsed;
                    gbSetupSummarySerialPort.Visibility = Visibility.Visible;
                    gbSetupSummaryModbus.Visibility = Visibility.Collapsed;
                    gbSetupSummaryFileReader.Visibility = Visibility.Collapsed;

                    btnSerialPortSetup.Visibility = Visibility.Visible;
                    btnModbusSetup.Visibility = Visibility.Collapsed;
                    btnFileReaderSetup.Visibility = Visibility.Collapsed;

                    // Laste data inn i UI
                    UILoadData_SerialPort();
                    break;

                case SensorType.ModbusRTU:
                case SensorType.ModbusASCII:
                case SensorType.ModbusTCP:
                    // Vise UI
                    gbSetupSummaryNone.Visibility = Visibility.Collapsed;
                    gbSetupSummarySerialPort.Visibility = Visibility.Collapsed;
                    gbSetupSummaryModbus.Visibility = Visibility.Visible;
                    gbSetupSummaryFileReader.Visibility = Visibility.Collapsed;

                    btnSerialPortSetup.Visibility = Visibility.Collapsed;
                    btnModbusSetup.Visibility = Visibility.Visible;
                    btnFileReaderSetup.Visibility = Visibility.Collapsed;

                    if (type == SensorType.ModbusTCP)
                    {
                        dbModbusPortName.IsEnabled = false;
                        lbModbusPortName.Visibility = Visibility.Hidden;

                        dbModbusBaudRate.IsEnabled = false;
                        lbModbusBaudRate.Visibility = Visibility.Hidden;

                        dbModbusDataBits.IsEnabled = false;
                        lbModbusDataBits.Visibility = Visibility.Hidden;

                        dbModbusStopBits.IsEnabled = false;
                        lbModbusStopBits.Visibility = Visibility.Hidden;

                        dbModbusHandShake.IsEnabled = false;
                        lbModbusHandShake.Visibility = Visibility.Hidden;

                        dbModbusParity.IsEnabled = false;
                        lbModbusParity.Visibility = Visibility.Hidden;


                        dpModbusTCPAddress.IsEnabled = true;
                        lbModbusTCPAddress.Visibility = Visibility.Visible;

                        dpModbusTCPPort.IsEnabled = true;
                        lbModbusTCPPort.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        dbModbusPortName.IsEnabled = true;
                        lbModbusPortName.Visibility = Visibility.Visible;

                        dbModbusBaudRate.IsEnabled = true;
                        lbModbusBaudRate.Visibility = Visibility.Visible;

                        dbModbusDataBits.IsEnabled = true;
                        lbModbusDataBits.Visibility = Visibility.Visible;

                        dbModbusStopBits.IsEnabled = true;
                        lbModbusStopBits.Visibility = Visibility.Visible;

                        dbModbusHandShake.IsEnabled = true;
                        lbModbusHandShake.Visibility = Visibility.Visible;

                        dbModbusParity.IsEnabled = true;
                        lbModbusParity.Visibility = Visibility.Visible;


                        dpModbusTCPAddress.IsEnabled = false;
                        lbModbusTCPAddress.Visibility = Visibility.Hidden;

                        dpModbusTCPPort.IsEnabled = false;
                        lbModbusTCPPort.Visibility = Visibility.Hidden;
                    }

                    // Laste data inn i UI
                    UILoadData_MODBUS();
                    break;

                case SensorType.FileReader:
                    // Vise UI
                    gbSetupSummaryNone.Visibility = Visibility.Collapsed;
                    gbSetupSummarySerialPort.Visibility = Visibility.Collapsed;
                    gbSetupSummaryModbus.Visibility = Visibility.Collapsed;
                    gbSetupSummaryFileReader.Visibility = Visibility.Visible;

                    btnSerialPortSetup.Visibility = Visibility.Collapsed;
                    btnModbusSetup.Visibility = Visibility.Collapsed;
                    btnFileReaderSetup.Visibility = Visibility.Visible;

                    // Laste data inn i UI
                    UILoadData_FileReader();
                    break;

                default:
                    break;
            }
        }

        public void UILoadData_SerialPort()
        {
            // Fylle database option save frequency combobox
            cboDBStorageFrequencyType.Items.Clear();
            foreach (var value in Enum.GetValues(typeof(DatabaseSaveFrequency)))
                cboDBStorageFrequencyType.Items.Add(value.ToString());

            lbSerialPortName.Content = sensorDataSelected.serialPort.portName;
            lbSerialPortBaudRate.Content = sensorDataSelected.serialPort.baudRate.ToString();
            lbSerialPortDataBits.Content = sensorDataSelected.serialPort.dataBits.ToString();
            lbSerialPortStopBits.Content = sensorDataSelected.serialPort.stopBits.ToString();
            lbSerialPortHandShake.Content = sensorDataSelected.serialPort.handshake.ToString();
            lbSerialPortParity.Content = sensorDataSelected.serialPort.parity.ToString();

            lbSerialPortPacketHeader.Content = sensorDataSelected.serialPort.packetHeader;
            lbSerialPortPacketEnd.Content = sensorDataSelected.serialPort.packetEnd;
            lbSerialPortDelimiter.Content = sensorDataSelected.serialPort.packetDelimiter;
            lbSerialPortCombineFields.Content = sensorDataSelected.serialPort.packetCombineFields;

            lbSerialPortFixedPosData.Content = sensorDataSelected.serialPort.fixedPosData.ToString();
            lbSerialPortFixedPosStart.Content = sensorDataSelected.serialPort.fixedPosStart.ToString();
            lbSerialPortFixedPosTotal.Content = sensorDataSelected.serialPort.fixedPosTotal.ToString();

            lbSerialPortDataField.Content = sensorDataSelected.serialPort.dataField;
            lbSerialPortDecimalSeparator.Content = sensorDataSelected.serialPort.decimalSeparator.ToString();
            lbSerialPortAutoExtractValue.Content = sensorDataSelected.serialPort.autoExtractValue.ToString();

            lbSerialPortCalculationType1.Content = sensorDataSelected.serialPort.calculationSetup[0].type.GetDescription();
            lbSerialPortCalculationParameter1.Content = sensorDataSelected.serialPort.calculationSetup[0].parameter.ToString();

            lbSerialPortCalculationType2.Content = sensorDataSelected.serialPort.calculationSetup[1].type.GetDescription();
            lbSerialPortCalculationParameter2.Content = sensorDataSelected.serialPort.calculationSetup[1].parameter.ToString();

            lbSerialPortCalculationType3.Content = sensorDataSelected.serialPort.calculationSetup[2].type.GetDescription();
            lbSerialPortCalculationParameter3.Content = sensorDataSelected.serialPort.calculationSetup[2].parameter.ToString();

            lbSerialPortCalculationType4.Content = sensorDataSelected.serialPort.calculationSetup[3].type.GetDescription();
            lbSerialPortCalculationParameter4.Content = sensorDataSelected.serialPort.calculationSetup[3].parameter.ToString();

            if (sensorDataSelected.saveToDatabase)
                chkSaveToDatabase.IsChecked = true;
            else
                chkSaveToDatabase.IsChecked = false;

            cboDBStorageFrequencyType.Text = sensorDataSelected.saveFreq.ToString();
        }

        public void UILoadData_MODBUS()
        {
            // Fylle sensor data option save frequency combobox
            // Frekvensen "Sensor" skal ikke brukes for MODBUS sensorer
            cboDBStorageFrequencyType.Items.Clear();
            foreach (var value in Enum.GetValues(typeof(DatabaseSaveFrequency)))
            {
                if (!((sensorDataSelected.type == SensorType.ModbusRTU ||
                            sensorDataSelected.type == SensorType.ModbusASCII ||
                            sensorDataSelected.type == SensorType.ModbusTCP) &&
                        value.ToString() == DatabaseSaveFrequency.Sensor.ToString()))
                {
                    cboDBStorageFrequencyType.Items.Add(value.ToString());
                }
            }

            if (sensorDataSelected.type == SensorType.ModbusRTU ||
                sensorDataSelected.type == SensorType.ModbusASCII)
            {
                lbModbusPortName.Content = sensorDataSelected.modbus.portName;
                lbModbusBaudRate.Content = sensorDataSelected.modbus.baudRate.ToString();
                lbModbusDataBits.Content = sensorDataSelected.modbus.dataBits.ToString();
                lbModbusStopBits.Content = sensorDataSelected.modbus.stopBits.ToString();
                lbModbusHandShake.Content = sensorDataSelected.modbus.handshake.ToString();
                lbModbusParity.Content = sensorDataSelected.modbus.parity.ToString();
            }
            else
            {
                if (sensorDataSelected.type == SensorType.ModbusTCP)
                {
                    lbModbusTCPAddress.Content = sensorDataSelected.modbus.tcpAddress;
                    lbModbusTCPPort.Content = sensorDataSelected.modbus.tcpPort.ToString();
                }
            }

            lbModbusSlaveID.Content = sensorDataSelected.modbus.slaveID.ToString();

            lbModbusDataAddress.Content = sensorDataSelected.modbus.dataAddress.ToString();

            lbModbusCalculationType1.Content = sensorDataSelected.modbus.calculationSetup[0].type.GetDescription();
            lbModbusCalculationParameter1.Content = sensorDataSelected.modbus.calculationSetup[0].parameter.ToString();

            lbModbusCalculationType2.Content = sensorDataSelected.modbus.calculationSetup[1].type.GetDescription();
            lbModbusCalculationParameter2.Content = sensorDataSelected.modbus.calculationSetup[1].parameter.ToString();

            lbModbusCalculationType3.Content = sensorDataSelected.modbus.calculationSetup[2].type.GetDescription();
            lbModbusCalculationParameter3.Content = sensorDataSelected.modbus.calculationSetup[2].parameter.ToString();

            lbModbusCalculationType4.Content = sensorDataSelected.modbus.calculationSetup[3].type.GetDescription();
            lbModbusCalculationParameter4.Content = sensorDataSelected.modbus.calculationSetup[3].parameter.ToString();

            if (sensorDataSelected.saveToDatabase)
                chkSaveToDatabase.IsChecked = true;
            else
                chkSaveToDatabase.IsChecked = false;

            cboDBStorageFrequencyType.Text = sensorDataSelected.saveFreq.ToString();
        }

        public void UILoadData_FileReader()
        {
            lbFileReaderFileFolder.Content = sensorDataSelected.fileReader.fileFolder;
            lbFileReaderFileName.Content = sensorDataSelected.fileReader.fileName;
            lbFileReaderFrequency.Content = sensorDataSelected.fileReader.readFrequency;

            lbFileReaderDelimiter.Content = sensorDataSelected.fileReader.delimiter;
            lbFileReaderPosData.Content = sensorDataSelected.fileReader.fixedPosData.ToString();
            lbFileReaderFixedPosStart.Content = sensorDataSelected.fileReader.fixedPosStart.ToString();
            lbFileReaderFixedPosTotal.Content = sensorDataSelected.fileReader.fixedPosTotal.ToString();

            lbFileReaderDataField.Content = sensorDataSelected.fileReader.dataField;
            lbFileReaderDecimalSeparator.Content = sensorDataSelected.fileReader.decimalSeparator.ToString();
            lbFileReaderAutoExtractValue.Content = sensorDataSelected.fileReader.autoExtractValue.ToString();

            lbFileReaderCalculationType1.Content = sensorDataSelected.fileReader.calculationSetup[0].type.GetDescription();
            lbFileReaderCalculationParameter1.Content = sensorDataSelected.fileReader.calculationSetup[0].parameter.ToString();

            lbFileReaderCalculationType2.Content = sensorDataSelected.fileReader.calculationSetup[1].type.GetDescription();
            lbFileReaderCalculationParameter2.Content = sensorDataSelected.fileReader.calculationSetup[1].parameter.ToString();

            lbFileReaderCalculationType3.Content = sensorDataSelected.fileReader.calculationSetup[2].type.GetDescription();
            lbFileReaderCalculationParameter3.Content = sensorDataSelected.fileReader.calculationSetup[2].parameter.ToString();

            lbFileReaderCalculationType4.Content = sensorDataSelected.fileReader.calculationSetup[3].type.GetDescription();
            lbFileReaderCalculationParameter4.Content = sensorDataSelected.fileReader.calculationSetup[3].parameter.ToString();

        }
    }
}
