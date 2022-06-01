using System.Windows.Controls;

namespace HMS_Client
{
    /// <summary>
    /// Interaction logic for EMSWaveData.xaml
    /// </summary>
    public partial class EMSWaveData : UserControl
    {
        // View Model
        public EMSWaveDataVM viewModel;

        public EMSWaveData()
        {
            InitializeComponent();
        }

        public void Init(EMSWaveDataVM emsWaveDataVM)
        {
            this.viewModel = emsWaveDataVM;

            DataContext = emsWaveDataVM;

            // Init UI
            InitUI();
        }

        private void InitUI()
        {
            // Koble chart til data
            waveSWH20mChart.Series[0].ItemsSource = viewModel.waveSWH20mList;
            waveSWHMax20mChart.Series[0].ItemsSource = viewModel.waveSWHMax20mList;
            wavePeriod20mChart.Series[0].ItemsSource = viewModel.wavePeriod20mList;
            wavePeriodMax20mChart.Series[0].ItemsSource = viewModel.wavePeriodMax20mList;

            waveSWH3hChart.Series[0].ItemsSource = viewModel.waveSWH3hList;
            waveSWHMax3hChart.Series[0].ItemsSource = viewModel.waveSWHMax3hList;
            wavePeriod3hChart.Series[0].ItemsSource = viewModel.wavePeriod3hList;
            wavePeriodMax3hChart.Series[0].ItemsSource = viewModel.wavePeriodMax3hList;
        }
    }
}
