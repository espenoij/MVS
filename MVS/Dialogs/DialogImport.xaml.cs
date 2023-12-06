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

        public DialogImport()
        {
            InitializeComponent();
        }

        public void Init(ImportVM importVM, Config config, RecordingSession selectedSession)
        {
            DataContext = importVM;
            this.importVM = importVM;
            this.config = config;

            // Database User ID
            tbSettingsDatabaseUserID.Text = Encryption.ToInsecureString(Encryption.DecryptString(config.Read(ConfigKey.HMSDatabaseUserID)));

            // Database Password
            tbSettingsDatabasePassword.Text = Encryption.ToInsecureString(Encryption.DecryptString(config.Read(ConfigKey.HMSDatabasePassword)));

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

            importVM.databaseAddress = validatedInput;
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

            importVM.databasePort = validatedInput;
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
            importVM.databaseName = (sender as TextBox).Text;
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
            if ((sender as TextBox).Text != Encryption.ToInsecureString(Encryption.DecryptString(config.Read(ConfigKey.HMSDatabaseUserID))))
            {
                config.Write(ConfigKey.HMSDatabaseUserID, Encryption.EncryptString(Encryption.ToSecureString((sender as TextBox).Text)));
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
            if ((sender as TextBox).Text != Encryption.ToInsecureString(Encryption.DecryptString(config.Read(ConfigKey.HMSDatabasePassword))))
            {
                config.Write(ConfigKey.HMSDatabasePassword, Encryption.EncryptString(Encryption.ToSecureString((sender as TextBox).Text)));
            }
        }
    }
}
