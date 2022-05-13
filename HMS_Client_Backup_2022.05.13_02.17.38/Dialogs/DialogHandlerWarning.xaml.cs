using Telerik.Windows.Controls;

namespace HMS_Client
{
    /// <summary>
    /// Interaction logic for DialogHandlerWarning.xaml
    /// </summary>
    public partial class DialogHandlerWarning : RadWindow
    {
        public DialogHandlerWarning(string header, string text)
        {
            InitializeComponent();

            tbHeader.Text = header;
            tbText.Text = text;
        }

        private void btnClose_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Close();
        }
    }
}
