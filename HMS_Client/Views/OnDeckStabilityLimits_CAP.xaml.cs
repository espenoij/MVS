using System.Windows;
using System.Windows.Controls;
using Telerik.Windows.Controls;
using Telerik.Windows.Data;

namespace HMS_Client
{
    /// <summary>
    /// Interaction logic for OnDeckStabilityLimits_CAP.xaml
    /// </summary>
    public partial class OnDeckStabilityLimits_CAP : UserControl
    {
        private OnDeckStabilityLimitsVM viewModel;
        private LandingStatusTrendVM landingStatusTrendVM;

        public OnDeckStabilityLimits_CAP()
        {
            InitializeComponent();
        }

        public void Init(OnDeckStabilityLimitsVM viewModel, LandingStatusTrendVM landingStatusTrendVM)
        {
            this.viewModel = viewModel;
            this.landingStatusTrendVM = landingStatusTrendVM;
            DataContext = viewModel;

            // Koble chart til data
            chartMSIWSI20m.Series[0].ItemsSource = viewModel.msiwsi20mDataList;
            chartMSIWSI3h.Series[0].ItemsSource = viewModel.msiwsi3hDataList;

            // Blå bakgrunn på graf
            RadObservableCollection<Point> blueArea = new RadObservableCollection<Point>();

            blueArea.Add(new Point() { X = 0, Y = Constants.MSIMax });
            blueArea.Add(new Point() { X = Constants.WSIMax, Y = 0 });

            chartMSIWSI20m.Series[1].ItemsSource = blueArea;
            chartMSIWSI3h.Series[1].ItemsSource = blueArea;
        }

        private void RadTabControl_SelectionChanged(object sender, Telerik.Windows.Controls.RadSelectionChangedEventArgs e)
        {
            if (viewModel != null)
            {
                switch ((sender as RadTabControl).SelectedIndex)
                {
                    case 0:
                        viewModel.selectedGraphTime = GraphTime.Minutes20;
                        landingStatusTrendVM.selectedGraphTime = GraphTime.Minutes20;
                        break;

                    case 1:
                        viewModel.selectedGraphTime = GraphTime.Hours3;
                        landingStatusTrendVM.selectedGraphTime = GraphTime.Hours3;
                        break;
                }
            }
        }
    }
}
