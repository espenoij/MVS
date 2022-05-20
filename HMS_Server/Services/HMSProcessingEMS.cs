using System;
using Telerik.Windows.Data;

namespace HMS_Server
{
    class HMSProcessingEMS
    {
        private HMSData waveData = new HMSData();
        private HMSData waveMax20mData = new HMSData();
        private HMSData waveMax3hData = new HMSData();

        private HMSData waveHeightData = new HMSData();
        private HMSData waveHeightMax20mData = new HMSData();
        private HMSData waveHeightMax3hData = new HMSData();

        public HMSProcessingEMS(HMSDataCollection hmsOutputData, ErrorHandler errorHandler)
        {
            // Fyller output listen med HMS Output data
            // NB! Variablene som legges inn i listen her fungerer som pekere: Oppdateres variabelen -> oppdateres listen
            // NB! Dersom nye variabler legges til i hmsOutputDataList må databasen opprettes på nytt

            RadObservableCollection<HMSData> hmsOutputDataList = hmsOutputData.GetDataList();

            hmsOutputDataList.Add(waveData);
            hmsOutputDataList.Add(waveMax20mData);
            hmsOutputDataList.Add(waveMax3hData);

            hmsOutputDataList.Add(waveHeightData);
            hmsOutputDataList.Add(waveHeightMax20mData);
            hmsOutputDataList.Add(waveHeightMax3hData);

            // Legge på litt informasjon på de variablene som ikke får dette automatisk fra sensor input data
            // (Disse verdiene blir kalkulert her, mens sensor input data bare kopieres videre med denne typen info allerede inkludert)

            waveMax20mData.id = (int)ValueType.WaveMax20m;
            waveMax20mData.name = "Wave Max (20m)";
            waveMax20mData.dbColumn = "wave_max_20m";
            waveMax20mData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser);
            waveMax20mData.AddProcessing(CalculationType.RoundingDecimals, 1);
            waveMax20mData.AddProcessing(CalculationType.TimeMaxAbsolute, Constants.Minutes20);

            waveMax3hData.id = (int)ValueType.WaveMax3h;
            waveMax3hData.name = "Wave Max (3h)";
            waveMax3hData.dbColumn = "wave_max_3h";
            waveMax3hData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser);
            waveMax3hData.AddProcessing(CalculationType.RoundingDecimals, 1);
            waveMax3hData.AddProcessing(CalculationType.TimeMaxAbsolute, Constants.Hours3);

            waveHeightData.id = (int)ValueType.WaveHeight;
            waveHeightData.name = "Wave Height";
            waveHeightData.dbColumn = "wave_height";
            waveHeightData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser);
            waveHeightData.AddProcessing(CalculationType.WaveHeight, 0);
            waveHeightData.AddProcessing(CalculationType.RoundingDecimals, 1);

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

            waveHeightData.Set(hmsInputDataList.GetData(ValueType.Wave));

            waveHeightMax20mData.DoProcessing(waveHeightData);
            waveHeightMax3hData.DoProcessing(waveHeightData);
        }
    }
}