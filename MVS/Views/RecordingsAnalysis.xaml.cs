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

        }

        private void btnRunAnalysis_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            // Laste session data fra databasen
            mvsDatabase.LoadSessionData(mainWindowVM.SelectedSession, mvsInputData, recordingsAnalysisVM.refDataList);

            // Prosessere session dasta
            recordingsAnalysisVM.ProcessSessionData();

            // Overføre session data til grafer
            recordingsAnalysisVM.TransferSessionData();
        }
    }
}
