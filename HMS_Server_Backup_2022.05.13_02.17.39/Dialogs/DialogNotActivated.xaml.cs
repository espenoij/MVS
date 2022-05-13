using Telerik.Windows.Controls;

namespace HMS_Server
{
    /// <summary>
    /// Interaction logic for DialogNotActivated
    /// .xaml
    /// </summary>
    public partial class DialogNotActivated : RadWindow
    {
        public DialogNotActivated()
        {
            InitializeComponent();
        }

        private void btnClose_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Close();
        }
    }
}
