using System.Windows;

namespace MVS
{
    /// <summary>
    /// Interaction logic for DialogHandlerWarning.xaml
    /// </summary>
    public partial class DialogHandlerWarning : Window
    {
        public DialogHandlerWarning(string header, string text)
        {
            InitializeComponent();

            tbHeader.Text = header;
            tbText.Text = text;
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
