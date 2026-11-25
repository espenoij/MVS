using System.Windows.Controls;
using MVS.Views.Controls;

namespace MVS
{
    /// <summary>
    /// Interaction logic for CaptureCharts.xaml
    ///
    /// Live Pitch/Roll/Heave input charts shown on the Capture wizard step.
    /// The charts observe the same live lists that the ProjectVM chart-update
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
            DataContext = projectVM;

            chartPitch.ReferenceItemsSource = projectVM.refPitchList;
            chartPitch.TestItemsSource = projectVM.testPitchList;
            chartRoll.ReferenceItemsSource = projectVM.refRollList;
            chartRoll.TestItemsSource = projectVM.testRollList;
            chartHeave.ReferenceItemsSource = projectVM.refHeaveList;
            chartHeave.TestItemsSource = projectVM.testHeaveList;
        }
    }
}
