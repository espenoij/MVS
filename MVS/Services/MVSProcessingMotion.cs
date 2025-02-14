﻿using System;
using Telerik.Windows.Data;

namespace MVS
{
    public class MVSProcessingMotion
    {
        // Input data
        private HMSData refSensorPitch = new HMSData();
        private HMSData refSensorRoll = new HMSData();
        private HMSData refSensorHeave = new HMSData();

        private HMSData testSensorPitch = new HMSData();
        private HMSData testSensorRoll = new HMSData();
        private HMSData testSensorHeave = new HMSData();

        // Pitch
        private HMSData refPitchData = new HMSData();
        private HMSData refPitchMaxData = new HMSData();
        private HMSData refPitchMaxUpData = new HMSData();
        private HMSData refPitchMaxDownData = new HMSData();
        private HMSData refPitchMeanData = new HMSData();
        private HMSData refPitchMeanMaxData = new HMSData();

        private HMSData testPitchData = new HMSData();
        private HMSData testPitchMaxData = new HMSData();
        private HMSData testPitchMaxUpData = new HMSData();
        private HMSData testPitchMaxDownData = new HMSData();
        private HMSData testPitchMeanData = new HMSData();
        private HMSData testPitchMeanMaxData = new HMSData();

        private HMSData devPitchData = new HMSData();
        private HMSData devPitchMeanData = new HMSData();
        private HMSData devPitchMaxData = new HMSData();

        // Roll
        private HMSData refRollData = new HMSData();
        private HMSData refRollMaxData = new HMSData();
        private HMSData refRollMaxLeftData = new HMSData();
        private HMSData refRollMaxRightData = new HMSData();
        private HMSData refRollMeanData = new HMSData();
        private HMSData refRollMeanMaxData = new HMSData();

        private HMSData testRollData = new HMSData();
        private HMSData testRollMaxData = new HMSData();
        private HMSData testRollMaxLeftData = new HMSData();
        private HMSData testRollMaxRightData = new HMSData();
        private HMSData testRollMeanData = new HMSData();
        private HMSData testRollMeanMaxData = new HMSData();

        private HMSData devRollData = new HMSData();
        private HMSData devRollMeanData = new HMSData();
        private HMSData devRollMaxData = new HMSData();

        // Heave
        private HMSData refHeaveData = new HMSData();
        private HMSData refHeaveMaxData = new HMSData();
        private HMSData refHeaveMaxUpData = new HMSData();
        private HMSData refHeaveMaxDownData = new HMSData();
        private HMSData refHeaveMeanData = new HMSData();
        private HMSData refHeaveMeanMaxData = new HMSData();

        private HMSData testHeaveData = new HMSData();
        private HMSData testHeaveMaxData = new HMSData();
        private HMSData testHeaveMaxUpData = new HMSData();
        private HMSData testHeaveMaxDownData = new HMSData();
        private HMSData testHeaveMeanData = new HMSData();
        private HMSData testHeaveMeanMaxData = new HMSData();

        private HMSData devHeaveData = new HMSData();
        private HMSData devHeaveMeanData = new HMSData();
        private HMSData devHeaveMaxData = new HMSData();

        // Admin Settings
        private AdminSettingsVM adminSettingsVM;

