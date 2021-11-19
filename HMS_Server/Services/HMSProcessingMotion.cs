using System;
using System.Collections.Generic;

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
        private HMSData inclinationMax20mData = new HMSData();
        private HMSData inclinationMax3hData = new HMSData();

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

        // MSI / WSI
        private HMSData msiData = new HMSData();
        private HMSData wsiData = new HMSData();

        // Liste for å finne max MSI
        private List<TimeData> mms_msi_list = new List<TimeData>();

        // Liste for å finne gjennomsnitt WSI
        private List<TimeData> wsi_list = new List<TimeData>();
        // Total sum WSI
        private HMSData wsiTotal20m = new HMSData();

        // Motion Limits
        private HelideckMotionLimits motionLimits;
        private bool SHRIsWithinLimits = false;

        private AdminSettingsVM adminSettingsVM;

        private UserInputs userInputs;

        // Significant Heave Rate - Limit conditions
        private double significantHeaveRate2mMin = double.MaxValue;
        private double significantHeaveRate10mSum = 0;
        private double significantHeaveRate10mMean = 0;
        private double significantHeaveRate20mMax = 0;

        private RadObservableCollectionEx<TimeData> significantHeaveRate2mMinData = new RadObservableCollectionEx<TimeData>();
        private RadObservableCollectionEx<TimeData> significantHeaveRate10mMeanData = new RadObservableCollectionEx<TimeData>();
        private RadObservableCollectionEx<TimeData> significantHeaveRate20mMaxData = new RadObservableCollectionEx<TimeData>();

        public HMSProcessingMotion(HMSDataCollection hmsOutputData, HelideckMotionLimits motionLimits, AdminSettingsVM adminSettingsVM, UserInputs userInputs, ErrorHandler errorHandler)
        {
            this.motionLimits = motionLimits;
            this.adminSettingsVM = adminSettingsVM;
            this.userInputs = userInputs;

            // Fyller output listen med HMS Output data
            // NB! Variablene som legges inn i listen her fungerer som pekere: Oppdateres variabelen -> oppdateres listen

            RadObservableCollectionEx<HMSData> hmsOutputDataList = hmsOutputData.GetDataList();

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
            hmsOutputDataList.Add(inclinationMax20mData);
            hmsOutputDataList.Add(inclinationMax3hData);

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

            // Legge på litt informasjon på de variablene som ikke får dette automatisk fra sensor input data
            // (Disse verdiene blir kalkulert her, mens sensor input data bare kopieres videre med denne typen info allerede inkludert)

            pitchMax20mData.id = (int)ValueType.PitchMax20m;
            pitchMax20mData.name = "Pitch Max (20m)";
            pitchMax20mData.dbTableName = "pitch_max_20m";
            pitchMax20mData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser);
            pitchMax20mData.AddProcessing(CalculationType.TimeMaxAbsolute, Constants.Minutes20);
            pitchMax20mData.AddProcessing(CalculationType.RoundingDecimals, 1);

            pitchMax3hData.id = (int)ValueType.PitchMax3h;
            pitchMax3hData.name = "Pitch Max (3h)";
            pitchMax3hData.dbTableName = "pitch_max_3h";
            pitchMax3hData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser);
            pitchMax3hData.AddProcessing(CalculationType.TimeMaxAbsolute, Constants.Hours3);
            pitchMax3hData.AddProcessing(CalculationType.RoundingDecimals, 1);

            pitchMaxUp20mData.id = (int)ValueType.PitchMaxUp20m;
            pitchMaxUp20mData.name = "Pitch Max Up (20m)";
            pitchMaxUp20mData.dbTableName = "pitch_max_up_20m";
            pitchMaxUp20mData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser);
            pitchMaxUp20mData.AddProcessing(CalculationType.TimeMaxPositive, Constants.Minutes20);
            pitchMaxUp20mData.AddProcessing(CalculationType.RoundingDecimals, 1);

            pitchMaxDown20mData.id = (int)ValueType.PitchMaxDown20m;
            pitchMaxDown20mData.name = "Pitch Max Down (20m)";
            pitchMaxDown20mData.dbTableName = "pitch_max_down_20m";
            pitchMaxDown20mData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser);
            pitchMaxDown20mData.AddProcessing(CalculationType.TimeMaxNegative, Constants.Minutes20);
            pitchMaxDown20mData.AddProcessing(CalculationType.RoundingDecimals, 1);

            rollMax20mData.id = (int)ValueType.RollMax20m;
            rollMax20mData.name = "Roll Max (20m)";
            rollMax20mData.dbTableName = "roll_max_20m";
            rollMax20mData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser);
            rollMax20mData.AddProcessing(CalculationType.TimeMaxAbsolute, Constants.Minutes20);
            rollMax20mData.AddProcessing(CalculationType.RoundingDecimals, 1);

            rollMax3hData.id = (int)ValueType.RollMax3h;
            rollMax3hData.name = "Roll Max (3h)";
            rollMax3hData.dbTableName = "roll_max_3h";
            rollMax3hData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser);
            rollMax3hData.AddProcessing(CalculationType.TimeMaxAbsolute, Constants.Hours3);
            rollMax3hData.AddProcessing(CalculationType.RoundingDecimals, 1);

            rollMaxLeft20mData.id = (int)ValueType.RollMaxLeft20m;
            rollMaxLeft20mData.name = "Roll Max Left (20m)";
            rollMaxLeft20mData.dbTableName = "roll_max_left_20m";
            rollMaxLeft20mData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser);
            rollMaxLeft20mData.AddProcessing(CalculationType.TimeMaxPositive, Constants.Minutes20);
            rollMaxLeft20mData.AddProcessing(CalculationType.RoundingDecimals, 1);

            rollMaxRight20mData.id = (int)ValueType.RollMaxRight20m;
            rollMaxRight20mData.name = "Roll Max Right (20m)";
            rollMaxRight20mData.dbTableName = "roll_max_right_20m";
            rollMaxRight20mData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser);
            rollMaxRight20mData.AddProcessing(CalculationType.TimeMaxNegative, Constants.Minutes20);
            rollMaxRight20mData.AddProcessing(CalculationType.RoundingDecimals, 1);

            inclinationData.id = (int)ValueType.Inclination;
            inclinationData.name = "Inclination";
            inclinationData.dbTableName = "inclination";

            inclinationMax20mData.id = (int)ValueType.InclinationMax20m;
            inclinationMax20mData.name = "Inclination Max (20m)";
            inclinationMax20mData.dbTableName = "inclination_max_20m";

            inclinationMax3hData.id = (int)ValueType.InclinationMax3h;
            inclinationMax3hData.name = "Inclination Max (3h)";
            inclinationMax3hData.dbTableName = "inclination_max_3h";

            heaveAmplitudeData.id = (int)ValueType.HeaveAmplitude;
            heaveAmplitudeData.name = "Heave Amplitude";
            heaveAmplitudeData.dbTableName = "heave_amplitude";
            heaveAmplitudeData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser);
            heaveAmplitudeData.AddProcessing(CalculationType.Amplitude, 0);
            heaveAmplitudeData.AddProcessing(CalculationType.RoundingDecimals, 1);

            heaveAmplitudeMax20mData.id = (int)ValueType.HeaveAmplitudeMax20m;
            heaveAmplitudeMax20mData.name = "Heave Amplitude Max (20m)";
            heaveAmplitudeMax20mData.dbTableName = "heave_amplitude_max_20m";
            heaveAmplitudeMax20mData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser);
            heaveAmplitudeMax20mData.AddProcessing(CalculationType.TimeMaxAmplitude, Constants.Minutes20);
            heaveAmplitudeMax20mData.AddProcessing(CalculationType.RoundingDecimals, 1);

            heaveAmplitudeMax3hData.id = (int)ValueType.HeaveAmplitudeMax3h;
            heaveAmplitudeMax3hData.name = "Heave Amplitude Max (3h)";
            heaveAmplitudeMax3hData.dbTableName = "heave_amplitude_max_3h";
            heaveAmplitudeMax3hData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser);
            heaveAmplitudeMax3hData.AddProcessing(CalculationType.TimeMaxAmplitude, Constants.Hours3);
            heaveAmplitudeMax3hData.AddProcessing(CalculationType.RoundingDecimals, 1);

            heavePeriodMeanData.id = (int)ValueType.HeavePeriodMean;
            heavePeriodMeanData.name = "Heave Period";
            heavePeriodMeanData.dbTableName = "heave_period";
            heavePeriodMeanData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser);
            heavePeriodMeanData.AddProcessing(CalculationType.TimeMeanPeriod, Constants.Minutes20);
            heavePeriodMeanData.AddProcessing(CalculationType.RoundingDecimals, 1);

            significantHeaveRateData.id = (int)ValueType.SignificantHeaveRate;
            significantHeaveRateData.name = "Significant Heave Rate";
            significantHeaveRateData.dbTableName = "significant_heave_rate";
            significantHeaveRateData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser);
            significantHeaveRateData.AddProcessing(CalculationType.SignificantHeaveRate, Constants.Minutes20);
            significantHeaveRateData.AddProcessing(CalculationType.RoundingDecimals, 1);

            significantHeaveRateMax20mData.id = (int)ValueType.SignificantHeaveRateMax20m;
            significantHeaveRateMax20mData.name = "Significant Heave Rate Max (20m)";
            significantHeaveRateMax20mData.dbTableName = "shr_max_20m";
            significantHeaveRateMax20mData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser);
            significantHeaveRateMax20mData.AddProcessing(CalculationType.TimeMaxPositive, Constants.Minutes20);
            significantHeaveRateMax20mData.AddProcessing(CalculationType.RoundingDecimals, 1);

            significantHeaveRateMax3hData.id = (int)ValueType.SignificantHeaveRateMax3h;
            significantHeaveRateMax3hData.name = "Significant Heave Rate Max (3h)";
            significantHeaveRateMax3hData.dbTableName = "shr_max_3h";
            significantHeaveRateMax3hData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser);
            significantHeaveRateMax3hData.AddProcessing(CalculationType.TimeMaxPositive, Constants.Hours3);
            significantHeaveRateMax3hData.AddProcessing(CalculationType.RoundingDecimals, 1);

            maxHeaveRateData.id = (int)ValueType.MaxHeaveRate;
            maxHeaveRateData.name = "Max Heave Rate";
            maxHeaveRateData.dbTableName = "max_heave_rate";
            maxHeaveRateData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser);
            maxHeaveRateData.AddProcessing(CalculationType.TimeMaxAbsolute, Constants.Minutes20);
            maxHeaveRateData.AddProcessing(CalculationType.RoundingDecimals, 1);

            significantWaveHeightData.id = (int)ValueType.SignificantWaveHeight;
            significantWaveHeightData.name = "Significant Wave Height";
            significantWaveHeightData.dbTableName = "significant_wave_height";
            significantWaveHeightData.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser);
            significantWaveHeightData.AddProcessing(CalculationType.SignificantWaveHeight, Constants.Minutes20);
            significantWaveHeightData.AddProcessing(CalculationType.RoundingDecimals, 1);

            motionLimitPitchRoll.id = (int)ValueType.MotionLimitPitchRoll;
            motionLimitPitchRoll.name = "Motion Limit Pitch and Roll";
            motionLimitPitchRoll.sensorGroupId = Constants.NO_SENSOR_GROUP_ID;
            motionLimitPitchRoll.dbTableName = "motion_limit_pitch_roll";

            motionLimitInclination.id = (int)ValueType.MotionLimitInclination;
            motionLimitInclination.name = "Motion Limit Inclination";
            motionLimitInclination.sensorGroupId = Constants.NO_SENSOR_GROUP_ID;
            motionLimitInclination.dbTableName = "motion_limit_inclination";

            motionLimitHeaveAmplitude.id = (int)ValueType.MotionLimitHeaveAmplitude;
            motionLimitHeaveAmplitude.name = "Motion Limit Heave Amplitude";
            motionLimitHeaveAmplitude.sensorGroupId = Constants.NO_SENSOR_GROUP_ID;
            motionLimitHeaveAmplitude.dbTableName = "motion_limit_heave_amplitude";

            motionLimitSignificantHeaveRate.id = (int)ValueType.MotionLimitSignificantHeaveRate;
            motionLimitSignificantHeaveRate.name = "Motion Limit Significant Heave Rate";
            motionLimitSignificantHeaveRate.sensorGroupId = Constants.NO_SENSOR_GROUP_ID;
            motionLimitSignificantHeaveRate.dbTableName = "motion_limit_shr";

            if (adminSettingsVM.regulationStandard == RegulationStandard.CAP)
            {
                hmsOutputDataList.Add(msiData);
                hmsOutputDataList.Add(wsiData);

                msiData.id = (int)ValueType.MSI;
                msiData.name = "MSI";
                msiData.sensorGroupId = Constants.NO_SENSOR_GROUP_ID;
                msiData.dbTableName = "msi";

                wsiData.id = (int)ValueType.WSI;
                wsiData.name = "WSI";
                wsiData.sensorGroupId = Constants.NO_SENSOR_GROUP_ID;
                wsiData.dbTableName = "wsi";
            }
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

            // Roll
            rollData.Set(hmsInputDataList.GetData(ValueType.Roll));
            rollMax20mData.DoProcessing(rollData);
            rollMax3hData.DoProcessing(rollData);
            rollMaxLeft20mData.DoProcessing(rollData);
            rollMaxRight20mData.DoProcessing(rollData);

            // Inclination
            CalcInclination(pitchData, rollData, inclinationData);
            CalcInclination(pitchMax20mData, rollMax20mData, inclinationMax20mData);
            CalcInclination(pitchMax3hData, rollMax3hData, inclinationMax3hData);

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

            // Maximum Heave Rate
            maxHeaveRateData.DoProcessing(hmsInputDataList.GetData(ValueType.HeaveRate));

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
                // MSI / WSI
                /////////////////////////////////////////////////////////////////////////////////////////
                ///
                HMSData mms_msi = new HMSData();
                HMSData wsit = new HMSData();

                HMSData accelerationX = new HMSData(hmsInputDataList.GetData(ValueType.AccelerationX));
                HMSData accelerationY = new HMSData(hmsInputDataList.GetData(ValueType.AccelerationY));
                HMSData accelerationZ = new HMSData(hmsInputDataList.GetData(ValueType.AccelerationZ));

                if (accelerationX.status == DataStatus.OK &&
                    accelerationY.status == DataStatus.OK &&
                    accelerationZ.status == DataStatus.OK)
                {
                    double mms = 0;

                    if (accelerationZ.data != 0)
                    {
                        // Kalkulere MMS (CAP formel)
                        mms = Math.Sqrt(Math.Pow(accelerationX.data, 2) + Math.Pow(accelerationY.data, 2)) / Math.Abs(accelerationZ.data);
                    }

                    // Kalkulere MMS_MSI (CAP formel)
                    mms_msi.data = Math.Round(10 * (180 / Math.PI) * Math.Atan(mms * (Math.PI / 180)), 0, MidpointRounding.AwayFromZero);
                    mms_msi.status = DataStatus.OK;
                    mms_msi.timestamp = accelerationX.timestamp;
                }

                // Find max value
                CalculateMSIMax(mms_msi, mms_msi_list, msiData, Constants.Minutes20);

                // Wind Data
                HMSData windDir = hmsInputDataList.GetData(ValueType.AreaWindDirectionRT); // NB! Bruker area wind her fordi vi korrigerer med egen CAP formel/fremgangsmåte
                HMSData windSpd = hmsInputDataList.GetData(ValueType.AreaWindSpeedRT);
                HMSData vesselDir = hmsInputDataList.GetData(ValueType.VesselHeading);
                HMSData vesselSpd = hmsInputDataList.GetData(ValueType.VesselSpeed);

                if (windDir.status == DataStatus.OK &&
                    windSpd.status == DataStatus.OK &&
                    vesselDir.status == DataStatus.OK &&
                    vesselSpd.status == DataStatus.OK)
                {
                    double vesselComp = 0;

                    // Kalkulere vind-komponenten som følge av fartøyets hastighet
                    if (vesselDir.status == DataStatus.OK &&
                        vesselSpd.status == DataStatus.OK)
                    {
                        // Beregner først vinkel mellom fartøyets retning og vind-retning
                        double angle = windDir.data - vesselDir.data;

                        // Beregne fartøys-komponenten
                        vesselComp = vesselSpd.data * Math.Cos(Constants.DEG_to_RAD * angle);
                    }

                    // Beregne korrigert vind-hastighet
                    double vesselCorrectedWind = windSpd.data - vesselComp;

                    // Vind korreksjon for høyde over havet
                    double windCorrected;
                    if (adminSettingsVM.windSensorHeight != 0)
                        // (CAP formel)
                        windCorrected = vesselCorrectedWind * Math.Pow((adminSettingsVM.helideckHeight + 10) / adminSettingsVM.windSensorHeight, 0.13);
                    else
                        windCorrected = vesselCorrectedWind;

                    // Legge til fartøys-komponenten igjen etter høyde-korreksjon
                    windCorrected += vesselComp;

                    // Lagre data
                    wsit.status = windDir.status;
                    wsit.timestamp = windDir.timestamp;
                    wsit.data = windCorrected;
                }
                else
                {
                    wsit.status = DataStatus.TIMEOUT_ERROR;
                    wsit.timestamp = DateTime.UtcNow;
                    wsit.data = 0;
                }

                // Finne gjennomsnittsverdier
                CalculateWSIMean(wsit, wsi_list, wsiTotal20m, wsiData, Constants.Minutes20);
            }

            // Sjekker motion limits
            CheckLimits();
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
                    maxValue.data = Math.Round(valueCorr, 1);
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
            bool doneRemovingOldValues = false;

            while (!doneRemovingOldValues && dataList.Count > 0)
            {
                // Time stamp eldre enn satt grense?
                if (dataList[0]?.timestamp.AddSeconds(time) < DateTime.UtcNow)
                {
                    // Sjekke om dette var høyeste verdi
                    if (dataList[0].data == maxValue.data)
                    {
                        // Finne ny høyeste verdi
                        findNewMaxValue = true;
                    }

                    // Fjerne gammel verdi fra verdiliste
                    dataList.RemoveAt(0);
                }
                else
                {
                    // Vi har kommet til nyere tidsverdier som ikke skal fjernes
                    // Kan da avslutte søk etter gamle verdier
                    doneRemovingOldValues = true;
                }
            }

            // Finne ny høyeste verdi
            if (findNewMaxValue)
            {
                double oldMaxValue = maxValue.data;
                maxValue.data = 0;
                doneRemovingOldValues = false;
                for (int j = 0; j < dataList.Count && !doneRemovingOldValues; j++)
                {
                    // Kan avslutte søket dersom vi finne en verdi like den gamle max verdien (ingen er høyere)
                    if (dataList[j]?.data == oldMaxValue)
                    {
                        maxValue.data = Math.Round(dataList[j].data * adminSettingsVM.msiCorrectionR, 1);
                        maxValue.timestamp = dataList[j].timestamp;
                        maxValue.status = DataStatus.OK;

                        doneRemovingOldValues = true;
                    }
                    else
                    {
                        if (dataList[j]?.data > maxValue.data)
                        {
                            maxValue.data = Math.Round(dataList[j].data * adminSettingsVM.msiCorrectionR, 1);
                            maxValue.timestamp = dataList[j].timestamp;
                            maxValue.status = DataStatus.OK;
                        }
                    }
                }
            }
        }

        private void CalculateWSIMean(HMSData value, List<TimeData> dataList, HMSData totalSum, HMSData meanValue, double time)
        {
            if (value.status == DataStatus.OK &&
                !double.IsNaN(value.data))
            {
                // Legge inn den nye verdien i data settet
                dataList.Add(new TimeData() { data = value.data, timestamp = value.timestamp });

                // Legge til i total summen
                totalSum.data += value.data;
            }

            // Sjekke om vi skal ta ut gamle verdier
            bool doneRemovingOldValues = false;

            while (!doneRemovingOldValues && dataList.Count > 0)
            {
                if (dataList[0] != null)
                {
                    // Time stamp eldre enn satt grense?
                    if (dataList[0].timestamp.AddSeconds(time) < DateTime.UtcNow)
                    {
                        // Trekke fra i total summen
                        totalSum.data -= dataList[0].data;

                        // Fjerne gammel verdi fra verdiliste
                        dataList.RemoveAt(0);
                    }
                    else
                    {
                        // Vi har kommet til nyere tidsverdier som ikke skal fjernes
                        // Kan da avslutte søk etter gamle verdier
                        doneRemovingOldValues = true;
                    }
                }
                else
                {
                    // Fjerne null forekomst
                    // Denne er plassert her fordi jeg fikk et tilfelle der hele listen dataList[i] var lik null.
                    dataList.RemoveAt(0);
                }
            }

            // Beregne gjennomsnitt av de verdiene som ligger i datasettet
            double mean = totalSum.data / dataList.Count;

            // Korrigere for WSI max basert på helikopter type
            double wsiDisp = (mean / adminSettingsVM.GetWSILimit(userInputs.helicopterType)) * 100;

            // Lagre data
            meanValue.data = Math.Round(wsiDisp, 1);
            meanValue.timestamp = value.timestamp;
            meanValue.status = value.status;
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
            CheckLimit(inclinationMax20mData, LimitType.Inclination);

            // Heave
            CheckLimit(heaveAmplitudeMax20mData, LimitType.HeaveAmplitude);

            // Significant Heave Rate
            if (SHRIsWithinLimits)
            {
                if (IsSHR2mMinAboveLimit())
                {
                    SHRIsWithinLimits = false;
                    significantHeaveRateData.limitStatus = LimitStatus.OVER;
                }
                else
                {
                    significantHeaveRateData.limitStatus = LimitStatus.OK;
                }
            }
            else
            {
                if (IsSHR10mMeanBelowLimit() && significantHeaveRateData.data <= motionLimits.GetLimit(LimitType.SignificantHeaveRate) * 0.95)
                {
                    SHRIsWithinLimits = true;
                    significantHeaveRateData.limitStatus = LimitStatus.OK;
                }
                else
                {
                    significantHeaveRateData.limitStatus = LimitStatus.OVER;
                }
            }
        }

        private void CheckLimit(HMSData hmsData, LimitType limitType)
        {
            if (hmsData.data <= motionLimits.GetLimit(limitType))
                hmsData.limitStatus = LimitStatus.OK;
            else
                hmsData.limitStatus = LimitStatus.OVER;
        }

        public bool IsSHR10mMeanBelowLimit()
        {
            return significantHeaveRate10mMean <= motionLimits.GetLimit(LimitType.SignificantHeaveRate);
        }

        public bool IsSHR2mMinAboveLimit()
        {
            return significantHeaveRate2mMin > motionLimits.GetLimit(LimitType.SignificantHeaveRate);
        }

        /////////////////////////////////////////////////////////////////////////////
        // MSI / WSI State
        /////////////////////////////////////////////////////////////////////////////
        public HelideckStatusType GetMSIWSIState
        {
            get
            {
                if (msiData.data > 100)
                {
                    return HelideckStatusType.AMBER;
                }
                else
                {
                    double maxWSI = Constants.MSIMax - (Constants.MSIMax * (msiData.data / Constants.WSIMax));

                    if (wsiData.data > maxWSI)
                    {
                        return HelideckStatusType.AMBER;
                    }
                    else
                    {
                        return HelideckStatusType.BLUE;
                    }
                }
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Inclination Kalkulasjon
        /////////////////////////////////////////////////////////////////////////////
        private void CalcInclination(HMSData pitch, HMSData roll, HMSData outputData)
        {
            if (pitch != null &&
                roll != null)
            {
                // Beregne inclination
                double inclination = HMSCalculations.Inclination(pitch.data, roll.data);

                // Data
                outputData.data = Math.Round(inclination, 1);

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
            // Sjekker status på data først
            if (sensorData.status == DataStatus.OK)
            {
                // 2-minute minimum
                ///////////////////////////////////////////////////////////

                // Lagre i 2-minutters listen
                significantHeaveRate2mMinData.Add(new TimeData()
                {
                    data = sensorData.data,
                    timestamp = sensorData.timestamp
                });

                // Sjekke om data er mindre enn minste SHR lagret siste 2 minutter
                if (sensorData.data < significantHeaveRate2mMin || significantHeaveRate2mMin == double.MaxValue)
                    significantHeaveRate2mMin = sensorData.data;

                // Sjekke om vi skal fjerne data fra 2 min listen
                bool doneRemovingOldValues = false;
                bool findNewMinValue = false;

                while (!doneRemovingOldValues && significantHeaveRate2mMinData.Count > 0)
                {
                    if (significantHeaveRate2mMinData[0]?.timestamp.AddMinutes(2) < DateTime.UtcNow)
                    {
                        // Er den verdien vi nå skal fjerne lik minimunsverdien?
                        if (significantHeaveRate2mMinData[0].data == significantHeaveRate2mMin)
                            findNewMinValue = true;

                        significantHeaveRate2mMinData.RemoveAt(0);
                    }
                    else
                    {
                        doneRemovingOldValues = true;
                    }
                }

                // Gå gjennom hele listen og finne ny minimumsverdi
                if (findNewMinValue)
                {
                    double oldMinValue = significantHeaveRate2mMin;
                    significantHeaveRate2mMin = double.MaxValue;

                    doneRemovingOldValues = false;
                    for (int j = 0; j < significantHeaveRate2mMinData.Count && !doneRemovingOldValues; j++)
                    {
                        // Kan avslutte søket dersom vi finne en verdi like den gamle minimumsverdien (ingen er lavere)
                        if (significantHeaveRate2mMinData[j]?.data == oldMinValue)
                        {
                            significantHeaveRate2mMin = oldMinValue;
                            doneRemovingOldValues = true;
                        }
                        else
                        {
                            // Sjekke om data er mindre enn minste lagret
                            if (significantHeaveRate2mMinData[j]?.data < significantHeaveRate2mMin)
                                significantHeaveRate2mMin = significantHeaveRate2mMinData[j].data;
                        }
                    }
                }

                // 10-minute mean
                ///////////////////////////////////////////////////////////
                ///
                // Legge inn den nye verdien i data settet
                significantHeaveRate10mMeanData.Add(new TimeData()
                {
                    data = sensorData.data,
                    timestamp = sensorData.timestamp
                });

                // Legge til i total summen
                significantHeaveRate10mSum += sensorData.data;

                // Sjekke om vi skal ta ut gamle verdier
                doneRemovingOldValues = false;
                while (!doneRemovingOldValues && significantHeaveRate10mMeanData.Count > 0)
                {
                    if (significantHeaveRate10mMeanData[0]?.timestamp.AddMinutes(10) < DateTime.UtcNow)
                    {
                        // Trekke fra i total summen
                        significantHeaveRate10mSum -= significantHeaveRate10mMeanData[0].data;

                        // Fjerne fra verdi listne
                        significantHeaveRate10mMeanData.RemoveAt(0);
                    }
                    else
                    {
                        // Vi har kommet til nyere tidsverdier som ikke skal fjernes
                        // Kan da avslutte søk etter gamle verdier
                        doneRemovingOldValues = true;
                    }
                }

                // Beregne gjennomsnitt av de verdiene som ligger i datasettet
                significantHeaveRate10mMean = significantHeaveRate10mSum / significantHeaveRate10mMeanData.Count;

                // 20-minute max
                ///////////////////////////////////////////////////////////
                ///
                // Legge inn den nye verdien i data settet
                significantHeaveRate20mMaxData.Add(new TimeData()
                {
                    data = sensorData.data,
                    timestamp = sensorData.timestamp
                });

                // Sjekke om data er større enn største SHR lagret siste 20 minutter
                if (sensorData.data > significantHeaveRate20mMax)
                    significantHeaveRate20mMax = sensorData.data;

                // Sjekke om vi skal fjerne data fra 20 min listen
                doneRemovingOldValues = false;
                bool findNewMaxValue = false;
                while (!doneRemovingOldValues && significantHeaveRate20mMaxData.Count > 0)
                {
                    if (significantHeaveRate20mMaxData[0]?.timestamp.AddMinutes(20) < DateTime.UtcNow)
                    {
                        // Er den verdien vi nå skal fjerne lik maximunsverdien?
                        if (significantHeaveRate20mMaxData[0].data == significantHeaveRate20mMax)
                            findNewMaxValue = true;

                        significantHeaveRate20mMaxData.RemoveAt(0);
                    }
                    else
                    {
                        doneRemovingOldValues = true;
                    }
                }

                // Gå gjennom hele listen og finne ny maximumsverdi
                if (findNewMaxValue)
                {
                    double oldMaxValue = significantHeaveRate20mMax;
                    significantHeaveRate20mMax = double.MaxValue;
                    doneRemovingOldValues = false;
                    for (int j = 0; j < significantHeaveRate20mMaxData.Count && !doneRemovingOldValues; j++)
                    {
                        // Kan avslutte søket dersom vi finne en verdi like den gamle minimumsverdien (ingen er lavere)
                        if (significantHeaveRate20mMaxData[j]?.data == oldMaxValue)
                        {
                            significantHeaveRate20mMax = oldMaxValue;
                            doneRemovingOldValues = true;
                        }
                        else
                        {
                            // Sjekke om data er mindre enn minste lagret
                            if (significantHeaveRate20mMaxData[j]?.data < significantHeaveRate20mMax)
                                significantHeaveRate20mMax = significantHeaveRate20mMaxData[j].data;
                        }
                    }
                }
            }
        }
    }
}
