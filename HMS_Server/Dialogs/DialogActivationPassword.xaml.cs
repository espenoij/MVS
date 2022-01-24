using System.Windows;
using Telerik.Windows.Controls;

namespace HMS_Server
{
    /// <summary>
    /// Interaction logic for DialogAdminMode.xaml
    /// </summary>
    public partial class DialogActivationPassword : RadWindow
    {
        private ActivationVM activationVM;

        public DialogActivationPassword()
        {
            InitializeComponent();
        }

        public void Init(ActivationVM activationVM)
        {
            DataContext = activationVM;
            this.activationVM = activationVM;
        }

        private void passwordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (passwordBox.Password == Constants.ActivationPassword)
            {
                // Lukke passord vinduet
                Close();

                // Åpne activation vindu
                DialogActivation activatonDialog = new DialogActivation();
                activatonDialog.Init(activationVM);
                activatonDialog.Owner = App.Current.MainWindow;
                activatonDialog.ShowDialog();
            }
        }
    }
}