        public MVSProcessingMotion(MVSDataCollection hmsOutputData, AdminSettingsVM adminSettingsVM, ErrorHandler errorHandler)
        {
            this.adminSettingsVM = adminSettingsVM;

            // Fyller output listen med MVS Output data
            // NB! Variablene som legges inn i listen her fungerer som pekere: Oppdateres variabelen -> oppdateres listen
            // NB! Dersom nye variabler legges til i hmsOutputDataList må databasen opprettes på nytt

            RadObservableCollection<HMSData> hmsOutputDataList = hmsOutputData.GetDataList();

            // Reference MRU
            hmsOutputDataList.Add(refPitchData);
            hmsOutputDataList.Add(refPitchMaxData);
            hmsOutputDataList.Add(refPitchMaxUpData);
            hmsOutputDataList.Add(refPitchMaxDownData);
            hmsOutputDataList.Add(refPitchMeanData);
            hmsOutputDataList.Add(refPitchMeanMaxData);

            hmsOutputDataList.Add(refRollData);
            hmsOutputDataList.Add(refRollMaxData);
            hmsOutputDataList.Add(refRollMaxLeftData);
            hmsOutputDataList.Add(refRollMaxRightData);
            hmsOutputDataList.Add(refRollMeanData);
            hmsOutputDataList.Add(refRollMeanMaxData);

            hmsOutputDataList.Add(refHeaveData);
            hmsOutputDataList.Add(refHeaveMaxData);
            hmsOutputDataList.Add(refHeaveMaxUpData);
            hmsOutputDataList.Add(refHeaveMaxDownData);
            hmsOutputDataList.Add(refHeaveMeanData);
            hmsOutputDataList.Add(refHeaveMeanMaxData);

            // Test MRU
            hmsOutputDataList.Add(testPitchData);
            hmsOutputDataList.Add(testPitchMaxData);
            hmsOutputDataList.Add(testPitchMaxUpData);
            hmsOutputDataList.Add(testPitchMaxDownData);
            hmsOutputDataList.Add(testPitchMeanData);
            hmsOutputDataList.Add(testPitchMeanMaxData);

            hmsOutputDataList.Add(testRollData);
            hmsOutputDataList.Add(testRollMaxData);
            hmsOutputDataList.Add(testRollMaxLeftData);
            hmsOutputDataList.Add(testRollMaxRightData);
            hmsOutputDataList.Add(testRollMeanData);
            hmsOutputDataList.Add(testRollMeanMaxData);

            hmsOutputDataList.Add(testHeaveData);
            hmsOutputDataList.Add(testHeaveMaxData);
            hmsOutputDataList.Add(testHeaveMaxUpData);
            hmsOutputDataList.Add(testHeaveMaxDownData);
            hmsOutputDataList.Add(testHeaveMeanData);
            hmsOutputDataList.Add(testHeaveMeanMaxData);

            // Deviation
            hmsOutputDataList.Add(devPitchData);
            hmsOutputDataList.Add(devPitchMeanData);
            hmsOutputDataList.Add(devPitchMaxData);
            
            hmsOutputDataList.Add(devRollData);
            hmsOutputDataList.Add(devRollMeanData);
            hmsOutputDataList.Add(devRollMaxData);
            
            hmsOutputDataList.Add(devHeaveData);
            hmsOutputDataList.Add(devHeaveMeanData);
            hmsOutputDataList.Add(devHeaveMaxData);

            // Legge på litt informasjon på de variablene som ikke får dette automatisk fra sensor input data
            // (Disse verdiene blir kalkulert her, mens sensor input data over bare kopieres videre med denne typen info allerede inkludert)

            // Reference Pitch
            refPitchMaxData.id = (int)ValueType.Ref_PitchMax;
            refPitchMaxData.name = "Ref MRU: Pitch Max";
            refPitchMaxData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser, adminSettingsVM);
            refPitchMaxData.AddProcessing(CalculationType.TimeMaxAbsolute, Constants.Minutes20);

            refPitchMaxUpData.id = (int)ValueType.Ref_PitchMaxUp;
            refPitchMaxUpData.name = "Ref MRU: Pitch Max Up";
            refPitchMaxUpData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser, adminSettingsVM);
            refPitchMaxUpData.AddProcessing(CalculationType.TimeMax, Constants.Minutes20);

