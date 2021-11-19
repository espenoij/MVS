namespace HMS_Server
{
    public class HMSProcessing
    {
        private HMSProcessingSettings hmsProcessingSettings;
        private HMSProcessingGeneralInfo hmsProcessingGeneralInfo;
        private HMSProcessingMotion hmsProcessingMotion;
        private HMSProcessingWindHeading hmsProcessingWindHeading;
        private HMSProcessingMeteorological hmsProcessingMeteorological;
        private HMSProcessingHelideckStatus hmsProcessingHelideckStatus;

        public HMSProcessing(
            HelideckMotionLimits motionLimits,
            HMSDataCollection hmsOutputData,
            AdminSettingsVM adminSettingsVM,
            UserInputs userInputs,
            ErrorHandler errorHandler)
        {
            hmsProcessingSettings = new HMSProcessingSettings(hmsOutputData, adminSettingsVM);
            hmsProcessingGeneralInfo = new HMSProcessingGeneralInfo(hmsOutputData, adminSettingsVM);
            hmsProcessingMotion = new HMSProcessingMotion(hmsOutputData, motionLimits, adminSettingsVM, userInputs, errorHandler);
            hmsProcessingWindHeading = new HMSProcessingWindHeading(hmsOutputData, adminSettingsVM, userInputs);
            hmsProcessingMeteorological = new HMSProcessingMeteorological(hmsOutputData, adminSettingsVM);
            hmsProcessingHelideckStatus = new HMSProcessingHelideckStatus(hmsOutputData, motionLimits, adminSettingsVM, userInputs, hmsProcessingMotion, hmsProcessingWindHeading);
        }

        public void Update(HMSDataCollection hmsInputDataList)
        {
            hmsProcessingSettings.Update();
            hmsProcessingGeneralInfo.Update(hmsInputDataList);
            hmsProcessingMotion.Update(hmsInputDataList);
            hmsProcessingWindHeading.Update(hmsInputDataList);
            hmsProcessingMeteorological.Update(hmsInputDataList);
            hmsProcessingHelideckStatus.Update();
        }
    }
}
