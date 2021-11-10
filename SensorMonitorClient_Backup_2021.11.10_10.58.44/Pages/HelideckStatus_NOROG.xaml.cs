using System.Windows.Controls;

namespace SensorMonitorClient
{
    /// <summary>
    /// Interaction logic for HelideckStatus_NOROG.xaml
    /// </summary>
    public partial class HelideckStatus_NOROG : UserControl
    {
        public HelideckStatus_NOROG()
        {
            InitializeComponent();
        }

        public void Init(HelideckStatusVM helideckStatusVM)
        {
            DataContext = helideckStatusVM;
        }
    }
}
