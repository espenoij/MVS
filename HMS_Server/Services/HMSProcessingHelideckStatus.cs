using System;
using Telerik.Windows.Data;

namespace HMS_Server
{
    class HMSProcessingHelideckStatus
    {
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
        }

        public void UpdateHelideckLight()
        {
            // NOROG
            if (adminSettingsVM.regulationStandard == RegulationStandard.NOROG)
            {
                helideckLightStatus = CheckHelideckLightStatusNOROG(helideckLightStatus);

                // Disse brukes bare til CAP
                landingStatus = HelideckStatusType.OFF;
                rwdStatus = HelideckStatusType.OFF;
            }
            else
            // CAP
            if (adminSettingsVM.regulationStandard == RegulationStandard.CAP)
            {
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
        }

        private HelideckStatusType CheckLandingStatusCAP(HelideckStatusType status)
        {
            // Slår av lysene når vi ikke har ordentlige data
            if (hmsOutputData.GetData(ValueType.PitchMax20m).status == DataStatus.TIMEOUT_ERROR ||
                hmsOutputData.GetData(ValueType.RollMax20m).status == DataStatus.TIMEOUT_ERROR ||
                hmsOutputData.GetData(ValueType.SignificantHeaveRate).status == DataStatus.TIMEOUT_ERROR ||
                hmsOutputData.GetData(ValueType.MSI).status == DataStatus.TIMEOUT_ERROR ||
                hmsOutputData.GetData(ValueType.MSI).status == DataStatus.OK_NA ||
                hmsOutputData.GetData(ValueType.WSI).status == DataStatus.TIMEOUT_ERROR ||
                hmsOutputData.GetData(ValueType.WSI).status == DataStatus.OK_NA)
            {
                // Status: OFF
                return HelideckStatusType.OFF;
            }
            else
            {
                switch (status)
                {
                    // Init
                    case HelideckStatusType.OFF:
                        if (IsWithinLimits(ValueType.PitchMax20m) &&
                            IsWithinLimits(ValueType.RollMax20m) &&
                            IsWithinLimits(ValueType.InclinationMax20m) &&
                            IsWithinLimits(ValueType.SignificantHeaveRate))
                        {
                            return GetMSIWSIState();
                        }
                        else
                        {
                            return HelideckStatusType.RED;
                        }

                    // Blue/amber -> Red
                    case HelideckStatusType.BLUE:
                    case HelideckStatusType.AMBER:
                        if (!IsWithinLimits(ValueType.PitchMax20m) ||
                            !IsWithinLimits(ValueType.RollMax20m) ||
                            !IsWithinLimits(ValueType.InclinationMax20m) ||
                            (!IsWithinLimits(ValueType.SignificantHeaveRate) && hmsProcessingMotion.IsSHR2mMinAboveLimit()))
                        {
                            return HelideckStatusType.RED;
                        }
                        else
                        {
                            return GetMSIWSIState();
                        }

                    // Red -> Blue/amber
                    case HelideckStatusType.RED:
                        if (IsWithinLimits(ValueType.PitchMax20m) &&
                            IsWithinLimits(ValueType.RollMax20m) &&
                            IsWithinLimits(ValueType.InclinationMax20m) &&
                            IsWithinLimits(ValueType.SignificantHeaveRate) && hmsProcessingMotion.IsSHR10mMeanBelowLimit()) 
                        {
                            return GetMSIWSIState();
                        }
                        else
                        {
                            return status;
                        }

                    default:
                        return status;
                }
            }
        }

        private HelideckStatusType CheckRWDStatusCAP()
        {
            if (hmsOutputData.GetData(ValueType.VesselHeading).status == DataStatus.TIMEOUT_ERROR ||
                hmsOutputData.GetData(ValueType.HelideckWindSpeed2m).status == DataStatus.TIMEOUT_ERROR ||
                hmsOutputData.GetData(ValueType.RelativeWindDir).status == DataStatus.TIMEOUT_ERROR ||
                hmsOutputData.GetData(ValueType.HelideckWindSpeed2m).status == DataStatus.OK_NA ||
                hmsOutputData.GetData(ValueType.RelativeWindDir).status == DataStatus.OK_NA)
            {
                // Status: OFF
                return HelideckStatusType.OFF;
            }
            else
            {
                // Status: BLUE / AMBER / RED
                return hmsProcessingWindHeading.GetRWDLimitState;
            }
        }

        private HelideckStatusType CheckHelideckLightStatusNOROG(HelideckStatusType status)
        {
            // Slår av lysene når vi ikke har ordentlige data
            if (hmsOutputData.GetData(ValueType.PitchMax20m).status == DataStatus.TIMEOUT_ERROR ||
                hmsOutputData.GetData(ValueType.RollMax20m).status == DataStatus.TIMEOUT_ERROR ||
                hmsOutputData.GetData(ValueType.HeaveAmplitudeMax20m).status == DataStatus.TIMEOUT_ERROR ||
                hmsOutputData.GetData(ValueType.SignificantHeaveRateMax20m).status == DataStatus.TIMEOUT_ERROR)
            {
                return HelideckStatusType.OFF;
            }
            else
            {
                switch (status)
                {
                    // Init
                    case HelideckStatusType.OFF:
                        if (IsWithinLimits(ValueType.PitchMax20m) &&
                            IsWithinLimits(ValueType.RollMax20m) &&
                            IsWithinLimits(ValueType.InclinationMax20m) &&
                            IsWithinLimits(ValueType.HeaveAmplitudeMax20m) &&
                            IsWithinLimits(ValueType.SignificantHeaveRate))
                        {
                            return HelideckStatusType.GREEN;
                        }
                        else
                        {
                            return HelideckStatusType.RED;
                        }

                    // Green -> Red
                    case HelideckStatusType.GREEN:
                        if (!IsWithinLimits(ValueType.PitchMax20m) ||
                            !IsWithinLimits(ValueType.RollMax20m) ||
                            !IsWithinLimits(ValueType.InclinationMax20m) ||
                            !IsWithinLimits(ValueType.HeaveAmplitudeMax20m) ||
                            (!IsWithinLimits(ValueType.SignificantHeaveRate) && hmsProcessingMotion.IsSHR2mMinAboveLimit()))
                        {
                            return HelideckStatusType.RED;
                        }
                        else
                        {
                            return status;
                        }

                    // Red -> Green
                    case HelideckStatusType.RED:
                        if (IsWithinLimits(ValueType.PitchMax20m) &&
                            IsWithinLimits(ValueType.RollMax20m) &&
                            IsWithinLimits(ValueType.InclinationMax20m) &&
                            IsWithinLimits(ValueType.HeaveAmplitudeMax20m) &&
                            IsWithinLimits(ValueType.SignificantHeaveRate) && hmsProcessingMotion.IsSHR10mMeanBelowLimit())
                        {
                            return HelideckStatusType.GREEN;
                        }
                        else
                        {
                            return status;
                        }

                    default:
                        return status;
                }
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // MSI / WSI State
        /////////////////////////////////////////////////////////////////////////////
        public HelideckStatusType GetMSIWSIState()
        {
            if (hmsOutputData.GetData(ValueType.MSI).data > 100)
            {
                return HelideckStatusType.AMBER;
            }
            else
            {
                double maxWSI = Constants.WSIMax - ((Constants.WSIMax * hmsOutputData.GetData(ValueType.MSI).data) / Constants.MSIMax);

                if (hmsOutputData.GetData(ValueType.WSI).data > maxWSI)
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

                case ValueType.HeaveAmplitudeMax20m:
                    return hmsOutputData.GetData(value)?.data <= motionLimits.GetLimit(LimitType.HeaveAmplitude);

                case ValueType.SignificantHeaveRate:
                    // SHR skal gå i rød dersom den er PÅ ELLER OVER grensen
                    return hmsOutputData.GetData(value)?.data < motionLimits.GetLimit(LimitType.SignificantHeaveRate);

                //case ValueType.SignificantHeaveRate95pct:
                //    return hmsProcessingMotion.GetSHR95Pct() <= motionLimits.GetLimit(LimitType.SignificantHeaveRate) * 0.95;

                default:
                    return false;
            }
        }

        public void ResetDataCalculations()
        {

        }
    }
}
