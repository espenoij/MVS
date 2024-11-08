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
            chartPitch.Series[0].ItemsSource = projectVM.testPitchList;
            chartRoll.Series[0].ItemsSource = projectVM.testRollList;
            chartHeaveAmplitude.Series[0].ItemsSource = projectVM.testHeaveAmplitudeList;

            chartPitch.Series[1].ItemsSource = projectVM.refPitchList;
            chartRoll.Series[1].ItemsSource = projectVM.refRollList;
            chartHeaveAmplitude.Series[1].ItemsSource = projectVM.refHeaveAmplitudeList;

            chartPitch20m.Series[0].ItemsSource = projectVM.testPitchMean20mList;
            chartRoll20m.Series[0].ItemsSource = projectVM.testRollMean20mList;
            chartHeaveAmplitude20m.Series[0].ItemsSource = projectVM.testHeaveAmplitudeMean20mList;

            chartPitch20m.Series[1].ItemsSource = projectVM.refPitchMean20mList;
            chartRoll20m.Series[1].ItemsSource = projectVM.refRollMean20mList;
            chartHeaveAmplitude20m.Series[1].ItemsSource = projectVM.refHeaveAmplitudeMean20mList;
        }
    }
}
