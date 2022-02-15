using System.Windows.Controls;

namespace HMS_Client
{
    /// <summary>
    /// Interaction logic for WindHeadingChange_CAP.xaml
    /// </summary>
    public partial class WindHeadingChange_CAP : UserControl
    {
        // View Model
        public WindHeadingChangeVM viewModel;

        public WindHeadingChange_CAP()
        {
            InitializeComponent();
        }

        public void Init(WindHeadingChangeVM viewModel)
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
