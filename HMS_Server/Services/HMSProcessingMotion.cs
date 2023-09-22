using System;
using System.Collections.Generic;
using System.Linq;
using Telerik.Windows.Data;

namespace HMS_Server
{
    public class HMSProcessingMotion
    {
        // TEST
        private DateTime testTimer;

        // Input data
        private HMSData sensorPitch = new HMSData();
        private HMSData sensorRoll = new HMSData();
        private HMSData sensorHeave = new HMSData();
        private HMSData sensorHeaveRate = new HMSData();
        private HMSData sensorAccelerationX = new HMSData();
        private HMSData sensorAccelerationY = new HMSData();
        private HMSData sensorAccelerationZ = new HMSData();

        private HMSData sensorSensorMRU = new HMSData();

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

        private RadObservableCollection<TimeData> inclination20mMaxList = new RadObservableCollection<TimeData>();
        private RadObservableCollection<TimeData> inclination3hMaxList = new RadObservableCollection<TimeData>();

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

        // Status
        private HMSData statusRoll = new HMSData();
        private HMSData statusPitch = new HMSData();
        private HMSData statusInclination = new HMSData();
        private HMSData statusSHR = new HMSData();

        private HMSData statusMRU = new HMSData();

        private RadObservableCollection<TimeData> significantHeaveRate2mMinData = new RadObservableCollection<TimeData>();
        private RadObservableCollection<TimeData> significantHeaveRate10mMeanData = new RadObservableCollection<TimeData>();
        private RadObservableCollection<TimeData> significantHeaveRate20mMaxData = new RadObservableCollection<TimeData>();

        private ErrorHandler errorHandler;

        private bool databaseSetupRun = true;

        public HMSProcessingMotion(HMSDataCollection hmsOutputData, HelideckMotionLimits motionLimits, AdminSettingsVM adminSettingsVM, ErrorHandler errorHandler)
        {
            this.motionLimits = motionLimits;
            this.adminSettingsVM = adminSettingsVM;
            this.errorHandler = errorHandler;

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

            hmsOutputDataList.Add(statusRoll);
            hmsOutputDataList.Add(statusPitch);
            hmsOutputDataList.Add(statusInclination);
            hmsOutputDataList.Add(statusSHR);

            hmsOutputDataList.Add(statusMRU);

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
            pitchMaxUp20mData.AddProcessing(CalculationType.TimeMax, Constants.Minutes20);

            pitchMaxDown20mData.id = (int)ValueType.PitchMaxDown20m;
            pitchMaxDown20mData.name = "Pitch Max Down (20m)";
            pitchMaxDown20mData.dbColumn = "pitch_max_down_20m";
            pitchMaxDown20mData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser, adminSettingsVM);
            pitchMaxDown20mData.AddProcessing(CalculationType.RoundingDecimals, 1);
            pitchMaxDown20mData.AddProcessing(CalculationType.TimeMin, Constants.Minutes20);

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
            rollMaxLeft20mData.AddProcessing(CalculationType.TimeMax, Constants.Minutes20);

            rollMaxRight20mData.id = (int)ValueType.RollMaxRight20m;
            rollMaxRight20mData.name = "Roll Max Right (20m)";
            rollMaxRight20mData.dbColumn = "roll_max_right_20m";
            rollMaxRight20mData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser, adminSettingsVM);
            rollMaxRight20mData.AddProcessing(CalculationType.RoundingDecimals, 1);
            rollMaxRight20mData.AddProcessing(CalculationType.TimeMin, Constants.Minutes20);

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
            heaveHeightData.AddProcessing(CalculationType.WaveHeight, Constants.Minutes20);

