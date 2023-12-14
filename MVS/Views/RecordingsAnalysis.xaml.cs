using System.Windows.Controls;

namespace MVS
{
    /// <summary>
    /// Interaction logic for RecordingsData.xaml
    /// </summary>
    public partial class RecordingsAnalysis : UserControl
    {
        public RecordingsAnalysis()
        {
            InitializeComponent();
        }

        public void Init(RecordingsVM recordingsVM)
        {
            // Context
            DataContext = recordingsVM;

            // Koble chart til data
            chartPitch.Series[0].ItemsSource = recordingsVM.testPitchList;
            chartRoll.Series[0].ItemsSource = recordingsVM.testRollList;
            chartHeaveAmplitude.Series[0].ItemsSource = recordingsVM.testHeaveAmplitudeList;

            chartPitch.Series[1].ItemsSource = recordingsVM.refPitchList;
            chartRoll.Series[1].ItemsSource = recordingsVM.refRollList;
            chartHeaveAmplitude.Series[1].ItemsSource = recordingsVM.refHeaveAmplitudeList;

            chartPitch20m.Series[0].ItemsSource = recordingsVM.testPitchMean20mList;
            chartRoll20m.Series[0].ItemsSource = recordingsVM.testRollMean20mList;
            chartHeaveAmplitude20m.Series[0].ItemsSource = recordingsVM.testHeaveAmplitudeMean20mList;

            chartPitch20m.Series[1].ItemsSource = recordingsVM.refPitchMean20mList;
            chartRoll20m.Series[1].ItemsSource = recordingsVM.refRollMean20mList;
            chartHeaveAmplitude20m.Series[1].ItemsSource = recordingsVM.refHeaveAmplitudeMean20mList;
        }
    }
}
