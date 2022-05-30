using System;
using System.Collections.Generic;
using System.Linq;
using Telerik.Windows.Data;

namespace HMS_Server
{
    public class HMSProcessingMotion
    {
        // Input data
        private HMSData inputPitchData = new HMSData();
        private HMSData inputPitchDataPrev = new HMSData();
        private HMSData inputRollData = new HMSData();
        private HMSData inputRollDataPrev = new HMSData();
        private HMSData inputHeaveData = new HMSData();
        private HMSData inputHeaveDataPrev = new HMSData();
        private HMSData inputHeaveRateData = new HMSData();
        private HMSData inputHeaveRateDataPrev = new HMSData();
        private HMSData inputAccelerationXData = new HMSData();
        private HMSData inputAccelerationXDataPrev = new HMSData();
        private HMSData inputAccelerationYData = new HMSData();
        private HMSData inputAccelerationYDataPrev = new HMSData();
        private HMSData inputAccelerationZData = new HMSData();
        private HMSData inputAccelerationZDataPrev = new HMSData();

        // Pitch
        private HMSData pitchData = new HMSData();
        private HMSData pitchMax20mData = new HMSData();
        private HMSData pitchMax3hData = new HMSData();
        private HMSData pitchMaxUp20mData = new HMSData();
        private HMSData pitchMaxDown20mData = new HMSData();

        // Roll
        private HMSData rollData = new HMSData();
        private HMSData rollMax20mData = new HMSData();
        private HMSData rollMax3hData = new HMSData();
        private HMSData rollMaxLeft20mData = new HMSData();
        private HMSData rollMaxRight20mData = new HMSData();

        // Inclination
        private HMSData inclinationData = new HMSData();
        private HMSData inclination20mMaxData = new HMSData();
        private HMSData inclination3hMaxData = new HMSData();

        private RadObservableCollectionEx<TimeData> inclination20mMaxList = new RadObservableCollectionEx<TimeData>();
        private RadObservableCollectionEx<TimeData> inclination3hMaxList = new RadObservableCollectionEx<TimeData>();

        // Heave Height
        private HMSData heaveHeightData = new HMSData();
        private HMSData heaveHeightMax20mData = new HMSData();
        private HMSData heaveHeightMax3hData = new HMSData();
        private HMSData heavePeriodMeanData = new HMSData();

        // Significant Heave Rate
        private HMSData significantHeaveRateData = new HMSData();
        private HMSData significantHeaveRateMax20mData = new HMSData();
        private HMSData significantHeaveRateMax3hData = new HMSData();

        // Max Heave Rate
        private HMSData maxHeaveRateData = new HMSData();

        // Significant Wave Height
        //private HMSData significantWaveHeightData = new HMSData();

        // Limits
        private HMSData motionLimitPitchRoll = new HMSData();
        private HMSData motionLimitInclination = new HMSData();
        private HMSData motionLimitHeaveHeight = new HMSData();
        private HMSData motionLimitSignificantHeaveRate = new HMSData();

        // MSI
        private HMSData msiData = new HMSData();

        // Liste for å finne max MSI
        private List<TimeData> mms_msi_list = new List<TimeData>();

        // Motion Limits
        private HelideckMotionLimits motionLimits;
        private bool SHRIsWithinLimits = false;

        private AdminSettingsVM adminSettingsVM;

        // Significant Heave Rate - Limit conditions
        private double significantHeaveRate2mMin = double.MaxValue;
        private double significantHeaveRate10mSum = 0;
        private double significantHeaveRate10mMean = 0;
        private double significantHeaveRate20mMax = 0;

        private RadObservableCollectionEx<TimeData> significantHeaveRate2mMinData = new RadObservableCollectionEx<TimeData>();
        private RadObservableCollectionEx<TimeData> significantHeaveRate10mMeanData = new RadObservableCollectionEx<TimeData>();
        private RadObservableCollectionEx<TimeData> significantHeaveRate20mMaxData = new RadObservableCollectionEx<TimeData>();

        // Må ha en gjennomkjøring av data første gang for lagre grunnleggende data i HMS output data strukturen
        private bool firstRun = true;

