using System.Windows;
using System.Windows.Controls;
using Telerik.Windows.Controls;

namespace HMS_Client
{
    /// <summary>
    /// Interaction logic for HelideckStabilityLimits_CAP.xaml
    /// </summary>
    public partial class HelideckStabilityLimits_CAP : UserControl
    {
        public RadObservableCollectionEx<Point> blueArea = new RadObservableCollectionEx<Point>();

        private HelideckStabilityLimitsVM viewModel;
        private LandingStatusTrendVM helideckStatusTrendVM;

        public HelideckStabilityLimits_CAP()
        {
            InitializeComponent();
        }

        public void Init(HelideckStabilityLimitsVM viewModel, LandingStatusTrendVM helideckStatusTrendVM)
        {
            this.viewModel = viewModel;
            this.helideckStatusTrendVM = helideckStatusTrendVM;
            DataContext = viewModel;

            // Koble chart til data
            chartMSIWSI20m.Series[0].ItemsSource = viewModel.msiwsi20mDataList;
            chartMSIWSI3h.Series[0].ItemsSource = viewModel.msiwsi3hDataList;

            // Blå bakgrunn på graf
            // Har laget til denne koden i tilfelle vi trenger å dynamisk endre det blå området
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
                        helideckStatusTrendVM.selectedGraphTime = GraphTime.Minutes20;
                        break;

                    case 1:
                        viewModel.selectedGraphTime = GraphTime.Hours3;
                        helideckStatusTrendVM.selectedGraphTime = GraphTime.Hours3;
                        break;
                }
            }
        }
    }
}
