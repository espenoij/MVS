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
            chartPitch1.Series[0].ItemsSource = projectVM.testPitchList;
            chartRoll1.Series[0].ItemsSource = projectVM.testRollList;
            chartHeaveAmplitude1.Series[0].ItemsSource = projectVM.testHeaveAmplitudeList;

            chartPitch1.Series[1].ItemsSource = projectVM.refPitchList;
            chartRoll1.Series[1].ItemsSource = projectVM.refRollList;
            chartHeaveAmplitude1.Series[1].ItemsSource = projectVM.refHeaveAmplitudeList;

            chartPitch2.Series[0].ItemsSource = projectVM.testPitchMeanList;
            chartRoll2.Series[0].ItemsSource = projectVM.testRollMeanList;
            chartHeaveAmplitude2.Series[0].ItemsSource = projectVM.testHeaveAmplitudeMeanList;

            chartPitch2.Series[1].ItemsSource = projectVM.refPitchMeanList;
            chartRoll2.Series[1].ItemsSource = projectVM.refRollMeanList;
            chartHeaveAmplitude2.Series[1].ItemsSource = projectVM.refHeaveAmplitudeMeanList;
        }
    }
}
