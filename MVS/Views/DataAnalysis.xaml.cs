using System.Windows.Controls;

namespace MVS
{
    /// <summary>
    /// Interaction logic for DataAnalysis.xaml
    /// </summary>
    public partial class DataAnalysis : UserControl
    {
        public DataAnalysis()
        {
            InitializeComponent();
        }

        public void Init(ProjectVM projectVM)
        {
            // Context
            DataContext = projectVM;

            // Koble chart til data
            // Input
            chartPitch1.Series[0].ItemsSource = projectVM.testPitchList;
            chartRoll1.Series[0].ItemsSource = projectVM.testRollList;
            chartHeave1.Series[0].ItemsSource = projectVM.testHeaveList;

            chartPitch1.Series[1].ItemsSource = projectVM.refPitchList;
            chartRoll1.Series[1].ItemsSource = projectVM.refRollList;
            chartHeave1.Series[1].ItemsSource = projectVM.refHeaveList;

            // Mean
            chartPitch2.Series[0].ItemsSource = projectVM.testPitchMeanList;
            chartRoll2.Series[0].ItemsSource = projectVM.testRollMeanList;
            chartHeave2.Series[0].ItemsSource = projectVM.testHeaveMeanList;

            chartPitch2.Series[1].ItemsSource = projectVM.refPitchMeanList;
            chartRoll2.Series[1].ItemsSource = projectVM.refRollMeanList;
            chartHeave2.Series[1].ItemsSource = projectVM.refHeaveMeanList;

            // Deviation
            chartPitch3.Series[0].ItemsSource = projectVM.devPitchList;
            chartRoll3.Series[0].ItemsSource = projectVM.devRollList;
            chartHeave3.Series[0].ItemsSource = projectVM.devHeaveList;
        }
    }
}
