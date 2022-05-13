using System.Windows;
using Telerik.Windows.Controls;

namespace HMS_Client
{
    /// <summary>
    /// Interaction logic for DialogAdminPassword.xaml
    /// </summary>
    public partial class DialogAdminPassword : RadWindow
    {
        public DialogAdminPassword()
        {
            InitializeComponent();
        }

        private void passwordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (passwordBox.Password == Constants.AdminModePassword)
            {
                AdminMode.IsActive = true;
                Close();
            }
        }
    }
}
