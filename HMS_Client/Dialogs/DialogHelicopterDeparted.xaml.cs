using System.Windows;
using System.Windows.Controls;
using Telerik.Windows.Controls;

namespace HMS_Client
{
    /// <summary>
    /// Interaction logic for DialogHelicopterDeparted
    /// .xaml
    /// </summary>
    public partial class DialogHelicopterDeparted : RadWindow
    {
        private UserInputsVM userInputsVM;
        private TextBox inputField;

        public DialogHelicopterDeparted(UserInputsVM userInputsVM, TextBox inputField)
        {
            InitializeComponent();

            this.userInputsVM = userInputsVM;
            this.inputField = inputField;
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            // Display Mode
            userInputsVM.displayMode = DisplayMode.PreLanding;

            // Slette input feltet
            inputField.Text = string.Empty;

            Close();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

    }
}
