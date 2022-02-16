using System.Linq;
using System.Windows.Data;
using Telerik.Windows.Data;

namespace HMS_Client
{
    public class HMSDataCollection
    {
        // Denne klassen fungerer som en wrapper for en HMS data liste (HMSData)
        // Funksjonalitet:
        // - Hente hele listen
        // - Hente individuelle data basert på ID

        // Liste med HMS data
        public RadObservableCollectionEx<HMSData> hmsDataList;
        private object hmsDataListLock = new object();

        // Init
        public HMSDataCollection()
        {
            hmsDataList = new RadObservableCollectionEx<HMSData>();
            BindingOperations.EnableCollectionSynchronization(hmsDataList, hmsDataListLock);
        }

        public RadObservableCollectionEx<HMSData> GetDataList()
        {
            return hmsDataList;
        }

        public RadObservableCollectionEx<HMSData> GetDataList(int sensorGroupId)
        {
            lock (hmsDataListLock)
            {
                var sensorData = hmsDataList?.Where(x => x?.sensorGroupId == sensorGroupId);
                if (sensorData.Count() > 0)
                    return sensorData as RadObservableCollectionEx<HMSData>;
                else
                    return null;
            }
        }

        public HMSData GetData(ValueType id)
        {
            lock (hmsDataListLock)
            {
                var sensorData = hmsDataList?.Where(x => x?.id == (int)id);
                if (sensorData.Count() > 0)
                    return sensorData.First();
                else
                    return new HMSData() { id = (int)id, status = DataStatus.TIMEOUT_ERROR };
            }
        }
    }
}
