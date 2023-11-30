using System.Windows.Controls;
using Telerik.Windows.Data;

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

        public void Init(RecordingsAnalysisVM recordingsAnalysisVM)
        {
            // Context
            DataContext = recordingsAnalysisVM;

            // Koble chart til data
            chartPitch.Series[0].ItemsSource = recordingsAnalysisVM.testPitchList;
            chartRoll.Series[0].ItemsSource = recordingsAnalysisVM.testRollList;
            chartHeave.Series[0].ItemsSource = recordingsAnalysisVM.testHeaveList;

            chartPitch.Series[1].ItemsSource = recordingsAnalysisVM.refPitchList;
            chartRoll.Series[1].ItemsSource = recordingsAnalysisVM.refRollList;
            chartHeave.Series[1].ItemsSource = recordingsAnalysisVM.refHeaveList;

            chartPitch20m.Series[0].ItemsSource = recordingsAnalysisVM.testPitchMean20mList;
            chartRoll20m.Series[0].ItemsSource = recordingsAnalysisVM.testRollMean20mList;
            chartHeaveAmplitude20m.Series[0].ItemsSource = recordingsAnalysisVM.testHeaveAmplitudeMean20mList;

            chartPitch20m.Series[1].ItemsSource = recordingsAnalysisVM.refPitchMean20mList;
            chartRoll20m.Series[1].ItemsSource = recordingsAnalysisVM.refRollMean20mList;
            chartHeaveAmplitude20m.Series[1].ItemsSource = recordingsAnalysisVM.refHeaveAmplitudeMean20mList;
        }
    }
}
