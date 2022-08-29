﻿using System;
using Telerik.Windows.Data;

namespace HMS_Server
{
    class HMSProcessingEMS
    {
        private HMSData seaTemperature = new HMSData();

        private HMSData waveData = new HMSData();
        private HMSData waveMax20mData = new HMSData();
        private HMSData waveMax3hData = new HMSData();

        private HMSData wavePeriodData = new HMSData();
        private HMSData wavePeriodMax20mData = new HMSData();
        private HMSData wavePeriodMax3hData = new HMSData();

        private HMSData waveSWHData = new HMSData();
        private HMSData waveSWHMax20mData = new HMSData();
        private HMSData waveSWHMax3hData = new HMSData();

        private AdminSettingsVM adminSettingsVM;

        public HMSProcessingEMS(HMSDataCollection hmsOutputData, ErrorHandler errorHandler, AdminSettingsVM adminSettingsVM)
        {
            this.adminSettingsVM = adminSettingsVM;

            // Fyller output listen med HMS Output data
            // NB! Variablene som legges inn i listen her fungerer som pekere: Oppdateres variabelen -> oppdateres listen
            // NB! Dersom nye variabler (m/ny dbColumn) legges til i hmsOutputDataList må databasen opprettes på nytt

            RadObservableCollection<HMSData> hmsOutputDataList = hmsOutputData.GetDataList();

            hmsOutputDataList.Add(seaTemperature);

            hmsOutputDataList.Add(waveData);
            hmsOutputDataList.Add(waveMax20mData);
            hmsOutputDataList.Add(waveMax3hData);

            hmsOutputDataList.Add(wavePeriodData);
            hmsOutputDataList.Add(wavePeriodMax20mData);
            hmsOutputDataList.Add(wavePeriodMax3hData);

            hmsOutputDataList.Add(waveSWHData);
            hmsOutputDataList.Add(waveSWHMax20mData);
            hmsOutputDataList.Add(waveSWHMax3hData);

            // Legge på litt informasjon på de variablene som ikke får dette automatisk fra sensor input data
            // (Disse verdiene blir kalkulert her, mens sensor input data bare kopieres videre med denne typen info allerede inkludert)

            // Wave Data
            waveMax20mData.id = (int)ValueType.WaveMax20m;
            waveMax20mData.name = "Wave Max (20m)";
            waveMax20mData.dbColumn = "wave_max_20m";
            waveMax20mData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser, adminSettingsVM);
            waveMax20mData.AddProcessing(CalculationType.RoundingDecimals, 1);
            waveMax20mData.AddProcessing(CalculationType.TimeMaxAbsolute, Constants.Minutes20);

            waveMax3hData.id = (int)ValueType.WaveMax3h;
            waveMax3hData.name = "Wave Max (3h)";
            waveMax3hData.dbColumn = "wave_max_3h";
            waveMax3hData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser, adminSettingsVM);
            waveMax3hData.AddProcessing(CalculationType.RoundingDecimals, 1);
            waveMax3hData.AddProcessing(CalculationType.TimeMaxAbsolute, Constants.Hours3);

