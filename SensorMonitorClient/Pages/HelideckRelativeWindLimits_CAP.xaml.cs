using System.Windows.Controls;

namespace HMS_Client
{
    /// <summary>
    /// Interaction logic for HelideckRelativeWindLimits_CAP.xaml
    /// </summary>
    public partial class HelideckRelativeWindLimits_CAP : UserControl
    {
        //public RadObservableCollectionEx<Point> blueArea = new RadObservableCollectionEx<Point>();

        public HelideckRelativeWindLimits_CAP()
        {
            InitializeComponent();
        }

        public void Init(HelideckRelativeWindLimitsVM viewModel)
        {
            DataContext = viewModel;

            // Koble chart til data
            chartRelativeWindLimits.Series[0].ItemsSource = viewModel.relativeWindDir20mDataList;
        }
    }
}