            heaveHeightMax20mData.id = (int)ValueType.HeaveHeightMax20m;
            heaveHeightMax20mData.name = "Heave Height Max (20m)";
            heaveHeightMax20mData.dbColumn = "heave_height_max_20m";
            heaveHeightMax20mData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser, adminSettingsVM);
            heaveHeightMax20mData.AddProcessing(CalculationType.RoundingDecimals, 1);
            heaveHeightMax20mData.AddProcessing(CalculationType.TimeMaxHeight, Constants.Minutes20);

            heaveHeightMax3hData.id = (int)ValueType.HeaveHeightMax3h;
            heaveHeightMax3hData.name = "Heave Height Max (3h)";
            heaveHeightMax3hData.dbColumn = "heave_height_max_3h";
            heaveHeightMax3hData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser, adminSettingsVM);
            heaveHeightMax3hData.AddProcessing(CalculationType.RoundingDecimals, 1);
            heaveHeightMax3hData.AddProcessing(CalculationType.TimeMaxHeight, Constants.Hours3);

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
            significantHeaveRateMax20mData.AddProcessing(CalculationType.TimeMax, Constants.Minutes20);

            significantHeaveRateMax3hData.id = (int)ValueType.SignificantHeaveRateMax3h; // Brukes til å justere akse på graf
            significantHeaveRateMax3hData.name = "Significant Heave Rate Max (3h)";
            significantHeaveRateMax3hData.dbColumn = "shr_max_3h";
            significantHeaveRateMax3hData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser, adminSettingsVM);
            significantHeaveRateMax3hData.AddProcessing(CalculationType.RoundingDecimals, 1);
            significantHeaveRateMax3hData.AddProcessing(CalculationType.TimeMax, Constants.Hours3);

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

            // Status
            statusRoll.id = (int)ValueType.StatusRoll;
            statusRoll.name = "Status Roll";
            statusRoll.sensorGroupId = Constants.NO_SENSOR_GROUP_ID;
            statusRoll.dbColumn = "status_roll";

            statusPitch.id = (int)ValueType.StatusPitch;
            statusPitch.name = "Status Pitch";
            statusPitch.sensorGroupId = Constants.NO_SENSOR_GROUP_ID;
            statusPitch.dbColumn = "status_pitch";

            statusInclination.id = (int)ValueType.StatusInclination;
            statusInclination.name = "Status Inclination";
            statusInclination.sensorGroupId = Constants.NO_SENSOR_GROUP_ID;
            statusInclination.dbColumn = "status_inclination";

            statusSHR.id = (int)ValueType.StatusSHR;
            statusSHR.name = "Status SHR";
            statusSHR.sensorGroupId = Constants.NO_SENSOR_GROUP_ID;
            statusSHR.dbColumn = "status_shr";

            statusMRU.id = (int)ValueType.StatusMRU;
            statusMRU.name = "Status MRU";
            statusMRU.sensorGroupId = Constants.NO_SENSOR_GROUP_ID;
            statusMRU.dbColumn = "status_mru";
        }

        public void Update(HMSDataCollection hmsInputDataList/*, ErrorHandler errorHandler*/)
        {
            // Hente input data vi skal bruke
            sensorPitch.Set(hmsInputDataList.GetData(ValueType.Pitch));
            sensorRoll.Set(hmsInputDataList.GetData(ValueType.Roll));
            sensorHeave.Set(hmsInputDataList.GetData(ValueType.Heave));
            sensorHeaveRate.Set(hmsInputDataList.GetData(ValueType.HeaveRate));
            sensorAccelerationX.Set(hmsInputDataList.GetData(ValueType.AccelerationX));
            sensorAccelerationY.Set(hmsInputDataList.GetData(ValueType.AccelerationY));
            sensorAccelerationZ.Set(hmsInputDataList.GetData(ValueType.AccelerationZ));
            sensorSensorMRU.Set(hmsInputDataList.GetData(ValueType.SensorMRUStatus));

            if (sensorPitch.TimeStampCheck ||
                sensorRoll.TimeStampCheck ||
                sensorHeave.TimeStampCheck ||
                sensorHeaveRate.TimeStampCheck ||
                sensorAccelerationX.TimeStampCheck ||
                sensorAccelerationY.TimeStampCheck ||
                sensorAccelerationZ.TimeStampCheck ||
                sensorSensorMRU.TimeStampCheck ||
                databaseSetupRun)
            {
                databaseSetupRun = false;

                // Status data
                if (adminSettingsVM.statusMRUEnabled)
                {
                    if (sensorSensorMRU?.timestamp.AddMilliseconds(adminSettingsVM.dataTimeout) < DateTime.UtcNow)
                        sensorSensorMRU.status = DataStatus.TIMEOUT_ERROR;

                    if (sensorSensorMRU?.status == DataStatus.OK)
                    {
                        statusMRU.data = sensorSensorMRU.data;
                        statusMRU.timestamp = sensorSensorMRU.timestamp;
                        statusMRU.status = sensorSensorMRU.status;
                    }
                    else
                    {
                        statusMRU.data = 0;
                        statusMRU.timestamp = DateTime.UtcNow;
                        statusMRU.status = DataStatus.TIMEOUT_ERROR;
                    }
                }
                else
                {
                    statusMRU.data = 1;
                    statusMRU.timestamp = DateTime.UtcNow;
                    statusMRU.status = DataStatus.OK;
                }

                // Sjekke status
                if (adminSettingsVM.statusMRUEnabled && statusMRU.data != 1)
                {
                    sensorPitch.status = DataStatus.TIMEOUT_ERROR;
                    sensorRoll.status = DataStatus.TIMEOUT_ERROR;
                    sensorHeave.status = DataStatus.TIMEOUT_ERROR;
                    sensorHeaveRate.status = DataStatus.TIMEOUT_ERROR;
                    sensorAccelerationX.status = DataStatus.TIMEOUT_ERROR;
                    sensorAccelerationY.status = DataStatus.TIMEOUT_ERROR;
                    sensorAccelerationZ.status = DataStatus.TIMEOUT_ERROR;
                }
                else
                {
                    // Sjekke data timeout
                    if (sensorPitch.timestamp.AddMilliseconds(adminSettingsVM.dataTimeout) < DateTime.UtcNow)
                        sensorPitch.status = DataStatus.TIMEOUT_ERROR;

                    if (sensorRoll.timestamp.AddMilliseconds(adminSettingsVM.dataTimeout) < DateTime.UtcNow)
                        sensorRoll.status = DataStatus.TIMEOUT_ERROR;

                    if (sensorHeave.timestamp.AddMilliseconds(adminSettingsVM.dataTimeout) < DateTime.UtcNow)
                        sensorHeave.status = DataStatus.TIMEOUT_ERROR;

                    if (sensorHeaveRate.timestamp.AddMilliseconds(adminSettingsVM.dataTimeout) < DateTime.UtcNow)
                        sensorHeaveRate.status = DataStatus.TIMEOUT_ERROR;

                    if (sensorAccelerationX.timestamp.AddMilliseconds(adminSettingsVM.dataTimeout) < DateTime.UtcNow)
                        sensorAccelerationX.status = DataStatus.TIMEOUT_ERROR;

                    if (sensorAccelerationY.timestamp.AddMilliseconds(adminSettingsVM.dataTimeout) < DateTime.UtcNow)
                        sensorAccelerationY.status = DataStatus.TIMEOUT_ERROR;

                    if (sensorAccelerationZ.timestamp.AddMilliseconds(adminSettingsVM.dataTimeout) < DateTime.UtcNow)
                        sensorAccelerationZ.status = DataStatus.TIMEOUT_ERROR;
                }

                // Tar data fra input delen av server og overfører til HMS output delen
                // og prosesserer input for overføring til HMS output også.

                // Pitch
                pitchData.Set(sensorPitch);

                pitchMax20mData.DoProcessing(pitchData);
                pitchMax3hData.DoProcessing(pitchData);
                pitchMaxUp20mData.DoProcessing(pitchData);
                pitchMaxDown20mData.DoProcessing(pitchData);

                if (adminSettingsVM.regulationStandard == RegulationStandard.CAP && !adminSettingsVM.overrideMotionBuffer)
                {
                    pitchMax20mData.BufferFillCheck(1, Constants.MotionBufferFill99Pct);
                    pitchMaxUp20mData.BufferFillCheck(1, Constants.MotionBufferFill99Pct);
                    pitchMaxDown20mData.BufferFillCheck(1, Constants.MotionBufferFill99Pct);
                }

                // Debug Output
                if (AdminMode.IsActive &&
                    adminSettingsVM.outputBufferSize)
                {
                    if (DateTime.Now > testTimer)
                    {
                        testTimer = DateTime.Now.AddMilliseconds(5000);
                        errorHandler.Insert(
                            new ErrorMessage(
                                DateTime.UtcNow,
                                ErrorMessageType.Debug,
                                ErrorMessageCategory.None,
                                string.Format("Motion Buffer: {0} (1188)", pitchMax20mData.BufferSize(1))));
                    }
                }

                // Roll
                rollData.Set(sensorRoll);

                // I data fra sensor er positive tall roll til høyre.
                // Internt er positive tall roll til venstre. Venstre er høyest på grafen. Dette er standard i CAP.
                rollData.data *= -1;

                rollMax20mData.DoProcessing(rollData);
                rollMax3hData.DoProcessing(rollData);
                rollMaxLeft20mData.DoProcessing(rollData);
                rollMaxRight20mData.DoProcessing(rollData);

                if (adminSettingsVM.regulationStandard == RegulationStandard.CAP && !adminSettingsVM.overrideMotionBuffer)
                {
                    rollMax20mData.BufferFillCheck(1, Constants.MotionBufferFill99Pct);
                    rollMaxLeft20mData.BufferFillCheck(1, Constants.MotionBufferFill99Pct);
                    rollMaxRight20mData.BufferFillCheck(1, Constants.MotionBufferFill99Pct);
                }

                // Inclination
                if ((pitchData.status == DataStatus.OK ||
                     pitchData.status == DataStatus.OK_NA) &&
                    (rollData.status == DataStatus.OK ||
                     rollData.status == DataStatus.OK_NA))
                {
                    if ((rollMax20mData.status == DataStatus.OK ||
                         rollMax20mData.status == DataStatus.OK_NA) &&
                        (pitchMax20mData.status == DataStatus.OK ||
                         pitchMax20mData.status == DataStatus.OK_NA))
                    {
                        UpdateInclinationData(pitchData, rollData, inclination20mMaxData, Constants.Minutes20);
                    }

                    if ((rollMax3hData.status == DataStatus.OK ||
                         rollMax3hData.status == DataStatus.OK_NA) &&
                        (pitchMax3hData.status == DataStatus.OK ||
                         pitchMax3hData.status == DataStatus.OK_NA))
                    {
                        UpdateInclinationData(pitchData, rollData, inclination3hMaxData, Constants.Hours3);
                    }

                    // Status settes i UpdateInclinationData, og denne er grei for NOROG, men for CAP
                    // må status også reflektere motion buffer fyllingsgrad.
                    if (adminSettingsVM.regulationStandard == RegulationStandard.CAP)
                    {
                        // 20 mins
                        if (pitchMax20mData.status == DataStatus.OK && rollMax20mData.status == DataStatus.OK)
                        {
                            inclination20mMaxData.status = DataStatus.OK;
                        }
                        else
                        if ((pitchMax20mData.status == DataStatus.OK && rollMax20mData.status == DataStatus.OK_NA) ||
                            (pitchMax20mData.status == DataStatus.OK_NA && rollMax20mData.status == DataStatus.OK) ||
                            (pitchMax20mData.status == DataStatus.OK_NA && rollMax20mData.status == DataStatus.OK_NA))
                        {
                            inclination20mMaxData.status = DataStatus.OK_NA;
                        }
                        else
                        {
                            inclination20mMaxData.status = DataStatus.TIMEOUT_ERROR;
                        }

                        // 3 hours
                        if (pitchMax3hData.status == DataStatus.OK && rollMax3hData.status == DataStatus.OK)
                        {
                            inclination3hMaxData.status = DataStatus.OK;
                        }
                        else
                        if ((pitchMax3hData.status == DataStatus.OK && rollMax3hData.status == DataStatus.OK_NA) ||
                            (pitchMax3hData.status == DataStatus.OK_NA && rollMax3hData.status == DataStatus.OK) ||
                            (pitchMax3hData.status == DataStatus.OK_NA && rollMax3hData.status == DataStatus.OK_NA))
                        {
                            inclination3hMaxData.status = DataStatus.OK_NA;
                        }
                        else
                        {
                            inclination3hMaxData.status = DataStatus.TIMEOUT_ERROR;
                        }
                    }
                }

                //// TEST
                //if (inclination20mMaxData.status == DataStatus.OK)
                //    inclination3hMaxData.status = DataStatus.OK;


                // Heave Height
                heaveHeightData.DoProcessing(sensorHeave);
                heaveHeightMax20mData.DoProcessing(sensorHeave);
                heaveHeightMax3hData.DoProcessing(sensorHeave);

                // Heave Period
                heavePeriodMeanData.DoProcessing(sensorHeave);

                // Significant Heave Rate
                significantHeaveRateData.DoProcessing(sensorHeaveRate);
                significantHeaveRateMax20mData.DoProcessing(significantHeaveRateData);
                significantHeaveRateMax3hData.DoProcessing(significantHeaveRateData);

                if (adminSettingsVM.regulationStandard == RegulationStandard.CAP && !adminSettingsVM.overrideMotionBuffer)
                {
                    significantHeaveRateData.BufferFillCheck(0, Constants.MotionBufferFill99Pct);
                    significantHeaveRateMax20mData.BufferFillCheck(1, Constants.MotionBufferFill99Pct);
                    //significantHeaveRateMax3hData.BufferFillCheck(Constants.MotionBufferFill99Pct);
                }

                // Maximum Heave Rate
                maxHeaveRateData.DoProcessing(sensorHeaveRate);

                if (adminSettingsVM.regulationStandard == RegulationStandard.CAP && !adminSettingsVM.overrideMotionBuffer)
                    maxHeaveRateData.BufferFillCheck(1, Constants.MotionBufferFill99Pct);

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

                    HMSData accelerationX = new HMSData(sensorAccelerationX);
                    HMSData accelerationY = new HMSData(sensorAccelerationY);
                    HMSData accelerationZ = new HMSData(sensorAccelerationZ);

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

                        // Find max value
                        CalculateMSIMax(mms_msi, mms_msi_list, msiData);

                        // Sjekke buffer fyllingsgrad
                        if (!adminSettingsVM.overrideMotionBuffer)
                            MSIBufferFillCheck(mms_msi_list, Constants.MotionBufferFill99Pct, msiData);

                    }
                    else
                    {
                        msiData.data = 0;
                        msiData.status = DataStatus.TIMEOUT_ERROR;
                        msiData.timestamp = accelerationX.timestamp;
                    }
                    //// TEST
                    //if (msiData.status == DataStatus.OK)
                    //{
                    //    msiData.data3 = String.Empty;
                    //}
                }

                // Sjekker motion limits
                CheckLimits();
            }
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
            inclination20mMaxData.data = 0;
            inclination3hMaxData.data = 0;

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

        private void CalculateMSIMax(HMSData value, List<TimeData> dataList, HMSData maxValue)
        {
            // Først sjekke om vi har en ny verdi
            if (value.timestamp != dataList.LastOrDefault()?.timestamp)
            {
                // Sjekke status
                if (value.status == DataStatus.OK ||
                    value.status == DataStatus.OK_NA)
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
                }

                // Timestamp
                maxValue.timestamp = value.timestamp;

                // Status
                maxValue.status = value.status;
            }

            // Sjekke om vi skal ta ut gamle verdier
            bool findNewMaxValue = false;

            for (int i = 0; i < dataList.Count && dataList.Count > 0; i++)
            {
                // Time stamp eldre enn satt grense?
                if (dataList[i]?.timestamp.AddSeconds(Constants.Minutes20) < DateTime.UtcNow)
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
                maxValue.timestamp = value.timestamp;
                maxValue.status = value.status;
                
                bool foundNewMax = false;

                for (int i = 0; i < dataList.Count && !foundNewMax; i++)
                {
                    // Kan avslutte søket dersom vi finner en verdi lik den gamle max verdien (ingen er høyere)
                    if (dataList[i]?.data == oldMaxValue)
                    {
                        maxValue.data = Math.Round(dataList[i].data * adminSettingsVM.msiCorrectionR, 1, MidpointRounding.AwayFromZero);

                        foundNewMax = true;
                    }
                    else
                    {
                        if (dataList[i]?.data > maxValue.data)
                        {
                            maxValue.data = Math.Round(dataList[i].data * adminSettingsVM.msiCorrectionR, 1, MidpointRounding.AwayFromZero);
                        }
                    }
                }
            }
        }

        private void MSIBufferFillCheck(List<TimeData> dataList, double targetCount, HMSData data)
        {
            // Sjekk status først, dersom OK sjekk count
            if (data.status == DataStatus.OK)
                // Er bufferet fyllt opp
                if (dataList.Count < targetCount)
                    // Set OK, men ikke tilgjengelig
                    data.status = DataStatus.OK_NA;
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

            // Lagre status til database
            if (rollMax20mData.limitStatus == LimitStatus.OVER_LIMIT)
                statusRoll.data = 1;
            else
                statusRoll.data = 0;
            statusRoll.timestamp = rollMax20mData.timestamp;
            statusRoll.status = rollMax20mData.status;

            if (pitchMax20mData.limitStatus == LimitStatus.OVER_LIMIT)
                statusPitch.data = 1;
            else
                statusPitch.data = 0;
            statusPitch.timestamp = pitchMax20mData.timestamp;
            statusPitch.status = pitchMax20mData.status;

            if (inclination20mMaxData.limitStatus == LimitStatus.OVER_LIMIT)
                statusInclination.data = 1;
            else
                statusInclination.data = 0;
            statusInclination.timestamp = inclination20mMaxData.timestamp;
            statusInclination.status = inclination20mMaxData.status;

            if (significantHeaveRateData.limitStatus == LimitStatus.OVER_LIMIT)
                statusSHR.data = 1;
            else
                statusSHR.data = 0;
            statusSHR.timestamp = significantHeaveRateData.timestamp;
            statusSHR.status = significantHeaveRateData.status;
        }

        private void CheckLimit(HMSData hmsData, LimitType limitType)
        {
            if (Math.Abs(hmsData.data) <= motionLimits.GetLimit(limitType))
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
        private void CalcInclination(HMSData pitch, HMSData roll, HMSData outputInclination)
        {
            if (pitch != null &&
                roll != null)
            {
                // Beregne inclination
                double inclination = HMSCalc.Inclination(pitch.data, roll.data);

                // Output
                outputInclination.data = Math.Round(inclination, 1, MidpointRounding.AwayFromZero);

                // Timestamp
                if (pitch.timestamp < roll.timestamp)
                    outputInclination.timestamp = pitch.timestamp;
                else
                    outputInclination.timestamp = roll.timestamp;

                // Status
                if (pitch.status == DataStatus.OK && roll.status == DataStatus.OK)
                    outputInclination.status = DataStatus.OK;
                else
                if ((pitch.status == DataStatus.OK && roll.status == DataStatus.OK_NA) ||
                    (pitch.status == DataStatus.OK_NA && roll.status == DataStatus.OK) ||
                    (pitch.status == DataStatus.OK_NA && roll.status == DataStatus.OK_NA))
                    outputInclination.status = DataStatus.OK_NA;
                else
                    outputInclination.status = DataStatus.TIMEOUT_ERROR;
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

            // Avrunding
            inclinationData.data = Math.Round(inclinationData.data, 1, MidpointRounding.AwayFromZero);

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