        public HMSProcessingMotion(HMSDataCollection hmsOutputData, HelideckMotionLimits motionLimits, AdminSettingsVM adminSettingsVM, ErrorHandler errorHandler)
        {
            this.motionLimits = motionLimits;
            this.adminSettingsVM = adminSettingsVM;

            // Fyller output listen med HMS Output data
            // NB! Variablene som legges inn i listen her fungerer som pekere: Oppdateres variabelen -> oppdateres listen
            // NB! Dersom nye variabler legges til i hmsOutputDataList må databasen opprettes på nytt

            RadObservableCollection<HMSData> hmsOutputDataList = hmsOutputData.GetDataList();

            hmsOutputDataList.Add(pitchData);
            hmsOutputDataList.Add(pitchMax20mData);
            hmsOutputDataList.Add(pitchMax3hData);
            hmsOutputDataList.Add(pitchMaxUp20mData);
            hmsOutputDataList.Add(pitchMaxDown20mData);

            hmsOutputDataList.Add(rollData);
            hmsOutputDataList.Add(rollMax20mData);
            hmsOutputDataList.Add(rollMax3hData);
            hmsOutputDataList.Add(rollMaxLeft20mData);
            hmsOutputDataList.Add(rollMaxRight20mData);

            hmsOutputDataList.Add(inclinationData);
            hmsOutputDataList.Add(inclination20mMaxData);
            hmsOutputDataList.Add(inclination3hMaxData);

            hmsOutputDataList.Add(significantHeaveRateData);
            hmsOutputDataList.Add(significantHeaveRateMax20mData);
            hmsOutputDataList.Add(significantHeaveRateMax3hData);

            hmsOutputDataList.Add(maxHeaveRateData);

            //hmsOutputDataList.Add(significantWaveHeightData);

            hmsOutputDataList.Add(heaveHeightData);
            hmsOutputDataList.Add(heaveHeightMax20mData);
            hmsOutputDataList.Add(heaveHeightMax3hData);
            hmsOutputDataList.Add(heavePeriodMeanData);

            hmsOutputDataList.Add(motionLimitPitchRoll);
            hmsOutputDataList.Add(motionLimitInclination);
            hmsOutputDataList.Add(motionLimitHeaveHeight);
            hmsOutputDataList.Add(motionLimitSignificantHeaveRate);

            // NB! Selv om WSI ikke brukes i NOROG må vi legge den inn her
            // slik at database-tabell blir lik for CAP/NOROG.
            // Får database-feil ved bytte mellom CAP/NOROG når tabellene ikke er like.
            hmsOutputDataList.Add(msiData);

            // Legge på litt informasjon på de variablene som ikke får dette automatisk fra sensor input data
            // (Disse verdiene blir kalkulert her, mens sensor input data bare kopieres videre med denne typen info allerede inkludert)

            pitchMax20mData.id = (int)ValueType.PitchMax20m;
            pitchMax20mData.name = "Pitch Max (20m)";
            pitchMax20mData.dbColumn = "pitch_max_20m";
            pitchMax20mData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser, adminSettingsVM);
            pitchMax20mData.AddProcessing(CalculationType.RoundingDecimals, 1);
            pitchMax20mData.AddProcessing(CalculationType.TimeMaxAbsolute, Constants.Minutes20);

            pitchMax3hData.id = (int)ValueType.PitchMax3h;
            pitchMax3hData.name = "Pitch Max (3h)";
            pitchMax3hData.dbColumn = "pitch_max_3h";
            pitchMax3hData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser, adminSettingsVM);
            pitchMax3hData.AddProcessing(CalculationType.RoundingDecimals, 1);
            pitchMax3hData.AddProcessing(CalculationType.TimeMaxAbsolute, Constants.Hours3);

