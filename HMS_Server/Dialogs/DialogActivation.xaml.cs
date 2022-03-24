using DeviceId;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Telerik.Windows.Controls;

namespace HMS_Server
{
    /// <summary>
    /// Interaction logic for DialogActivation
    /// .xaml
    /// </summary>
    public partial class DialogActivation : RadWindow
    {
        private ActivationVM activationVM;

        public DialogActivation()
        {
            InitializeComponent();
        }

        public void Init(ActivationVM activationVM)
        {
            DataContext = activationVM;
            this.activationVM = activationVM;

            if (activationVM.isActivated)
            {
                btnActivate.IsEnabled = false;
            }
        }

        private void btnActivate_Click(object sender, RoutedEventArgs e)
        {
            // Leser device ID fra hardware
            // Bruker hovedkort serienummer som identifikator
            // https://github.com/MatthewKing/DeviceId
            string deviceId = new DeviceIdBuilder()
                .OnWindows(windows => windows
                    .AddMotherboardSerialNumber())
                .ToString();

            activationVM.deviceID = deviceId;
            activationVM.isActivated = true;
            btnActivate.IsEnabled = false;

            // Åpne activation OK vindu
            DialogActivationOK activationDialogOK = new DialogActivationOK();
            activationDialogOK.Owner = App.Current.MainWindow;
            activationDialogOK.ShowDialog();
        }

        private void tbLicenseOwner_LostFocus(object sender, RoutedEventArgs e)
        {
            tbLicenseOwner_Update(sender);
        }

        private void tbLicenseOwner_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                tbLicenseOwner_Update(sender);
                Keyboard.ClearFocus();
            }
        }

        private void tbLicenseOwner_Update(object sender)
        {
            activationVM.licenseOwner = (sender as TextBox).Text;
        }

        private void tbLicenseLocation_LostFocus(object sender, RoutedEventArgs e)
        {
            tbLicenseLocation_Update(sender);
        }

        private void tbLicenseLocation_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                tbLicenseLocation_Update(sender);
                Keyboard.ClearFocus();
            }
        }

        private void tbLicenseLocation_Update(object sender)
        {
            activationVM.licenseLocation = (sender as TextBox).Text;
        }

        private void tbLicenseMaxClients_LostFocus(object sender, RoutedEventArgs e)
        {
            tbLicenseMaxClients_Update(sender);
        }

        private void tbLicenseMaxClients_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                tbLicenseMaxClients_Update(sender);
                Keyboard.ClearFocus();
            }
        }

        private void tbLicenseMaxClients_Update(object sender)
        {
            // Sjekk av input
            DataValidation.Double(
                (sender as TextBox).Text,
                Constants.LicenseMaxClientsMin,
                Constants.LicenseMaxClientsMax,
                Constants.LicenseMaxClientsDefault,
                out double validatedInput);

            (sender as TextBox).Text = validatedInput.ToString();
            activationVM.licenseMaxClients = (int)validatedInput;
        }
    }
}
