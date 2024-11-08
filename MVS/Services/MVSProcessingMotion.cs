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
        private HMSData refPitchMean20mData = new HMSData();
        private HMSData refPitchMax20mData = new HMSData();
        private HMSData refPitchMaxUp20mData = new HMSData();
        private HMSData refPitchMaxDown20mData = new HMSData();

        private HMSData testPitchData = new HMSData();
        private HMSData testPitchMean20mData = new HMSData();
        private HMSData testPitchMax20mData = new HMSData();
        private HMSData testPitchMaxUp20mData = new HMSData();
        private HMSData testPitchMaxDown20mData = new HMSData();

        // Roll
        private HMSData refRollData = new HMSData();
        private HMSData refRollMean20mData = new HMSData();
        private HMSData refRollMax20mData = new HMSData();
        private HMSData refRollMaxLeft20mData = new HMSData();
        private HMSData refRollMaxRight20mData = new HMSData();

        private HMSData testRollData = new HMSData();
        private HMSData testRollMean20mData = new HMSData();
        private HMSData testRollMax20mData = new HMSData();
        private HMSData testRollMaxLeft20mData = new HMSData();
        private HMSData testRollMaxRight20mData = new HMSData();

        // Heave
        private HMSData refHeaveData = new HMSData();
        private HMSData refHeaveAmplitudeData = new HMSData();
        private HMSData refHeaveAmplitudeMean20mData = new HMSData();
        private HMSData refHeaveAmplitudeMax20mData = new HMSData();

        private HMSData testHeaveData = new HMSData();
        private HMSData testHeaveAmplitudeData = new HMSData();
        private HMSData testHeaveAmplitudeMean20mData = new HMSData();
        private HMSData testHeaveAmplitudeMax20mData = new HMSData();

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
            hmsOutputDataList.Add(refPitchMean20mData);
            hmsOutputDataList.Add(refPitchMax20mData);
            hmsOutputDataList.Add(refPitchMaxUp20mData);
            hmsOutputDataList.Add(refPitchMaxDown20mData);

            hmsOutputDataList.Add(refRollData);
            hmsOutputDataList.Add(refRollMean20mData);
            hmsOutputDataList.Add(refRollMax20mData);
            hmsOutputDataList.Add(refRollMaxLeft20mData);
            hmsOutputDataList.Add(refRollMaxRight20mData);

            hmsOutputDataList.Add(refHeaveData);
            hmsOutputDataList.Add(refHeaveAmplitudeData);
            hmsOutputDataList.Add(refHeaveAmplitudeMean20mData);
            hmsOutputDataList.Add(refHeaveAmplitudeMax20mData);

            // Test MRU
            hmsOutputDataList.Add(testPitchData);
            hmsOutputDataList.Add(testPitchMean20mData);
            hmsOutputDataList.Add(testPitchMax20mData);
            hmsOutputDataList.Add(testPitchMaxUp20mData);
            hmsOutputDataList.Add(testPitchMaxDown20mData);

            hmsOutputDataList.Add(testRollData);
            hmsOutputDataList.Add(testRollMean20mData);
            hmsOutputDataList.Add(testRollMax20mData);
            hmsOutputDataList.Add(testRollMaxLeft20mData);
            hmsOutputDataList.Add(testRollMaxRight20mData);

            hmsOutputDataList.Add(testHeaveData);
            hmsOutputDataList.Add(testHeaveAmplitudeData);
            hmsOutputDataList.Add(testHeaveAmplitudeMean20mData);
            hmsOutputDataList.Add(testHeaveAmplitudeMax20mData);

            // Legge på litt informasjon på de variablene som ikke får dette automatisk fra sensor input data
            // (Disse verdiene blir kalkulert her, mens sensor input data bare kopieres videre med denne typen info allerede inkludert)

            // Reference MRU
            refPitchMean20mData.id = (int)ValueType.Ref_PitchMean20m;
            refPitchMean20mData.name = "Ref MRU: Pitch Mean (20m)";
            refPitchMean20mData.dbColumn = "ref_pitch_mean";
            refPitchMean20mData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser, adminSettingsVM);
            refPitchMean20mData.AddProcessing(CalculationType.TimeAverage, Constants.Minutes20);

            refPitchMax20mData.id = (int)ValueType.Ref_PitchMax20m;
            refPitchMax20mData.name = "Ref MRU: Pitch Max (20m)";
            refPitchMax20mData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser, adminSettingsVM);
            refPitchMax20mData.AddProcessing(CalculationType.TimeMaxAbsolute, Constants.Minutes20);

            refPitchMaxUp20mData.id = (int)ValueType.Ref_PitchMaxUp20m;
            refPitchMaxUp20mData.name = "Ref MRU: Pitch Max Up (20m)";
            refPitchMaxUp20mData.dbColumn = "ref_pitch_max_up";
            refPitchMaxUp20mData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser, adminSettingsVM);
            refPitchMaxUp20mData.AddProcessing(CalculationType.TimeMax, Constants.Minutes20);

            refPitchMaxDown20mData.id = (int)ValueType.Ref_PitchMaxDown20m;
            refPitchMaxDown20mData.name = "Ref MRU: Pitch Max Down (20m)";
            refPitchMaxDown20mData.dbColumn = "ref_pitch_max_down";
            refPitchMaxDown20mData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser, adminSettingsVM);
            refPitchMaxDown20mData.AddProcessing(CalculationType.TimeMin, Constants.Minutes20);

            refRollMean20mData.id = (int)ValueType.Ref_RollMean20m;
            refRollMean20mData.name = "Ref MRU: Roll Mean (20m)";
            refRollMean20mData.dbColumn = "ref_roll_mean";
            refRollMean20mData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser, adminSettingsVM);
            refRollMean20mData.AddProcessing(CalculationType.TimeAverage, Constants.Minutes20);

            refRollMaxLeft20mData.id = (int)ValueType.Ref_RollMaxLeft20m;
            refRollMaxLeft20mData.name = "Ref MRU: Roll Max Left (20m)";
            refRollMaxLeft20mData.dbColumn = "ref_roll_max_left";
            refRollMaxLeft20mData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser, adminSettingsVM);
            refRollMaxLeft20mData.AddProcessing(CalculationType.TimeMax, Constants.Minutes20);

            refRollMax20mData.id = (int)ValueType.Ref_RollMax20m;
            refRollMax20mData.name = "Ref MRU: Roll Max (20m)";
            refRollMax20mData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser, adminSettingsVM);
            refRollMax20mData.AddProcessing(CalculationType.TimeMaxAbsolute, Constants.Minutes20);

            refRollMaxRight20mData.id = (int)ValueType.Ref_RollMaxRight20m;
            refRollMaxRight20mData.name = "Ref MRU: Roll Max Right (20m)";
            refRollMaxRight20mData.dbColumn = "ref_roll_max_right";
            refRollMaxRight20mData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser, adminSettingsVM);
            refRollMaxRight20mData.AddProcessing(CalculationType.TimeMin, Constants.Minutes20);

            refHeaveAmplitudeData.id = (int)ValueType.Ref_HeaveAmplitude;
            refHeaveAmplitudeData.name = "Ref MRU: Heave Amplitude";
            refHeaveAmplitudeData.dbColumn = "ref_heave_amplitude";
            refHeaveAmplitudeData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser, adminSettingsVM);
            refHeaveAmplitudeData.AddProcessing(CalculationType.WaveAmplitude, 0);

            refHeaveAmplitudeMean20mData.id = (int)ValueType.Ref_HeaveAmplitudeMean20m;
            refHeaveAmplitudeMean20mData.name = "Ref MRU: Heave Amplitude Mean (20m)";
            refHeaveAmplitudeMean20mData.dbColumn = "ref_heave_amplitude_mean";
            refHeaveAmplitudeMean20mData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser, adminSettingsVM);
            refHeaveAmplitudeMean20mData.AddProcessing(CalculationType.MeanWaveAmplitude, Constants.Minutes20);

            refHeaveAmplitudeMax20mData.id = (int)ValueType.Ref_HeaveAmplitudeMax20m;
            refHeaveAmplitudeMax20mData.name = "Ref MRU: Heave Amplitude Max (20m)";
            refHeaveAmplitudeMax20mData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser, adminSettingsVM);
            refHeaveAmplitudeMax20mData.AddProcessing(CalculationType.TimeMaxAmplitude, Constants.Minutes20);

            // Test MRU
            testPitchMean20mData.id = (int)ValueType.Test_PitchMean20m;
            testPitchMean20mData.name = "Test MRU: Pitch Mean (20m)";
            testPitchMean20mData.dbColumn = "test_pitch_mean";
            testPitchMean20mData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser, adminSettingsVM);
            testPitchMean20mData.AddProcessing(CalculationType.TimeAverage, Constants.Minutes20);

            testPitchMax20mData.id = (int)ValueType.Test_PitchMax20m;
            testPitchMax20mData.name = "Test MRU: Pitch Max (20m)";
            testPitchMax20mData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser, adminSettingsVM);
            testPitchMax20mData.AddProcessing(CalculationType.TimeMaxAbsolute, Constants.Minutes20);

            testPitchMaxUp20mData.id = (int)ValueType.Test_PitchMaxUp20m;
            testPitchMaxUp20mData.name = "Test MRU: Pitch Max Up (20m)";
            testPitchMaxUp20mData.dbColumn = "test_pitch_max_up";
            testPitchMaxUp20mData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser, adminSettingsVM);
            testPitchMaxUp20mData.AddProcessing(CalculationType.TimeMax, Constants.Minutes20);

            testPitchMaxDown20mData.id = (int)ValueType.Test_PitchMaxDown20m;
            testPitchMaxDown20mData.name = "Test MRU: Pitch Max Down (20m)";
            testPitchMaxDown20mData.dbColumn = "test_pitch_max_down";
            testPitchMaxDown20mData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser, adminSettingsVM);
            testPitchMaxDown20mData.AddProcessing(CalculationType.TimeMin, Constants.Minutes20);

            testRollMean20mData.id = (int)ValueType.Test_RollMean20m;
            testRollMean20mData.name = "Test MRU: Roll Mean (20m)";
            testRollMean20mData.dbColumn = "test_roll_mean";
            testRollMean20mData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser, adminSettingsVM);
            testRollMean20mData.AddProcessing(CalculationType.TimeAverage, Constants.Minutes20);

            testRollMaxLeft20mData.id = (int)ValueType.Test_RollMaxLeft20m;
            testRollMaxLeft20mData.name = "Test MRU: Roll Max Left (20m)";
            testRollMaxLeft20mData.dbColumn = "test_roll_max_left";
            testRollMaxLeft20mData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser, adminSettingsVM);
            testRollMaxLeft20mData.AddProcessing(CalculationType.TimeMax, Constants.Minutes20);

            testRollMax20mData.id = (int)ValueType.Test_RollMax20m;
            testRollMax20mData.name = "Test MRU: Roll Max (20m)";
            testRollMax20mData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser, adminSettingsVM);
            testRollMax20mData.AddProcessing(CalculationType.TimeMaxAbsolute, Constants.Minutes20);

            testRollMaxRight20mData.id = (int)ValueType.Test_RollMaxRight20m;
            testRollMaxRight20mData.name = "Test MRU: Roll Max Right (20m)";
            testRollMaxRight20mData.dbColumn = "test_roll_max_right";
            testRollMaxRight20mData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser, adminSettingsVM);
            testRollMaxRight20mData.AddProcessing(CalculationType.TimeMin, Constants.Minutes20);

            testHeaveAmplitudeData.id = (int)ValueType.Test_HeaveAmplitude;
            testHeaveAmplitudeData.name = "Test MRU: Heave Amplitude";
            testHeaveAmplitudeData.dbColumn = "test_heave_amplitude";
            testHeaveAmplitudeData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser, adminSettingsVM);
            testHeaveAmplitudeData.AddProcessing(CalculationType.WaveAmplitude, 0);

            testHeaveAmplitudeMean20mData.id = (int)ValueType.Test_HeaveAmplitudeMean20m;
            testHeaveAmplitudeMean20mData.name = "Test MRU: Heave Amplitude Mean (20m)";
            testHeaveAmplitudeMean20mData.dbColumn = "test_heave_amplitude_mean";
            testHeaveAmplitudeMean20mData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser, adminSettingsVM);
            testHeaveAmplitudeMean20mData.AddProcessing(CalculationType.MeanWaveAmplitude, Constants.Minutes20);

            testHeaveAmplitudeMax20mData.id = (int)ValueType.Test_HeaveAmplitudeMax20m;
            testHeaveAmplitudeMax20mData.name = "Test MRU: Heave Amplitude Max (20m)";
            testHeaveAmplitudeMax20mData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser, adminSettingsVM);
            testHeaveAmplitudeMax20mData.AddProcessing(CalculationType.TimeMaxAmplitude, Constants.Minutes20);
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
                refPitchMean20mData.DoProcessing(refPitchData);
                refPitchMax20mData.DoProcessing(refPitchData);
                refPitchMaxUp20mData.DoProcessing(refPitchData);
                refPitchMaxDown20mData.DoProcessing(refPitchData);

                // Ref: Roll
                refRollData.Set(refSensorRoll);
                // I data fra sensor er positive tall roll til høyre.
                // Internt er positive tall roll til venstre. Venstre er høyest på grafen. Dette er standard i CAP.
                //refRollData.data *= -1;
                refRollMean20mData.DoProcessing(refRollData);
                refRollMax20mData.DoProcessing(refRollData);
                refRollMaxLeft20mData.DoProcessing(refRollData);
                refRollMaxRight20mData.DoProcessing(refRollData);

                // Ref: Heave
                refHeaveData.Set(refSensorHeave);
                refHeaveAmplitudeData.DoProcessing(refSensorHeave);
                refHeaveAmplitudeMean20mData.DoProcessing(refSensorHeave);
                refHeaveAmplitudeMax20mData.DoProcessing(refSensorHeave);

                // Avrunding av data
                refPitchData.data = Math.Round(refPitchData.data, 3, MidpointRounding.AwayFromZero);
                refPitchMean20mData.data = Math.Round(refPitchMean20mData.data, 3, MidpointRounding.AwayFromZero);
                refPitchMax20mData.data = Math.Round(refPitchMax20mData.data, 3, MidpointRounding.AwayFromZero);
                refPitchMaxUp20mData.data = Math.Round(refPitchMaxUp20mData.data, 3, MidpointRounding.AwayFromZero);
                refPitchMaxDown20mData.data = Math.Round(refPitchMaxDown20mData.data, 3, MidpointRounding.AwayFromZero);

                refRollData.data = Math.Round(refRollData.data, 3, MidpointRounding.AwayFromZero);
                refRollMean20mData.data = Math.Round(refRollMean20mData.data, 3, MidpointRounding.AwayFromZero);
                refRollMax20mData.data = Math.Round(refRollMax20mData.data, 3, MidpointRounding.AwayFromZero);
                refRollMaxLeft20mData.data = Math.Round(refRollMaxLeft20mData.data, 3, MidpointRounding.AwayFromZero);
                refRollMaxRight20mData.data = Math.Round(refRollMaxRight20mData.data, 3, MidpointRounding.AwayFromZero);

                refHeaveData.data = Math.Round(refHeaveData.data, 3, MidpointRounding.AwayFromZero);
                refHeaveAmplitudeData.data = Math.Round(refHeaveAmplitudeData.data, 3, MidpointRounding.AwayFromZero);
                refHeaveAmplitudeMean20mData.data = Math.Round(refHeaveAmplitudeMean20mData.data, 3, MidpointRounding.AwayFromZero);
                refHeaveAmplitudeMax20mData.data = Math.Round(refHeaveAmplitudeMax20mData.data, 3, MidpointRounding.AwayFromZero);
            }
            else
            {
                refSensorPitch.status = DataStatus.NONE;
                refSensorRoll.status = DataStatus.NONE;
                refSensorHeave.status = DataStatus.NONE;

                refPitchData.Set(refSensorPitch);
                refPitchData.data = double.NaN;
                refPitchData.status = DataStatus.NONE;
                refPitchMean20mData.data = double.NaN;
                refPitchMean20mData.status = DataStatus.NONE;
                refPitchMax20mData.data = double.NaN;
                refPitchMax20mData.status = DataStatus.NONE;
                refPitchMaxUp20mData.data = double.NaN;
                refPitchMaxUp20mData.status = DataStatus.NONE;
                refPitchMaxDown20mData.data = double.NaN;
                refPitchMaxDown20mData.status = DataStatus.NONE;

                refRollData.Set(refSensorRoll);
                refRollData.data = double.NaN;
                refRollData.status = DataStatus.NONE;
                refRollMean20mData.data = double.NaN;
                refRollMean20mData.status = DataStatus.NONE;
                refRollMax20mData.data = double.NaN;
                refRollMax20mData.status = DataStatus.NONE;
                refRollMaxLeft20mData.data = double.NaN;
                refRollMaxLeft20mData.status = DataStatus.NONE;
                refRollMaxRight20mData.data = double.NaN;
                refRollMaxRight20mData.status = DataStatus.NONE;

                refHeaveData.Set(refSensorHeave);
                refHeaveData.data = double.NaN;
                refHeaveData.status = DataStatus.NONE;
                refHeaveAmplitudeData.data = double.NaN;
                refHeaveAmplitudeData.status = DataStatus.NONE;
                refHeaveAmplitudeMean20mData.data = double.NaN;
                refHeaveAmplitudeMean20mData.status = DataStatus.NONE;
                refHeaveAmplitudeMax20mData.data = double.NaN;
                refHeaveAmplitudeMax20mData.status = DataStatus.NONE;
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
                testPitchMean20mData.DoProcessing(testPitchData);
                testPitchMax20mData.DoProcessing(testPitchData);
                testPitchMaxUp20mData.DoProcessing(testPitchData);
                testPitchMaxDown20mData.DoProcessing(testPitchData);

                // Test: Roll
                testRollData.Set(testSensorRoll);
                // I data fra sensor er positive tall roll til høyre.
                // Internt er positive tall roll til venstre. Venstre er høyest på grafen. Dette er standard i CAP.
                //testRollData.data *= -1;
                testRollMean20mData.DoProcessing(testRollData);
                testRollMax20mData.DoProcessing(testRollData);
                testRollMaxLeft20mData.DoProcessing(testRollData);
                testRollMaxRight20mData.DoProcessing(testRollData);

                // Test: Heave
                testHeaveData.Set(testSensorHeave);
                testHeaveAmplitudeData.DoProcessing(testSensorHeave);
                testHeaveAmplitudeMean20mData.DoProcessing(testSensorHeave);
                testHeaveAmplitudeMax20mData.DoProcessing(testSensorHeave);

                // Avrunding av data
                testPitchData.data = Math.Round(testPitchData.data, 3, MidpointRounding.AwayFromZero);
                testPitchMean20mData.data = Math.Round(testPitchMean20mData.data, 3, MidpointRounding.AwayFromZero);
                testPitchMax20mData.data = Math.Round(testPitchMax20mData.data, 3, MidpointRounding.AwayFromZero);
                testPitchMaxUp20mData.data = Math.Round(testPitchMaxUp20mData.data, 3, MidpointRounding.AwayFromZero);
                testPitchMaxDown20mData.data = Math.Round(testPitchMaxDown20mData.data, 3, MidpointRounding.AwayFromZero);

                testRollData.data = Math.Round(testRollData.data, 3, MidpointRounding.AwayFromZero);
                testRollMean20mData.data = Math.Round(testRollMean20mData.data, 3, MidpointRounding.AwayFromZero);
                testRollMax20mData.data = Math.Round(testRollMax20mData.data, 3, MidpointRounding.AwayFromZero);
                testRollMaxLeft20mData.data = Math.Round(testRollMaxLeft20mData.data, 3, MidpointRounding.AwayFromZero);
                testRollMaxRight20mData.data = Math.Round(testRollMaxRight20mData.data, 3, MidpointRounding.AwayFromZero);

                testHeaveData.data = Math.Round(testHeaveData.data, 3, MidpointRounding.AwayFromZero);
                testHeaveAmplitudeData.data = Math.Round(testHeaveAmplitudeData.data, 3, MidpointRounding.AwayFromZero);
                testHeaveAmplitudeMean20mData.data = Math.Round(testHeaveAmplitudeMean20mData.data, 3, MidpointRounding.AwayFromZero);
                testHeaveAmplitudeMax20mData.data = Math.Round(testHeaveAmplitudeMax20mData.data, 3, MidpointRounding.AwayFromZero);
            }
            else
            {
                testSensorPitch.status = DataStatus.NONE;
                testSensorRoll.status = DataStatus.NONE;
                testSensorHeave.status = DataStatus.NONE;

                testPitchData.Set(testSensorPitch);
                testPitchData.data = double.NaN;
                testPitchData.status = DataStatus.NONE;
                testPitchMean20mData.data = double.NaN;
                testPitchMean20mData.status = DataStatus.NONE;
                testPitchMax20mData.data = double.NaN;
                testPitchMax20mData.status = DataStatus.NONE;
                testPitchMaxUp20mData.data = double.NaN;
                testPitchMaxUp20mData.status = DataStatus.NONE;
                testPitchMaxDown20mData.data = double.NaN;
                testPitchMaxDown20mData.status = DataStatus.NONE;

                testRollData.Set(testSensorRoll);
                testRollData.data = double.NaN;
                testRollData.status = DataStatus.NONE;
                testRollMean20mData.data = double.NaN;
                testRollMean20mData.status = DataStatus.NONE;
                testRollMax20mData.data = double.NaN;
                testRollMax20mData.status = DataStatus.NONE;
                testRollMaxLeft20mData.data = double.NaN;
                testRollMaxLeft20mData.status = DataStatus.NONE;
                testRollMaxRight20mData.data = double.NaN;
                testRollMaxRight20mData.status = DataStatus.NONE;

                testHeaveData.Set(testSensorHeave);
                testHeaveData.data = double.NaN;
                testHeaveData.status = DataStatus.NONE;
                testHeaveAmplitudeData.data = double.NaN;
                testHeaveAmplitudeData.status = DataStatus.NONE;
                testHeaveAmplitudeMean20mData.data = double.NaN;
                testHeaveAmplitudeMean20mData.status = DataStatus.NONE;
                testHeaveAmplitudeMax20mData.data = double.NaN;
                testHeaveAmplitudeMax20mData.status = DataStatus.NONE;
            }
        }

        // Resette dataCalculations
        public void ResetDataCalculations()
        {
            // Diverse
            refPitchMean20mData.ResetDataCalculations();
            refPitchMax20mData.ResetDataCalculations();
            refPitchMaxUp20mData.ResetDataCalculations();
            refPitchMaxDown20mData.ResetDataCalculations();
            refRollMean20mData.ResetDataCalculations();
            refRollMax20mData.ResetDataCalculations();
            refRollMaxLeft20mData.ResetDataCalculations();
            refRollMaxRight20mData.ResetDataCalculations();
            refHeaveAmplitudeData.ResetDataCalculations();
            refHeaveAmplitudeMean20mData.ResetDataCalculations();
            refHeaveAmplitudeMax20mData.ResetDataCalculations();

            testPitchMean20mData.ResetDataCalculations();
            testPitchMax20mData.ResetDataCalculations();
            testPitchMaxUp20mData.ResetDataCalculations();
            testPitchMaxDown20mData.ResetDataCalculations();
            testRollMean20mData.ResetDataCalculations();
            testRollMax20mData.ResetDataCalculations();
            testRollMaxLeft20mData.ResetDataCalculations();
            testRollMaxRight20mData.ResetDataCalculations();
            testHeaveAmplitudeData.ResetDataCalculations();
            testHeaveAmplitudeMean20mData.ResetDataCalculations();
            testHeaveAmplitudeMax20mData.ResetDataCalculations();
        }
    }
}
