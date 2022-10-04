using System.Windows.Controls;

namespace HMS_Client
{
    /// <summary>
    /// Interaction logic for SensorStatusDisplay_CAP.xaml
    /// </summary>
    public partial class SensorStatusDisplay : UserControl
    {
        public SensorStatusDisplay()
        {
            InitializeComponent();
        }

        public void Init(SensorStatusVM sensorStatusVM)
        {
            DataContext = sensorStatusVM;

            lbSensorStatus.ItemsSource = sensorStatusVM.sensorStatusDisplayList;
        }
    }
}
