using System.Windows.Controls;
using Telerik.Windows.Data;

namespace MVS
{
    /// <summary>
    /// Interaction logic for RecordingsData.xaml
    /// </summary>
    public partial class RecordingsAnalysis : UserControl
    {
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

        public void Init(RecordingsAnalysisVM RecordingsDataVM)
        {
            // Context
            DataContext = RecordingsDataVM;

            // Koble chart til data
            chartPitch.Series[0].ItemsSource = testPitchList;
            chartRoll.Series[0].ItemsSource = testRollList;
            chartHeave.Series[0].ItemsSource = testHeaveList;

            chartPitch.Series[1].ItemsSource = refPitchList;
            chartRoll.Series[1].ItemsSource = refRollList;
            chartHeave.Series[1].ItemsSource = refHeaveList;

            chartPitch20m.Series[0].ItemsSource = testPitchMean20mList;
            chartRoll20m.Series[0].ItemsSource = testRollMean20mList;
            chartHeaveAmplitude20m.Series[0].ItemsSource = testHeaveAmplitudeMean20mList;

            chartPitch20m.Series[1].ItemsSource = refPitchMean20mList;
            chartRoll20m.Series[1].ItemsSource = refRollMean20mList;
            chartHeaveAmplitude20m.Series[1].ItemsSource = refHeaveAmplitudeMean20mList;
        }

        public void TransferToDisplay(RecordingsAnalysisVM recordingsAnalysisVM)
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
