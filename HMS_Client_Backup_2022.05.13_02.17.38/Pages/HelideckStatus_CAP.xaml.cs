using System.Windows.Controls;

namespace HMS_Client
{
    /// <summary>
    /// Interaction logic for HelideckStatus_CAP.xaml
    /// </summary>
    public partial class HelideckStatus_CAP : UserControl
    {
        public HelideckStatus_CAP()
        {
            InitializeComponent();
        }

        public void Init(HelideckStatusVM helideckStatusVM)
        {
            DataContext = helideckStatusVM;
        }
    }
}
