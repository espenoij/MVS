using System.Windows.Controls;

namespace HMS_Client
{
    /// <summary>
    /// Interaction logic for HelideckMotionTrend_NOROG.xaml
    /// </summary>
    public partial class HelideckMotionTrend_NOROG : UserControl
    {
        // Configuration settings
        private Config config;

        // View Model
        public HelideckMotionTrendVM viewModel;

        public HelideckMotionTrend_NOROG()
        {
            InitializeComponent();
        }

        public void Init(HelideckMotionTrendVM viewModel, Config config)
        {
            this.viewModel = viewModel;
            this.config = config;

            DataContext = viewModel;

            // Init UI
            InitUI();
        }

        private void InitUI()
        {
            // Koble chart til data
            chartPitch20m.Series[0].ItemsSource = viewModel.pitch20mList;
            chartRoll20m.Series[0].ItemsSource = viewModel.roll20mList;
            chartInclination20m.Series[0].ItemsSource = viewModel.inclinationData20mList;
            chartHeaveAmplitude20m.Series[0].ItemsSource = viewModel.heaveAmplitudeData20mList;
            chartSignificantHeaveRate20m.Series[0].ItemsSource = viewModel.significantHeaveRateData20mList;

            chartPitch3h.Series[0].ItemsSource = viewModel.pitch3hList;
            chartRoll3h.Series[0].ItemsSource = viewModel.rollData3hList;
            chartInclination3h.Series[0].ItemsSource = viewModel.inclinationData3hList;
            chartHeaveAmplitude3h.Series[0].ItemsSource = viewModel.heaveAmplitudeData3hList;
            chartSignificantHeaveRate3h.Series[0].ItemsSource = viewModel.significantHeaveRateData3hList;
        }
    }
}
