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

        public RadObservableCollection<SessionData> refDataList = new RadObservableCollection<SessionData>();

        public RadObservableCollection<HMSData> refPitchList = new RadObservableCollection<HMSData>();

        public RadObservableCollection<HMSData> testPitchList = new RadObservableCollection<HMSData>();

        public void Init()
        {
        }

        // Variabel oppdatert
        // Dersom navn ikke er satt brukes kallende medlem sitt navn
        protected virtual void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
