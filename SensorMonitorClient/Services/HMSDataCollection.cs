using System.Linq;
using Telerik.Windows.Data;

namespace SensorMonitorClient
{
    public class HMSDataCollection
    {
        // Denne klassen fungerer som en wrapper for en HMS data liste (HMSData)
        // Funksjonalitet:
        // - Hente hele listen
        // - Hente individuelle data basert på ID

        // Liste med HMS data
        public RadObservableCollectionEx<HMSData> hmsDataList = new RadObservableCollectionEx<HMSData>();

        // Init
        public HMSDataCollection()
        {
        }

        public RadObservableCollectionEx<HMSData> GetDataList()
        {
            return hmsDataList;
        }

        public HMSData GetData(ValueType id)
        {
            var sensorData = hmsDataList?.Where(x => x.id == (int)id);
            if (sensorData.Count() > 0)
                return sensorData.First();
            else
                return new HMSData() { id = (int)id, status = DataStatus.TIMEOUT_ERROR };
        }
    }
}
