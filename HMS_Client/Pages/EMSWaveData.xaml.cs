using System.Windows.Controls;

namespace HMS_Client
{
    /// <summary>
    /// Interaction logic for EMSWaveData.xaml
    /// </summary>
    public partial class EMSWaveData : UserControl
    {
        public EMSWaveData()
        {
            InitializeComponent();
        }

        public void Init(EMSWaveDataVM emsWaveDataVM)
        {
            DataContext = emsWaveDataVM;

            // Koble chart til data
            waveSWH20mChart.Series[0].ItemsSource = emsWaveDataVM.waveSWH20mList;
            waveSWHMax20mChart.Series[0].ItemsSource = emsWaveDataVM.waveSWHMax20mList;
            wavePeriod20mChart.Series[0].ItemsSource = emsWaveDataVM.wavePeriod20mList;
            wavePeriodMax20mChart.Series[0].ItemsSource = emsWaveDataVM.wavePeriodMax20mList;

            waveSWH3hChart.Series[0].ItemsSource = emsWaveDataVM.waveSWH3hList;
            waveSWHMax3hChart.Series[0].ItemsSource = emsWaveDataVM.waveSWHMax3hList;
            wavePeriod3hChart.Series[0].ItemsSource = emsWaveDataVM.wavePeriod3hList;
            wavePeriodMax3hChart.Series[0].ItemsSource = emsWaveDataVM.wavePeriodMax3hList;

            // Sette tab
            //tcEMSWaveData.SelectedIndex = 0;
        }
    }
}
