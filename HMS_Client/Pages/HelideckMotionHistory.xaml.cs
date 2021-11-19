using System.Windows.Controls;

namespace HMS_Client
{
    /// <summary>
    /// Interaction logic for HelideckMotionTrendZoom.xaml
    /// </summary>
    public partial class HelideckMotionHistory : UserControl
    {
        // View Model
        public HelideckMotionTrendVM helideckMotionTrendVM;

        public HelideckMotionHistory()
        {
            InitializeComponent();
        }

        public void Init(HelideckMotionTrendVM viewModel)
        {
            helideckMotionTrendVM = viewModel;

            // Context
            DataContext = this.helideckMotionTrendVM;

            // Init UI
            InitUI();
        }

        private void InitUI()
        {
            // Koble pitch chart til pitch data
            chartPitch20m.Series[0].ItemsSource = helideckMotionTrendVM.pitchData20mList;
            chartRoll20m.Series[0].ItemsSource = helideckMotionTrendVM.rollData20mList;
            chartSignificantHeaveRate20m.Series[0].ItemsSource = helideckMotionTrendVM.significantHeaveRateData20mList;
            chartInclination20m.Series[0].ItemsSource = helideckMotionTrendVM.inclinationData20mList;

            chartPitch3h.Series[0].ItemsSource = helideckMotionTrendVM.pitchData3hList;
            chartRoll3h.Series[0].ItemsSource = helideckMotionTrendVM.rollData3hList;
            chartSignificantHeaveRate3h.Series[0].ItemsSource = helideckMotionTrendVM.significantHeaveRateData3hList;
            chartInclination3h.Series[0].ItemsSource = helideckMotionTrendVM.inclinationData3hList;
        }
    }
}
