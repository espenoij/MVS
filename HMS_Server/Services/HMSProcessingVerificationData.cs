using System;
using Telerik.Windows.Data;

namespace HMS_Server
{
    class HMSProcessingVerificationData
    {
        //private HMSData sensorMRU = new HMSData();
        //private HMSData sensorGyro = new HMSData();
        //private HMSData sensorWind = new HMSData();

        public HMSProcessingVerificationData(HMSDataCollection hmsOutputData)
        {
            // Fyller output listen med HMS Output data
            // NB! Variablene som legges inn i listen her fungerer som pekere: Oppdateres variabelen -> oppdateres listen
            // NB! Dersom nye variabler legges til i hmsOutputDataList må databasen opprettes på nytt

            RadObservableCollection<HMSData> hmsOutputDataList = hmsOutputData.GetDataList();

            //hmsOutputDataList.Add(sensorMRU);
            //hmsOutputDataList.Add(sensorGyro);
            //hmsOutputDataList.Add(sensorWind);
        }

        public void Update(HMSDataCollection hmsInputDataList)
        {
            //// Tar data fra input delen av server og overfører til HMS output delen
            //sensorMRU.Set(hmsInputDataList.GetData(ValueType.SensorMRU));
            //sensorGyro.Set(hmsInputDataList.GetData(ValueType.SensorGyro));
            //sensorWind.Set(hmsInputDataList.GetData(ValueType.SensorWind));
        }
    }
}