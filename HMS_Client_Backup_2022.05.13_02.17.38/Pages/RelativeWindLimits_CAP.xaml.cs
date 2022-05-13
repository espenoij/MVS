using System.Windows.Controls;

namespace HMS_Client
{
    /// <summary>
    /// Interaction logic for HelideckRelativeWindLimits_CAP.xaml
    /// </summary>
    public partial class RelativeWindLimits_CAP : UserControl
    {
        //public RadObservableCollectionEx<Point> blueArea = new RadObservableCollectionEx<Point>();

        public RelativeWindLimits_CAP()
        {
            InitializeComponent();
        }

        public void Init(RelativeWindLimitsVM relativeWindLimitsVM)
        {
            DataContext = relativeWindLimitsVM;

            // Koble chart til data
            chartRelativeWindLimits.Series[0].ItemsSource = relativeWindLimitsVM.relativeWindDir30mDataList;
        }
    }
}
