using System.Windows.Controls;

namespace MVS
{
    /// <summary>
    /// Interaction logic for CaptureCharts.xaml
    ///
    /// Live Pitch/Roll/Heave input charts shown on the Capture wizard step.
    /// The series are bound to the same live lists that the ProjectVM chart-update
    /// timer maintains while a recording session is active.
    /// </summary>
    public partial class CaptureCharts : UserControl
    {
        public CaptureCharts()
        {
            InitializeComponent();
        }

        public void Init(ProjectVM projectVM)
        {
            // Context
            DataContext = projectVM;

            // Bind to the same live lists that the ProjectVM chart-update timer maintains.
            // Series[0] = Ref, Series[1] = Test (swapped from DataAnalysis to show vessel on top).
            chartPitch.Series[0].ItemsSource = projectVM.refPitchList;
            chartPitch.Series[1].ItemsSource = projectVM.testPitchList;

            chartRoll.Series[0].ItemsSource = projectVM.refRollList;
            chartRoll.Series[1].ItemsSource = projectVM.testRollList;

            chartHeave.Series[0].ItemsSource = projectVM.refHeaveList;
            chartHeave.Series[1].ItemsSource = projectVM.testHeaveList;
        }
    }
}
