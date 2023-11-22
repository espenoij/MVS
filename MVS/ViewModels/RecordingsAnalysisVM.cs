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

        // Alle session data
        public RadObservableCollection<SessionData> refDataList = new RadObservableCollection<SessionData>();

        // Graf data
        public RadObservableCollection<HMSData> refPitchList = new RadObservableCollection<HMSData>();
        public RadObservableCollection<HMSData> refPitchMean20mList = new RadObservableCollection<HMSData>();

        public RadObservableCollection<HMSData> testPitchList = new RadObservableCollection<HMSData>();
        public RadObservableCollection<HMSData> testPitchMean20mList = new RadObservableCollection<HMSData>();

        public void Init(MainWindowVM mainWindowVM, MVSProcessing mvsProcessing)
        {
            this.mainWindowVM = mainWindowVM;
            this.mvsProcessing = mvsProcessing;
        }

        public void ProcessSessionData()
        {
            foreach (var item in refDataList)
            {
                mvsInputData.TransferData(item);
                mvsProcessing.Update(mvsInputData, mainWindowVM);
            }
        }

        public void TransferSessionData()
        {
            refPitchList.Clear();
            refPitchMean20mList.Clear();

            refPitchList.Clear();
            testPitchMean20mList.Clear();

            foreach (var item in refDataList) 
            {
                refPitchList.Add(new HMSData()
                {
                    data = item.refPitch,
                    timestamp = item.timestamp,
                    status = DataStatus.OK
                });

                testPitchList.Add(new HMSData()
                {
                    data = item.testPitch,
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
