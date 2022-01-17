namespace HMS_Server
{
    // Klasse for prosessering av data fra sensor til HMS output
    public class HMSProcessing
    {
        private AdminSettingsVM adminSettingsVM;

        private HMSProcessingSettings hmsProcessingSettings;
        private HMSProcessingUserInputs hmsProcessingUserInputs;
        private HMSProcessingGeneralInfo hmsProcessingGeneralInfo;
        private HMSProcessingMotion hmsProcessingMotion;
        private HMSProcessingWindHeading hmsProcessingWindHeading;
        private HMSProcessingMeteorological hmsProcessingMeteorological;
        private HMSProcessingHelideckStatus hmsProcessingHelideckStatus;
        private HMSProcessingVerificationData hmsProcessingVerificationData;

        // Init data prosessering
        public HMSProcessing(
            HelideckMotionLimits motionLimits,
            HMSDataCollection hmsOutputData,
            AdminSettingsVM adminSettingsVM,
            UserInputs userInputs,
            ErrorHandler errorHandler,
            bool dataVerificationIsActive)
        {
            this.adminSettingsVM = adminSettingsVM;

            hmsProcessingSettings = new HMSProcessingSettings(hmsOutputData, adminSettingsVM);
            hmsProcessingUserInputs = new HMSProcessingUserInputs(hmsOutputData, userInputs, adminSettingsVM);
            hmsProcessingGeneralInfo = new HMSProcessingGeneralInfo(hmsOutputData);
            hmsProcessingMotion = new HMSProcessingMotion(hmsOutputData, motionLimits, adminSettingsVM, userInputs, errorHandler);
            hmsProcessingWindHeading = new HMSProcessingWindHeading(hmsOutputData, adminSettingsVM, userInputs, errorHandler);
            hmsProcessingMeteorological = new HMSProcessingMeteorological(hmsOutputData, adminSettingsVM);
            hmsProcessingHelideckStatus = new HMSProcessingHelideckStatus(hmsOutputData, motionLimits, adminSettingsVM, userInputs, hmsProcessingMotion, hmsProcessingWindHeading);

            // Data Verification Module
            if (dataVerificationIsActive)
                hmsProcessingVerificationData = new HMSProcessingVerificationData(hmsOutputData);
        }

        // Kjøre prosessering og oppdatere data
        public void Update(HMSDataCollection hmsInputDataList, bool dataVerificationIsActive)
        {
            hmsProcessingSettings.Update();
            hmsProcessingUserInputs.Update();
            hmsProcessingGeneralInfo.Update(hmsInputDataList);
            hmsProcessingMotion.Update(hmsInputDataList);
            hmsProcessingWindHeading.Update(hmsInputDataList);
            hmsProcessingMeteorological.Update(hmsInputDataList);
            hmsProcessingHelideckStatus.Update();

            // Data Verification Module
            if (dataVerificationIsActive)
                hmsProcessingVerificationData.Update(hmsInputDataList);
        }

        public void ResetDataCalculations()
        {
            hmsProcessingMotion.ResetDataCalculations();
            hmsProcessingWindHeading.ResetDataCalculations();
            hmsProcessingHelideckStatus.ResetDataCalculations();
        }
    }
}
