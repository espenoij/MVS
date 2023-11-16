using System.Windows.Controls;

namespace MVS
{
    /// <summary>
    /// Interaction logic for RecordingsData.xaml
    /// </summary>
    public partial class RecordingsData : UserControl
    {
        public RecordingsData()
        {
            InitializeComponent();
        }

        public void Init(RecordingsDataVM helideckMotionVM)
        {
            // Context
            DataContext = helideckMotionVM;

            // Koble chart til data
            chartPitch.Series[0].ItemsSource = helideckMotionVM.testPitchList;
            chartRoll.Series[0].ItemsSource = helideckMotionVM.testRollList;
            chartHeave.Series[0].ItemsSource = helideckMotionVM.testHeaveList;

            chartPitch.Series[1].ItemsSource = helideckMotionVM.refPitchList;
            chartRoll.Series[1].ItemsSource = helideckMotionVM.refRollList;
            chartHeave.Series[1].ItemsSource = helideckMotionVM.refHeaveList;

            chartPitch20m.Series[0].ItemsSource = helideckMotionVM.testPitch20mList;
            chartRoll20m.Series[0].ItemsSource = helideckMotionVM.testRoll20mList;
            chartHeaveAmplitude20m.Series[0].ItemsSource = helideckMotionVM.testHeave20mList;

            chartPitch20m.Series[1].ItemsSource = helideckMotionVM.refPitch20mList;
            chartRoll20m.Series[1].ItemsSource = helideckMotionVM.refRoll20mList;
            chartHeaveAmplitude20m.Series[1].ItemsSource = helideckMotionVM.refHeave20mList;
        }
    }
}
