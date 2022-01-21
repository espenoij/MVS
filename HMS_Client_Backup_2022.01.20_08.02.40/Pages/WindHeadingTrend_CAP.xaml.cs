using System.Windows.Controls;

namespace HMS_Client
{
    /// <summary>
    /// Interaction logic for HelideckWindHeadingTrend_CAP.xaml
    /// </summary>
    public partial class WindHeadingTrend_CAP : UserControl
    {
        // View Model
        public WindHeadingTrendVM viewModel;

        public WindHeadingTrend_CAP()
        {
            InitializeComponent();
        }

        public void Init(WindHeadingTrendVM viewModel)
        {
            this.viewModel = viewModel;

            DataContext = viewModel;

            // Init UI
            InitUI();
        }

        private void InitUI()
        {
            // Koble chart til data
            chartVesselHeading.Series[0].ItemsSource = viewModel.vesselHdg20mDataList;
            chartWindHeading.Series[0].ItemsSource = viewModel.windDir20mDataList;
        }
    }
}
