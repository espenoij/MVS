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
            chartPitch3.Series[0].ItemsSource = projectVM.devPitchMeanList;
            chartRoll3.Series[0].ItemsSource = projectVM.devRollMeanList;
            chartHeave3.Series[0].ItemsSource = projectVM.devHeaveMeanList;
        }

        /// <summary>
        /// Populates the per-axis result cards shown above each axis' charts.
        /// </summary>
        public void SetResults(ProjectVM projectVM, Project project, bool hasCorrection)
        {
            if (projectVM == null)
                return;

            cardPitch.RefStats = projectVM.RefPitchStats;
            cardPitch.TestStats = projectVM.TestPitchStats;
            cardPitch.DevStats = projectVM.DevPitchStats;
            cardPitch.AppliedCorrection = project?.AppliedCorrectionPitch ?? 0d;
            cardPitch.HasCorrectionApplied = hasCorrection;

            cardRoll.RefStats = projectVM.RefRollStats;
            cardRoll.TestStats = projectVM.TestRollStats;
            cardRoll.DevStats = projectVM.DevRollStats;
            cardRoll.AppliedCorrection = project?.AppliedCorrectionRoll ?? 0d;
            cardRoll.HasCorrectionApplied = hasCorrection;

            cardHeave.RefStats = projectVM.RefHeaveStats;
            cardHeave.TestStats = projectVM.TestHeaveStats;
            cardHeave.DevStats = projectVM.DevHeaveStats;
            cardHeave.AppliedCorrection = project?.AppliedCorrectionHeave ?? 0d;
            cardHeave.HasCorrectionApplied = hasCorrection;
        }
    }
}
