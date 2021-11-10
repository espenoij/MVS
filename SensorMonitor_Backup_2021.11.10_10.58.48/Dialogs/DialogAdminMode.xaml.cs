using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Telerik.Windows.Controls;

namespace SensorMonitor
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
