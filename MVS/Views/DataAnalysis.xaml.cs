using System.Windows.Controls;
using MVS.Views.Controls;

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

            BindComparisonChart(chartPitch1, projectVM.refPitchList, projectVM.testPitchList);
            BindComparisonChart(chartRoll1, projectVM.refRollList, projectVM.testRollList);
            BindComparisonChart(chartHeave1, projectVM.refHeaveList, projectVM.testHeaveList);

            BindComparisonChart(chartPitch2, projectVM.refPitchMeanList, projectVM.testPitchMeanList);
            BindComparisonChart(chartRoll2, projectVM.refRollMeanList, projectVM.testRollMeanList);
            BindComparisonChart(chartHeave2, projectVM.refHeaveMeanList, projectVM.testHeaveMeanList);

            BindDeviationChart(chartPitch3, projectVM.devPitchMeanList);
            BindDeviationChart(chartRoll3, projectVM.devRollMeanList);
            BindDeviationChart(chartHeave3, projectVM.devHeaveMeanList);
        }

        private static void BindComparisonChart(
            LiveLineChart chart,
            ObservableCollectionEx<HMSData> reference,
            ObservableCollectionEx<HMSData> test)
        {
            chart.ReferenceItemsSource = reference;
            chart.TestItemsSource = test;
        }

        private static void BindDeviationChart(LiveLineChart chart, ObservableCollectionEx<HMSData> deviation)
        {
            chart.TestItemsSource = deviation;
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
