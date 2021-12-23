using System;
using System.Net;
using System.Net.Sockets;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Telerik.Windows.Controls;

namespace HMS_Server
{
    /// <summary>
    /// Interaction logic for AdminSettings.xaml
    /// </summary>
    public partial class AdminSettings : UserControl
    {
        // Configuration settings
        private Config config;

        // Database
        private DatabaseHandler database;

        // Settings View Model
        private AdminSettingsVM adminSettingsVM;

        // Error Handler
        private ErrorHandler errorHandler;

        // Sensor Data List
        private RadObservableCollectionEx<SensorData> sensorDataList = new RadObservableCollectionEx<SensorData>();

        // HMS Data
        private RadObservableCollectionEx<HMSData> hmsOutputDataList;

        public AdminSettings()
        {
            InitializeComponent();
        }

        public void Init(AdminSettingsVM adminSettingsVM, Config config, DatabaseHandler database, ErrorHandler errorHandler, RadObservableCollectionEx<SensorData> sensorDataList, RadObservableCollectionEx<HMSData> hmsOutputDataList)
        {
            DataContext = adminSettingsVM;
            this.adminSettingsVM = adminSettingsVM;

            // Config
            this.config = config;

            // Database
            this.database = database;

            // Error Handler
            this.errorHandler = errorHandler;

            // Sensor Data List
            this.sensorDataList = sensorDataList;

            // HMS Data
            this.hmsOutputDataList = hmsOutputDataList;

            InitUI();

            adminSettingsVM.ApplicationRestartRequired(false);
        }

        private void InitUI()
        {
            // Serve Address
            lbSettingsServerAddress.Content = GetIPAddress().ToString();

            // Database User ID
            tbSettingsDatabaseUserID.Text = Encryption.ToInsecureString(Encryption.DecryptString(config.Read(ConfigKey.DatabaseUserID)));

            // Database Password
            tbSettingsDatabasePassword.Text = Encryption.ToInsecureString(Encryption.DecryptString(config.Read(ConfigKey.DatabasePassword)));

            // HMS: Regulation Standard
            foreach (RegulationStandard value in Enum.GetValues(typeof(RegulationStandard)))
                cboRegulationStandard.Items.Add(value.ToString());

            adminSettingsVM.regulationStandard = (RegulationStandard)Enum.Parse(typeof(RegulationStandard), config.Read(ConfigKey.RegulationStandard));

            cboRegulationStandard.SelectedIndex = (int)adminSettingsVM.regulationStandard;
            cboRegulationStandard.Text = adminSettingsVM.regulationStandard.ToString();

            // Helicopter WSI Limits: Helicopter Type
            foreach (HelicopterType value in Enum.GetValues(typeof(HelicopterType)))
                cboHelicopterWSILimitType.Items.Add(value.ToString());

            cboHelicopterWSILimitType.SelectedIndex = 0;
            cboHelicopterWSILimitType.Text = cboHelicopterWSILimitType.Items[0].ToString();

            // Lese limit fra config fil
            HelicopterWSILimitConfigCollection helicopterWSILimitListCollection = config.GetHelicopterWSILimits();

            if (helicopterWSILimitListCollection != null)
            {
                // Legge alle data inn i listview for visning på skjerm
                foreach (HelicopterWSILimitConfig item in helicopterWSILimitListCollection)
                {
                    HelicopterWSILimit helicopterWSILimit = new HelicopterWSILimit(item);
                    adminSettingsVM.helicopterWSILimitList.Add(helicopterWSILimit);
                }
            }

            // Sette limit i text feltet
            if (adminSettingsVM.helicopterWSILimitList.Count > 0)
                tbHelicopterWSILimit.Text = adminSettingsVM.helicopterWSILimitList[0].limit.ToString();

            // NOROG: Helideck Category
            foreach (HelideckCategory value in Enum.GetValues(typeof(HelideckCategory)))
                cboHelideckCategory.Items.Add(value.GetDescription());

            adminSettingsVM.helideckCategory = (HelideckCategory)Enum.Parse(typeof(HelideckCategory), config.Read(ConfigKey.HelideckCategory));

            cboHelideckCategory.Text = adminSettingsVM.helideckCategory.GetDescription();
            cboHelideckCategory.SelectedIndex = (int)adminSettingsVM.helideckCategory;

            // NOROG / CAP Options available
            if (adminSettingsVM.regulationStandard == RegulationStandard.NOROG)
            {
                // CAP
                lbMSICorrectionR.IsEnabled = false;
                tbMSICorrectionR.IsEnabled = false;

                lbHelicopterWSILimitType.IsEnabled = false;
                cboHelicopterWSILimitType.IsEnabled = false;

                lbHelicopterWSILimit.IsEnabled = false;
                tbHelicopterWSILimit.IsEnabled = false;

                lbRestrictedSectorFrom.IsEnabled = false;
                tbRestrictedSectorFrom.IsEnabled = false;
                lbRestrictedSectorTo.IsEnabled = false;
                tbRestrictedSectorTo.IsEnabled = false;
                lbRestrictedSector.IsEnabled = false;

                // Offshore Weather Report
                lbNDBInstalled_CAP.IsEnabled = false;
                cbNDBInstalled_CAP.IsEnabled = false;

                lbNDBFreq_CAP.IsEnabled = false;
                tbNDBFreq_CAP.IsEnabled = false;

                lbNDBIdent_CAP.IsEnabled = false;
                tbNDBIdent_CAP.IsEnabled = false;

                lbTrafficFreq_CAP.IsEnabled = false;
                tbTrafficFreq_CAP.IsEnabled = false;

                lbLogFreq_CAP.IsEnabled = false;
                tbLogFreq_CAP.IsEnabled = false;

                lbMarineChannel.IsEnabled = false;
                tbMarineChannel.IsEnabled = false;
            }
            else
            {
                // NOROG
                cboHelideckCategory.IsEnabled = false;
                lbHelideckCategory.IsEnabled = false;

                // Helideck Report
                lbNDBInstalled_NOROG.IsEnabled = false;
                cbNDBInstalled_NOROG.IsEnabled = false;

                lbNDBFreq_NOROG.IsEnabled = false;
                tbNDBFreq_NOROG.IsEnabled = false;

                lbVHFFreq_NOROG.IsEnabled = false;
                tbVHFFreq_NOROG.IsEnabled = false;
            }
        }

