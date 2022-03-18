using System;
using System.Collections.Generic;
using Telerik.Windows.Data;

namespace HMS_Server
{
    public class HMSProcessingMotion
    {
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

        // Heave Amplitude
        private HMSData heaveAmplitudeData = new HMSData();
        private HMSData heaveAmplitudeMax20mData = new HMSData();
        private HMSData heaveAmplitudeMax3hData = new HMSData();
        private HMSData heavePeriodMeanData = new HMSData();

        // Significant Heave Rate
        private HMSData significantHeaveRateData = new HMSData();
        private HMSData significantHeaveRateMax20mData = new HMSData();
        private HMSData significantHeaveRateMax3hData = new HMSData();

        // Max Heave Rate
        private HMSData maxHeaveRateData = new HMSData();

        // Significant Wave Height
        private HMSData significantWaveHeightData = new HMSData();

        // Limits
        private HMSData motionLimitPitchRoll = new HMSData();
        private HMSData motionLimitInclination = new HMSData();
        private HMSData motionLimitHeaveAmplitude = new HMSData();
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

            hmsOutputDataList.Add(significantWaveHeightData);

            hmsOutputDataList.Add(heaveAmplitudeData);
            hmsOutputDataList.Add(heaveAmplitudeMax20mData);
            hmsOutputDataList.Add(heaveAmplitudeMax3hData);
            hmsOutputDataList.Add(heavePeriodMeanData);

            hmsOutputDataList.Add(motionLimitPitchRoll);
            hmsOutputDataList.Add(motionLimitInclination);
            hmsOutputDataList.Add(motionLimitHeaveAmplitude);
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
            pitchMax20mData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser);
            pitchMax20mData.AddProcessing(CalculationType.TimeMaxAbsolute, Constants.Minutes20);
            pitchMax20mData.AddProcessing(CalculationType.RoundingDecimals, 1);

