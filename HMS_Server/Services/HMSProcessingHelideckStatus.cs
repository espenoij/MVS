using System;
using Telerik.Windows.Data;

namespace HMS_Server
{
    class HMSProcessingHelideckStatus
    {
        private HMSData sensorPitchMax20m = new HMSData();
        private HMSData sensorRollMax20m = new HMSData();
        private HMSData sensorInclinationMax20m = new HMSData();
        private HMSData sensorHeaveHeightMax20m = new HMSData();
        private HMSData sensorSignificantHeaveRate = new HMSData();

        private HMSData sensorVesselHeading = new HMSData();
        private HMSData sensorMSI = new HMSData();
        private HMSData sensorWSI = new HMSData();
        private HMSData sensorHelideckWindSpeed2m = new HMSData();
        private HMSData sensorRelativeWindDir = new HMSData();

        private HMSDataCollection hmsOutputData;
        private HelideckMotionLimits motionLimits;
        private UserInputs userInputs;
        private AdminSettingsVM adminSettingsVM;
        private HMSProcessingMotion hmsProcessingMotion;
        private HMSProcessingWindHeading hmsProcessingWindHeading;

        private HelideckStatusType helideckLightStatus;
        private HelideckStatusType landingStatus;
        private HelideckStatusType rwdStatus;

        private HMSData helideckLightStatusData = new HMSData();
        private HMSData landingStatusData = new HMSData();
        private HMSData rwdStatusData = new HMSData();

        private HMSData msiwsiStatus = new HMSData();

        private bool databaseSetupRun = true;

        public HMSProcessingHelideckStatus(HMSDataCollection hmsOutputData, HelideckMotionLimits motionLimits, AdminSettingsVM adminSettingsVM, UserInputs userInputs, HMSProcessingMotion hmsProcessingMotion, HMSProcessingWindHeading hmsProcessingWindHeading)
        {
            this.hmsOutputData = hmsOutputData;
            this.motionLimits = motionLimits;
            this.userInputs = userInputs;
            this.adminSettingsVM = adminSettingsVM;
            this.hmsProcessingMotion = hmsProcessingMotion;
            this.hmsProcessingWindHeading = hmsProcessingWindHeading;

            // Init status
            helideckLightStatus = HelideckStatusType.OFF;
            landingStatus = HelideckStatusType.OFF;
            rwdStatus = HelideckStatusType.OFF;

            // Fyller output listen med HMS Output data
            // NB! Variablene som legges inn i listen her fungerer som pekere: Oppdateres variabelen -> oppdateres listen
            // NB! Dersom nye variabler legges til i hmsOutputDataList må databasen opprettes på nytt

            RadObservableCollection<HMSData> hmsOutputDataList = hmsOutputData.GetDataList();

            hmsOutputDataList.Add(helideckLightStatusData);
            hmsOutputDataList.Add(landingStatusData);
            hmsOutputDataList.Add(rwdStatusData);
            hmsOutputDataList.Add(msiwsiStatus);

            // Sette grunnleggende data
            helideckLightStatusData.id = (int)ValueType.HelideckLight;
            helideckLightStatusData.name = "Helideck Light Status";
            helideckLightStatusData.sensorGroupId = Constants.NO_SENSOR_GROUP_ID;
            helideckLightStatusData.dbColumn = "helideck_status";

            landingStatusData.id = (int)ValueType.LandingStatus;
            landingStatusData.name = "Landing Status";
            landingStatusData.sensorGroupId = Constants.NO_SENSOR_GROUP_ID;
            landingStatusData.dbColumn = "landing_status";

            rwdStatusData.id = (int)ValueType.RWDStatus;
            rwdStatusData.name = "RWD Status";
            rwdStatusData.sensorGroupId = Constants.NO_SENSOR_GROUP_ID;
            rwdStatusData.dbColumn = "rwd_status";

            msiwsiStatus.id = (int)ValueType.StatusMSIWSI;
            msiwsiStatus.name = "MSI/WSI Exceeded";
            msiwsiStatus.sensorGroupId = Constants.NO_SENSOR_GROUP_ID;
            msiwsiStatus.dbColumn = "msi_wsi_exceeded";
        }

