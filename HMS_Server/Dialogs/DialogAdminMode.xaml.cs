using System.Windows;
using Telerik.Windows.Controls;

namespace HMS_Server
{
    /// <summary>
    /// Interaction logic for DialogAdminMode.xaml
    /// </summary>
    public partial class DialogAdminMode : RadWindow
    {
        public DialogAdminMode()
        {
            InitializeComponent();
        }

        private void passwordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (passwordBox.Password == Constants.AdminModePassword)
            {
                AdminMode.IsActive = true;
                this.Close();
            }
        }
    }
}