            // Wave Period
            wavePeriodData.id = (int)ValueType.WavePeriod;
            wavePeriodData.name = "Wave Mean Period";
            wavePeriodData.dbColumn = "wave_mean_period";
            wavePeriodData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser, adminSettingsVM);
            wavePeriodData.AddProcessing(CalculationType.TimeMeanPeriod, Constants.Minutes20);
            wavePeriodData.AddProcessing(CalculationType.RoundingDecimals, 1);

            wavePeriodMax20mData.id = (int)ValueType.WavePeriodMax20m;
            wavePeriodMax20mData.name = "Wave Mean Period Max (20m)";
            wavePeriodMax20mData.dbColumn = "wave_mean_period_max_20m";
            wavePeriodMax20mData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser, adminSettingsVM);
            wavePeriodMax20mData.AddProcessing(CalculationType.TimeMaxAbsolute, Constants.Minutes20);

            wavePeriodMax3hData.id = (int)ValueType.WavePeriodMax3h;
            wavePeriodMax3hData.name = "Wave Mean Period Max (3h)";
            wavePeriodMax3hData.dbColumn = "wave_mean_period_max_3h";
            wavePeriodMax3hData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser, adminSettingsVM);
            wavePeriodMax3hData.AddProcessing(CalculationType.TimeMaxAbsolute, Constants.Hours3);

            // Significant Wave Height
            waveSWHData.id = (int)ValueType.SignificantWaveHeight;
            waveSWHData.name = "Significant Wave Height";
            waveSWHData.dbColumn = "swh";
            waveSWHData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser, adminSettingsVM);
            waveSWHData.AddProcessing(CalculationType.SignificantWaveHeight, Constants.Minutes20);
            waveSWHData.AddProcessing(CalculationType.RoundingDecimals, 1);

            waveSWHMax20mData.id = (int)ValueType.SignificantWaveHeightMax20m;
            waveSWHMax20mData.name = "Significant Wave Height Max (20m)";
            waveSWHMax20mData.dbColumn = "swh_max_20m";
            waveSWHMax20mData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser, adminSettingsVM);
            waveSWHMax20mData.AddProcessing(CalculationType.TimeMaxAbsolute, Constants.Minutes20);

            waveSWHMax3hData.id = (int)ValueType.SignificantWaveHeightMax3h;
            waveSWHMax3hData.name = "Significant Wave Height Max (3h)";
            waveSWHMax3hData.dbColumn = "swh_max_3h";
            waveSWHMax3hData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser, adminSettingsVM);
            waveSWHMax3hData.AddProcessing(CalculationType.TimeMaxAbsolute, Constants.Hours3);
        }

        public void Update(HMSDataCollection hmsInputDataList)
        {
            // Tar data fra input delen av server og overfører til HMS output delen
            seaTemperature.Set(hmsInputDataList.GetData(ValueType.SeaTemperature));

            waveData.Set(hmsInputDataList.GetData(ValueType.Wave));
            waveMax20mData.DoProcessing(waveData);
            waveMax3hData.DoProcessing(waveData);

            wavePeriodData.DoProcessing(waveData);
            wavePeriodMax20mData.DoProcessing(wavePeriodData);
            wavePeriodMax3hData.DoProcessing(wavePeriodData);

            waveSWHData.DoProcessing(waveData);
            waveSWHMax20mData.DoProcessing(waveSWHData);
            waveSWHMax3hData.DoProcessing(waveSWHData);


            // Sjekke data timeout
            if (seaTemperature.timestamp.AddMilliseconds(adminSettingsVM.dataTimeout) < DateTime.UtcNow)
                seaTemperature.status = DataStatus.TIMEOUT_ERROR;

            if (waveData.timestamp.AddMilliseconds(adminSettingsVM.dataTimeout) < DateTime.UtcNow)
                waveData.status = DataStatus.TIMEOUT_ERROR;

            if (waveMax20mData.timestamp.AddMilliseconds(adminSettingsVM.dataTimeout) < DateTime.UtcNow)
                waveMax20mData.status = DataStatus.TIMEOUT_ERROR;

            if (waveMax3hData.timestamp.AddMilliseconds(adminSettingsVM.dataTimeout) < DateTime.UtcNow)
                waveMax3hData.status = DataStatus.TIMEOUT_ERROR;

            if (wavePeriodData.timestamp.AddMilliseconds(adminSettingsVM.dataTimeout) < DateTime.UtcNow)
                wavePeriodData.status = DataStatus.TIMEOUT_ERROR;

            if (wavePeriodMax20mData.timestamp.AddMilliseconds(adminSettingsVM.dataTimeout) < DateTime.UtcNow)
                wavePeriodMax20mData.status = DataStatus.TIMEOUT_ERROR;

            if (wavePeriodMax3hData.timestamp.AddMilliseconds(adminSettingsVM.dataTimeout) < DateTime.UtcNow)
                wavePeriodMax3hData.status = DataStatus.TIMEOUT_ERROR;

            if (waveSWHData.timestamp.AddMilliseconds(adminSettingsVM.dataTimeout) < DateTime.UtcNow)
                waveSWHData.status = DataStatus.TIMEOUT_ERROR;

            if (waveSWHMax20mData.timestamp.AddMilliseconds(adminSettingsVM.dataTimeout) < DateTime.UtcNow)
                waveSWHMax20mData.status = DataStatus.TIMEOUT_ERROR;

            if (waveSWHMax3hData.timestamp.AddMilliseconds(adminSettingsVM.dataTimeout) < DateTime.UtcNow)
                waveSWHMax3hData.status = DataStatus.TIMEOUT_ERROR;
        }

        // Resette dataCalculations
        public void ResetDataCalculations()
        {
            waveMax20mData.ResetDataCalculations();
            waveMax3hData.ResetDataCalculations();

            wavePeriodData.ResetDataCalculations();
            wavePeriodMax20mData.ResetDataCalculations();
            wavePeriodMax3hData.ResetDataCalculations();

            waveSWHData.ResetDataCalculations();
            waveSWHMax20mData.ResetDataCalculations();
            waveSWHMax3hData.ResetDataCalculations();
        }
    }
}