            refPitchMaxDownData.id = (int)ValueType.Ref_PitchMaxDown;
            refPitchMaxDownData.name = "Ref MRU: Pitch Max Down";
            refPitchMaxDownData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser, adminSettingsVM);
            refPitchMaxDownData.AddProcessing(CalculationType.TimeMin, Constants.Minutes20);

            refPitchMeanData.id = (int)ValueType.Ref_PitchMean;
            refPitchMeanData.name = "Ref MRU: Pitch Mean";
            refPitchMeanData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser, adminSettingsVM);
            refPitchMeanData.AddProcessing(CalculationType.TimeAverage, Constants.Minutes20);

            refPitchMeanMaxData.id = (int)ValueType.Ref_PitchMeanMax;
            refPitchMeanMaxData.name = "Ref MRU: Pitch Mean Max";
            refPitchMeanMaxData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser, adminSettingsVM);
            refPitchMeanMaxData.AddProcessing(CalculationType.TimeMaxAbsolute, Constants.Minutes20);

            // Reference Roll
            refRollMaxLeftData.id = (int)ValueType.Ref_RollMaxLeft;
            refRollMaxLeftData.name = "Ref MRU: Roll Max Left";
            refRollMaxLeftData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser, adminSettingsVM);
            refRollMaxLeftData.AddProcessing(CalculationType.TimeMax, Constants.Minutes20);

            refRollMaxData.id = (int)ValueType.Ref_RollMax;
            refRollMaxData.name = "Ref MRU: Roll Max";
            refRollMaxData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser, adminSettingsVM);
            refRollMaxData.AddProcessing(CalculationType.TimeMaxAbsolute, Constants.Minutes20);

            refRollMaxRightData.id = (int)ValueType.Ref_RollMaxRight;
            refRollMaxRightData.name = "Ref MRU: Roll Max Right";
            refRollMaxRightData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser, adminSettingsVM);
            refRollMaxRightData.AddProcessing(CalculationType.TimeMin, Constants.Minutes20);

            refRollMeanData.id = (int)ValueType.Ref_RollMean;
            refRollMeanData.name = "Ref MRU: Roll Mean";
            refRollMeanData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser, adminSettingsVM);
            refRollMeanData.AddProcessing(CalculationType.TimeAverage, Constants.Minutes20);

            refRollMeanMaxData.id = (int)ValueType.Ref_RollMeanMax;
            refRollMeanMaxData.name = "Ref MRU: Roll Mean Max";
            refRollMeanMaxData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser, adminSettingsVM);
            refRollMeanMaxData.AddProcessing(CalculationType.TimeMaxAbsolute, Constants.Minutes20);

            // Reference Heave
            refHeaveMaxData.id = (int)ValueType.Ref_HeaveMax;
            refHeaveMaxData.name = "Ref MRU: Heave Max";
            refHeaveMaxData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser, adminSettingsVM);
            refHeaveMaxData.AddProcessing(CalculationType.TimeMaxAbsolute, Constants.Minutes20);

            refHeaveMaxUpData.id = (int)ValueType.Ref_HeaveMaxUp;
            refHeaveMaxUpData.name = "Ref MRU: Heave Max Up";
            refHeaveMaxUpData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser, adminSettingsVM);
            refHeaveMaxUpData.AddProcessing(CalculationType.TimeMax, Constants.Minutes20);

            refHeaveMaxDownData.id = (int)ValueType.Ref_HeaveMaxDown;
            refHeaveMaxDownData.name = "Ref MRU: Heave Max Down";
            refHeaveMaxDownData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser, adminSettingsVM);
            refHeaveMaxDownData.AddProcessing(CalculationType.TimeMin, Constants.Minutes20);

            refHeaveMeanData.id = (int)ValueType.Ref_HeaveMean;
            refHeaveMeanData.name = "Ref MRU: Heave Mean";
            refHeaveMeanData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser, adminSettingsVM);
            refHeaveMeanData.AddProcessing(CalculationType.TimeAverage, Constants.Minutes20);

            refHeaveMeanMaxData.id = (int)ValueType.Ref_HeaveMeanMax;
            refHeaveMeanMaxData.name = "Ref MRU: Heave Mean Max";
            refHeaveMeanMaxData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser, adminSettingsVM);
            refHeaveMeanMaxData.AddProcessing(CalculationType.TimeMaxAbsolute, Constants.Minutes20);

            // Test Pitch
            testPitchMaxData.id = (int)ValueType.Test_PitchMax;
            testPitchMaxData.name = "Test MRU: Pitch Max";
            testPitchMaxData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser, adminSettingsVM);
            testPitchMaxData.AddProcessing(CalculationType.TimeMaxAbsolute, Constants.Minutes20);

            testPitchMaxUpData.id = (int)ValueType.Test_PitchMaxUp;
            testPitchMaxUpData.name = "Test MRU: Pitch Max Up";
            testPitchMaxUpData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser, adminSettingsVM);
            testPitchMaxUpData.AddProcessing(CalculationType.TimeMax, Constants.Minutes20);

            testPitchMaxDownData.id = (int)ValueType.Test_PitchMaxDown;
            testPitchMaxDownData.name = "Test MRU: Pitch Max Down";
            testPitchMaxDownData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser, adminSettingsVM);
            testPitchMaxDownData.AddProcessing(CalculationType.TimeMin, Constants.Minutes20);

            testPitchMeanData.id = (int)ValueType.Test_PitchMean;
            testPitchMeanData.name = "Test MRU: Pitch Mean";
            testPitchMeanData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser, adminSettingsVM);
            testPitchMeanData.AddProcessing(CalculationType.TimeAverage, Constants.Minutes20);

            testPitchMeanMaxData.id = (int)ValueType.Test_PitchMeanMax;
            testPitchMeanMaxData.name = "Test MRU: Pitch Mean Max";
            testPitchMeanMaxData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser, adminSettingsVM);
            testPitchMeanMaxData.AddProcessing(CalculationType.TimeMaxAbsolute, Constants.Minutes20);

            // Test Roll
            testRollMaxLeftData.id = (int)ValueType.Test_RollMaxLeft;
            testRollMaxLeftData.name = "Test MRU: Roll Max Left";
            testRollMaxLeftData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser, adminSettingsVM);
            testRollMaxLeftData.AddProcessing(CalculationType.TimeMax, Constants.Minutes20);

            testRollMaxData.id = (int)ValueType.Test_RollMax;
            testRollMaxData.name = "Test MRU: Roll Max";
            testRollMaxData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser, adminSettingsVM);
            testRollMaxData.AddProcessing(CalculationType.TimeMaxAbsolute, Constants.Minutes20);

            testRollMaxRightData.id = (int)ValueType.Test_RollMaxRight;
            testRollMaxRightData.name = "Test MRU: Roll Max Right";
            testRollMaxRightData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser, adminSettingsVM);
            testRollMaxRightData.AddProcessing(CalculationType.TimeMin, Constants.Minutes20);

            testRollMeanData.id = (int)ValueType.Test_RollMean;
            testRollMeanData.name = "Test MRU: Roll Mean";
            testRollMeanData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser, adminSettingsVM);
            testRollMeanData.AddProcessing(CalculationType.TimeAverage, Constants.Minutes20);

            testRollMeanMaxData.id = (int)ValueType.Test_RollMeanMax;
            testRollMeanMaxData.name = "Test MRU: Roll Mean Max";
            testRollMeanMaxData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser, adminSettingsVM);
            testRollMeanMaxData.AddProcessing(CalculationType.TimeMaxAbsolute, Constants.Minutes20);

            // Test Heave
            testHeaveMaxData.id = (int)ValueType.Test_HeaveMax;
            testHeaveMaxData.name = "Test MRU: Heave Amplitude Max";
            testHeaveMaxData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser, adminSettingsVM);
            testHeaveMaxData.AddProcessing(CalculationType.TimeMaxAbsolute, Constants.Minutes20);

            testHeaveMaxUpData.id = (int)ValueType.Test_HeaveMaxUp;
            testHeaveMaxUpData.name = "Test MRU: Heave Amplitude Max Up";
            testHeaveMaxUpData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser, adminSettingsVM);
            testHeaveMaxUpData.AddProcessing(CalculationType.TimeMax, Constants.Minutes20);

            testHeaveMaxDownData.id = (int)ValueType.Test_HeaveMaxDown;
            testHeaveMaxDownData.name = "Test MRU: Heave Amplitude Max Down";
            testHeaveMaxDownData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser, adminSettingsVM);
            testHeaveMaxDownData.AddProcessing(CalculationType.TimeMin, Constants.Minutes20);

            testHeaveMeanData.id = (int)ValueType.Test_HeaveMean;
            testHeaveMeanData.name = "Test MRU: Heave Amplitude Mean";
            testHeaveMeanData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser, adminSettingsVM);
            testHeaveMeanData.AddProcessing(CalculationType.TimeAverage, Constants.Minutes20);

            testHeaveMeanMaxData.id = (int)ValueType.Test_HeaveMeanMax;
            testHeaveMeanMaxData.name = "Test MRU: Heave Amplitude Mean Max";
            testHeaveMeanMaxData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser, adminSettingsVM);
            testHeaveMeanMaxData.AddProcessing(CalculationType.TimeMaxAbsolute, Constants.Minutes20);

            // Deviation Pitch
            devPitchData.id = (int)ValueType.Dev_Pitch;
            devPitchData.name = "Deviation: Pitch";

            devPitchMeanData.id = (int)ValueType.Dev_PitchMean;
            devPitchMeanData.name = "Deviation: Pitch Mean";
            devPitchMeanData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser, adminSettingsVM);
            devPitchMeanData.AddProcessing(CalculationType.TimeAverage, Constants.Minutes20);

            devPitchMaxData.id = (int)ValueType.Dev_PitchMax;
            devPitchMaxData.name = "Deviation: Pitch Max";
            devPitchMaxData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser, adminSettingsVM);
            devPitchMaxData.AddProcessing(CalculationType.TimeMaxAbsolute, Constants.Minutes20);

            // Deviation Roll
            devRollData.id = (int)ValueType.Dev_Roll;
            devRollData.name = "Deviation: Roll";

            devRollMeanData.id = (int)ValueType.Dev_RollMean;
            devRollMeanData.name = "Deviation: Roll Mean";
            devRollMeanData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser, adminSettingsVM);
            devRollMeanData.AddProcessing(CalculationType.TimeAverage, Constants.Minutes20);

            devRollMaxData.id = (int)ValueType.Dev_RollMax;
            devRollMaxData.name = "Deviation: Roll Max";
            devRollMaxData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser, adminSettingsVM);
            devRollMaxData.AddProcessing(CalculationType.TimeMaxAbsolute, Constants.Minutes20);

            // Deviation Heave
            devHeaveData.id = (int)ValueType.Dev_Heave;
            devHeaveData.name = "Deviation: Heave";

            devHeaveMeanData.id = (int)ValueType.Dev_HeaveMean;
            devHeaveMeanData.name = "Deviation: Heave Mean";
            devHeaveMeanData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser, adminSettingsVM);
            devHeaveMeanData.AddProcessing(CalculationType.TimeAverage, Constants.Minutes20);

            devHeaveMaxData.id = (int)ValueType.Dev_HeaveMax;
            devHeaveMaxData.name = "Deviation: Heave Max";
            devHeaveMaxData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser, adminSettingsVM);
            devHeaveMaxData.AddProcessing(CalculationType.TimeMaxAbsolute, Constants.Minutes20);
        }

        public void Update(MVSDataCollection hmsInputDataList, MainWindowVM mainWindowVM, ProcessingType processingType)
        {
            // Hente input data vi skal bruke
            refSensorPitch.Set(hmsInputDataList.GetData(ValueType.Ref_Pitch));
            refSensorRoll.Set(hmsInputDataList.GetData(ValueType.Ref_Roll));
            refSensorHeave.Set(hmsInputDataList.GetData(ValueType.Ref_Heave));

            testSensorPitch.Set(hmsInputDataList.GetData(ValueType.Test_Pitch));
            testSensorRoll.Set(hmsInputDataList.GetData(ValueType.Test_Roll));
            testSensorHeave.Set(hmsInputDataList.GetData(ValueType.Test_Heave));

            // Reference MRU
            //////////////////////////////////////////////////////////////////
            if ((mainWindowVM.OperationsMode == OperationsMode.Recording || 
                 mainWindowVM.OperationsMode == OperationsMode.Test ||
                 mainWindowVM.OperationsMode == OperationsMode.ViewData) &&
                (mainWindowVM.SelectedProject?.InputMRUs == InputMRUType.ReferenceMRU ||
                 mainWindowVM.SelectedProject?.InputMRUs == InputMRUType.ReferenceMRU_TestMRU) &&
                refSensorPitch.status != DataStatus.TIMEOUT_ERROR &&
                refSensorRoll.status != DataStatus.TIMEOUT_ERROR &&
                refSensorHeave.status != DataStatus.TIMEOUT_ERROR &&
                !double.IsNaN(refSensorPitch.data) &&
                !double.IsNaN(refSensorRoll.data) &&
                !double.IsNaN(refSensorHeave.data))
            {
                // Sjekke data timeout
                if (mainWindowVM.OperationsMode == OperationsMode.ViewData)
                {
                    refSensorPitch.status = DataStatus.OK;
                    refSensorRoll.status = DataStatus.OK;
                    refSensorHeave.status = DataStatus.OK;
                }
                else
                {
                    if (refSensorPitch.timestamp.AddMilliseconds(adminSettingsVM.dataTimeout) < DateTime.UtcNow)
                        refSensorPitch.status = DataStatus.TIMEOUT_ERROR;

                    if (refSensorRoll.timestamp.AddMilliseconds(adminSettingsVM.dataTimeout) < DateTime.UtcNow)
                        refSensorRoll.status = DataStatus.TIMEOUT_ERROR;

                    if (refSensorHeave.timestamp.AddMilliseconds(adminSettingsVM.dataTimeout) < DateTime.UtcNow)
                        refSensorHeave.status = DataStatus.TIMEOUT_ERROR;
                }

                // Ref: Pitch
                refPitchData.Set(refSensorPitch);
                refPitchMaxData.DoProcessing(refPitchData);
                refPitchMaxUpData.DoProcessing(refPitchData);
                refPitchMaxDownData.DoProcessing(refPitchData);
                refPitchMeanData.DoProcessing(refPitchData);
                refPitchMeanMaxData.DoProcessing(refPitchMeanData);

                // Ref: Roll
                refRollData.Set(refSensorRoll);
                refRollMaxData.DoProcessing(refRollData);
                refRollMaxLeftData.DoProcessing(refRollData);
                refRollMaxRightData.DoProcessing(refRollData);
                refRollMeanData.DoProcessing(refRollData);
                refRollMeanMaxData.DoProcessing(refRollMeanData);

                // Ref: Heave
                refHeaveData.Set(refSensorHeave);
                refHeaveMaxData.DoProcessing(refSensorHeave);
                refHeaveMaxUpData.DoProcessing(refSensorHeave);
                refHeaveMaxDownData.DoProcessing(refSensorHeave);
                refHeaveMeanData.DoProcessing(refSensorHeave);
                refHeaveMeanMaxData.DoProcessing(refHeaveMeanData);
            }
            else
            {
                if (processingType == ProcessingType.LIVE_DATA)
                {
                    refSensorPitch.status = DataStatus.NONE;
                    refSensorRoll.status = DataStatus.NONE;
                    refSensorHeave.status = DataStatus.NONE;

                    refPitchData.Set(refSensorPitch);
                    refPitchData.data = double.NaN;
                    refPitchData.status = DataStatus.NONE;
                    refPitchMaxData.data = double.NaN;
                    refPitchMaxData.status = DataStatus.NONE;
                    refPitchMaxUpData.data = double.NaN;
                    refPitchMaxUpData.status = DataStatus.NONE;
                    refPitchMaxDownData.data = double.NaN;
                    refPitchMaxDownData.status = DataStatus.NONE;
                    refPitchMeanData.data = double.NaN;
                    refPitchMeanData.status = DataStatus.NONE;
                    refPitchMeanMaxData.data = double.NaN;
                    refPitchMeanMaxData.status = DataStatus.NONE;

                    refRollData.Set(refSensorRoll);
                    refRollData.data = double.NaN;
                    refRollData.status = DataStatus.NONE;
                    refRollMaxData.data = double.NaN;
                    refRollMaxData.status = DataStatus.NONE;
                    refRollMaxLeftData.data = double.NaN;
                    refRollMaxLeftData.status = DataStatus.NONE;
                    refRollMaxRightData.data = double.NaN;
                    refRollMaxRightData.status = DataStatus.NONE;
                    refRollMeanData.data = double.NaN;
                    refRollMeanData.status = DataStatus.NONE;
                    refRollMeanMaxData.data = double.NaN;
                    refRollMeanMaxData.status = DataStatus.NONE;

                    refHeaveData.Set(refSensorHeave);
                    refHeaveData.data = double.NaN;
                    refHeaveData.status = DataStatus.NONE;
                    refHeaveMaxData.data = double.NaN;
                    refHeaveMaxData.status = DataStatus.NONE;
                    refHeaveMaxUpData.data = double.NaN;
                    refHeaveMaxUpData.status = DataStatus.NONE;
                    refHeaveMaxDownData.data = double.NaN;
                    refHeaveMaxDownData.status = DataStatus.NONE;
                    refHeaveMeanData.data = double.NaN;
                    refHeaveMeanData.status = DataStatus.NONE;
                    refHeaveMeanMaxData.data = double.NaN;
                    refHeaveMeanMaxData.status = DataStatus.NONE;
                }
            }

            // Tested MRU
            //////////////////////////////////////////////////////////////////
            if ((mainWindowVM.OperationsMode == OperationsMode.Recording ||
                 mainWindowVM.OperationsMode == OperationsMode.Test ||
                 mainWindowVM.OperationsMode == OperationsMode.ViewData) &&
                (mainWindowVM.SelectedProject?.InputMRUs == InputMRUType.TestMRU ||
                 mainWindowVM.SelectedProject?.InputMRUs == InputMRUType.ReferenceMRU_TestMRU) &&
                testSensorPitch.status != DataStatus.TIMEOUT_ERROR &&
                testSensorRoll.status != DataStatus.TIMEOUT_ERROR &&
                testSensorHeave.status != DataStatus.TIMEOUT_ERROR &&
                !double.IsNaN(testSensorPitch.data) &&
                !double.IsNaN(testSensorRoll.data) &&
                !double.IsNaN(testSensorHeave.data))
            {
                if (mainWindowVM.OperationsMode == OperationsMode.ViewData)
                {
                    testSensorPitch.status = DataStatus.OK;
                    testSensorRoll.status = DataStatus.OK;
                    testSensorHeave.status = DataStatus.OK;
                }
                else
                {
                    // Sjekke data timeout
                    if (testSensorPitch.timestamp.AddMilliseconds(adminSettingsVM.dataTimeout) < DateTime.UtcNow)
                        testSensorPitch.status = DataStatus.TIMEOUT_ERROR;

                    if (testSensorRoll.timestamp.AddMilliseconds(adminSettingsVM.dataTimeout) < DateTime.UtcNow)
                        testSensorRoll.status = DataStatus.TIMEOUT_ERROR;

                    if (testSensorHeave.timestamp.AddMilliseconds(adminSettingsVM.dataTimeout) < DateTime.UtcNow)
                        testSensorHeave.status = DataStatus.TIMEOUT_ERROR;
                }

                // Test: Pitch
                testPitchData.Set(testSensorPitch);
                testPitchMaxData.DoProcessing(testPitchData);
                testPitchMaxUpData.DoProcessing(testPitchData);
                testPitchMaxDownData.DoProcessing(testPitchData);
                testPitchMeanData.DoProcessing(testPitchData);
                testPitchMeanMaxData.DoProcessing(testPitchMeanData);

                // Test: Roll
                testRollData.Set(testSensorRoll);
                testRollMaxData.DoProcessing(testRollData);
                testRollMaxLeftData.DoProcessing(testRollData);
                testRollMaxRightData.DoProcessing(testRollData);
                testRollMeanData.DoProcessing(testRollData);
                testRollMeanMaxData.DoProcessing(testRollMeanData);

                // Test: Heave
                testHeaveData.Set(testSensorHeave);
                testHeaveMaxData.DoProcessing(testSensorHeave);
                testHeaveMaxUpData.DoProcessing(testSensorHeave);
                testHeaveMaxDownData.DoProcessing(testSensorHeave);
                testHeaveMeanData.DoProcessing(testSensorHeave);
                testHeaveMeanMaxData.DoProcessing(testHeaveMeanData);
            }
            else
            {
                if (processingType == ProcessingType.LIVE_DATA)
                {
                    testSensorPitch.status = DataStatus.NONE;
                    testSensorRoll.status = DataStatus.NONE;
                    testSensorHeave.status = DataStatus.NONE;

                    testPitchData.Set(testSensorPitch);
                    testPitchData.data = double.NaN;
                    testPitchData.status = DataStatus.NONE;
                    testPitchMaxData.data = double.NaN;
                    testPitchMaxData.status = DataStatus.NONE;
                    testPitchMaxUpData.data = double.NaN;
                    testPitchMaxUpData.status = DataStatus.NONE;
                    testPitchMaxDownData.data = double.NaN;
                    testPitchMaxDownData.status = DataStatus.NONE;
                    testPitchMeanData.data = double.NaN;
                    testPitchMeanData.status = DataStatus.NONE;
                    testPitchMeanMaxData.data = double.NaN;
                    testPitchMeanMaxData.status = DataStatus.NONE;

                    testRollData.Set(testSensorRoll);
                    testRollData.data = double.NaN;
                    testRollData.status = DataStatus.NONE;
                    testRollMaxData.data = double.NaN;
                    testRollMaxData.status = DataStatus.NONE;
                    testRollMaxLeftData.data = double.NaN;
                    testRollMaxLeftData.status = DataStatus.NONE;
                    testRollMaxRightData.data = double.NaN;
                    testRollMaxRightData.status = DataStatus.NONE;
                    testRollMeanData.data = double.NaN;
                    testRollMeanData.status = DataStatus.NONE;
                    testRollMeanMaxData.data = double.NaN;
                    testRollMeanMaxData.status = DataStatus.NONE;

                    testHeaveData.Set(testSensorHeave);
                    testHeaveData.data = double.NaN;
                    testHeaveData.status = DataStatus.NONE;
                    testHeaveMaxData.data = double.NaN;
                    testHeaveMaxData.status = DataStatus.NONE;
                    testHeaveMaxUpData.data = double.NaN;
                    testHeaveMaxUpData.status = DataStatus.NONE;
                    testHeaveMaxDownData.data = double.NaN;
                    testHeaveMaxDownData.status = DataStatus.NONE;
                    testHeaveMeanData.data = double.NaN;
                    testHeaveMeanData.status = DataStatus.NONE;
                    testHeaveMeanMaxData.data = double.NaN;
                    testHeaveMeanMaxData.status = DataStatus.NONE;
                }
            }

            // Deviation
            //////////////////////////////////////////////////////////////////
            ///
            // Pitch
            if (refPitchData.status == DataStatus.OK && !double.IsNaN(refPitchData.data) &&
                testPitchData.status == DataStatus.OK && !double.IsNaN(testPitchData.data))
            {
                devPitchData.data = testPitchData.data - refPitchData.data;
                devPitchData.timestamp = DateTime.UtcNow;
                devPitchData.status = DataStatus.OK;
            }
            else
            {
                devPitchData.data = double.NaN;
                devPitchData.status = DataStatus.NONE;
            }

            devPitchMeanData.DoProcessing(devPitchData);
            devPitchMaxData.DoProcessing(devPitchData);

            // Roll
            if (refRollData.status == DataStatus.OK && !double.IsNaN(refRollData.data) &&
                testRollData.status == DataStatus.OK && !double.IsNaN(testRollData.data))
            {
                devRollData.data = testRollData.data - refRollData.data;
                devRollData.timestamp = DateTime.UtcNow;
                devRollData.status = DataStatus.OK;
            }
            else
            {
                devRollData.data = double.NaN;
                devRollData.status = DataStatus.NONE;
            }

            devRollMeanData.DoProcessing(devRollData);
            devRollMaxData.DoProcessing(devRollData);

            // Heave
            if (refHeaveData.status == DataStatus.OK && !double.IsNaN(refHeaveData.data) &&
                testHeaveData.status == DataStatus.OK && !double.IsNaN(testHeaveData.data))
            {
                devHeaveData.data = testHeaveData.data - refHeaveData.data;
                devHeaveData.timestamp = DateTime.UtcNow;
                devHeaveData.status = DataStatus.OK;
            }
            else
            {
                devHeaveData.data = double.NaN;
                devHeaveData.status = DataStatus.NONE;
            }

            devHeaveMeanData.DoProcessing(devHeaveData);
            devHeaveMaxData.DoProcessing(devHeaveData);
        }

        // Resette dataCalculations
        public void ResetDataCalculations()
        {
            // Diverse
            refPitchMaxData.ResetDataCalculations();
            refPitchMaxUpData.ResetDataCalculations();
            refPitchMaxDownData.ResetDataCalculations();
            refPitchMeanData.ResetDataCalculations();
            refPitchMeanMaxData.ResetDataCalculations();

            refRollMaxData.ResetDataCalculations();
            refRollMaxLeftData.ResetDataCalculations();
            refRollMaxRightData.ResetDataCalculations();
            refRollMeanData.ResetDataCalculations();
            refRollMeanMaxData.ResetDataCalculations();

            refHeaveMaxData.ResetDataCalculations();
            refHeaveMaxUpData.ResetDataCalculations();
            refHeaveMaxDownData.ResetDataCalculations();
            refHeaveMeanData.ResetDataCalculations();
            refHeaveMeanMaxData.ResetDataCalculations();

            testPitchMaxData.ResetDataCalculations();
            testPitchMaxUpData.ResetDataCalculations();
            testPitchMaxDownData.ResetDataCalculations();
            testPitchMeanData.ResetDataCalculations();
            testPitchMeanMaxData.ResetDataCalculations();

            testRollMaxData.ResetDataCalculations();
            testRollMaxLeftData.ResetDataCalculations();
            testRollMaxRightData.ResetDataCalculations();
            testRollMeanData.ResetDataCalculations();
            testRollMeanMaxData.ResetDataCalculations();

            testHeaveMaxData.ResetDataCalculations();
            testHeaveMaxUpData.ResetDataCalculations();
            testHeaveMaxDownData.ResetDataCalculations();
            testHeaveMeanData.ResetDataCalculations();
            testHeaveMeanMaxData.ResetDataCalculations();

            devPitchMeanData.ResetDataCalculations();
            devPitchMaxData.ResetDataCalculations();

            devRollMaxData.ResetDataCalculations();
            devRollMeanData.ResetDataCalculations();

            devHeaveMeanData.ResetDataCalculations();
            devHeaveMaxData.ResetDataCalculations();
        }
    }
}
