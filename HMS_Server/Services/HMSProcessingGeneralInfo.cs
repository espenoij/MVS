using System;

namespace HMS_Server
{
    class HMSProcessingGeneralInfo
    {
        private HMSData gpsLatitude = new HMSData();
        private HMSData gpsLongitude = new HMSData();

        public HMSProcessingGeneralInfo(DataCollection hmsOutputData)
        {
            // Fyller output listen med HMS Output data
            // NB! Variablene som legges inn i listen her fungerer som pekere: Oppdateres variabelen -> oppdateres listen

            RadObservableCollectionEx<HMSData> hmsOutputDataList = hmsOutputData.GetDataList();

            hmsOutputDataList.Add(gpsLatitude);
            hmsOutputDataList.Add(gpsLongitude);
        }

        public void Update(DataCollection hmsInputDataList)
        {
            // Tar data fra input delen av server og overfører til HMS output delen

            gpsLatitude.Set(hmsInputDataList.GetData(ValueType.Latitude));
            gpsLongitude.Set(hmsInputDataList.GetData(ValueType.Longitude));
        }
    }
}