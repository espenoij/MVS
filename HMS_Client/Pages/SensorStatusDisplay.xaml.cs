using System.Windows.Controls;

namespace HMS_Client
{
    /// <summary>
    /// Interaction logic for NOROG_SensorStatus.xaml
    /// </summary>
    public partial class SensorStatusDisplay : UserControl
    {
        public SensorStatusDisplay()
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
