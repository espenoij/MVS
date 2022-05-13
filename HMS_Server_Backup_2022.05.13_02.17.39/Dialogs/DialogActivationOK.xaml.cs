using System.Windows;
using Telerik.Windows.Controls;

namespace HMS_Server
{
    /// <summary>
    /// Interaction logic for DialogActivationOK
    /// .xaml
    /// </summary>
    public partial class DialogActivationOK : RadWindow
    {
        public DialogActivationOK()
        {
            InitializeComponent();
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