        public static IPAddress GetIPAddress()
        {
            IPAddress[] hostAddresses = Dns.GetHostAddresses("");

            foreach (IPAddress hostAddress in hostAddresses)
            {
                if (hostAddress.AddressFamily == AddressFamily.InterNetwork &&
                    !IPAddress.IsLoopback(hostAddress) &&                       // Ignore loopback addresses
                    !hostAddress.ToString().StartsWith("169.254."))             // Ignore link-local addresses
                    return hostAddress;
            }
            return IPAddress.None;
        }

        private void tbSettingsServerPort_LostFocus(object sender, RoutedEventArgs e)
        {
            tbSettingsServerPort_Update(sender);
        }

        private void tbSettingsServerPort_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                tbSettingsServerPort_Update(sender);
                Keyboard.ClearFocus();
            }
        }

        private void tbSettingsServerPort_Update(object sender)
        {
            // Sjekk av input
            DataValidation.Double(
                (sender as TextBox).Text,
                Constants.PortMin,
                Constants.PortMax,
                Constants.ServerPortDefault,
                out double validatedInput);

            if (adminSettingsVM.serverPort != validatedInput)
                adminSettingsVM.ApplicationRestartRequired();

            adminSettingsVM.serverPort = validatedInput;
        }

        private void tbSettingsProgramSaveFrequency_LostFocus(object sender, RoutedEventArgs e)
        {
            tbSettingsProgramSaveFrequency_Update(sender);
        }

        private void tbSettingsProgramSaveFrequency_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                tbSettingsProgramSaveFrequency_Update(sender);
                Keyboard.ClearFocus();
            }
        }

        private void tbSettingsProgramSaveFrequency_Update(object sender)
        {
            // Sjekk av input
            DataValidation.Double(
                    (sender as TextBox).Text,
                    Constants.ProgramSaveFreqMin,
                    Constants.ProgramSaveFreqMax,
                    Constants.ProgramSaveFreqDefault,
                    out double validatedInput);

            if (adminSettingsVM.programSaveFrequency != validatedInput)
                adminSettingsVM.ApplicationRestartRequired();

            adminSettingsVM.programSaveFrequency = validatedInput;
        }

        private void tbSettingsDataTimeOut_LostFocus(object sender, RoutedEventArgs e)
        {
            tbSettingsDataTimeOut_Update(sender);
        }

        private void tbSettingsDataTimeOut_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                tbSettingsDataTimeOut_Update(sender);
                Keyboard.ClearFocus();
            }
        }

        private void tbSettingsDataTimeOut_Update(object sender)
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

        private void tbSettingsGUIDataLimit_LostFocus(object sender, RoutedEventArgs e)
        {
            tbSettingsGUIDataLimit_Update(sender);
        }

        private void tbSettingsGUIDataLimit_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                tbSettingsGUIDataLimit_Update(sender);
                Keyboard.ClearFocus();
            }
        }

        private void tbSettingsGUIDataLimit_Update(object sender)
        {
            // Sjekk av input
            DataValidation.Double(
                    (sender as TextBox).Text,
                    Constants.GUIDataLimitMin,
                    Constants.GUIDataLimitMax,
                    Constants.GUIDataLimitDefault,
                    out double validatedInput);

            if (adminSettingsVM.guiDataLimit != validatedInput)
                adminSettingsVM.ApplicationRestartRequired();

            adminSettingsVM.guiDataLimit = validatedInput;
        }

        private void tbSettingsDatabaseAddress_LostFocus(object sender, RoutedEventArgs e)
        {
            tbSettingsDatabaseAddress_Update(sender);
        }

        private void tbSettingsDatabaseAddress_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                tbSettingsDatabaseAddress_Update(sender);
                Keyboard.ClearFocus();
            }
        }

        private void tbSettingsDatabaseAddress_Update(object sender)
        {
            DataValidation.IPAddress(
                   (sender as TextBox).Text,
                   Constants.DefaultServerAddress,
                   out string validatedInput);

            if (adminSettingsVM.databaseAddress != validatedInput)
                adminSettingsVM.ApplicationRestartRequired();

            adminSettingsVM.databaseAddress = validatedInput;
        }

        private void tbSettingsDatabasePort_LostFocus(object sender, RoutedEventArgs e)
        {
            tbSettingsDatabasePort_Update(sender);
        }

        private void tbSettingsDatabasePort_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                tbSettingsDatabasePort_Update(sender);
                Keyboard.ClearFocus();
            }
        }

        private void tbSettingsDatabasePort_Update(object sender)
        {
            // Sjekk av input
            DataValidation.Double(
                (sender as TextBox).Text,
                Constants.PortMin,
                Constants.PortMax,
                Constants.ServerPortDefault,
                out double validatedInput);

            if (adminSettingsVM.databasePort != validatedInput)
                adminSettingsVM.ApplicationRestartRequired();

            adminSettingsVM.databasePort = validatedInput;
        }

        private void tbSettingsDatabaseName_LostFocus(object sender, RoutedEventArgs e)
        {
            tbSettingsDatabaseName_Update(sender);
        }

        private void tbSettingsDatabaseName_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                tbSettingsDatabaseName_Update(sender);
                Keyboard.ClearFocus();
            }
        }

        private void tbSettingsDatabaseName_Update(object sender)
        {
            if (adminSettingsVM.databaseName != (sender as TextBox).Text)
            {
                adminSettingsVM.ApplicationRestartRequired();
                adminSettingsVM.databaseName = (sender as TextBox).Text;
            }
        }

        private void tbSettingsDatabaseUserID_LostFocus(object sender, RoutedEventArgs e)
        {
            tbSettingsDatabaseUserID_Update(sender);
        }

        private void tbSettingsDatabaseUserID_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                tbSettingsDatabaseUserID_Update(sender);
                Keyboard.ClearFocus();
            }
        }

        private void tbSettingsDatabaseUserID_Update(object sender)
        {
            if ((sender as TextBox).Text != Encryption.ToInsecureString(Encryption.DecryptString(config.Read(ConfigKey.DatabaseUserID))))
            {
                config.Write(ConfigKey.DatabaseUserID, Encryption.EncryptString(Encryption.ToSecureString((sender as TextBox).Text)));
                adminSettingsVM.ApplicationRestartRequired();
            }
        }

        private void tbSettingsDatabasePassword_LostFocus(object sender, RoutedEventArgs e)
        {
            tbSettingsDatabasePassword_Update(sender);
        }

        private void tbSettingsDatabasePassword_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                tbSettingsDatabasePassword_Update(sender);
                Keyboard.ClearFocus();
            }
        }

        private void tbSettingsDatabasePassword_Update(object sender)
        {
            if ((sender as TextBox).Text != Encryption.ToInsecureString(Encryption.DecryptString(config.Read(ConfigKey.DatabasePassword))))
            {
                config.Write(ConfigKey.DatabasePassword, Encryption.EncryptString(Encryption.ToSecureString((sender as TextBox).Text)));
                adminSettingsVM.ApplicationRestartRequired();
            }
        }

        private void tbSettingsDatabaseDataStorageTime_LostFocus(object sender, RoutedEventArgs e)
        {
            tbSettingsDatabaseDataStorageTime_Update(sender);
        }

        private void tbSettingsDatabaseDataStorageTime_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                tbSettingsDatabaseDataStorageTime_Update(sender);
                Keyboard.ClearFocus();
            }
        }

        private void tbSettingsDatabaseDataStorageTime_Update(object sender)
        {
            // Sjekk av input
            DataValidation.Double(
                    (sender as TextBox).Text,
                    Constants.DatabaseStorageTimeMin,
                    Constants.DatabaseStorageTimeMax,
                    Constants.DatabaseStorageTimeDefault,
                    out double validatedInput);

            if (adminSettingsVM.databaseDataStorageTime != validatedInput)
                adminSettingsVM.ApplicationRestartRequired();

            adminSettingsVM.databaseDataStorageTime = validatedInput;
        }

        private void tbSettingsDatabaseErrorMessagesStorageTime_LostFocus(object sender, RoutedEventArgs e)
        {
            tbSettingsDatabaseErrorMessagesStorageTime_Update(sender);
        }

        private void tbSettingsDatabaseErrorMessagesStorageTime_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                tbSettingsDatabaseErrorMessagesStorageTime_Update(sender);
                Keyboard.ClearFocus();
            }
        }

        private void tbSettingsDatabaseErrorMessagesStorageTime_Update(object sender)
        {
            // Sjekk av input
            DataValidation.Double(
                    (sender as TextBox).Text,
                    Constants.DatabaseStorageTimeMin,
                    Constants.DatabaseStorageTimeMax,
                    Constants.DatabaseMessagesStorageTimeDefault,
                    out double validatedInput);

            if (adminSettingsVM.databaseErrorMessageStorageTime != validatedInput)
                adminSettingsVM.ApplicationRestartRequired();

            adminSettingsVM.databaseErrorMessageStorageTime = validatedInput;
        }

        private void btnRemoveUnusedDatabaseTables_Click(object sender, RoutedEventArgs e)
        {
            RadWindow.Confirm("This will delete all data tables not associated\nwith a sensor data value.", OnClosed);

            void OnClosed(object sendero, WindowClosedEventArgs ea)
            {
                if ((bool)ea.DialogResult == true)
                {
                    try
                    {
                        database.RemoveUnusedTables(sensorDataList);

                        errorHandler.ResetDatabaseError(ErrorHandler.DatabaseErrorType.RemoveUnusedTables);
                    }
                    catch (Exception ex)
                    {
                        errorHandler.Insert(
                            new ErrorMessage(
                                DateTime.UtcNow,
                                ErrorMessageType.Database,
                                ErrorMessageCategory.AdminUser,
                                string.Format("Database Error (RemoveUnusedTables)\n\nSystem Message:\n{0}", ex.Message)));

                        errorHandler.SetDatabaseError(ErrorHandler.DatabaseErrorType.RemoveUnusedTables);
                    }
                }
            }
        }

        private void btnEmptyDatabaseTables_Click(object sender, RoutedEventArgs e)
        {
            RadWindow.Confirm("This will delete all data in the database.", OnClosed);

            void OnClosed(object sendero, WindowClosedEventArgs ea)
            {
                if ((bool)ea.DialogResult == true)
                {
                    try
                    {
                        // Oppretter tabellene dersom de ikke eksisterer
                        database.CreateTables(sensorDataList);

                        errorHandler.ResetDatabaseError(ErrorHandler.DatabaseErrorType.CreateTablesSensorData1);
                    }
                    catch (Exception ex)
                    {
                        errorHandler.Insert(
                            new ErrorMessage(
                                DateTime.UtcNow,
                                ErrorMessageType.Database,
                                ErrorMessageCategory.Admin,
                                string.Format("Database Error (CreateTables 3)\n\nSystem Message:\n{0}", ex.Message)));

                        errorHandler.SetDatabaseError(ErrorHandler.DatabaseErrorType.CreateTablesSensorData1);
                    }

                    try
                    {
                        // Slette data fra alle tabeller
                        database.DeleteAllData(sensorDataList);
                        database.DeleteAllDataHMS();
                        database.DeleteAllDataSensorStatus();
                        database.DeleteAllDataVerification();

                        errorHandler.ResetDatabaseError(ErrorHandler.DatabaseErrorType.DeleteAllData);
                    }
                    catch (Exception ex)
                    {
                        errorHandler.Insert(
                            new ErrorMessage(
                                DateTime.UtcNow,
                                ErrorMessageType.Database,
                                ErrorMessageCategory.Admin,
                                string.Format("Database Error (DeleteAllData)\n\nSystem Message:\n{0}", ex.Message)));

                        errorHandler.SetDatabaseError(ErrorHandler.DatabaseErrorType.DeleteAllData);
                    }
                }
            }
        }

        private void cboRegulationStandard_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (adminSettingsVM.regulationStandard != (RegulationStandard)cboRegulationStandard.SelectedIndex)
            {
                adminSettingsVM.regulationStandard = (RegulationStandard)cboRegulationStandard.SelectedIndex;
                adminSettingsVM.ApplicationRestartRequired();
            }
        }

        private void tbVesselName_LostFocus(object sender, RoutedEventArgs e)
        {
            tbVesselName_Update(sender);
        }

        private void tbVesselName_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                tbVesselName_Update(sender);
                Keyboard.ClearFocus();
            }
        }

        private void tbVesselName_Update(object sender)
        {
            adminSettingsVM.vesselName = (sender as TextBox).Text;
        }

        private void tbHelideckHeadingOffset_LostFocus(object sender, RoutedEventArgs e)
        {
            tbHelideckHeadingOffset_Update(sender);
        }

        private void tbHelideckHeadingOffset_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                tbHelideckHeadingOffset_Update(sender);
                Keyboard.ClearFocus();
            }
        }

        private void tbHelideckHeadingOffset_Update(object sender)
        {
            // Sjekk av input
            DataValidation.Double(
                (sender as TextBox).Text,
                Constants.HeadingMin,
                Constants.HeadingMax,
                Constants.HeadingMin,
                out double validatedInput);

            adminSettingsVM.helideckHeadingOffset = (int)validatedInput;
        }

        private void tbMSICorrectionR_LostFocus(object sender, RoutedEventArgs e)
        {
            tbMSICorrectionR_Update(sender);
        }

        private void tbMSICorrectionR_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                tbMSICorrectionR_Update(sender);
                Keyboard.ClearFocus();
            }
        }

        private void tbMSICorrectionR_Update(object sender)
        {
            // Sjekk av input
            DataValidation.Double(
                (sender as TextBox).Text,
                Constants.MSICorrectionRMin,
                Constants.WindCorrectionRMax,
                Constants.MSICorrectionRMin,
                out double validatedInput);

            adminSettingsVM.msiCorrectionR = validatedInput;
        }

        private void tbRestrictedSectorFrom_LostFocus(object sender, RoutedEventArgs e)
        {
            tbRestrictedSectorFrom_Update(sender);
        }

        private void tbRestrictedSectorFrom_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                tbRestrictedSectorFrom_Update(sender);
                Keyboard.ClearFocus();
            }
        }

        private void tbRestrictedSectorFrom_Update(object sender)
        {
            // Sjekk av input
            DataValidation.Double(
                (sender as TextBox).Text,
                Constants.HeadingMin,
                Constants.HeadingMax,
                Constants.HeadingDefault,
                out double validatedInput);

            adminSettingsVM.restrictedSectorFrom = validatedInput.ToString("000");
        }

        private void tbRestrictedSectorTo_LostFocus(object sender, RoutedEventArgs e)
        {
            tbRestrictedSectorTo_Update(sender);
        }

        private void tbRestrictedSectorTo_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                tbRestrictedSectorTo_Update(sender);
                Keyboard.ClearFocus();
            }
        }

        private void tbRestrictedSectorTo_Update(object sender)
        {
            // Sjekk av input
            DataValidation.Double(
                (sender as TextBox).Text,
                Constants.HeadingMin,
                Constants.HeadingMax,
                Constants.HeadingDefault,
                out double validatedInput);

            adminSettingsVM.restrictedSectorTo = validatedInput.ToString("000");
        }

        private void cboHelicopterWSILimitType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (adminSettingsVM.helicopterWSILimitList.Count > 0)
                tbHelicopterWSILimit.Text = adminSettingsVM.helicopterWSILimitList[cboHelicopterWSILimitType.SelectedIndex].limit.ToString();
        }

        private void tbHelicopterWSILimit_LostFocus(object sender, RoutedEventArgs e)
        {
            tbHelicopterWSILimit_Update(sender);
        }

        private void tbHelicopterWSILimit_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                tbHelicopterWSILimit_Update(sender);
                Keyboard.ClearFocus();
            }
        }

        private void tbHelicopterWSILimit_Update(object sender)
        {
            // Sjekk av input
            DataValidation.Double(
                (sender as TextBox).Text,
                Constants.HelicopterWSIMin,
                Constants.HelicopterWSIMax,
                Constants.HelicopterWSIDefault,
                out double validatedInput);

            // Lagre ny limit
            adminSettingsVM.helicopterWSILimitList[cboHelicopterWSILimitType.SelectedIndex].limit = validatedInput;

            // Lagre til config fil
            config.SetHelicopterWSILimit((HelicopterType)cboHelicopterWSILimitType.SelectedIndex, validatedInput);

            // Skrive tilbake validert verdi til input-feltet
            (sender as TextBox).Text = validatedInput.ToString();
        }

        private void cboHelideckCategory_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            adminSettingsVM.helideckCategory = (HelideckCategory)cboHelideckCategory.SelectedIndex;
        }

        private void tbNDBFreq_NOROG_LostFocus(object sender, RoutedEventArgs e)
        {
            tbNDBFreq_NOROG_Update(sender);
        }

        private void tbNDBFreq_NOROG_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                tbNDBFreq_NOROG_Update(sender);
                Keyboard.ClearFocus();
            }
        }

        private void tbNDBFreq_NOROG_Update(object sender)
        {
            // Sjekk av input
            DataValidation.Double(
                (sender as TextBox).Text,
                Constants.NDBFrequencyMin,
                Constants.NDBFrequencyMax,
                Constants.NDBFrequencyDefault,
                out double validatedInput);

            adminSettingsVM.ndbFreq_NOROG = validatedInput.ToString("0.000", Constants.cultureInfo);
        }

        private void tbNDBFreq_CAP_LostFocus(object sender, RoutedEventArgs e)
        {
            tbNDBFreq_CAP_Update(sender);
        }

        private void tbNDBFreq_CAP_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                tbNDBFreq_CAP_Update(sender);
                Keyboard.ClearFocus();
            }
        }

        private void tbNDBFreq_CAP_Update(object sender)
        {
            // Sjekk av input
            DataValidation.Double(
                (sender as TextBox).Text,
                Constants.NDBFrequencyMin,
                Constants.NDBFrequencyMax,
                Constants.NDBFrequencyDefault,
                out double validatedInput);

            adminSettingsVM.ndbFreq_CAP = validatedInput.ToString("0.000", Constants.cultureInfo);
        }

        private void tbNDBIdent_LostFocus(object sender, RoutedEventArgs e)
        {
            tbNDBIdent_Update(sender);
        }

        private void tbNDBIdent_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                tbNDBIdent_Update(sender);
                Keyboard.ClearFocus();
            }
        }

        private void tbNDBIdent_Update(object sender)
        {
            adminSettingsVM.ndbIdent = (sender as TextBox).Text.ToUpper();
        }

        private void tbVHF_LostFocus(object sender, RoutedEventArgs e)
        {
            tbVHF_Update(sender);
        }

        private void tbVHF_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                tbVHF_Update(sender);
                Keyboard.ClearFocus();
            }
        }

        private void tbVHF_Update(object sender)
        {
            // Sjekk av input
            DataValidation.Double(
                (sender as TextBox).Text,
                Constants.VHFFrequencyMin,
                Constants.VHFFrequencyMax,
                Constants.VHFFrequencyDefault,
                out double validatedInput);

            adminSettingsVM.vhfFreq = validatedInput.ToString("0.000", Constants.cultureInfo);
        }

        private void tbHelideckHeight_LostFocus(object sender, RoutedEventArgs e)
        {
            tbHelideckHeight_Update(sender);
        }

        private void tbHelideckHeight_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                tbHelideckHeight_Update(sender);
                Keyboard.ClearFocus();
            }
        }

        private void tbHelideckHeight_Update(object sender)
        {
            // Sjekk av input
            DataValidation.Double(
                (sender as TextBox).Text,
                Constants.HelideckHeightMin,
                Constants.HelideckHeightMax,
                Constants.HelideckHeightDefault,
                out double validatedInput);

            adminSettingsVM.helideckHeight = validatedInput;
        }

        private void tbWindSensorHeight_LostFocus(object sender, RoutedEventArgs e)
        {
            tbWindSensorHeight_Update(sender);
        }

        private void tbWindSensorHeight_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                tbWindSensorHeight_Update(sender);
                Keyboard.ClearFocus();
            }
        }

        private void tbWindSensorHeight_Update(object sender)
        {
            // Sjekk av input
            DataValidation.Double(
                (sender as TextBox).Text,
                Constants.WindSensorHeightMin,
                Constants.WindSensorHeightMax,
                Constants.WindSensorHeightDefault,
                out double validatedInput);

            adminSettingsVM.windSensorHeight = validatedInput;
        }

        private void tbWindSensorDistance_LostFocus(object sender, RoutedEventArgs e)
        {
            tbWindSensorDistance_Update(sender);
        }

        private void tbWindSensorDistance_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                tbWindSensorDistance_Update(sender);
                Keyboard.ClearFocus();
            }
        }

        private void tbWindSensorDistance_Update(object sender)
        {
            // Sjekk av input
            DataValidation.Double(
                (sender as TextBox).Text,
                Constants.WindSensorDistanceMin,
                Constants.WindSensorDistanceMax,
                Constants.WindSensorDistanceDefault,
                out double validatedInput);

            adminSettingsVM.windSensorDistance = validatedInput;
        }

        private void tbAirPressureSensorHeight_LostFocus(object sender, RoutedEventArgs e)
        {
            tbAirPressureSensorHeight_Update(sender);
        }

        private void tbAirPressureSensorHeight_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                tbAirPressureSensorHeight_Update(sender);
                Keyboard.ClearFocus();
            }
        }

        private void tbAirPressureSensorHeight_Update(object sender)
        {
            // Sjekk av input
            DataValidation.Double(
                (sender as TextBox).Text,
                Constants.WindSensorHeightMin,
                Constants.WindSensorHeightMax,
                Constants.WindSensorHeightDefault,
                out double validatedInput);

            adminSettingsVM.airPressureSensorHeight = validatedInput;
        }

        private void tbEmailServer_LostFocus(object sender, RoutedEventArgs e)
        {
            tbEmailServer_Update(sender);
        }

        private void tbEmailServer_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                tbEmailServer_Update(sender);
                Keyboard.ClearFocus();
            }
        }

        private void tbEmailServer_Update(object sender)
        {
            adminSettingsVM.emailServer = (sender as TextBox).Text;
        }

        private void tbPort_LostFocus(object sender, RoutedEventArgs e)
        {
            tbPort_Update(sender);
        }

        private void tbPort_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                tbPort_Update(sender);
                Keyboard.ClearFocus();
            }
        }

        private void tbPort_Update(object sender)
        {
            // Sjekk av input
            DataValidation.Double(
                (sender as TextBox).Text,
                Constants.PortMin,
                Constants.PortMax,
                Constants.DefaultSMTPPort,
                out double validatedInput);

            adminSettingsVM.emailPort = validatedInput;
        }

        private void tbUsername_LostFocus(object sender, RoutedEventArgs e)
        {
            tbUsername_Update(sender);
        }

        private void tbUsername_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                tbUsername_Update(sender);
                Keyboard.ClearFocus();
            }
        }

        private void tbUsername_Update(object sender)
        {
            adminSettingsVM.emailUsername = (sender as TextBox).Text;
        }

        private void tbPassword_LostFocus(object sender, RoutedEventArgs e)
        {
            tbPassword_Update(sender);
        }

        private void tbPassword_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                tbPassword_Update(sender);
                Keyboard.ClearFocus();
            }
        }

        private void tbPassword_Update(object sender)
        {
            adminSettingsVM.emailPassword = (sender as TextBox).Text;
        }

        private void tbLogFreq_CAP_LostFocus(object sender, RoutedEventArgs e)
        {
            tbLogFreq_CAP_Update(sender);
        }

        private void tbLogFreq_CAP_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                tbLogFreq_CAP_Update(sender);
                Keyboard.ClearFocus();
            }
        }

        private void tbLogFreq_CAP_Update(object sender)
        {
            // Sjekk av input
            DataValidation.Double(
                (sender as TextBox).Text,
                Constants.VHFFrequencyMin,
                Constants.VHFFrequencyMax,
                Constants.VHFFrequencyDefault,
                out double validatedInput);

            adminSettingsVM.logFreq = validatedInput.ToString("0.000", Constants.cultureInfo);
        }

        private void tbMarineChannel_LostFocus(object sender, RoutedEventArgs e)
        {
            tbMarineChannel_Update(sender);
        }

        private void tbMarineChannel_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                tbMarineChannel_Update(sender);
                Keyboard.ClearFocus();
            }
        }

        private void tbMarineChannel_Update(object sender)
        {
            // Sjekk av input
            DataValidation.Double(
                (sender as TextBox).Text,
                Constants.MarineChannelMin,
                Constants.MarineChannelMax,
                Constants.MarineChannelDefault,
                out double validatedInput);

            adminSettingsVM.marineChannel = (int)validatedInput;
        }

        private void chNDBInstalled_NOROG_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as CheckBox).IsChecked == true)
            {
                lbNDBFreq_NOROG.IsEnabled = true;
            }
            else
            {
                lbNDBFreq_NOROG.IsEnabled = false;
            }
        }

        private void chNDBInstalled_CAP_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as CheckBox).IsChecked == true)
            {
                lbNDBFreq_CAP.IsEnabled = true;
                lbNDBIdent_CAP.IsEnabled = true;
            }
            else
            {
                lbNDBFreq_CAP.IsEnabled = false;
                lbNDBIdent_CAP.IsEnabled = false;
            }
        }

        private void chkEnableDataVerification_Click(object sender, RoutedEventArgs e)
        {
            adminSettingsVM.ApplicationRestartRequired();
        }
    }
}
