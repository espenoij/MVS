using System.Windows.Controls;

namespace HMS_Client
{
    /// <summary>
    /// Interaction logic for EMSWaveData.xaml
    /// </summary>
    public partial class EMSWaveData : UserControl
    {
        // Configuration settings
        private Config config;

        // View Model
        public EMSWaveDataVM viewModel;

        public EMSWaveData()
        {
            InitializeComponent();
        }

        public void Init(EMSWaveDataVM emsWaveDataVM, Config config)
        {
            this.viewModel = emsWaveDataVM;
            this.config = config;

            DataContext = emsWaveDataVM;

            // Init UI
            InitUI();
        }

        private void InitUI()
        {
            // Koble chart til data
            waveHeightChart.Series[0].ItemsSource = viewModel.waveHeight20mList;
        }
    }
}
