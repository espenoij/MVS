using System.Windows;
using System.Windows.Controls;
using Telerik.Windows.Controls;

namespace HMS_Client
{
    /// <summary>
    /// Interaction logic for OnDeckStabilityLimits_CAP.xaml
    /// </summary>
    public partial class OnDeckStabilityLimits_CAP : UserControl
    {
        public RadObservableCollectionEx<Point> blueArea = new RadObservableCollectionEx<Point>();

        private OnDeckStabilityLimitsVM viewModel;
        private RelativeWindLimitsVM relativeWindLimitsVM;

        public OnDeckStabilityLimits_CAP()
        {
            InitializeComponent();
        }

        public void Init(OnDeckStabilityLimitsVM viewModel, RelativeWindLimitsVM relativeWindLimitsVM)
        {
            this.viewModel = viewModel;
            this.relativeWindLimitsVM = relativeWindLimitsVM;
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
                        relativeWindLimitsVM.selectedGraphTime = GraphTime.Minutes20;
                        break;

                    case 1:
                        viewModel.selectedGraphTime = GraphTime.Hours3;
                        relativeWindLimitsVM.selectedGraphTime = GraphTime.Hours3;
                        break;
                }
            }
        }
    }
}