        public void Update()
        {
            // NOROG
            if (adminSettingsVM.regulationStandard == RegulationStandard.NOROG)
            {
                // Hente sensor data
                sensorPitchMax20m.Set(hmsOutputData.GetData(ValueType.PitchMax20m));
                sensorRollMax20m.Set(hmsOutputData.GetData(ValueType.RollMax20m));
                sensorInclinationMax20m.Set(hmsOutputData.GetData(ValueType.InclinationMax20m));
                sensorHeaveHeightMax20m.Set(hmsOutputData.GetData(ValueType.HeaveHeightMax20m));
                sensorSignificantHeaveRate.Set(hmsOutputData.GetData(ValueType.SignificantHeaveRate));

                // Sjekker om vi har nye data før vi starter prosessering
                if (sensorPitchMax20m.TimeStampCheck ||
                    sensorRollMax20m.TimeStampCheck ||
                    sensorInclinationMax20m.TimeStampCheck ||
                    sensorHeaveHeightMax20m.TimeStampCheck ||
                    sensorSignificantHeaveRate.TimeStampCheck)
                {
                    helideckLightStatus = CheckHelideckLightStatusNOROG(helideckLightStatus);

                    // Disse brukes bare til CAP
                    landingStatus = HelideckStatusType.OFF;
                    rwdStatus = HelideckStatusType.OFF;
                }
            }
            else
            // CAP
            if (adminSettingsVM.regulationStandard == RegulationStandard.CAP)
            {
                // Hente sensor data
                sensorPitchMax20m.Set(hmsOutputData.GetData(ValueType.PitchMax20m));
                sensorRollMax20m.Set(hmsOutputData.GetData(ValueType.RollMax20m));
                sensorInclinationMax20m.Set(hmsOutputData.GetData(ValueType.InclinationMax20m));
                sensorHeaveHeightMax20m.Set(hmsOutputData.GetData(ValueType.HeaveHeightMax20m));
                sensorSignificantHeaveRate.Set(hmsOutputData.GetData(ValueType.SignificantHeaveRate));

                sensorVesselHeading.Set(hmsOutputData.GetData(ValueType.VesselHeading));
                sensorMSI.Set(hmsOutputData.GetData(ValueType.MSI));
                sensorWSI.Set(hmsOutputData.GetData(ValueType.WSI));
                sensorHelideckWindSpeed2m.Set(hmsOutputData.GetData(ValueType.HelideckWindSpeed2m));
                sensorRelativeWindDir.Set(hmsOutputData.GetData(ValueType.RelativeWindDir));

                // Sjekker om vi har nye data før vi starter prosessering
                if (sensorPitchMax20m.TimeStampCheck ||
                    sensorRollMax20m.TimeStampCheck ||
                    sensorInclinationMax20m.TimeStampCheck ||
                    sensorHeaveHeightMax20m.TimeStampCheck ||
                    sensorSignificantHeaveRate.TimeStampCheck ||
                    sensorVesselHeading.TimeStampCheck ||
                    sensorMSI.TimeStampCheck ||
                    sensorWSI.TimeStampCheck ||
                    sensorHelideckWindSpeed2m.TimeStampCheck ||
                    sensorRelativeWindDir.TimeStampCheck ||
                    databaseSetupRun)
                {
                    databaseSetupRun = false;

                    // Landing Status
                    ///////////////////////////////////////////////////////////////////////////////////////
                    landingStatus = CheckLandingStatusCAP(landingStatus);

                    // RWD Status
                    ///////////////////////////////////////////////////////////////////////////////////////
                    rwdStatus = CheckRWDStatusCAP();

                    // Helideck Light Status
                    ///////////////////////////////////////////////////////////////////////////////////////
                    // PRE-LANDING page
                    if (userInputs.displayMode == DisplayMode.PreLanding)
                    {
                        helideckLightStatus = landingStatus;
                    }
                    // ON-DECK page
                    else
                    {
                        helideckLightStatus = rwdStatus;
                    }
                }
            }

            // Overføre data til output liste

            // Helideck Light Status
            helideckLightStatusData.data = (double)helideckLightStatus;
            helideckLightStatusData.timestamp = DateTime.UtcNow;
            helideckLightStatusData.status = DataStatus.OK;

            // Landing Status
            landingStatusData.data = (double)landingStatus;
            landingStatusData.timestamp = DateTime.UtcNow;
            landingStatusData.status = DataStatus.OK;

            // RWD Status
            rwdStatusData.data = (double)rwdStatus;
            rwdStatusData.timestamp = DateTime.UtcNow;

            if (rwdStatus == HelideckStatusType.OFF)
                rwdStatusData.status = DataStatus.OK_NA;
            else
                rwdStatusData.status = DataStatus.OK;

            // MSI/WSI Exceeded
            if (GetMSIWSIState() == HelideckStatusType.BLUE)
                msiwsiStatus.data = 0;
            else
                msiwsiStatus.data = 1;
            msiwsiStatus.timestamp = DateTime.UtcNow;
            msiwsiStatus.status = DataStatus.OK;
        }

