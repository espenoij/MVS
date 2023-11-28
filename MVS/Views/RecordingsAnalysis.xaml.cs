using System.Windows.Controls;
using Telerik.Windows.Data;

namespace MVS
{
    /// <summary>
    /// Interaction logic for RecordingsAnalysis.xaml
    /// </summary>
    public partial class RecordingsAnalysis : UserControl
    {
        private RecordingsAnalysisVM recordingsAnalysisVM;

        private RadObservableCollection<HMSData> refPitchList = new RadObservableCollection<HMSData>();
        private RadObservableCollection<HMSData> refPitchMean20mList = new RadObservableCollection<HMSData>();
        private RadObservableCollection<HMSData> testPitchList = new RadObservableCollection<HMSData>();
        private RadObservableCollection<HMSData> testPitchMean20mList = new RadObservableCollection<HMSData>();

        private RadObservableCollection<HMSData> refRollList = new RadObservableCollection<HMSData>();
        private RadObservableCollection<HMSData> refRollMean20mList = new RadObservableCollection<HMSData>();
        private RadObservableCollection<HMSData> testRollList = new RadObservableCollection<HMSData>();
        private RadObservableCollection<HMSData> testRollMean20mList = new RadObservableCollection<HMSData>();

        private RadObservableCollection<HMSData> refHeaveList = new RadObservableCollection<HMSData>();
        private RadObservableCollection<HMSData> refHeaveAmplitudeMean20mList = new RadObservableCollection<HMSData>();
        private RadObservableCollection<HMSData> testHeaveList = new RadObservableCollection<HMSData>();
        private RadObservableCollection<HMSData> testHeaveAmplitudeMean20mList = new RadObservableCollection<HMSData>();

        public RecordingsAnalysis()
        {
            InitializeComponent();
        }

        public void Init(RecordingsAnalysisVM recordingsAnalysisVM)
        {
            // Context
            DataContext = recordingsAnalysisVM;

            this.recordingsAnalysisVM = recordingsAnalysisVM;

            // Koble chart til data
            chartPitch.Series[0].ItemsSource = testPitchList;
            chartPitch.Series[1].ItemsSource = refPitchList;
            chartPitch20m.Series[0].ItemsSource = testPitchMean20mList;
            chartPitch20m.Series[1].ItemsSource = refPitchMean20mList;

            chartRoll.Series[0].ItemsSource = testRollList;
            chartRoll.Series[1].ItemsSource = refRollList;
            chartRoll20m.Series[0].ItemsSource = testRollMean20mList;
            chartRoll20m.Series[1].ItemsSource = refRollMean20mList;

            chartHeave.Series[0].ItemsSource = testHeaveList;
            chartHeave.Series[1].ItemsSource = refHeaveList;
            chartHeave20m.Series[0].ItemsSource = testHeaveAmplitudeMean20mList;
            chartHeave20m.Series[1].ItemsSource = refHeaveAmplitudeMean20mList;
        }

        public void TransferToDisplay()
        {
            DisplayList.TransferDirect(recordingsAnalysisVM.refPitchList, refPitchList);
            DisplayList.TransferDirect(recordingsAnalysisVM.refPitchMean20mList, refPitchMean20mList);
            DisplayList.TransferDirect(recordingsAnalysisVM.testPitchList, testPitchList);
            DisplayList.TransferDirect(recordingsAnalysisVM.testPitchMean20mList, testPitchMean20mList);

            DisplayList.TransferDirect(recordingsAnalysisVM.refRollList, refRollList);
            DisplayList.TransferDirect(recordingsAnalysisVM.refRollMean20mList, refRollMean20mList);
            DisplayList.TransferDirect(recordingsAnalysisVM.testRollList, testRollList);
            DisplayList.TransferDirect(recordingsAnalysisVM.testRollMean20mList, testRollMean20mList);

            DisplayList.TransferDirect(recordingsAnalysisVM.refHeaveList, refHeaveList);
            DisplayList.TransferDirect(recordingsAnalysisVM.refHeaveAmplitudeMean20mList, refHeaveAmplitudeMean20mList);
            DisplayList.TransferDirect(recordingsAnalysisVM.testHeaveList, testHeaveList);
            DisplayList.TransferDirect(recordingsAnalysisVM.testHeaveAmplitudeMean20mList, testHeaveAmplitudeMean20mList);

        }
    }
}
