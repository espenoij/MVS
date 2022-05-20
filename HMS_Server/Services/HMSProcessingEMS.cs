using System;
using Telerik.Windows.Data;

namespace HMS_Server
{
    class HMSProcessingEMS
    {
        private HMSData waveData = new HMSData();
        private HMSData waveMax20mData = new HMSData();
        private HMSData waveMax3hData = new HMSData();

        private HMSData waveMeanHeightData = new HMSData();
        private HMSData waveMeanHeightMax20mData = new HMSData();
        private HMSData waveMeanHeightMax3hData = new HMSData();

        private HMSData wavePeriodData = new HMSData();
        private HMSData wavePeriodMax20mData = new HMSData();
        private HMSData wavePeriodMax3hData = new HMSData();

        public HMSProcessingEMS(HMSDataCollection hmsOutputData, ErrorHandler errorHandler)
        {
            // Fyller output listen med HMS Output data
            // NB! Variablene som legges inn i listen her fungerer som pekere: Oppdateres variabelen -> oppdateres listen
            // NB! Dersom nye variabler legges til i hmsOutputDataList må databasen opprettes på nytt

            RadObservableCollection<HMSData> hmsOutputDataList = hmsOutputData.GetDataList();

            hmsOutputDataList.Add(waveData);
            hmsOutputDataList.Add(waveMax20mData);
            hmsOutputDataList.Add(waveMax3hData);

            hmsOutputDataList.Add(waveMeanHeightData);
            hmsOutputDataList.Add(waveMeanHeightMax20mData);
            hmsOutputDataList.Add(waveMeanHeightMax3hData);

            hmsOutputDataList.Add(wavePeriodData);
            hmsOutputDataList.Add(wavePeriodMax20mData);
            hmsOutputDataList.Add(wavePeriodMax3hData);

            // Legge på litt informasjon på de variablene som ikke får dette automatisk fra sensor input data
            // (Disse verdiene blir kalkulert her, mens sensor input data bare kopieres videre med denne typen info allerede inkludert)

            // Wave Data
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

            // Wave Height
            waveMeanHeightData.id = (int)ValueType.WaveHeight;
            waveMeanHeightData.name = "Wave Height";
            waveMeanHeightData.dbColumn = "wave_height";
            waveMeanHeightData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser);
            waveMeanHeightData.AddProcessing(CalculationType.MeanWaveHeight, Constants.Minutes20);
            waveMeanHeightData.AddProcessing(CalculationType.RoundingDecimals, 1);

            waveMeanHeightMax20mData.id = (int)ValueType.WaveHeightMax20m;
            waveMeanHeightMax20mData.name = "Wave Height Max (20m)";
            waveMeanHeightMax20mData.dbColumn = "wave_height_max_20m";
            waveMeanHeightMax20mData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser);
            waveMeanHeightMax20mData.AddProcessing(CalculationType.RoundingDecimals, 1);
            waveMeanHeightMax20mData.AddProcessing(CalculationType.TimeMaxAbsolute, Constants.Minutes20);

            waveMeanHeightMax3hData.id = (int)ValueType.WaveHeightMax3h;
            waveMeanHeightMax3hData.name = "Wave Height Max (3h)";
            waveMeanHeightMax3hData.dbColumn = "wave_height_max_3h";
            waveMeanHeightMax3hData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser);
            waveMeanHeightMax3hData.AddProcessing(CalculationType.RoundingDecimals, 1);
            waveMeanHeightMax3hData.AddProcessing(CalculationType.TimeMaxAbsolute, Constants.Hours3);

            // Mean Wave Period
            wavePeriodData.id = (int)ValueType.WavePeriod;
            wavePeriodData.name = "Wave Period";
            wavePeriodData.dbColumn = "wave_period";
            wavePeriodData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser);
            wavePeriodData.AddProcessing(CalculationType.TimeMeanPeriod, Constants.Minutes20);
            wavePeriodData.AddProcessing(CalculationType.RoundingDecimals, 2);

            wavePeriodMax20mData.id = (int)ValueType.WavePeriodMax20m;
            wavePeriodMax20mData.name = "Wave Period Max (20m)";
            wavePeriodMax20mData.dbColumn = "wave_period_max_20m";
            wavePeriodMax20mData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser);
            wavePeriodMax20mData.AddProcessing(CalculationType.RoundingDecimals, 1);
            wavePeriodMax20mData.AddProcessing(CalculationType.TimeMaxAbsolute, Constants.Minutes20);

            wavePeriodMax3hData.id = (int)ValueType.WavePeriodMax3h;
            wavePeriodMax3hData.name = "Wave Period Max (3h)";
            wavePeriodMax3hData.dbColumn = "wave_period_max_3h";
            wavePeriodMax3hData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser);
            wavePeriodMax3hData.AddProcessing(CalculationType.RoundingDecimals, 1);
            wavePeriodMax3hData.AddProcessing(CalculationType.TimeMaxAbsolute, Constants.Hours3);
        }

        public void Update(HMSDataCollection hmsInputDataList)
        {
            // Tar data fra input delen av server og overfører til HMS output delen
            waveData.Set(hmsInputDataList.GetData(ValueType.Wave));
            waveMax20mData.DoProcessing(waveData);
            waveMax3hData.DoProcessing(waveData);

            waveMeanHeightData.DoProcessing(waveData);
            waveMeanHeightMax20mData.DoProcessing(waveMeanHeightData);
            waveMeanHeightMax3hData.DoProcessing(waveMeanHeightData);

            wavePeriodData.DoProcessing(waveData);
            wavePeriodMax20mData.DoProcessing(waveMeanHeightData);
            wavePeriodMax3hData.DoProcessing(waveMeanHeightData);
        }
    }
}