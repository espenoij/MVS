using System.Windows.Controls;

namespace MVS
{
    /// <summary>
    /// Interaction logic for MVSSessionGraphs.xaml
    /// </summary>
    public partial class MVSSessionGraphs : UserControl
    {
        public MVSSessionGraphs()
        {
            InitializeComponent();
        }

        public void Init(HelideckMotionVM helideckMotionVM)
        {
            // Context
            DataContext = helideckMotionVM;

            // Koble chart til data
            chartPitch.Series[0].ItemsSource = helideckMotionVM.refPitchList;
            chartRoll.Series[0].ItemsSource = helideckMotionVM.refRollList;
            chartHeave.Series[0].ItemsSource = helideckMotionVM.refHeaveList;

            chartPitch.Series[1].ItemsSource = helideckMotionVM.testPitchList;
            chartRoll.Series[1].ItemsSource = helideckMotionVM.testRollList;
            chartHeave.Series[1].ItemsSource = helideckMotionVM.testHeaveList;

            chartPitch20m.Series[0].ItemsSource = helideckMotionVM.refPitch20mList;
            chartRoll20m.Series[0].ItemsSource = helideckMotionVM.refRoll20mList;
            chartHeaveAmplitude20m.Series[0].ItemsSource = helideckMotionVM.refHeave20mList;

            chartPitch20m.Series[1].ItemsSource = helideckMotionVM.testPitch20mList;
            chartRoll20m.Series[1].ItemsSource = helideckMotionVM.testRoll20mList;
            chartHeaveAmplitude20m.Series[1].ItemsSource = helideckMotionVM.testHeave20mList;
        }
    }
}
