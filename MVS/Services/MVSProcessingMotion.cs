using System;
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

            // Legge på litt informasjon på de variablene som ikke får dette automatisk fra sensor input data
            // (Disse verdiene blir kalkulert her, mens sensor input data bare kopieres videre med denne typen info allerede inkludert)

            // Reference Pitch
            refPitchMaxData.id = (int)ValueType.Ref_PitchMax;
            refPitchMaxData.name = "Ref MRU: Pitch Max";
            refPitchMaxData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser, adminSettingsVM);
            refPitchMaxData.AddProcessing(CalculationType.TimeMaxAbsolute, Constants.Minutes20);

            refPitchMaxUpData.id = (int)ValueType.Ref_PitchMaxUp;
            refPitchMaxUpData.name = "Ref MRU: Pitch Max Up";
            refPitchMaxUpData.dbColumn = "ref_pitch_max_up";
            refPitchMaxUpData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser, adminSettingsVM);
            refPitchMaxUpData.AddProcessing(CalculationType.TimeMax, Constants.Minutes20);

            refPitchMaxDownData.id = (int)ValueType.Ref_PitchMaxDown;
            refPitchMaxDownData.name = "Ref MRU: Pitch Max Down";
            refPitchMaxDownData.dbColumn = "ref_pitch_max_down";
            refPitchMaxDownData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser, adminSettingsVM);
            refPitchMaxDownData.AddProcessing(CalculationType.TimeMin, Constants.Minutes20);

            refPitchMeanData.id = (int)ValueType.Ref_PitchMean;
            refPitchMeanData.name = "Ref MRU: Pitch Mean";
            refPitchMeanData.dbColumn = "ref_pitch_mean";
            refPitchMeanData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser, adminSettingsVM);
            refPitchMeanData.AddProcessing(CalculationType.TimeAverage, Constants.Minutes20);

            refPitchMeanMaxData.id = (int)ValueType.Ref_PitchMeanMax;
            refPitchMeanMaxData.name = "Ref MRU: Pitch Mean Max";
            refPitchMeanMaxData.dbColumn = "ref_pitch_mean_max";
            refPitchMeanMaxData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser, adminSettingsVM);
            refPitchMeanMaxData.AddProcessing(CalculationType.TimeMaxAbsolute, Constants.Minutes20);

            // Reference Roll
            refRollMaxLeftData.id = (int)ValueType.Ref_RollMaxLeft;
            refRollMaxLeftData.name = "Ref MRU: Roll Max Left";
            refRollMaxLeftData.dbColumn = "ref_roll_max_left";
            refRollMaxLeftData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser, adminSettingsVM);
            refRollMaxLeftData.AddProcessing(CalculationType.TimeMax, Constants.Minutes20);

            refRollMaxData.id = (int)ValueType.Ref_RollMax;
            refRollMaxData.name = "Ref MRU: Roll Max";
            refRollMaxData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser, adminSettingsVM);
            refRollMaxData.AddProcessing(CalculationType.TimeMaxAbsolute, Constants.Minutes20);

            refRollMaxRightData.id = (int)ValueType.Ref_RollMaxRight;
            refRollMaxRightData.name = "Ref MRU: Roll Max Right";
            refRollMaxRightData.dbColumn = "ref_roll_max_right";
            refRollMaxRightData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser, adminSettingsVM);
            refRollMaxRightData.AddProcessing(CalculationType.TimeMin, Constants.Minutes20);

            refRollMeanData.id = (int)ValueType.Ref_RollMean;
            refRollMeanData.name = "Ref MRU: Roll Mean";
            refRollMeanData.dbColumn = "ref_roll_mean";
            refRollMeanData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser, adminSettingsVM);
            refRollMeanData.AddProcessing(CalculationType.TimeAverage, Constants.Minutes20);

            refRollMeanMaxData.id = (int)ValueType.Ref_RollMeanMax;
            refRollMeanMaxData.name = "Ref MRU: Roll Mean Max";
            refRollMeanMaxData.dbColumn = "ref_roll_mean_max";
            refRollMeanMaxData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser, adminSettingsVM);
            refRollMeanMaxData.AddProcessing(CalculationType.TimeMaxAbsolute, Constants.Minutes20);

            // Reference Heave
            refHeaveMaxData.id = (int)ValueType.Ref_HeaveMax;
            refHeaveMaxData.name = "Ref MRU: Heave Max";
            refHeaveMeanData.dbColumn = "ref_heave_max";
            refHeaveMaxData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser, adminSettingsVM);
            refHeaveMaxData.AddProcessing(CalculationType.TimeMaxAbsolute, Constants.Minutes20);

            refHeaveMaxUpData.id = (int)ValueType.Ref_HeaveMaxUp;
            refHeaveMaxUpData.name = "Ref MRU: Heave Max Up";
            refHeaveMaxUpData.dbColumn = "ref_heave_max_up";
            refHeaveMaxUpData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser, adminSettingsVM);
            refHeaveMaxUpData.AddProcessing(CalculationType.TimeMax, Constants.Minutes20);

            refHeaveMaxDownData.id = (int)ValueType.Ref_HeaveMaxDown;
            refHeaveMaxDownData.name = "Ref MRU: Heave Max Down";
            refHeaveMaxDownData.dbColumn = "ref_heave_max_down";
            refHeaveMaxDownData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser, adminSettingsVM);
            refHeaveMaxDownData.AddProcessing(CalculationType.TimeMin, Constants.Minutes20);

            refHeaveMeanData.id = (int)ValueType.Ref_HeaveMean;
            refHeaveMeanData.name = "Ref MRU: Heave Mean";
            refHeaveMeanData.dbColumn = "ref_heave_mean";
            refHeaveMeanData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser, adminSettingsVM);
            refHeaveMeanData.AddProcessing(CalculationType.TimeAverage, Constants.Minutes20);

            refHeaveMeanMaxData.id = (int)ValueType.Ref_HeaveMeanMax;
            refHeaveMeanMaxData.name = "Ref MRU: Heave Mean Max";
            refHeaveMeanMaxData.dbColumn = "ref_heave_mean_max";
            refHeaveMeanMaxData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser, adminSettingsVM);
            refHeaveMeanMaxData.AddProcessing(CalculationType.TimeMaxAbsolute, Constants.Minutes20);

            // Test Pitch
            testPitchMaxData.id = (int)ValueType.Test_PitchMax;
            testPitchMaxData.name = "Test MRU: Pitch Max";
            testPitchMaxData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser, adminSettingsVM);
            testPitchMaxData.AddProcessing(CalculationType.TimeMaxAbsolute, Constants.Minutes20);

            testPitchMaxUpData.id = (int)ValueType.Test_PitchMaxUp;
            testPitchMaxUpData.name = "Test MRU: Pitch Max Up";
            testPitchMaxUpData.dbColumn = "test_pitch_max_up";
            testPitchMaxUpData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser, adminSettingsVM);
            testPitchMaxUpData.AddProcessing(CalculationType.TimeMax, Constants.Minutes20);

            testPitchMaxDownData.id = (int)ValueType.Test_PitchMaxDown;
            testPitchMaxDownData.name = "Test MRU: Pitch Max Down";
            testPitchMaxDownData.dbColumn = "test_pitch_max_down";
            testPitchMaxDownData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser, adminSettingsVM);
            testPitchMaxDownData.AddProcessing(CalculationType.TimeMin, Constants.Minutes20);

            testPitchMeanData.id = (int)ValueType.Test_PitchMean;
            testPitchMeanData.name = "Test MRU: Pitch Mean";
            testPitchMeanData.dbColumn = "test_pitch_mean";
            testPitchMeanData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser, adminSettingsVM);
            testPitchMeanData.AddProcessing(CalculationType.TimeAverage, Constants.Minutes20);

            testPitchMeanMaxData.id = (int)ValueType.Test_PitchMeanMax;
            testPitchMeanMaxData.name = "Test MRU: Pitch Mean Max";
            testPitchMeanMaxData.dbColumn = "test_pitch_mean_max";
            testPitchMeanMaxData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser, adminSettingsVM);
            testPitchMeanMaxData.AddProcessing(CalculationType.TimeMaxAbsolute, Constants.Minutes20);

            // Test Roll
            testRollMaxLeftData.id = (int)ValueType.Test_RollMaxLeft;
            testRollMaxLeftData.name = "Test MRU: Roll Max Left";
            testRollMaxLeftData.dbColumn = "test_roll_max_left";
            testRollMaxLeftData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser, adminSettingsVM);
            testRollMaxLeftData.AddProcessing(CalculationType.TimeMax, Constants.Minutes20);

            testRollMaxData.id = (int)ValueType.Test_RollMax;
            testRollMaxData.name = "Test MRU: Roll Max";
            testRollMaxData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser, adminSettingsVM);
            testRollMaxData.AddProcessing(CalculationType.TimeMaxAbsolute, Constants.Minutes20);

            testRollMaxRightData.id = (int)ValueType.Test_RollMaxRight;
            testRollMaxRightData.name = "Test MRU: Roll Max Right";
            testRollMaxRightData.dbColumn = "test_roll_max_right";
            testRollMaxRightData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser, adminSettingsVM);
            testRollMaxRightData.AddProcessing(CalculationType.TimeMin, Constants.Minutes20);

            testRollMeanData.id = (int)ValueType.Test_RollMean;
            testRollMeanData.name = "Test MRU: Roll Mean";
            testRollMeanData.dbColumn = "test_roll_mean";
            testRollMeanData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser, adminSettingsVM);
            testRollMeanData.AddProcessing(CalculationType.TimeAverage, Constants.Minutes20);

            testRollMeanMaxData.id = (int)ValueType.Test_RollMeanMax;
            testRollMeanMaxData.name = "Test MRU: Roll Mean Max";
            testRollMeanMaxData.dbColumn = "test_roll_mean_max";
            testRollMeanMaxData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser, adminSettingsVM);
            testRollMeanMaxData.AddProcessing(CalculationType.TimeMaxAbsolute, Constants.Minutes20);

            // Test Heave
            testHeaveMaxData.id = (int)ValueType.Test_HeaveMax;
            testHeaveMaxData.name = "Test MRU: Heave Amplitude Max";
            testHeaveMaxData.dbColumn = "test_heave_max";
            testHeaveMaxData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser, adminSettingsVM);
            testHeaveMaxData.AddProcessing(CalculationType.TimeMaxAbsolute, Constants.Minutes20);

            testHeaveMaxUpData.id = (int)ValueType.Test_HeaveMaxUp;
            testHeaveMaxUpData.name = "Test MRU: Heave Amplitude Max Up";
            testHeaveMaxUpData.dbColumn = "test_heave_max_up";
            testHeaveMaxUpData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser, adminSettingsVM);
            testHeaveMaxUpData.AddProcessing(CalculationType.TimeMax, Constants.Minutes20);

            testHeaveMaxDownData.id = (int)ValueType.Test_HeaveMaxDown;
            testHeaveMaxDownData.name = "Test MRU: Heave Amplitude Max Down";
            testHeaveMaxDownData.dbColumn = "test_heave_max_down";
            testHeaveMaxDownData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser, adminSettingsVM);
            testHeaveMaxDownData.AddProcessing(CalculationType.TimeMin, Constants.Minutes20);

            testHeaveMeanData.id = (int)ValueType.Test_HeaveMean;
            testHeaveMeanData.name = "Test MRU: Heave Amplitude Mean";
            testHeaveMeanData.dbColumn = "test_heave_mean";
            testHeaveMeanData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser, adminSettingsVM);
            testHeaveMeanData.AddProcessing(CalculationType.TimeAverage, Constants.Minutes20);

            testHeaveMeanMaxData.id = (int)ValueType.Test_HeaveMeanMax;
            testHeaveMeanMaxData.name = "Test MRU: Heave Amplitude Mean Max";
            testHeaveMeanMaxData.dbColumn = "test_heave_mean_max";
            testHeaveMeanMaxData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser, adminSettingsVM);
            testHeaveMeanMaxData.AddProcessing(CalculationType.TimeMaxAbsolute, Constants.Minutes20);

        }

        public void Update(MVSDataCollection hmsInputDataList, MainWindowVM mainWindowVM)
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

                // Avrunding av data
                refPitchData.data = Math.Round(refPitchData.data, 3, MidpointRounding.AwayFromZero);
                refPitchMaxData.data = Math.Round(refPitchMaxData.data, 3, MidpointRounding.AwayFromZero);
                refPitchMaxUpData.data = Math.Round(refPitchMaxUpData.data, 3, MidpointRounding.AwayFromZero);
                refPitchMaxDownData.data = Math.Round(refPitchMaxDownData.data, 3, MidpointRounding.AwayFromZero);
                refPitchMeanData.data = Math.Round(refPitchMeanData.data, 3, MidpointRounding.AwayFromZero);
                refPitchMeanMaxData.data = Math.Round(refPitchMeanMaxData.data, 3, MidpointRounding.AwayFromZero);

                refRollData.data = Math.Round(refRollData.data, 3, MidpointRounding.AwayFromZero);
                refRollMaxData.data = Math.Round(refRollMaxData.data, 3, MidpointRounding.AwayFromZero);
                refRollMaxLeftData.data = Math.Round(refRollMaxLeftData.data, 3, MidpointRounding.AwayFromZero);
                refRollMaxRightData.data = Math.Round(refRollMaxRightData.data, 3, MidpointRounding.AwayFromZero);
                refRollMeanData.data = Math.Round(refRollMeanData.data, 3, MidpointRounding.AwayFromZero);
                refRollMeanMaxData.data = Math.Round(refRollMeanMaxData.data, 3, MidpointRounding.AwayFromZero);

                refHeaveData.data = Math.Round(refHeaveData.data, 3, MidpointRounding.AwayFromZero);
                refHeaveMaxData.data = Math.Round(refHeaveMaxData.data, 3, MidpointRounding.AwayFromZero);
                refHeaveMaxUpData.data = Math.Round(refHeaveMaxUpData.data, 3, MidpointRounding.AwayFromZero);
                refHeaveMaxDownData.data = Math.Round(refHeaveMaxDownData.data, 3, MidpointRounding.AwayFromZero);
                refHeaveMeanData.data = Math.Round(refHeaveMeanData.data, 3, MidpointRounding.AwayFromZero);
                refHeaveMeanMaxData.data = Math.Round(refHeaveMeanMaxData.data, 3, MidpointRounding.AwayFromZero);
            }
            else
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

                // Avrunding av data
                testPitchData.data = Math.Round(testPitchData.data, 3, MidpointRounding.AwayFromZero);
                testPitchMaxData.data = Math.Round(testPitchMaxData.data, 3, MidpointRounding.AwayFromZero);
                testPitchMaxUpData.data = Math.Round(testPitchMaxUpData.data, 3, MidpointRounding.AwayFromZero);
                testPitchMaxDownData.data = Math.Round(testPitchMaxDownData.data, 3, MidpointRounding.AwayFromZero);
                testPitchMeanData.data = Math.Round(testPitchMeanData.data, 3, MidpointRounding.AwayFromZero);
                testPitchMeanMaxData.data = Math.Round(testPitchMeanMaxData.data, 3, MidpointRounding.AwayFromZero);

                testRollData.data = Math.Round(testRollData.data, 3, MidpointRounding.AwayFromZero);
                testRollMaxData.data = Math.Round(testRollMaxData.data, 3, MidpointRounding.AwayFromZero);
                testRollMaxLeftData.data = Math.Round(testRollMaxLeftData.data, 3, MidpointRounding.AwayFromZero);
                testRollMaxRightData.data = Math.Round(testRollMaxRightData.data, 3, MidpointRounding.AwayFromZero);
                testRollMeanData.data = Math.Round(testRollMeanData.data, 3, MidpointRounding.AwayFromZero);
                testRollMeanMaxData.data = Math.Round(testRollMeanMaxData.data, 3, MidpointRounding.AwayFromZero);

                testHeaveData.data = Math.Round(testHeaveData.data, 3, MidpointRounding.AwayFromZero);
                testHeaveMaxData.data = Math.Round(testHeaveMaxData.data, 3, MidpointRounding.AwayFromZero);
                testHeaveMaxUpData.data = Math.Round(testHeaveMaxUpData.data, 3, MidpointRounding.AwayFromZero);
                testHeaveMaxDownData.data = Math.Round(testHeaveMaxDownData.data, 3, MidpointRounding.AwayFromZero);
                testHeaveMeanData.data = Math.Round(testHeaveMeanData.data, 3, MidpointRounding.AwayFromZero);
                testHeaveMeanMaxData.data = Math.Round(testHeaveMeanMaxData.data, 3, MidpointRounding.AwayFromZero);
            }
            else
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
        }
    }
}