            pitchMax3hData.id = (int)ValueType.PitchMax3h;
            pitchMax3hData.name = "Pitch Max (3h)";
            pitchMax3hData.dbColumn = "pitch_max_3h";
            pitchMax3hData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser);
            pitchMax3hData.AddProcessing(CalculationType.TimeMaxAbsolute, Constants.Hours3);
            pitchMax3hData.AddProcessing(CalculationType.RoundingDecimals, 1);

            pitchMaxUp20mData.id = (int)ValueType.PitchMaxUp20m;
            pitchMaxUp20mData.name = "Pitch Max Up (20m)";
            pitchMaxUp20mData.dbColumn = "pitch_max_up_20m";
            pitchMaxUp20mData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser);
            pitchMaxUp20mData.AddProcessing(CalculationType.TimeMaxPositive, Constants.Minutes20);
            pitchMaxUp20mData.AddProcessing(CalculationType.RoundingDecimals, 1);
            pitchMaxUp20mData.AddProcessing(CalculationType.Absolute, 0);

            pitchMaxDown20mData.id = (int)ValueType.PitchMaxDown20m;
            pitchMaxDown20mData.name = "Pitch Max Down (20m)";
            pitchMaxDown20mData.dbColumn = "pitch_max_down_20m";
            pitchMaxDown20mData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser);
            pitchMaxDown20mData.AddProcessing(CalculationType.TimeMaxNegative, Constants.Minutes20);
            pitchMaxDown20mData.AddProcessing(CalculationType.RoundingDecimals, 1);
            pitchMaxDown20mData.AddProcessing(CalculationType.Absolute, 0);

            rollMax20mData.id = (int)ValueType.RollMax20m;
            rollMax20mData.name = "Roll Max (20m)";
            rollMax20mData.dbColumn = "roll_max_20m";
            rollMax20mData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser);
            rollMax20mData.AddProcessing(CalculationType.TimeMaxAbsolute, Constants.Minutes20);
            rollMax20mData.AddProcessing(CalculationType.RoundingDecimals, 1);

            rollMax3hData.id = (int)ValueType.RollMax3h;
            rollMax3hData.name = "Roll Max (3h)";
            rollMax3hData.dbColumn = "roll_max_3h";
            rollMax3hData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser);
            rollMax3hData.AddProcessing(CalculationType.TimeMaxAbsolute, Constants.Hours3);
            rollMax3hData.AddProcessing(CalculationType.RoundingDecimals, 1);

            rollMaxLeft20mData.id = (int)ValueType.RollMaxLeft20m;
            rollMaxLeft20mData.name = "Roll Max Left (20m)";
            rollMaxLeft20mData.dbColumn = "roll_max_left_20m";
            rollMaxLeft20mData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser);
            rollMaxLeft20mData.AddProcessing(CalculationType.TimeMaxNegative, Constants.Minutes20);
            rollMaxLeft20mData.AddProcessing(CalculationType.RoundingDecimals, 1);
            rollMaxLeft20mData.AddProcessing(CalculationType.Absolute, 0);

            rollMaxRight20mData.id = (int)ValueType.RollMaxRight20m;
            rollMaxRight20mData.name = "Roll Max Right (20m)";
            rollMaxRight20mData.dbColumn = "roll_max_right_20m";
            rollMaxRight20mData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser);
            rollMaxRight20mData.AddProcessing(CalculationType.TimeMaxPositive, Constants.Minutes20);
            rollMaxRight20mData.AddProcessing(CalculationType.RoundingDecimals, 1);
            rollMaxRight20mData.AddProcessing(CalculationType.Absolute, 0);

            inclinationData.id = (int)ValueType.Inclination;
            inclinationData.name = "Inclination";
            inclinationData.dbColumn = "inclination";

            inclination20mMaxData.id = (int)ValueType.InclinationMax20m;
            inclination20mMaxData.name = "Inclination Max (20m)";
            inclination20mMaxData.dbColumn = "inclination_max_20m";

            inclination3hMaxData.id = (int)ValueType.InclinationMax3h;
            inclination3hMaxData.name = "Inclination Max (3h)";
            inclination3hMaxData.dbColumn = "inclination_max_3h";

            heaveAmplitudeData.id = (int)ValueType.HeaveAmplitude;
            heaveAmplitudeData.name = "Heave Amplitude";
            heaveAmplitudeData.dbColumn = "heave_amplitude";
            heaveAmplitudeData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser);
            heaveAmplitudeData.AddProcessing(CalculationType.Amplitude, 0);
            heaveAmplitudeData.AddProcessing(CalculationType.RoundingDecimals, 1);

            heaveAmplitudeMax20mData.id = (int)ValueType.HeaveAmplitudeMax20m;
            heaveAmplitudeMax20mData.name = "Heave Amplitude Max (20m)";
            heaveAmplitudeMax20mData.dbColumn = "heave_amplitude_max_20m";
            heaveAmplitudeMax20mData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser);
            heaveAmplitudeMax20mData.AddProcessing(CalculationType.TimeMaxAmplitude, Constants.Minutes20);
            heaveAmplitudeMax20mData.AddProcessing(CalculationType.RoundingDecimals, 1);

            heaveAmplitudeMax3hData.id = (int)ValueType.HeaveAmplitudeMax3h;
            heaveAmplitudeMax3hData.name = "Heave Amplitude Max (3h)";
            heaveAmplitudeMax3hData.dbColumn = "heave_amplitude_max_3h";
            heaveAmplitudeMax3hData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser);
            heaveAmplitudeMax3hData.AddProcessing(CalculationType.TimeMaxAmplitude, Constants.Hours3);
            heaveAmplitudeMax3hData.AddProcessing(CalculationType.RoundingDecimals, 1);

            heavePeriodMeanData.id = (int)ValueType.HeavePeriodMean;
            heavePeriodMeanData.name = "Heave Period";
            heavePeriodMeanData.dbColumn = "heave_period";
            heavePeriodMeanData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser);
            heavePeriodMeanData.AddProcessing(CalculationType.TimeMeanPeriod, Constants.Minutes20);
            heavePeriodMeanData.AddProcessing(CalculationType.RoundingDecimals, 1);

            significantHeaveRateData.id = (int)ValueType.SignificantHeaveRate;
            significantHeaveRateData.name = "Significant Heave Rate";
            significantHeaveRateData.dbColumn = "significant_heave_rate";
            significantHeaveRateData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser);
            significantHeaveRateData.AddProcessing(CalculationType.SignificantHeaveRate, Constants.Minutes20);
            significantHeaveRateData.AddProcessing(CalculationType.RoundingDecimals, 1);

            significantHeaveRateMax20mData.id = (int)ValueType.SignificantHeaveRateMax20m; // Brukes til å justere akse på graf
            significantHeaveRateMax20mData.name = "Significant Heave Rate Max (20m)";
            significantHeaveRateMax20mData.dbColumn = "shr_max_20m";
            significantHeaveRateMax20mData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser);
            significantHeaveRateMax20mData.AddProcessing(CalculationType.TimeMaxPositive, Constants.Minutes20);
            significantHeaveRateMax20mData.AddProcessing(CalculationType.RoundingDecimals, 1);

            significantHeaveRateMax3hData.id = (int)ValueType.SignificantHeaveRateMax3h; // Brukes til å justere akse på graf
            significantHeaveRateMax3hData.name = "Significant Heave Rate Max (3h)";
            significantHeaveRateMax3hData.dbColumn = "shr_max_3h";
            significantHeaveRateMax3hData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser);
            significantHeaveRateMax3hData.AddProcessing(CalculationType.TimeMaxPositive, Constants.Hours3);
            significantHeaveRateMax3hData.AddProcessing(CalculationType.RoundingDecimals, 1);

            maxHeaveRateData.id = (int)ValueType.MaxHeaveRate;
            maxHeaveRateData.name = "Max Heave Rate";
            maxHeaveRateData.dbColumn = "max_heave_rate";
            maxHeaveRateData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser);
            maxHeaveRateData.AddProcessing(CalculationType.TimeMaxAbsolute, Constants.Minutes20);
            maxHeaveRateData.AddProcessing(CalculationType.RoundingDecimals, 1);

            significantWaveHeightData.id = (int)ValueType.SignificantWaveHeight;
            significantWaveHeightData.name = "Significant Wave Height";
            significantWaveHeightData.dbColumn = "significant_wave_height";
            significantWaveHeightData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser);
            significantWaveHeightData.AddProcessing(CalculationType.SignificantWaveHeight, Constants.Minutes20);
            significantWaveHeightData.AddProcessing(CalculationType.RoundingDecimals, 1);

            motionLimitPitchRoll.id = (int)ValueType.MotionLimitPitchRoll;
            motionLimitPitchRoll.name = "Motion Limit Pitch and Roll";
            motionLimitPitchRoll.sensorGroupId = Constants.NO_SENSOR_GROUP_ID;
            motionLimitPitchRoll.dbColumn = "motion_limit_pitch_roll";

            motionLimitInclination.id = (int)ValueType.MotionLimitInclination;
            motionLimitInclination.name = "Motion Limit Inclination";
            motionLimitInclination.sensorGroupId = Constants.NO_SENSOR_GROUP_ID;
            motionLimitInclination.dbColumn = "motion_limit_inclination";

            motionLimitHeaveAmplitude.id = (int)ValueType.MotionLimitHeaveAmplitude;
            motionLimitHeaveAmplitude.name = "Motion Limit Heave Amplitude";
            motionLimitHeaveAmplitude.sensorGroupId = Constants.NO_SENSOR_GROUP_ID;
            motionLimitHeaveAmplitude.dbColumn = "motion_limit_heave_amplitude";

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
            // Tar data fra input delen av server og overfører til HMS output delen
            // og prosesserer input for overføring til HMS output også.

            // Pitch
            pitchData.Set(hmsInputDataList.GetData(ValueType.Pitch));
            pitchMax20mData.DoProcessing(pitchData);
            pitchMax3hData.DoProcessing(pitchData);
            pitchMaxUp20mData.DoProcessing(pitchData);
            pitchMaxDown20mData.DoProcessing(pitchData);

            if (adminSettingsVM.regulationStandard == RegulationStandard.CAP)
            {
                pitchMax20mData.BufferFillCheck(Constants.MotionBufferFill99Pct);
                //pitchMax3hData.BufferFillCheck(Constants.MotionBufferFill99Pct);
                pitchMaxUp20mData.BufferFillCheck(Constants.MotionBufferFill99Pct);
                pitchMaxDown20mData.BufferFillCheck(Constants.MotionBufferFill99Pct);
            }

            // Roll
            rollData.Set(hmsInputDataList.GetData(ValueType.Roll));
            rollMax20mData.DoProcessing(rollData);
            rollMax3hData.DoProcessing(rollData);
            rollMaxLeft20mData.DoProcessing(rollData);
            rollMaxRight20mData.DoProcessing(rollData);

            if (adminSettingsVM.regulationStandard == RegulationStandard.CAP)
            {
                rollMax20mData.BufferFillCheck(Constants.MotionBufferFill99Pct);
                //rollMax3hData.BufferFillCheck(Constants.MotionBufferFill99Pct);
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

            // Heave Amplitude
            heaveAmplitudeData.DoProcessing(hmsInputDataList.GetData(ValueType.Heave));
            heaveAmplitudeMax20mData.DoProcessing(hmsInputDataList.GetData(ValueType.Heave));
            heaveAmplitudeMax3hData.DoProcessing(hmsInputDataList.GetData(ValueType.Heave));

            // Heave Period
            heavePeriodMeanData.DoProcessing(hmsInputDataList.GetData(ValueType.Heave));

            // Significant Heave Rate
            significantHeaveRateData.DoProcessing(hmsInputDataList.GetData(ValueType.HeaveRate));
            significantHeaveRateMax20mData.DoProcessing(significantHeaveRateData);
            significantHeaveRateMax3hData.DoProcessing(significantHeaveRateData);

            if (adminSettingsVM.regulationStandard == RegulationStandard.CAP)
            {
                significantHeaveRateData.BufferFillCheck(Constants.MotionBufferFill99Pct);
                significantHeaveRateMax20mData.BufferFillCheck(Constants.MotionBufferFill99Pct);
                //significantHeaveRateMax3hData.BufferFillCheck(Constants.MotionBufferFill99Pct);
            }

            // Maximum Heave Rate
            maxHeaveRateData.DoProcessing(hmsInputDataList.GetData(ValueType.HeaveRate));

            if (adminSettingsVM.regulationStandard == RegulationStandard.CAP)
                maxHeaveRateData.BufferFillCheck(Constants.MotionBufferFill99Pct);

            // Significant Wave Height
            significantWaveHeightData.DoProcessing(hmsInputDataList.GetData(ValueType.Heave));

            // Motion Limits
            motionLimitPitchRoll.data = motionLimits.GetLimit(LimitType.PitchRoll);
            motionLimitPitchRoll.timestamp = DateTime.UtcNow;
            motionLimitPitchRoll.status = DataStatus.OK;

            motionLimitInclination.data = motionLimits.GetLimit(LimitType.Inclination);
            motionLimitInclination.timestamp = DateTime.UtcNow;
            motionLimitInclination.status = DataStatus.OK;

            motionLimitHeaveAmplitude.data = motionLimits.GetLimit(LimitType.HeaveAmplitude);
            motionLimitHeaveAmplitude.timestamp = DateTime.UtcNow;
            motionLimitHeaveAmplitude.status = DataStatus.OK;

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

                HMSData accelerationX = new HMSData(hmsInputDataList.GetData(ValueType.AccelerationX));
                HMSData accelerationY = new HMSData(hmsInputDataList.GetData(ValueType.AccelerationY));
                HMSData accelerationZ = new HMSData(hmsInputDataList.GetData(ValueType.AccelerationZ));

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

                // Find max value
                CalculateMSIMax(mms_msi, mms_msi_list, msiData, Constants.Minutes20);
            }

            // Sjekker motion limits
            CheckLimits();
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
            heaveAmplitudeData.ResetDataCalculations();
            heaveAmplitudeMax20mData.ResetDataCalculations();
            heaveAmplitudeMax3hData.ResetDataCalculations();
            heavePeriodMeanData.ResetDataCalculations();
            significantHeaveRateData.ResetDataCalculations();
            significantHeaveRateMax20mData.ResetDataCalculations();
            significantHeaveRateMax3hData.ResetDataCalculations();
            maxHeaveRateData.ResetDataCalculations();
            significantWaveHeightData.ResetDataCalculations();

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
            if (value.status == DataStatus.OK)
            {
                // Korreksjon R
                double valueCorr = value.data * adminSettingsVM.msiCorrectionR;

                // Legge inn den nye verdien i data settet
                dataList.Add(new TimeData() { data = valueCorr, timestamp = value.timestamp });

                // Større max verdi?
                if (valueCorr > maxValue.data)
                {
                    maxValue.data = Math.Round(valueCorr, 1, MidpointRounding.AwayFromZero);
                }

                // Timestamp og status
                maxValue.timestamp = value.timestamp;
                maxValue.status = value.status;
            }
            else
            {
                maxValue.status = value.status;
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
                    // Kan avslutte søket dersom vi finne en verdi like den gamle max verdien (ingen er høyere)
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
            CheckLimit(heaveAmplitudeMax20mData, LimitType.HeaveAmplitude);

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
