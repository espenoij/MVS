using System;

namespace HMS_Server
{
    class HMSProcessingUserInputs
    {
        private HMSData helicopterType = new HMSData();
        private HMSData helideckCategory = new HMSData();
        private HMSData dayNight = new HMSData();
        private HMSData displayMode = new HMSData();

        private HMSData onDeckTime = new HMSData();
        private HMSData onDeckHelicopterHeading = new HMSData();
        private HMSData onDeckHelicopterHeadingIsCorrected = new HMSData();
        private HMSData onDeckVesselHeading = new HMSData();
        private HMSData onDeckWindDirection = new HMSData();

        private UserInputs userInputs;
        private AdminSettingsVM adminSettingsVM;

        public HMSProcessingUserInputs(DataCollection hmsOutputData, UserInputs userInputs, AdminSettingsVM adminSettings)
        {
            this.adminSettingsVM = adminSettings;
            this.userInputs = userInputs;

            // Fyller output listen med HMS Output data
            // NB! Variablene som legges inn i listen her fungerer som pekere: Oppdateres variabelen -> oppdateres listen

            RadObservableCollectionEx<HMSData> hmsOutputDataList = hmsOutputData.GetDataList();

            hmsOutputDataList.Add(helicopterType);
            hmsOutputDataList.Add(helideckCategory);
            hmsOutputDataList.Add(dayNight);
            hmsOutputDataList.Add(displayMode);

            hmsOutputDataList.Add(onDeckTime);
            hmsOutputDataList.Add(onDeckHelicopterHeading);
            hmsOutputDataList.Add(onDeckHelicopterHeadingIsCorrected);
            hmsOutputDataList.Add(onDeckVesselHeading);
            hmsOutputDataList.Add(onDeckWindDirection);

            helicopterType.id = (int)ValueType.SettingsHelicopterType;
            helicopterType.name = "Helicopter Type";
            helicopterType.dbColumnName = "helicopter_type";
            helicopterType.sensorGroupId = Constants.NO_SENSOR_GROUP_ID;

            helideckCategory.id = (int)ValueType.SettingsHelideckCategory;
            helideckCategory.name = "Helideck Category";
            helideckCategory.dbColumnName = "helideck_cat";
            helideckCategory.sensorGroupId = Constants.NO_SENSOR_GROUP_ID;

            dayNight.id = (int)ValueType.SettingsDayNight;
            dayNight.name = "Day - Night";
            dayNight.dbColumnName = "day_night";
            dayNight.sensorGroupId = Constants.NO_SENSOR_GROUP_ID;

            displayMode.id = (int)ValueType.SettingsDisplayMode;
            displayMode.name = "Display Mode";
            displayMode.dbColumnName = "display_mode";
            displayMode.sensorGroupId = Constants.NO_SENSOR_GROUP_ID;

            onDeckTime.id = (int)ValueType.SettingsOnDeckTime;
            onDeckTime.name = "On Deck Time";
            onDeckTime.dbColumnName = "on_deck_time";
            onDeckTime.sensorGroupId = Constants.NO_SENSOR_GROUP_ID;

            onDeckHelicopterHeading.id = (int)ValueType.SettingsOnDeckHelicopterHeading;
            onDeckHelicopterHeading.name = "On Deck Helicopter Heading";
            onDeckHelicopterHeading.dbColumnName = "on_deck_helicopter_hdg";
            onDeckHelicopterHeading.sensorGroupId = Constants.NO_SENSOR_GROUP_ID;

            onDeckHelicopterHeadingIsCorrected.id = (int)ValueType.SettingsOnDeckHelicopterHeadingCorrected;
            onDeckHelicopterHeadingIsCorrected.name = "On Deck Helicopter Heading Corrected";
            onDeckHelicopterHeadingIsCorrected.dbColumnName = "on_deck_helicopter_hdg_corr";
            onDeckHelicopterHeadingIsCorrected.sensorGroupId = Constants.NO_SENSOR_GROUP_ID;

            onDeckVesselHeading.id = (int)ValueType.SettingsOnDeckVesselHeading;
            onDeckVesselHeading.name = "On Deck Vessel Heading";
            onDeckVesselHeading.dbColumnName = "on_deck_vessel_hdg";
            onDeckVesselHeading.sensorGroupId = Constants.NO_SENSOR_GROUP_ID;

            onDeckWindDirection.id = (int)ValueType.SettingsOnDeckWindDirection;
            onDeckWindDirection.name = "On Deck Wind Direction";
            onDeckWindDirection.dbColumnName = "on_deck_wind_dir";
            onDeckWindDirection.sensorGroupId = Constants.NO_SENSOR_GROUP_ID;
        }

        public void Update()
        {
            // Tar data fra user inputs/settings og overfører til HMS output delen

            helicopterType.data = (int)userInputs.helicopterType;
            helicopterType.status = DataStatus.OK;
            helicopterType.timestamp = DateTime.UtcNow;

            if (adminSettingsVM.regulationStandard == RegulationStandard.NOROG)
                helideckCategory.data = (int)adminSettingsVM.helideckCategory;
            else
                helideckCategory.data = (int)userInputs.helideckCategory;
            helideckCategory.timestamp = DateTime.UtcNow;
            helideckCategory.status = DataStatus.OK;

            dayNight.data = (int)userInputs.dayNight;
            dayNight.status = DataStatus.OK;
            dayNight.timestamp = DateTime.UtcNow;

            displayMode.data = (int)userInputs.displayMode;
            displayMode.status = DataStatus.OK;
            displayMode.timestamp = DateTime.UtcNow;

            onDeckTime.data = userInputs.onDeckTime.ToOADate();
            onDeckTime.status = DataStatus.OK;
            onDeckTime.timestamp = DateTime.UtcNow;

            onDeckHelicopterHeading.data = userInputs.onDeckHelicopterHeading;
            onDeckHelicopterHeading.status = DataStatus.OK;
            onDeckHelicopterHeading.timestamp = DateTime.UtcNow;

            if (userInputs.onDeckHelicopterHeadingIsCorrected)
                onDeckHelicopterHeadingIsCorrected.data = 1;
            else
                onDeckHelicopterHeadingIsCorrected.data = 0;
            onDeckHelicopterHeadingIsCorrected.status = DataStatus.OK;
            onDeckHelicopterHeadingIsCorrected.timestamp = DateTime.UtcNow;

            onDeckVesselHeading.data = userInputs.onDeckVesselHeading;
            onDeckVesselHeading.status = DataStatus.OK;
            onDeckVesselHeading.timestamp = DateTime.UtcNow;

            onDeckWindDirection.data = userInputs.onDeckWindDirection;
            onDeckWindDirection.status = DataStatus.OK;
            onDeckWindDirection.timestamp = DateTime.UtcNow;
        }
    }
}