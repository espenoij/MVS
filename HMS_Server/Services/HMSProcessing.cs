using System;

namespace HMS_Server
{
    // Klasse for prosessering av data fra sensor til HMS output
    public class HMSProcessing
    {
        private HMSProcessingSettings hmsProcessingSettings;
        private HMSProcessingUserInputs hmsProcessingUserInputs;
        private HMSProcessingGeneralInfo hmsProcessingGeneralInfo;
        private HMSProcessingMotion hmsProcessingMotion;
        private HMSProcessingWindHeading hmsProcessingWindHeading;
        private HMSProcessingMeteorological hmsProcessingMeteorological;
        private HMSProcessingHelideckStatus hmsProcessingHelideckStatus;
        private HMSProcessingEMS hmsProcessingEMS;
        private HMSProcessingVerificationData hmsProcessingVerificationData;

        // Init data prosessering
        public HMSProcessing(
            HelideckMotionLimits motionLimits,
            HMSDataCollection hmsOutputData,
            AdminSettingsVM adminSettingsVM,
            UserSettingsVM userSettingsVM,
            UserInputs userInputs,
            ErrorHandler errorHandler,
            bool dataVerificationIsActive)
        {
            hmsProcessingSettings = new HMSProcessingSettings(hmsOutputData, adminSettingsVM, userSettingsVM);
            hmsProcessingUserInputs = new HMSProcessingUserInputs(hmsOutputData, userInputs, adminSettingsVM);
            hmsProcessingGeneralInfo = new HMSProcessingGeneralInfo(hmsOutputData, adminSettingsVM);
            hmsProcessingMotion = new HMSProcessingMotion(hmsOutputData, motionLimits, adminSettingsVM, errorHandler);
            hmsProcessingWindHeading = new HMSProcessingWindHeading(hmsOutputData, adminSettingsVM, userSettingsVM, userInputs, errorHandler);
            hmsProcessingMeteorological = new HMSProcessingMeteorological(hmsOutputData, adminSettingsVM, userSettingsVM);
            hmsProcessingHelideckStatus = new HMSProcessingHelideckStatus(hmsOutputData, motionLimits, adminSettingsVM, userInputs, hmsProcessingMotion, hmsProcessingWindHeading);
            hmsProcessingEMS = new HMSProcessingEMS(hmsOutputData, errorHandler, adminSettingsVM);

            // Data Verification Module
            if (dataVerificationIsActive)
                hmsProcessingVerificationData = new HMSProcessingVerificationData(hmsOutputData);
        }

        // Kjøre prosessering og oppdatere data
        public void Update(HMSDataCollection hmsInputDataList, bool dataVerificationIsActive/*, ErrorHandler errorHandler*/)
        {
            hmsProcessingSettings.Update();
            hmsProcessingUserInputs.Update();
            hmsProcessingGeneralInfo.Update(hmsInputDataList);
            hmsProcessingMotion.Update(hmsInputDataList/*, errorHandler*/);
            hmsProcessingWindHeading.Update(hmsInputDataList/*, errorHandler*/);
            hmsProcessingMeteorological.Update(hmsInputDataList);
            hmsProcessingHelideckStatus.Update();
            hmsProcessingEMS.Update(hmsInputDataList);

            // Data Verification Module
            if (dataVerificationIsActive)
                hmsProcessingVerificationData?.Update(hmsInputDataList);
        }

        public void ResetDataCalculations()
        {
            hmsProcessingMotion.ResetDataCalculations();
            hmsProcessingWindHeading.ResetDataCalculations();
            hmsProcessingEMS.ResetDataCalculations();
        }
    }
}
