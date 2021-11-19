using System;

namespace HMS_Server
{
    class HMSProcessingGeneralInfo
    {
        private HMSData gpsLatitude = new HMSData();
        private HMSData gpsLongitude = new HMSData();

        private HMSData helideckCategory = new HMSData();

        private AdminSettingsVM adminSettingsVM;

        public HMSProcessingGeneralInfo(HMSDataCollection hmsOutputData, AdminSettingsVM adminSettingsVM)
        {
            this.adminSettingsVM = adminSettingsVM;

            // Fyller output listen med HMS Output data
            // NB! Variablene som legges inn i listen her fungerer som pekere: Oppdateres variabelen -> oppdateres listen

            RadObservableCollectionEx<HMSData> hmsOutputDataList = hmsOutputData.GetDataList();

            hmsOutputDataList.Add(gpsLatitude);
            hmsOutputDataList.Add(gpsLongitude);

            hmsOutputDataList.Add(helideckCategory);

            helideckCategory.id = (int)ValueType.SettingsHelideckCategory;
            helideckCategory.name = "Helideck Category";
            helideckCategory.sensorGroupId = Constants.NO_SENSOR_GROUP_ID;
        }

        public void Update(HMSDataCollection hmsInputDataList)
        {
            // Tar data fra input delen av server og overfører til HMS output delen

            gpsLatitude.Set(hmsInputDataList.GetData(ValueType.Latitude));
            gpsLongitude.Set(hmsInputDataList.GetData(ValueType.Longitude));

            helideckCategory.data = (int)adminSettingsVM.helideckCategory;
            helideckCategory.timestamp = DateTime.UtcNow;
            helideckCategory.status = DataStatus.OK;
        }
    }
}