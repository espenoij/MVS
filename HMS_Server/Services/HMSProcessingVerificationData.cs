using System;

namespace HMS_Server
{
    class HMSProcessingVerificationData
    {
        private HMSData timeId = new HMSData();
        private HMSData sensorMRU = new HMSData();
        private HMSData sensorGyro = new HMSData();
        private HMSData sensorWind = new HMSData();

        public HMSProcessingVerificationData(DataCollection hmsOutputData)
        {
            // Fyller output listen med HMS Output data
            // NB! Variablene som legges inn i listen her fungerer som pekere: Oppdateres variabelen -> oppdateres listen
            // NB! Dersom nye variabler legges til i hmsOutputDataList må databasen opprettes på nytt

            RadObservableCollectionEx<HMSData> hmsOutputDataList = hmsOutputData.GetDataList();

            hmsOutputDataList.Add(timeId);
            hmsOutputDataList.Add(sensorMRU);
            hmsOutputDataList.Add(sensorGyro);
            hmsOutputDataList.Add(sensorWind);
        }

        public void Update(DataCollection hmsInputDataList)
        {
            // Tar data fra input delen av server og overfører til HMS output delen
            timeId.Set(hmsInputDataList.GetData(ValueType.TimeID));
            sensorMRU.Set(hmsInputDataList.GetData(ValueType.SensorMRU));
            sensorGyro.Set(hmsInputDataList.GetData(ValueType.SensorGyro));
            sensorWind.Set(hmsInputDataList.GetData(ValueType.SensorWind));
        }
    }
}