using System;

namespace HMS_Server
{
    class HMSProcessingGeneralInfo
    {
        private HMSData gpsLatitude = new HMSData();
        private HMSData gpsLongitude = new HMSData();

        public HMSProcessingGeneralInfo(HMSDataCollection hmsOutputData)
        {
            // Fyller output listen med HMS Output data
            // NB! Variablene som legges inn i listen her fungerer som pekere: Oppdateres variabelen -> oppdateres listen
            // NB! Dersom nye variabler legges til i hmsOutputDataList må databasen opprettes på nytt

            RadObservableCollectionEx<HMSData> hmsOutputDataList = hmsOutputData.GetDataList();

            hmsOutputDataList.Add(gpsLatitude);
            hmsOutputDataList.Add(gpsLongitude);
        }

        public void Update(HMSDataCollection hmsInputDataList)
        {
            // Tar data fra input delen av server og overfører til HMS output delen

            gpsLatitude.Set(hmsInputDataList.GetData(ValueType.Latitude));
            gpsLongitude.Set(hmsInputDataList.GetData(ValueType.Longitude));
        }
    }
}