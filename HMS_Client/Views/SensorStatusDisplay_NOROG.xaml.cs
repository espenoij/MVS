using System.Windows.Controls;

namespace HMS_Client
{
    /// <summary>
    /// Interaction logic for SensorStatusDisplay_NOROG.xaml
    /// </summary>
    public partial class SensorStatusDisplay_NOROG : UserControl
    {
        public SensorStatusDisplay_NOROG()
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
