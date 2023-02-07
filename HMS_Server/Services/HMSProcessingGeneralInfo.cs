using System;
using Telerik.Windows.Data;

namespace HMS_Server
{
    class HMSProcessingGeneralInfo
    {
        private HMSData sensorLatitude = new HMSData();
        private HMSData sensorLongitude = new HMSData();

        private HMSData gpsLatitude = new HMSData();
        private HMSData gpsLongitude = new HMSData();

        private AdminSettingsVM adminSettingsVM;

        public HMSProcessingGeneralInfo(HMSDataCollection hmsOutputData, AdminSettingsVM adminSettingsVM)
        {
            this.adminSettingsVM = adminSettingsVM;

            // Fyller output listen med HMS Output data
            // NB! Variablene som legges inn i listen her fungerer som pekere: Oppdateres variabelen -> oppdateres listen
            // NB! Dersom nye variabler legges til i hmsOutputDataList må databasen opprettes på nytt

            RadObservableCollection<HMSData> hmsOutputDataList = hmsOutputData.GetDataList();

            hmsOutputDataList.Add(gpsLatitude);
            hmsOutputDataList.Add(gpsLongitude);
        }

        public void Update(HMSDataCollection hmsInputDataList)
        {
            // Hente sensor data
            sensorLatitude.Set(hmsInputDataList.GetData(ValueType.Latitude));
            sensorLongitude.Set(hmsInputDataList.GetData(ValueType.Longitude));

            // Sjekker om vi har nye data før vi starter prosessering
            if (sensorLatitude.TimeStampCheck ||
                sensorLongitude.TimeStampCheck)
            {
                // Tar data fra input delen av server og overfører til HMS output delen
                gpsLatitude.Set(sensorLatitude);
                gpsLongitude.Set(sensorLongitude);

                // Sjekke data timeout
                if (gpsLatitude.timestamp.AddMilliseconds(adminSettingsVM.dataTimeout) < DateTime.UtcNow)
                    gpsLatitude.status = DataStatus.TIMEOUT_ERROR;

                if (gpsLongitude.timestamp.AddMilliseconds(adminSettingsVM.dataTimeout) < DateTime.UtcNow)
                    gpsLongitude.status = DataStatus.TIMEOUT_ERROR;
            }
        }
    }
}