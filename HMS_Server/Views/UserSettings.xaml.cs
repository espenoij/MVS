using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace HMS_Server
{
    /// <summary>
    /// Interaction logic for UserSettings.xaml
    /// </summary>
    public partial class UserSettings : UserControl
    {
        // Configuration settings
        private Config config;

        // Settings View Model
        private UserSettingsVM userSettingsVM;

        public UserSettings()
        {
            InitializeComponent();
        }

        public void Init(UserSettingsVM userSettingsVM, Config config)
        {
            DataContext = userSettingsVM;
            this.userSettingsVM = userSettingsVM;

            // Config
            this.config = config;

            InitUI();
        }

        private void InitUI()
        {
            // Fixed heading
            if (userSettingsVM.fixedInstallation)
            {
                lbFixedHeading.IsEnabled = true;
                tbFixedHeading.IsEnabled = true;
            }
            else
            {
                lbFixedHeading.IsEnabled = false;
                tbFixedHeading.IsEnabled = false;
            }
        }

        private void cbFixedHeadingEnable_Click(object sender, RoutedEventArgs e)
        {
            if (userSettingsVM.fixedInstallation)
            {
                lbFixedHeading.IsEnabled = true;
                tbFixedHeading.IsEnabled = true;
            }
            else
            {
                lbFixedHeading.IsEnabled = false;
                tbFixedHeading.IsEnabled = false;
            }
        }

        private void tbFixedHeading_LostFocus(object sender, RoutedEventArgs e)
        {
            tbFixedHeading_Update(sender);
        }

        private void tbFixedHeading_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                tbFixedHeading_Update(sender);
                Keyboard.ClearFocus();
            }
        }

        private void tbFixedHeading_Update(object sender)
        {
            // Sjekk av input
            DataValidation.Double(
                (sender as TextBox).Text,
                Constants.HeadingMin,
                Constants.HeadingMax,
                Constants.HeadingDefault,
                out double validatedInput);

            userSettingsVM.fixedHeading = validatedInput;
        }

        private void tbJackupHeight_LostFocus(object sender, RoutedEventArgs e)
        {
            tbJackupHeight_Update(sender);
        }

        private void tbJackupHeight_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                tbJackupHeight_Update(sender);
                Keyboard.ClearFocus();
            }
        }

        private void tbJackupHeight_Update(object sender)
        {
            // Sjekk av input
            DataValidation.Double(
                (sender as TextBox).Text,
                Constants.JackupHeightMin,
                Constants.JackupHeightMax,
                Constants.JackupHeightDefault,
                out double validatedInput);

            userSettingsVM.jackupHeight = validatedInput;
        }
    }
}
