using System;
using Telerik.Windows.Controls;

namespace MVS
{
    /// <summary>
    /// Interaction logic for DialogAbout
    /// .xaml
    /// </summary>
    public partial class DialogAbout : RadWindow
    {
        public DialogAbout()
        {
            InitializeComponent();
        }

        public void Init(AboutVM aboutVM)
        {
            DataContext = aboutVM;
        }

        private void btnActivate_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Close();
        }
    }
}
