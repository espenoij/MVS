using System.Windows.Controls;
using System.Windows.Input;
using System.Windows;
using Telerik.Windows.Controls;
using static Org.BouncyCastle.Math.EC.ECCurve;

namespace MVS
{
    /// <summary>
    /// Interaction logic for DialogImport
    /// .xaml
    /// </summary>
    public partial class DialogImport : RadWindow
    {
        private ImportVM importVM;
        private Config config;
        private MVSDatabase mvsDatabase;
        private MainWindowVM mainWindowVM;
        private RecordingSession selectedSession;

        public DialogImport()
        {
            InitializeComponent();
        }

        public void Init(ImportVM importVM, Config config, RecordingSession selectedSession, MVSDatabase mvsDatabase, MainWindowVM mainWindowVM)
        {
            DataContext = importVM;
            this.importVM = importVM;
            this.config = config;
            this.selectedSession = selectedSession;
            this.mvsDatabase = mvsDatabase;
            this.mainWindowVM = mainWindowVM;

            // Database User ID
            tbSettingsHMSDatabaseUserID.Text = Encryption.ToInsecureString(Encryption.DecryptString(config.Read(ConfigKey.HMSDatabaseUserID)));

            // Database Password
            tbSettingsHMSDatabasePassword.Text = Encryption.ToInsecureString(Encryption.DecryptString(config.Read(ConfigKey.HMSDatabasePassword)));

            tbDataSetName.Content = selectedSession.Name;
            lbDataSetDate.Content = selectedSession.DateString;
            lbDataSetStartTime.Content = selectedSession.StartTimeString2;
            lbDataSetEndTime.Content = selectedSession.EndTimeString2;
            lbDataSetDuration.Content = selectedSession.DurationString;

            if (selectedSession.StartTime == System.Data.SqlTypes.SqlDateTime.MinValue.Value)
                btnImport.IsEnabled = false;
            else
                btnImport.IsEnabled = true;
        }

        private void btnCancel_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Close();
        }

        private void btnImport_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (mvsDatabase.ImportHMSData(selectedSession))
            {
                // Ny valgt MRU type
                mainWindowVM.SelectedSession.InputMRUs = InputMRUType.ReferenceMRU_TestMRU;

                // Oppdatere database
                mvsDatabase.Update(mainWindowVM.SelectedSession);
            }
        }


        private void tbSettingsHMSDatabaseAddress_LostFocus(object sender, RoutedEventArgs e)
        {
            tbSettingsHMSDatabaseAddress_Update(sender);
        }

        private void tbSettingsHMSDatabaseAddress_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                tbSettingsHMSDatabaseAddress_Update(sender);
                Keyboard.ClearFocus();
            }
        }

        private void tbSettingsHMSDatabaseAddress_Update(object sender)
        {
            DataValidation.IPAddress(
                   (sender as TextBox).Text,
                   Constants.DefaultServerAddress,
                   out string validatedInput);

            importVM.databaseAddress = validatedInput;
        }

        private void tbSettingsHMSDatabasePort_LostFocus(object sender, RoutedEventArgs e)
        {
            tbSettingsHMSDatabasePort_Update(sender);
        }

        private void tbSettingsHMSDatabasePort_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                tbSettingsHMSDatabasePort_Update(sender);
                Keyboard.ClearFocus();
            }
        }

        private void tbSettingsHMSDatabasePort_Update(object sender)
        {
            // Sjekk av input
            DataValidation.Double(
                (sender as TextBox).Text,
                Constants.PortMin,
                Constants.PortMax,
                Constants.ServerPortDefault,
                out double validatedInput);

            importVM.databasePort = validatedInput;
        }

        private void tbSettingsHMSDatabaseName_LostFocus(object sender, RoutedEventArgs e)
        {
            tbSettingsHMSDatabaseName_Update(sender);
        }

        private void tbSettingsHMSDatabaseName_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                tbSettingsHMSDatabaseName_Update(sender);
                Keyboard.ClearFocus();
            }
        }

        private void tbSettingsHMSDatabaseName_Update(object sender)
        {
            importVM.databaseName = (sender as TextBox).Text;
        }

        private void tbSettingsHMSDatabaseUserID_LostFocus(object sender, RoutedEventArgs e)
        {
            tbSettingsHMSDatabaseUserID_Update(sender);
        }

        private void tbSettingsHMSDatabaseUserID_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                tbSettingsHMSDatabaseUserID_Update(sender);
                Keyboard.ClearFocus();
            }
        }

        private void tbSettingsHMSDatabaseUserID_Update(object sender)
        {
            if ((sender as TextBox).Text != Encryption.ToInsecureString(Encryption.DecryptString(config.Read(ConfigKey.HMSDatabaseUserID))))
            {
                config.Write(ConfigKey.HMSDatabaseUserID, Encryption.EncryptString(Encryption.ToSecureString((sender as TextBox).Text)));
            }
        }

        private void tbSettingsHMSDatabasePassword_LostFocus(object sender, RoutedEventArgs e)
        {
            tbSettingsHMSDatabasePassword_Update(sender);
        }

        private void tbSettingsHMSDatabasePassword_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                tbSettingsHMSDatabasePassword_Update(sender);
                Keyboard.ClearFocus();
            }
        }

        private void tbSettingsHMSDatabasePassword_Update(object sender)
        {
            if ((sender as TextBox).Text != Encryption.ToInsecureString(Encryption.DecryptString(config.Read(ConfigKey.HMSDatabasePassword))))
            {
                config.Write(ConfigKey.HMSDatabasePassword, Encryption.EncryptString(Encryption.ToSecureString((sender as TextBox).Text)));
            }
        }

        private void tbSettingsHMSDatabaseTableName_LostFocus(object sender, RoutedEventArgs e)
        {
            tbSettingsHMSDatabaseTableName_Update(sender);
        }

        private void tbSettingsHMSDatabaseTableName_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                tbSettingsHMSDatabaseTableName_Update(sender);
                Keyboard.ClearFocus();
            }
        }

        private void tbSettingsHMSDatabaseTableName_Update(object sender)
        {
            importVM.databaseTableName = (sender as TextBox).Text;
        }
    }
}
