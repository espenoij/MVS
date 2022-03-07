using System.Windows;
using Telerik.Windows.Controls;

namespace HMS_Client
{
    /// <summary>
    /// Interaction logic for DialogClientDenied
    /// .xaml
    /// </summary>
    public partial class DialogClientDenied : RadWindow
    {
        public DialogClientDenied()
        {
            InitializeComponent();
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
