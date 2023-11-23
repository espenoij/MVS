using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Threading;
using Telerik.Windows.Data;

namespace MVS
{
    public class RecordingsAnalysisVM : INotifyPropertyChanged
    {
        // Change notification
        public event PropertyChangedEventHandler PropertyChanged;

        private MainWindowVM mainWindowVM;
        private MVSProcessing mvsProcessing;
        private MVSDataCollection mvsInputData;
        private MVSDataCollection mvsOutputData;

        // Alle session data
        public RadObservableCollection<SessionData> refDataList = new RadObservableCollection<SessionData>();

        // Graf data
        public RadObservableCollection<HMSData> refPitchList = new RadObservableCollection<HMSData>();
        public RadObservableCollection<HMSData> refPitchMean20mList = new RadObservableCollection<HMSData>();

        public RadObservableCollection<HMSData> testPitchList = new RadObservableCollection<HMSData>();
        public RadObservableCollection<HMSData> testPitchMean20mList = new RadObservableCollection<HMSData>();

        public void Init(MainWindowVM mainWindowVM, MVSProcessing mvsProcessing, MVSDataCollection mvsInputData, MVSDataCollection mvsOutputData)
        {
            this.mainWindowVM = mainWindowVM;
            this.mvsProcessing = mvsProcessing;
            this.mvsInputData = mvsInputData;
            this.mvsOutputData = mvsOutputData;
        }

        public void ProcessSessionData()
        {
            // Slette gamle data i graf data lister
            refPitchList.Clear();
            refPitchMean20mList.Clear();

            testPitchList.Clear();
            testPitchMean20mList.Clear();

            foreach (var item in refDataList)
            {
                // Overføre data fra DB til input
                mvsInputData.TransferData(item);

                // Prosessere data i input
                mvsProcessing.Update(mvsInputData, mainWindowVM);

                // TEST TEST TEST
                double test1 = mvsOutputData.GetData(ValueType.Ref_Pitch).data;
                double test2 = mvsOutputData.GetData(ValueType.Test_Pitch).data;

                // Overføre data til grafer
                refPitchList.Add(new HMSData()
                {
                    data = mvsOutputData.GetData(ValueType.Ref_Pitch).data,
                    timestamp = item.timestamp,
                    status = DataStatus.OK
                });

                refPitchMean20mList.Add(new HMSData()
                {
                    data = mvsOutputData.GetData(ValueType.Ref_PitchMean20m).data,
                    timestamp = item.timestamp,
                    status = DataStatus.OK
                });

                testPitchList.Add(new HMSData()
                {
                    data = mvsOutputData.GetData(ValueType.Test_Pitch).data,
                    timestamp = item.timestamp,
                    status = DataStatus.OK
                });

                testPitchMean20mList.Add(new HMSData()
                {
                    data = mvsOutputData.GetData(ValueType.Test_PitchMean20m).data,
                    timestamp = item.timestamp,
                    status = DataStatus.OK
                });
            }
        }

        // Variabel oppdatert
        // Dersom navn ikke er satt brukes kallende medlem sitt navn
        protected virtual void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
