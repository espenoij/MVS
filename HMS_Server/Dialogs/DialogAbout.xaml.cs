using System;
using Telerik.Windows.Controls;

namespace HMS_Server
{
    /// <summary>
    /// Interaction logic for DialogAbout
    /// .xaml
    /// </summary>
    public partial class DialogAbout : RadWindow
    {
        private ActivationVM activationVM;

        public DialogAbout()
        {
            InitializeComponent();
        }

        public void Init(ActivationVM activationVM)
        {
            DataContext = activationVM;
            this.activationVM = activationVM;
        }

        private void btnActivate_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Close();
        }
    }
}
