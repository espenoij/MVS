using System.Windows.Controls;

namespace MVS
{
    /// <summary>
    /// Interaction logic for RecordingsAnalysis.xaml
    /// </summary>
    public partial class RecordingsAnalysis : UserControl
    {
        private RecordingsAnalysisVM recordingsAnalysisVM;
        private MainWindowVM mainWindowVM;
        private MVSDataCollection mvsInputData;
        private MVSDatabase mvsDatabase;

        public RecordingsAnalysis()
        {
            InitializeComponent();
        }

        public void Init(RecordingsAnalysisVM recordingsAnalysisVM, MainWindowVM mainWindowVM, MVSDataCollection mvsInputData, MVSDatabase mvsDatabase)
        {
            // Context
            DataContext = recordingsAnalysisVM;

            this.recordingsAnalysisVM = recordingsAnalysisVM;
            this.mainWindowVM = mainWindowVM;
            this.mvsInputData = mvsInputData;
            this.mvsDatabase = mvsDatabase;

            // Koble chart til data
            chartPitch.Series[0].ItemsSource = recordingsAnalysisVM.testPitchList;
            chartPitch.Series[1].ItemsSource = recordingsAnalysisVM.refPitchList;
            chartPitch20m.Series[0].ItemsSource = recordingsAnalysisVM.testPitchMean20mList;
            chartPitch20m.Series[1].ItemsSource = recordingsAnalysisVM.refPitchMean20mList;

            chartRoll.Series[0].ItemsSource = recordingsAnalysisVM.testRollList;
            chartRoll.Series[1].ItemsSource = recordingsAnalysisVM.refRollList;
            chartRoll20m.Series[0].ItemsSource = recordingsAnalysisVM.testRollMean20mList;
            chartRoll20m.Series[1].ItemsSource = recordingsAnalysisVM.refRollMean20mList;

            chartHeave.Series[0].ItemsSource = recordingsAnalysisVM.testHeaveList;
            chartHeave.Series[1].ItemsSource = recordingsAnalysisVM.refHeaveList;
            chartHeave20m.Series[0].ItemsSource = recordingsAnalysisVM.testHeaveAmplitudeMean20mList;
            chartHeave20m.Series[1].ItemsSource = recordingsAnalysisVM.refHeaveAmplitudeMean20mList;
        }

        private void btnRunAnalysis_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            mainWindowVM.OperationsMode = OperationsMode.Analysis;

            // Laste session data fra databasen
            mvsDatabase.LoadSessionData(mainWindowVM.SelectedSession, mvsInputData, recordingsAnalysisVM.refDataList);

            // Prosessere session data
            recordingsAnalysisVM.ProcessSessionData();

            mainWindowVM.OperationsMode = OperationsMode.Stop;
        }
    }
}
