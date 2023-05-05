﻿using System;

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

        // Init data prosessering
        public HMSProcessing(
            HelideckMotionLimits motionLimits,
            HMSDataCollection hmsOutputData,
            AdminSettingsVM adminSettingsVM,
            UserSettingsVM userSettingsVM,
            UserInputs userInputs,
            ErrorHandler errorHandler)
        {
            hmsProcessingSettings = new HMSProcessingSettings(hmsOutputData, adminSettingsVM, userSettingsVM);
            hmsProcessingUserInputs = new HMSProcessingUserInputs(hmsOutputData, userInputs, adminSettingsVM);
            hmsProcessingGeneralInfo = new HMSProcessingGeneralInfo(hmsOutputData, adminSettingsVM);
            hmsProcessingMotion = new HMSProcessingMotion(hmsOutputData, motionLimits, adminSettingsVM, errorHandler);
            hmsProcessingWindHeading = new HMSProcessingWindHeading(hmsOutputData, adminSettingsVM, userSettingsVM, userInputs, errorHandler);
            hmsProcessingMeteorological = new HMSProcessingMeteorological(hmsOutputData, adminSettingsVM, userSettingsVM);
            hmsProcessingHelideckStatus = new HMSProcessingHelideckStatus(hmsOutputData, motionLimits, adminSettingsVM, userInputs, hmsProcessingMotion, hmsProcessingWindHeading);
            hmsProcessingEMS = new HMSProcessingEMS(hmsOutputData, errorHandler, adminSettingsVM);
        }

        // Kjøre prosessering og oppdatere data
        public void Update(HMSDataCollection hmsInputDataList)
        {
            hmsProcessingSettings.Update();
            hmsProcessingUserInputs.Update();
            hmsProcessingGeneralInfo.Update(hmsInputDataList);
            hmsProcessingMotion.Update(hmsInputDataList);
            hmsProcessingWindHeading.Update(hmsInputDataList);
            hmsProcessingMeteorological.Update(hmsInputDataList);
            hmsProcessingHelideckStatus.Update();
            hmsProcessingEMS.Update(hmsInputDataList);
        }

        public void ResetDataCalculations()
        {
            hmsProcessingMotion.ResetDataCalculations();
            hmsProcessingWindHeading.ResetDataCalculations();
            hmsProcessingEMS.ResetDataCalculations();
        }
    }
}