            pitchMaxUp20mData.id = (int)ValueType.PitchMaxUp20m;
            pitchMaxUp20mData.name = "Pitch Max Up (20m)";
            pitchMaxUp20mData.dbColumn = "pitch_max_up_20m";
            pitchMaxUp20mData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser, adminSettingsVM);
            pitchMaxUp20mData.AddProcessing(CalculationType.RoundingDecimals, 1);
            pitchMaxUp20mData.AddProcessing(CalculationType.TimeHeighest, Constants.Minutes20);
            //pitchMaxUp20mData.AddProcessing(CalculationType.Absolute, 0);

            pitchMaxDown20mData.id = (int)ValueType.PitchMaxDown20m;
            pitchMaxDown20mData.name = "Pitch Max Down (20m)";
            pitchMaxDown20mData.dbColumn = "pitch_max_down_20m";
            pitchMaxDown20mData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser, adminSettingsVM);
            pitchMaxDown20mData.AddProcessing(CalculationType.RoundingDecimals, 1);
            pitchMaxDown20mData.AddProcessing(CalculationType.TimeLowest, Constants.Minutes20);
            //pitchMaxDown20mData.AddProcessing(CalculationType.Absolute, 0);

            rollMax20mData.id = (int)ValueType.RollMax20m;
            rollMax20mData.name = "Roll Max (20m)";
            rollMax20mData.dbColumn = "roll_max_20m";
            rollMax20mData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser, adminSettingsVM);
            rollMax20mData.AddProcessing(CalculationType.RoundingDecimals, 1);
            rollMax20mData.AddProcessing(CalculationType.TimeMaxAbsolute, Constants.Minutes20);

            rollMax3hData.id = (int)ValueType.RollMax3h;
            rollMax3hData.name = "Roll Max (3h)";
            rollMax3hData.dbColumn = "roll_max_3h";
            rollMax3hData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser, adminSettingsVM);
            rollMax3hData.AddProcessing(CalculationType.RoundingDecimals, 1);
            rollMax3hData.AddProcessing(CalculationType.TimeMaxAbsolute, Constants.Hours3);

            rollMaxLeft20mData.id = (int)ValueType.RollMaxLeft20m;
            rollMaxLeft20mData.name = "Roll Max Left (20m)";
            rollMaxLeft20mData.dbColumn = "roll_max_left_20m";
            rollMaxLeft20mData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser, adminSettingsVM);
            rollMaxLeft20mData.AddProcessing(CalculationType.RoundingDecimals, 1);
            rollMaxLeft20mData.AddProcessing(CalculationType.TimeLowest, Constants.Minutes20);
            //rollMaxLeft20mData.AddProcessing(CalculationType.Absolute, 0);

            rollMaxRight20mData.id = (int)ValueType.RollMaxRight20m;
            rollMaxRight20mData.name = "Roll Max Right (20m)";
            rollMaxRight20mData.dbColumn = "roll_max_right_20m";
            rollMaxRight20mData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser, adminSettingsVM);
            rollMaxRight20mData.AddProcessing(CalculationType.RoundingDecimals, 1);
            rollMaxRight20mData.AddProcessing(CalculationType.TimeHeighest, Constants.Minutes20);
            //rollMaxRight20mData.AddProcessing(CalculationType.Absolute, 0);

            inclinationData.id = (int)ValueType.Inclination;
            inclinationData.name = "Inclination";
            inclinationData.dbColumn = "inclination";

            inclination20mMaxData.id = (int)ValueType.InclinationMax20m;
            inclination20mMaxData.name = "Inclination Max (20m)";
            inclination20mMaxData.dbColumn = "inclination_max_20m";

            inclination3hMaxData.id = (int)ValueType.InclinationMax3h;
            inclination3hMaxData.name = "Inclination Max (3h)";
            inclination3hMaxData.dbColumn = "inclination_max_3h";

            heaveHeightData.id = (int)ValueType.HeaveHeight;
            heaveHeightData.name = "Heave Height";
            heaveHeightData.dbColumn = "heave_height";
            heaveHeightData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser, adminSettingsVM);
            heaveHeightData.AddProcessing(CalculationType.MeanWaveHeight, 0);
            heaveHeightData.AddProcessing(CalculationType.RoundingDecimals, 1);

            heaveHeightMax20mData.id = (int)ValueType.HeaveHeightMax20m;
            heaveHeightMax20mData.name = "Heave Height Max (20m)";
            heaveHeightMax20mData.dbColumn = "heave_height_max_20m";
            heaveHeightMax20mData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser, adminSettingsVM);
            heaveHeightMax20mData.AddProcessing(CalculationType.RoundingDecimals, 1);
            heaveHeightMax20mData.AddProcessing(CalculationType.TimeMaxWaveHeight, Constants.Minutes20);

            heaveHeightMax3hData.id = (int)ValueType.HeaveHeightMax3h;
            heaveHeightMax3hData.name = "Heave Height Max (3h)";
            heaveHeightMax3hData.dbColumn = "heave_height_max_3h";
            heaveHeightMax3hData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser, adminSettingsVM);
            heaveHeightMax3hData.AddProcessing(CalculationType.RoundingDecimals, 1);
            heaveHeightMax3hData.AddProcessing(CalculationType.TimeMaxWaveHeight, Constants.Hours3);

            heavePeriodMeanData.id = (int)ValueType.HeavePeriodMean;
            heavePeriodMeanData.name = "Heave Period";
            heavePeriodMeanData.dbColumn = "heave_period";
            heavePeriodMeanData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser, adminSettingsVM);
            heavePeriodMeanData.AddProcessing(CalculationType.TimeMeanPeriod, Constants.Minutes20);
            heavePeriodMeanData.AddProcessing(CalculationType.RoundingDecimals, 1);

            significantHeaveRateData.id = (int)ValueType.SignificantHeaveRate;
            significantHeaveRateData.name = "Significant Heave Rate";
            significantHeaveRateData.dbColumn = "significant_heave_rate";
            significantHeaveRateData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser, adminSettingsVM);
            significantHeaveRateData.AddProcessing(CalculationType.SignificantHeaveRate, Constants.Minutes20);
            significantHeaveRateData.AddProcessing(CalculationType.RoundingDecimals, 1);

            significantHeaveRateMax20mData.id = (int)ValueType.SignificantHeaveRateMax20m; // Brukes til å justere akse på graf
            significantHeaveRateMax20mData.name = "Significant Heave Rate Max (20m)";
            significantHeaveRateMax20mData.dbColumn = "shr_max_20m";
            significantHeaveRateMax20mData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser, adminSettingsVM);
            significantHeaveRateMax20mData.AddProcessing(CalculationType.RoundingDecimals, 1);
            significantHeaveRateMax20mData.AddProcessing(CalculationType.TimeHeighest, Constants.Minutes20);

            significantHeaveRateMax3hData.id = (int)ValueType.SignificantHeaveRateMax3h; // Brukes til å justere akse på graf
            significantHeaveRateMax3hData.name = "Significant Heave Rate Max (3h)";
            significantHeaveRateMax3hData.dbColumn = "shr_max_3h";
            significantHeaveRateMax3hData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser, adminSettingsVM);
            significantHeaveRateMax3hData.AddProcessing(CalculationType.RoundingDecimals, 1);
            significantHeaveRateMax3hData.AddProcessing(CalculationType.TimeHeighest, Constants.Hours3);

            maxHeaveRateData.id = (int)ValueType.MaxHeaveRate;
            maxHeaveRateData.name = "Max Heave Rate";
            maxHeaveRateData.dbColumn = "max_heave_rate";
            maxHeaveRateData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser, adminSettingsVM);
            maxHeaveRateData.AddProcessing(CalculationType.RoundingDecimals, 1);
            maxHeaveRateData.AddProcessing(CalculationType.TimeMaxAbsolute, Constants.Minutes20);

            //significantWaveHeightData.id = (int)ValueType.SignificantWaveHeight;
            //significantWaveHeightData.name = "Significant Wave Height";
            //significantWaveHeightData.dbColumn = "significant_wave_height";
            //significantWaveHeightData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser);
            //significantWaveHeightData.AddProcessing(CalculationType.SignificantWaveHeight, Constants.Minutes20);
            //significantWaveHeightData.AddProcessing(CalculationType.RoundingDecimals, 1);

            motionLimitPitchRoll.id = (int)ValueType.MotionLimitPitchRoll;
            motionLimitPitchRoll.name = "Motion Limit Pitch and Roll";
            motionLimitPitchRoll.sensorGroupId = Constants.NO_SENSOR_GROUP_ID;
            motionLimitPitchRoll.dbColumn = "motion_limit_pitch_roll";

            motionLimitInclination.id = (int)ValueType.MotionLimitInclination;
            motionLimitInclination.name = "Motion Limit Inclination";
            motionLimitInclination.sensorGroupId = Constants.NO_SENSOR_GROUP_ID;
            motionLimitInclination.dbColumn = "motion_limit_inclination";

            motionLimitHeaveHeight.id = (int)ValueType.MotionLimitHeaveHeight;
            motionLimitHeaveHeight.name = "Motion Limit Heave Height";
            motionLimitHeaveHeight.sensorGroupId = Constants.NO_SENSOR_GROUP_ID;
            motionLimitHeaveHeight.dbColumn = "motion_limit_heave_height";

            motionLimitSignificantHeaveRate.id = (int)ValueType.MotionLimitSignificantHeaveRate;
            motionLimitSignificantHeaveRate.name = "Motion Limit Significant Heave Rate";
            motionLimitSignificantHeaveRate.sensorGroupId = Constants.NO_SENSOR_GROUP_ID;
            motionLimitSignificantHeaveRate.dbColumn = "motion_limit_shr";

            msiData.id = (int)ValueType.MSI;
            msiData.name = "MSI";
            msiData.sensorGroupId = Constants.NO_SENSOR_GROUP_ID;
            msiData.dbColumn = "msi";
        }

        public void Update(HMSDataCollection hmsInputDataList)
        {
            // Hente input data vi skal bruke
            inputPitchData.Set(hmsInputDataList.GetData(ValueType.Pitch));
            inputRollData.Set(hmsInputDataList.GetData(ValueType.Roll));
            inputHeaveData.Set(hmsInputDataList.GetData(ValueType.Heave));
            inputHeaveRateData.Set(hmsInputDataList.GetData(ValueType.HeaveRate));
            inputAccelerationXData.Set(hmsInputDataList.GetData(ValueType.AccelerationX));
            inputAccelerationYData.Set(hmsInputDataList.GetData(ValueType.AccelerationY));
            inputAccelerationZData.Set(hmsInputDataList.GetData(ValueType.AccelerationZ));

            // Sjekke om det er endring i input data
            // Dersom det ikke er endring, trenger vi ikke utføre kalkulasjoner
            if (inputPitchData.timestamp != inputPitchDataPrev.timestamp ||
                inputRollData.timestamp != inputRollDataPrev.timestamp ||
                inputHeaveData.timestamp != inputHeaveDataPrev.timestamp ||
                inputHeaveRateData.timestamp != inputHeaveRateDataPrev.timestamp ||
                inputAccelerationXData.timestamp != inputAccelerationXDataPrev.timestamp ||
                inputAccelerationYData.timestamp != inputAccelerationYDataPrev.timestamp ||
                inputAccelerationZData.timestamp != inputAccelerationZDataPrev.timestamp ||
                firstRun)
            {
                firstRun = false; // Første gjennomkjøring unnagjort

                // Tar data fra input delen av server og overfører til HMS output delen
                // og prosesserer input for overføring til HMS output også.

                // Pitch
                pitchData.Set(inputPitchData);
                pitchMax20mData.DoProcessing(pitchData);
                pitchMax3hData.DoProcessing(pitchData);
                pitchMaxUp20mData.DoProcessing(pitchData);
                pitchMaxDown20mData.DoProcessing(pitchData);

                if (adminSettingsVM.regulationStandard == RegulationStandard.CAP && !adminSettingsVM.overrideMotionBuffer)
                {
                    pitchMax20mData.BufferFillCheck(Constants.MotionBufferFill99Pct);
                    pitchMaxUp20mData.BufferFillCheck(Constants.MotionBufferFill99Pct);
                    pitchMaxDown20mData.BufferFillCheck(Constants.MotionBufferFill99Pct);
                }

                // Roll
                rollData.Set(inputRollData);
                rollMax20mData.DoProcessing(rollData);
                rollMax3hData.DoProcessing(rollData);
                rollMaxLeft20mData.DoProcessing(rollData);
                rollMaxRight20mData.DoProcessing(rollData);

                if (adminSettingsVM.regulationStandard == RegulationStandard.CAP && !adminSettingsVM.overrideMotionBuffer)
                {
                    rollMax20mData.BufferFillCheck(Constants.MotionBufferFill99Pct);
                    rollMaxLeft20mData.BufferFillCheck(Constants.MotionBufferFill99Pct);
                    rollMaxRight20mData.BufferFillCheck(Constants.MotionBufferFill99Pct);
                }

                // Inclination
                UpdateInclinationData(pitchData, rollData, inclination20mMaxData, Constants.Minutes20);
                UpdateInclinationData(pitchData, rollData, inclination3hMaxData, Constants.Hours3);

                if (adminSettingsVM.regulationStandard == RegulationStandard.CAP)
                {
                    inclination20mMaxData.status = pitchMax20mData.status;
                    inclination3hMaxData.status = pitchMax3hData.status;
                }

                // Heave Height
                heaveHeightData.DoProcessing(inputHeaveData);
                heaveHeightMax20mData.DoProcessing(inputHeaveData);
                heaveHeightMax3hData.DoProcessing(inputHeaveData);

                // Heave Period
                heavePeriodMeanData.DoProcessing(inputHeaveData);

                // Significant Heave Rate
                significantHeaveRateData.DoProcessing(inputHeaveRateData);
                significantHeaveRateMax20mData.DoProcessing(significantHeaveRateData);
                significantHeaveRateMax3hData.DoProcessing(significantHeaveRateData);

                if (adminSettingsVM.regulationStandard == RegulationStandard.CAP && !adminSettingsVM.overrideMotionBuffer)
                {
                    significantHeaveRateData.BufferFillCheck(Constants.MotionBufferFill99Pct);
                    significantHeaveRateMax20mData.BufferFillCheck(Constants.MotionBufferFill99Pct);
                    //significantHeaveRateMax3hData.BufferFillCheck(Constants.MotionBufferFill99Pct);
                }

                // Maximum Heave Rate
                maxHeaveRateData.DoProcessing(inputHeaveRateData);

                if (adminSettingsVM.regulationStandard == RegulationStandard.CAP && !adminSettingsVM.overrideMotionBuffer)
                    maxHeaveRateData.BufferFillCheck(Constants.MotionBufferFill99Pct);

                // Significant Wave Height
                //significantWaveHeightData.DoProcessing(inputHeaveData);

                // Motion Limits
                motionLimitPitchRoll.data = motionLimits.GetLimit(LimitType.PitchRoll);
                motionLimitPitchRoll.timestamp = DateTime.UtcNow;
                motionLimitPitchRoll.status = DataStatus.OK;

                motionLimitInclination.data = motionLimits.GetLimit(LimitType.Inclination);
                motionLimitInclination.timestamp = DateTime.UtcNow;
                motionLimitInclination.status = DataStatus.OK;

                motionLimitHeaveHeight.data = motionLimits.GetLimit(LimitType.HeaveHeight);
                motionLimitHeaveHeight.timestamp = DateTime.UtcNow;
                motionLimitHeaveHeight.status = DataStatus.OK;

                motionLimitSignificantHeaveRate.data = motionLimits.GetLimit(LimitType.SignificantHeaveRate);
                motionLimitSignificantHeaveRate.timestamp = DateTime.UtcNow;
                motionLimitSignificantHeaveRate.status = DataStatus.OK;

                // SHR Limit
                UpdateSHRLimitConditions(significantHeaveRateData);

                // CAP spesifikke variabler / kalkulasjoner
                if (adminSettingsVM.regulationStandard == RegulationStandard.CAP)
                {
                    // MSI
                    /////////////////////////////////////////////////////////////////////////////////////////
                    ///
                    HMSData mms_msi = new HMSData();

                    HMSData accelerationX = new HMSData(inputAccelerationXData);
                    HMSData accelerationY = new HMSData(inputAccelerationYData);
                    HMSData accelerationZ = new HMSData(inputAccelerationZData);

                    if (accelerationX.status == DataStatus.OK &&
                        accelerationY.status == DataStatus.OK &&
                        accelerationZ.status == DataStatus.OK)
                    {
                        double mms;

                        // Kalkulere MMS (CAP formel)
                        if (accelerationZ.data != 0)
                            mms = Math.Sqrt(Math.Pow(accelerationX.data, 2.0) + Math.Pow(accelerationY.data, 2.0)) / Math.Abs(accelerationZ.data);
                        else
                            mms = 0.0;

                        // Kalkulere MMS_MSI (CAP formel)
                        mms_msi.data = 10.0 * HMSCalc.ToDegrees(Math.Atan(mms));
                        mms_msi.status = DataStatus.OK;
                        mms_msi.timestamp = accelerationX.timestamp;
                    }
                    else
                    {
                        mms_msi.data = 0;
                        mms_msi.status = accelerationX.status;
                        mms_msi.timestamp = accelerationX.timestamp;
                    }

                    // Find max value
                    CalculateMSIMax(mms_msi, mms_msi_list, msiData, Constants.Minutes20);

                    // Sjekke buffer fyllingsgrad
                    if (!adminSettingsVM.overrideMotionBuffer)
                        MSIBufferFillCheck(mms_msi_list, Constants.MotionBufferFill99Pct, msiData);
                }

                // Sjekker motion limits
                CheckLimits();
            }

            // Oppdatere "forrige" verdiene
            inputPitchDataPrev.Set(inputPitchData);
            inputRollDataPrev.Set(inputRollData);
            inputHeaveDataPrev.Set(inputHeaveData);
            inputHeaveRateDataPrev.Set(inputHeaveRateData);
            inputAccelerationXDataPrev.Set(inputAccelerationXData);
            inputAccelerationYDataPrev.Set(inputAccelerationYData);
            inputAccelerationZDataPrev.Set(inputAccelerationZData);
        }

        // Resette dataCalculations
        public void ResetDataCalculations()
        {
            // Diverse
            pitchMax20mData.ResetDataCalculations();
            pitchMax3hData.ResetDataCalculations();
            pitchMaxUp20mData.ResetDataCalculations();
            pitchMaxDown20mData.ResetDataCalculations();
            rollMax20mData.ResetDataCalculations();
            rollMax3hData.ResetDataCalculations();
            rollMaxLeft20mData.ResetDataCalculations();
            rollMaxRight20mData.ResetDataCalculations();
            heaveHeightData.ResetDataCalculations();
            heaveHeightMax20mData.ResetDataCalculations();
            heaveHeightMax3hData.ResetDataCalculations();
            heavePeriodMeanData.ResetDataCalculations();
            significantHeaveRateData.ResetDataCalculations();
            significantHeaveRateMax20mData.ResetDataCalculations();
            significantHeaveRateMax3hData.ResetDataCalculations();
            maxHeaveRateData.ResetDataCalculations();
            //significantWaveHeightData.ResetDataCalculations();

            // Inclination
            inclination20mMaxList.Clear();
            inclination3hMaxList.Clear();

            // MSI
            msiData.data = 0;
            mms_msi_list.Clear();

            // SHR
            significantHeaveRate2mMin = double.MaxValue;
            significantHeaveRate10mSum = 0;
            significantHeaveRate10mMean = 0;
            significantHeaveRate20mMax = 0;

            significantHeaveRate2mMinData.Clear();
            significantHeaveRate10mMeanData.Clear();
            significantHeaveRate20mMaxData.Clear();
        }

        private void CalculateMSIMax(HMSData value, List<TimeData> dataList, HMSData maxValue, double time)
        {
            // Først sjekke om vi har en ny verdi
            if (value.timestamp != dataList.LastOrDefault()?.timestamp)
            {
                // Sjekke status
                if (value.status == DataStatus.OK)
                {
                    // Korreksjon R og avrunding til en desimal
                    double newValueCorrR = Math.Round(value.data * adminSettingsVM.msiCorrectionR, 1, MidpointRounding.AwayFromZero);

                    // Legge inn den nye verdien i data settet
                    dataList.Add(new TimeData() { data = newValueCorrR, timestamp = value.timestamp });

                    // Større max verdi?
                    if (newValueCorrR > maxValue.data)
                    {
                        // Sett ny max verdi
                        maxValue.data = newValueCorrR;
                    }

                    // Timestamp og status
                    maxValue.timestamp = value.timestamp;
                    maxValue.status = value.status;
                }
                else
                {
                    maxValue.status = value.status;
                }
            }

            // Sjekke om vi skal ta ut gamle verdier
            bool findNewMaxValue = false;

            for (int i = 0; i < dataList.Count && dataList.Count > 0; i++)
            {
                // Time stamp eldre enn satt grense?
                if (dataList[i]?.timestamp.AddSeconds(time) < DateTime.UtcNow)
                {
                    // Sjekke om dette var høyeste verdi
                    if (dataList[i].data == maxValue.data)
                    {
                        // Finne ny høyeste verdi
                        findNewMaxValue = true;
                    }

                    // Fjerne gammel verdi fra verdiliste
                    dataList.RemoveAt(i--);
                }
            }

            // Finne ny høyeste verdi
            if (findNewMaxValue)
            {
                double oldMaxValue = maxValue.data;
                maxValue.data = 0;
                bool foundNewMax = false;

                for (int i = 0; i < dataList.Count && !foundNewMax; i++)
                {
                    // Kan avslutte søket dersom vi finner en verdi like den gamle max verdien (ingen er høyere)
                    if (dataList[i]?.data == oldMaxValue)
                    {
                        maxValue.data = Math.Round(dataList[i].data * adminSettingsVM.msiCorrectionR, 1, MidpointRounding.AwayFromZero);
                        maxValue.timestamp = dataList[i].timestamp;
                        maxValue.status = DataStatus.OK;

                        foundNewMax = true;
                    }
                    else
                    {
                        if (dataList[i]?.data > maxValue.data)
                        {
                            maxValue.data = Math.Round(dataList[i].data * adminSettingsVM.msiCorrectionR, 1, MidpointRounding.AwayFromZero);
                            maxValue.timestamp = dataList[i].timestamp;
                            maxValue.status = DataStatus.OK;
                        }
                    }
                }
            }
        }

        private void MSIBufferFillCheck(List<TimeData> dataList, double targetCount, HMSData msi)
        {
            // Er bufferet fyllt opp
            if (dataList.Count < targetCount)
                // Hvis ikke, sjekk status, dersom OK
                if (msi.status == DataStatus.OK)
                    // Set OK, men ikke tilgjengelig
                    msi.status = DataStatus.OK_NA;
        }

        private void CheckLimits()
        {
            // Pitch & roll
            CheckLimit(pitchMax20mData, LimitType.PitchRoll);
            CheckLimit(pitchMaxUp20mData, LimitType.PitchRoll);
            CheckLimit(pitchMaxDown20mData, LimitType.PitchRoll);

            CheckLimit(rollMax20mData, LimitType.PitchRoll);
            CheckLimit(rollMaxLeft20mData, LimitType.PitchRoll);
            CheckLimit(rollMaxRight20mData, LimitType.PitchRoll);

            // Inclination
            CheckLimit(inclinationData, LimitType.Inclination);
            CheckLimit(inclination20mMaxData, LimitType.Inclination);

            // Heave
            CheckLimit(heaveHeightMax20mData, LimitType.HeaveHeight);

            // Significant Heave Rate
            if (SHRIsWithinLimits)
            {
                if (IsSHR2mMinAboveLimit() && significantHeaveRateData.data >= motionLimits.GetLimit(LimitType.SignificantHeaveRate))
                {
                    SHRIsWithinLimits = false;
                    significantHeaveRateData.limitStatus = LimitStatus.OVER_LIMIT;
                }
                else
                {
                    significantHeaveRateData.limitStatus = LimitStatus.OK;
                }
            }
            else
            {
                if (IsSHR10mMeanBelowLimit() && significantHeaveRateData.data < motionLimits.GetLimit(LimitType.SignificantHeaveRate))
                {
                    SHRIsWithinLimits = true;
                    significantHeaveRateData.limitStatus = LimitStatus.OK;
                }
                else
                {
                    significantHeaveRateData.limitStatus = LimitStatus.OVER_LIMIT;
                }
            }
        }

        private void CheckLimit(HMSData hmsData, LimitType limitType)
        {
            if (hmsData.data <= motionLimits.GetLimit(limitType))
                hmsData.limitStatus = LimitStatus.OK;
            else
                hmsData.limitStatus = LimitStatus.OVER_LIMIT;
        }

        public bool IsSHR10mMeanBelowLimit()
        {
            return significantHeaveRate10mMean < motionLimits.GetLimit(LimitType.SignificantHeaveRate);
        }

        public bool IsSHR2mMinAboveLimit()
        {
            return significantHeaveRate2mMin >= motionLimits.GetLimit(LimitType.SignificantHeaveRate);
        }

        //public double GetSHR95Pct()
        //{
        //    return significantHeaveRateData.data * 0.95;
        //}

        /////////////////////////////////////////////////////////////////////////////
        // Inclination Kalkulasjon
        /////////////////////////////////////////////////////////////////////////////
        private void CalcInclination(HMSData pitch, HMSData roll, HMSData outputData)
        {
            if (pitch != null &&
                roll != null)
            {
                // Beregne inclination
                double inclination = HMSCalc.Inclination(pitch.data, roll.data);

                // Data
                outputData.data = Math.Round(inclination, 1, MidpointRounding.AwayFromZero);

                // Timestamp
                if (pitch.timestamp < roll.timestamp)
                    outputData.timestamp = pitch.timestamp;
                else
                    outputData.timestamp = roll.timestamp;

                // Status
                if (pitch.status == DataStatus.OK && roll.status == DataStatus.OK)
                    outputData.status = DataStatus.OK;
                else
                    outputData.status = DataStatus.TIMEOUT_ERROR;
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Significant Heave Rate
        /////////////////////////////////////////////////////////////////////////////
        private void UpdateSHRLimitConditions(HMSData sensorData)
        {
            // Bruker avrundet verdi i beregninger ifm helideck status
            double shrDataRounded = Math.Round(sensorData.data, 1, MidpointRounding.AwayFromZero);

            // Sjekker status på data først
            if (sensorData.status == DataStatus.OK)
            {
                // 2-minute minimum
                ///////////////////////////////////////////////////////////

                // Lagre i 2-minutters listen
                significantHeaveRate2mMinData.Add(new TimeData()
                {
                    data = shrDataRounded,
                    timestamp = sensorData.timestamp
                });

                // Sjekke om data er mindre enn minste SHR lagret siste 2 minutter
                if (shrDataRounded < significantHeaveRate2mMin || significantHeaveRate2mMin == double.MaxValue)
                    significantHeaveRate2mMin = shrDataRounded;

                // Sjekke om vi skal fjerne data fra 2 min listen
                bool findNewMinValue = false;

                for (int i = 0; i < significantHeaveRate2mMinData.Count && significantHeaveRate2mMinData.Count > 0; i++)
                {
                    if (significantHeaveRate2mMinData[i]?.timestamp.AddMinutes(2) < DateTime.UtcNow)
                    {
                        // Er den verdien vi nå skal fjerne lik minimunsverdien?
                        if (significantHeaveRate2mMinData[i].data == significantHeaveRate2mMin)
                            findNewMinValue = true;

                        significantHeaveRate2mMinData.RemoveAt(i--);
                    }
                }

                // Gå gjennom hele listen og finne ny minimumsverdi
                if (findNewMinValue)
                {
                    double oldMinValue = significantHeaveRate2mMin;
                    significantHeaveRate2mMin = double.MaxValue;
                    bool foundNewMax = false;

                    for (int i = 0; i < significantHeaveRate2mMinData.Count && !foundNewMax; i++)
                    {
                        // Kan avslutte søket dersom vi finne en verdi like den gamle minimumsverdien (ingen er lavere)
                        if (significantHeaveRate2mMinData[i]?.data == oldMinValue)
                        {
                            significantHeaveRate2mMin = oldMinValue;
                            foundNewMax = true;
                        }
                        else
                        {
                            // Sjekke om data er mindre enn minste lagret
                            if (significantHeaveRate2mMinData[i]?.data < significantHeaveRate2mMin)
                                significantHeaveRate2mMin = significantHeaveRate2mMinData[i].data;
                        }
                    }
                }

                // 10-minute mean
                ///////////////////////////////////////////////////////////
                ///
                // Legge inn den nye verdien i data settet
                significantHeaveRate10mMeanData.Add(new TimeData()
                {
                    data = shrDataRounded,
                    timestamp = sensorData.timestamp
                });

                // Legge til i total summen
                significantHeaveRate10mSum += shrDataRounded;

                // Sjekke om vi skal ta ut gamle verdier
                for (int i = 0; i < significantHeaveRate10mMeanData.Count && significantHeaveRate10mMeanData.Count > 0; i++)
                {
                    if (significantHeaveRate10mMeanData[i]?.timestamp.AddMinutes(10) < DateTime.UtcNow)
                    {
                        // Trekke fra i total summen
                        significantHeaveRate10mSum -= significantHeaveRate10mMeanData[i].data;

                        // Fjerne fra verdi listne
                        significantHeaveRate10mMeanData.RemoveAt(i--);
                    }
                }

                // Beregne gjennomsnitt av de verdiene som ligger i datasettet
                if (significantHeaveRate10mMeanData.Count > 0)
                    significantHeaveRate10mMean = Math.Round(significantHeaveRate10mSum / significantHeaveRate10mMeanData.Count, 1, MidpointRounding.AwayFromZero);
                else
                    significantHeaveRate10mMean = 0;

                // 20-minute max
                ///////////////////////////////////////////////////////////
                    ///
                    // Legge inn den nye verdien i data settet
                significantHeaveRate20mMaxData.Add(new TimeData()
                {
                    data = shrDataRounded,
                    timestamp = sensorData.timestamp
                });

                // Sjekke om data er større enn største SHR lagret siste 20 minutter
                if (shrDataRounded > significantHeaveRate20mMax)
                    significantHeaveRate20mMax = shrDataRounded;

                // Sjekke om vi skal fjerne data fra 20 min listen
                bool findNewMaxValue = false;
                for (int i = 0; i < significantHeaveRate20mMaxData.Count && significantHeaveRate20mMaxData.Count > 0; i++)
                {
                    if (significantHeaveRate20mMaxData[i]?.timestamp.AddMinutes(20) < DateTime.UtcNow)
                    {
                        // Er den verdien vi nå skal fjerne lik maximunsverdien?
                        if (significantHeaveRate20mMaxData[i].data == significantHeaveRate20mMax)
                            findNewMaxValue = true;

                        significantHeaveRate20mMaxData.RemoveAt(i--);
                    }
                }

                // Gå gjennom hele listen og finne ny maximumsverdi
                if (findNewMaxValue)
                {
                    double oldMaxValue = significantHeaveRate20mMax;
                    significantHeaveRate20mMax = double.MinValue;
                    bool foundNewMax = false;

                    for (int i = 0; i < significantHeaveRate20mMaxData.Count && !foundNewMax; i++)
                    {
                        // Kan avslutte søket dersom vi finne en verdi like den gamle minimumsverdien (ingen er lavere)
                        if (significantHeaveRate20mMaxData[i]?.data == oldMaxValue)
                        {
                            significantHeaveRate20mMax = oldMaxValue;
                            foundNewMax = true;
                        }
                        else
                        {
                            // Sjekke om data er mindre enn minste lagret
                            if (significantHeaveRate20mMaxData[i]?.data > significantHeaveRate20mMax)
                                significantHeaveRate20mMax = significantHeaveRate20mMaxData[i].data;
                        }
                    }
                }
            }
        }

        public void UpdateInclinationData(HMSData pitchData, HMSData rollData, HMSData inclinationMaxData, int time)
        {
            // Første regne nå-verdi for inclination
            CalcInclination(pitchData, rollData, inclinationData);

            // 20-minute max
            ///////////////////////////////////////////////////////////
            ///
            // Legge inn den nye verdien i data settet
            inclination20mMaxList.Add(new TimeData()
            {
                data = inclinationData.data,
                timestamp = inclinationData.timestamp
            });

            // Sjekke om data er større enn største inclination lagret siste 20 minutter
            if (inclinationData.data > inclinationMaxData.data)
            {
                inclinationMaxData.data = inclinationData.data;
            }

            // Sjekke om vi skal fjerne data fra 20 min listen
            bool findNewMaxValue = false;

            for (int i = 0; i < inclination20mMaxList.Count && inclination20mMaxList.Count > 0; i++)
            {
                if (inclination20mMaxList[i]?.timestamp.AddSeconds(time) < DateTime.UtcNow)
                {
                    // Er den verdien vi nå skal fjerne lik maximunsverdien?
                    if (inclination20mMaxList[i].data == inclinationMaxData.data)
                        findNewMaxValue = true;

                    inclination20mMaxList.RemoveAt(i--);
                }
            }

            // Gå gjennom hele listen og finne ny maximumsverdi
            if (findNewMaxValue)
            {
                double oldMaxValue = inclinationMaxData.data;
                inclinationMaxData.data = 0;
                bool foundNewMax = false;

                for (int i = 0; i < inclination20mMaxList.Count && !foundNewMax; i++)
                {
                    // Kan avslutte søket dersom vi finne en verdi like den gamle maximumsverdien (ingen er høyere)
                    if (inclination20mMaxList[i]?.data == oldMaxValue)
                    {
                        inclinationMaxData.data = inclination20mMaxList[i].data;
                        foundNewMax = true;
                    }
                    else
                    {
                        // Sjekke om data er mindre enn minste lagret
                        if (inclination20mMaxList[i]?.data > inclinationMaxData.data)
                            inclinationMaxData.data = inclination20mMaxList[i].data;
                    }
                }
            }

            // Oppdatere timestamp og status
            inclinationMaxData.timestamp = inclinationData.timestamp;
            inclinationMaxData.status = inclinationData.status;
        }
    }
}