        private HelideckStatusType CheckLandingStatusCAP(HelideckStatusType status)
        {
            // Slår av lysene når vi ikke har ordentlige data
            if (sensorVesselHeading?.status == DataStatus.TIMEOUT_ERROR ||
                sensorPitchMax20m?.status == DataStatus.TIMEOUT_ERROR ||
                sensorRollMax20m?.status == DataStatus.TIMEOUT_ERROR ||
                sensorSignificantHeaveRate?.status == DataStatus.TIMEOUT_ERROR ||
                sensorMSI?.status == DataStatus.TIMEOUT_ERROR ||
                sensorMSI?.status == DataStatus.OK_NA ||
                sensorWSI?.status == DataStatus.TIMEOUT_ERROR ||
                sensorWSI?.status == DataStatus.OK_NA)
            {
                // Status: OFF
                return HelideckStatusType.OFF;
            }
            else
            {
                if (sensorPitchMax20m.limitStatus == LimitStatus.OK &&
                    sensorRollMax20m.limitStatus == LimitStatus.OK &&
                    sensorInclinationMax20m.limitStatus == LimitStatus.OK &&
                    sensorSignificantHeaveRate.limitStatus == LimitStatus.OK)
                {
                    return GetMSIWSIState();
                }
                else
                {
                    return HelideckStatusType.RED;
                }
            }
        }

        private HelideckStatusType CheckRWDStatusCAP()
        {
            if (sensorVesselHeading?.status != DataStatus.TIMEOUT_ERROR &&
                sensorHelideckWindSpeed2m?.status != DataStatus.TIMEOUT_ERROR &&
                sensorRelativeWindDir?.status != DataStatus.TIMEOUT_ERROR &&
                sensorHelideckWindSpeed2m?.status != DataStatus.OK_NA &&
                sensorRelativeWindDir?.status != DataStatus.OK_NA)
            {
                // Status: BLUE / AMBER / RED
                return hmsProcessingWindHeading.GetRWDLimitState;
            }
            else
            {
                // Status: OFF
                return HelideckStatusType.OFF;
            }
        }

        private HelideckStatusType CheckHelideckLightStatusNOROG(HelideckStatusType status)
        {
            // Sjekke først at vi har gyldige data
            if (sensorPitchMax20m?.status != DataStatus.TIMEOUT_ERROR &&
                sensorRollMax20m?.status != DataStatus.TIMEOUT_ERROR &&
                sensorHeaveHeightMax20m?.status != DataStatus.TIMEOUT_ERROR &&
                sensorSignificantHeaveRate?.status != DataStatus.TIMEOUT_ERROR)
            {
                if (sensorPitchMax20m.limitStatus == LimitStatus.OK &&
                    sensorRollMax20m.limitStatus == LimitStatus.OK &&
                    sensorInclinationMax20m.limitStatus == LimitStatus.OK &&
                    sensorHeaveHeightMax20m.limitStatus == LimitStatus.OK &&
                    sensorSignificantHeaveRate.limitStatus == LimitStatus.OK)
                {
                    return HelideckStatusType.GREEN; // TODO: Verifiser at denne virker
                }
                else
                {
                    return HelideckStatusType.RED;
                }
            }
            // Slår av lysene når vi ikke har ordentlige data
            else
            {
                return HelideckStatusType.OFF;
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // MSI / WSI State
        /////////////////////////////////////////////////////////////////////////////
        public HelideckStatusType GetMSIWSIState()
        {
            if (sensorMSI.data > 100)
            {
                return HelideckStatusType.AMBER;
            }
            else
            {
                double maxWSI = Constants.WSIMax - ((Constants.WSIMax * sensorMSI.data) / Constants.MSIMax);

                if (sensorWSI.data > maxWSI)
                {
                    return HelideckStatusType.AMBER;
                }
                else
                {
                    return HelideckStatusType.BLUE;
                }
            }
        }

        private bool IsWithinLimits(ValueType value)
        {
            // Pitch/roll/inclination verdier skal gå i rødt dersom de er OVER grensen
            // SHR skal gå i rød dersom den er PÅ ELLER OVER grensen
            switch (value)
            {
                case ValueType.PitchMax20m:
                case ValueType.PitchMaxUp20m:
                case ValueType.PitchMaxDown20m:
                case ValueType.RollMax20m:
                case ValueType.RollMaxLeft20m:
                case ValueType.RollMaxRight20m:
                    return hmsOutputData.GetData(value)?.data <= motionLimits.GetLimit(LimitType.PitchRoll);

                case ValueType.InclinationMax20m:
                    return hmsOutputData.GetData(value)?.data <= motionLimits.GetLimit(LimitType.Inclination);

                case ValueType.HeaveHeightMax20m:
                    return hmsOutputData.GetData(value)?.data <= motionLimits.GetLimit(LimitType.HeaveHeight);

                case ValueType.SignificantHeaveRate:
                    // SHR skal gå i rød dersom den er PÅ ELLER OVER grensen
                    return hmsOutputData.GetData(value)?.data < motionLimits.GetLimit(LimitType.SignificantHeaveRate);

                //case ValueType.SignificantHeaveRate95pct:
                //    return hmsProcessingMotion.GetSHR95Pct() <= motionLimits.GetLimit(LimitType.SignificantHeaveRate) * 0.95;

                default:
                    return false;
            }
        }
    }
}
