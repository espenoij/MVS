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
            //waveChart.Series[0].ItemsSource = viewModel.wave20mList;
            waveSWHChart.Series[0].ItemsSource = viewModel.waveSWH20mList;
            waveSWHMaxChart.Series[0].ItemsSource = viewModel.waveSWHMax20mList;
            wavePeriodChart.Series[0].ItemsSource = viewModel.wavePeriod20mList;
            wavePeriodMaxChart.Series[0].ItemsSource = viewModel.wavePeriodMax20mList;
        }
    }
}
