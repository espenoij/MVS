using System;
using Telerik.Windows.Data;

namespace HMS_Server
{
    class HMSProcessingEMS
    {
        private HMSData waveHeightData = new HMSData();
        private HMSData waveHeightMax20mData = new HMSData();
        private HMSData waveHeightMax3hData = new HMSData();

        public HMSProcessingEMS(HMSDataCollection hmsOutputData, ErrorHandler errorHandler)
        {
            // Fyller output listen med HMS Output data
            // NB! Variablene som legges inn i listen her fungerer som pekere: Oppdateres variabelen -> oppdateres listen
            // NB! Dersom nye variabler legges til i hmsOutputDataList må databasen opprettes på nytt

            RadObservableCollection<HMSData> hmsOutputDataList = hmsOutputData.GetDataList();

            hmsOutputDataList.Add(waveHeightData);
            hmsOutputDataList.Add(waveHeightMax20mData);
            hmsOutputDataList.Add(waveHeightMax3hData);

            // Legge på litt informasjon på de variablene som ikke får dette automatisk fra sensor input data
            // (Disse verdiene blir kalkulert her, mens sensor input data bare kopieres videre med denne typen info allerede inkludert)

            waveHeightMax20mData.id = (int)ValueType.WaveHeightMax20m;
            waveHeightMax20mData.name = "Wave Height Max (20m)";
            waveHeightMax20mData.dbColumn = "wave_height_max_20m";
            waveHeightMax20mData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser);
            waveHeightMax20mData.AddProcessing(CalculationType.RoundingDecimals, 1);
            waveHeightMax20mData.AddProcessing(CalculationType.TimeMaxAbsolute, Constants.Minutes20);

            waveHeightMax3hData.id = (int)ValueType.WaveHeightMax3h;
            waveHeightMax3hData.name = "Wave Height Max (3h)";
            waveHeightMax3hData.dbColumn = "wave_height_max_3h";
            waveHeightMax3hData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser);
            waveHeightMax3hData.AddProcessing(CalculationType.RoundingDecimals, 1);
            waveHeightMax3hData.AddProcessing(CalculationType.TimeMaxAbsolute, Constants.Hours3);
        }

        public void Update(HMSDataCollection hmsInputDataList)
        {
            // Tar data fra input delen av server og overfører til HMS output delen

            waveHeightData.Set(hmsInputDataList.GetData(ValueType.WaveHeight));

            waveHeightMax20mData.DoProcessing(waveHeightData);
            waveHeightMax3hData.DoProcessing(waveHeightData);
        }
    }
}