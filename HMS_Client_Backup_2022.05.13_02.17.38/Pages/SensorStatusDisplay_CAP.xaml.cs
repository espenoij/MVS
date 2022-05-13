using System.Windows.Controls;

namespace HMS_Client
{
    /// <summary>
    /// Interaction logic for SensorStatusDisplay_CAP.xaml
    /// </summary>
    public partial class SensorStatusDisplay_CAP : UserControl
    {
        public SensorStatusDisplay_CAP()
        {
            InitializeComponent();
        }

        public void Init(SensorStatusDisplayVM sensorStatusVM)
        {
            DataContext = sensorStatusVM;

            lbSensorStatus.ItemsSource = sensorStatusVM.sensorStatusDisplayList;
        }
    }
}
