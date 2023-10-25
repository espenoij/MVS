using System;
using System.Net;
using System.Net.Sockets;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Telerik.Windows.Controls;
using Telerik.Windows.Data;

namespace MVS
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
        private RadObservableCollection<SensorData> sensorDataList = new RadObservableCollection<SensorData>();

        public AdminSettings()
        {
            InitializeComponent();
        }

        public void Init(AdminSettingsVM adminSettingsVM, Config config, DatabaseHandler database, ErrorHandler errorHandler, RadObservableCollection<SensorData> sensorDataList)
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

            InitUI();

            adminSettingsVM.ApplicationRestartRequired(false);
        }

        private void InitUI()
        {
            // Database User ID
            tbSettingsDatabaseUserID.Text = Encryption.ToInsecureString(Encryption.DecryptString(config.Read(ConfigKey.DatabaseUserID)));

            // Database Password
            tbSettingsDatabasePassword.Text = Encryption.ToInsecureString(Encryption.DecryptString(config.Read(ConfigKey.DatabasePassword)));
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

        private void tbSettingsDatabaseSaveFrequency_LostFocus(object sender, RoutedEventArgs e)
        {
            tbSettingsDatabaseSaveFrequency_Update(sender);
        }

        private void tbSettingsDatabaseSaveFrequency_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                tbSettingsDatabaseSaveFrequency_Update(sender);
                Keyboard.ClearFocus();
            }
        }

        private void tbSettingsDatabaseSaveFrequency_Update(object sender)
        {
            // Sjekk av input
            DataValidation.Double(
                    (sender as TextBox).Text,
                    Constants.DatabaseSaveFreqMin,
                    Constants.DatabaseSaveFreqMax,
                    Constants.DatabaseSaveFreqDefault,
                    out double validatedInput);

            if (adminSettingsVM.databaseSaveFrequency != validatedInput)
                adminSettingsVM.ApplicationRestartRequired();

            adminSettingsVM.databaseSaveFrequency = validatedInput;
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

            if (adminSettingsVM.setupGUIDataLimit != validatedInput)
                adminSettingsVM.ApplicationRestartRequired();

            adminSettingsVM.setupGUIDataLimit = validatedInput;
        }

        private void tbSettingsServerUIFrequency_LostFocus(object sender, RoutedEventArgs e)
        {
            tbSettingsServerUIFrequency_Update(sender);
        }

        private void tbSettingsServerUIFrequency_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                tbSettingsServerUIFrequency_Update(sender);
                Keyboard.ClearFocus();
            }
        }

        private void tbSettingsServerUIFrequency_Update(object sender)
        {
            // Sjekk av input
            DataValidation.Double(
                    (sender as TextBox).Text,
                    Constants.ServerUIUpdateFrequencyMin,
                    Constants.ServerUIUpdateFrequencyMax,
                    Constants.ServerUIUpdateFrequencyDefault,
                    out double validatedInput);

            if (adminSettingsVM.serverUIUpdateFrequency != validatedInput)
                adminSettingsVM.ApplicationRestartRequired();

            adminSettingsVM.serverUIUpdateFrequency = validatedInput;
        }

        private void tbSettingsHMSProcessingFrequency_LostFocus(object sender, RoutedEventArgs e)
        {
            tbSettingsHMSProcessingFrequency_Update(sender);
        }

        private void tbSettingsHMSProcessingFrequency_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                tbSettingsHMSProcessingFrequency_Update(sender);
                Keyboard.ClearFocus();
            }
        }

        private void tbSettingsHMSProcessingFrequency_Update(object sender)
        {
            // Sjekk av input
            DataValidation.Double(
                    (sender as TextBox).Text,
                    Constants.HMSProcessingFrequencyMin,
                    Constants.HMSProcessingFrequencyMax,
                    Constants.HMSProcessingFrequencyDefault,
                    out double validatedInput);

            if (adminSettingsVM.hmsProcessingFrequency != validatedInput)
                adminSettingsVM.ApplicationRestartRequired();

            adminSettingsVM.hmsProcessingFrequency = validatedInput;
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

        //private void btnEmptyDatabaseTables_Click(object sender, RoutedEventArgs e)
        //{
        //    RadWindow.Confirm("This will delete all data in the database.", OnClosed);

        //    void OnClosed(object sendero, WindowClosedEventArgs ea)
        //    {
        //        if ((bool)ea.DialogResult == true)
        //        {
        //            try
        //            {
        //                // Slette data fra alle tabeller
        //                //database.DeleteAllData(sensorDataList);
        //                database.DeleteAllDataSets();

        //                errorHandler.ResetDatabaseError(ErrorHandler.DatabaseErrorType.DeleteAllData);
        //            }
        //            catch (Exception ex)
        //            {
        //                errorHandler.Insert(
        //                    new ErrorMessage(
        //                        DateTime.UtcNow,
        //                        ErrorMessageType.Database,
        //                        ErrorMessageCategory.Admin,
        //                        string.Format("Database Error (DeleteAllData)\n\nSystem Message:\n{0}", ex.Message)));

        //                errorHandler.SetDatabaseError(ErrorHandler.DatabaseErrorType.DeleteAllData);
        //            }
        //        }
        //    }
        //}

        private void tbWaveHeightCutoff_LostFocus(object sender, RoutedEventArgs e)
        {
            tbWaveHeightCutoff_Update(sender);
        }

        private void tbWaveHeightCutoff_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                tbWaveHeightCutoff_Update(sender);
                Keyboard.ClearFocus();
            }
        }

        private void tbWaveHeightCutoff_Update(object sender)
        {
            // Sjekk av input
            DataValidation.Double(
                (sender as TextBox).Text,
                Constants.WaveHeightCutoffMin,
                Constants.WaveHeightCutoffMax,
                Constants.WaveHeightCutoffDefault,
                out double validatedInput);

            adminSettingsVM.waveHeightCutoff = validatedInput;
        }
    }
}
