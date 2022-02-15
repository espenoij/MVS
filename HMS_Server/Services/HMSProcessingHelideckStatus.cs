using System;

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

        private HelideckStatusType helideckStatus;

        private HMSData helideckStatusData = new HMSData();

        public HMSProcessingHelideckStatus(HMSDataCollection hmsOutputData, HelideckMotionLimits motionLimits, AdminSettingsVM adminSettingsVM, UserInputs userInputs, HMSProcessingMotion hmsProcessingMotion, HMSProcessingWindHeading hmsProcessingWindHeading)
        {
            this.hmsOutputData = hmsOutputData;
            this.motionLimits = motionLimits;
            this.userInputs = userInputs;
            this.adminSettingsVM = adminSettingsVM;
            this.hmsProcessingMotion = hmsProcessingMotion;
            this.hmsProcessingWindHeading = hmsProcessingWindHeading;

            // Init status
            helideckStatus = HelideckStatusType.OFF;

            // Fyller output listen med HMS Output data
            // NB! Variablene som legges inn i listen her fungerer som pekere: Oppdateres variabelen -> oppdateres listen
            // NB! Dersom nye variabler legges til i hmsOutputDataList må databasen opprettes på nytt

            RadObservableCollectionEx<HMSData> hmsOutputDataList = hmsOutputData.GetDataList();

            hmsOutputDataList.Add(helideckStatusData);

            // Sette grunnleggende data
            helideckStatusData.id = (int)ValueType.HelideckStatus;
            helideckStatusData.name = "Helideck Status";
            helideckStatusData.sensorGroupId = Constants.NO_SENSOR_GROUP_ID;
            helideckStatusData.dbColumn = "helideck_status";
        }

        public void Update()
        {
            // NOROG
            if (adminSettingsVM.regulationStandard == RegulationStandard.NOROG)
            {
                // Slår av lysene når vi ikke har ordentlige data
                if (hmsOutputData.GetData(ValueType.PitchMax20m).status == DataStatus.TIMEOUT_ERROR ||
                    hmsOutputData.GetData(ValueType.RollMax20m).status == DataStatus.TIMEOUT_ERROR ||
                    hmsOutputData.GetData(ValueType.HeaveAmplitudeMax20m).status == DataStatus.TIMEOUT_ERROR ||
                    hmsOutputData.GetData(ValueType.SignificantHeaveRateMax20m).status == DataStatus.TIMEOUT_ERROR)
                {
                    helideckStatus = HelideckStatusType.OFF;
                }
                else
                {
                    switch (helideckStatus)
                    {
                        // Init
                        case HelideckStatusType.OFF:
                            if (IsWithinLimits(ValueType.PitchMax20m) &&
                                IsWithinLimits(ValueType.RollMax20m) &&
                                IsWithinLimits(ValueType.InclinationMax20m) &&
                                IsWithinLimits(ValueType.HeaveAmplitudeMax20m) &&
                                IsWithinLimits(ValueType.SignificantHeaveRate))
                            {
                                helideckStatus = HelideckStatusType.GREEN;
                            }
                            else
                            {
                                helideckStatus = HelideckStatusType.RED;
                            }
                            break;

                        // Green -> Red
                        case HelideckStatusType.GREEN:
                            if (!IsWithinLimits(ValueType.PitchMax20m) ||
                                !IsWithinLimits(ValueType.RollMax20m) ||
                                !IsWithinLimits(ValueType.InclinationMax20m) ||
                                !IsWithinLimits(ValueType.HeaveAmplitudeMax20m) ||
                                (!IsWithinLimits(ValueType.SignificantHeaveRate) && hmsProcessingMotion.IsSHR2mMinAboveLimit()))
                            {
                                helideckStatus = HelideckStatusType.RED;
                            }
                            break;

                        // Red -> Green
                        case HelideckStatusType.RED:
                            if (IsWithinLimits(ValueType.PitchMax20m) &&
                                IsWithinLimits(ValueType.RollMax20m) &&
                                IsWithinLimits(ValueType.InclinationMax20m) &&
                                IsWithinLimits(ValueType.HeaveAmplitudeMax20m) &&
                                /*IsWithinLimits(ValueType.SignificantHeaveRate95pct) && */hmsProcessingMotion.IsSHR10mMeanBelowLimit()) // 95% regelen utgår i CAP 9c
                            {
                                helideckStatus = HelideckStatusType.GREEN;
                            }
                            break;
                    }
                }
            }
            else
            // CAP
            if (adminSettingsVM.regulationStandard == RegulationStandard.CAP)
            {
                // PRE-LANDING page
                if (userInputs.displayMode == DisplayMode.PreLanding)
                {
                    // Slår av lysene når vi ikke har ordentlige data
                    if (hmsOutputData.GetData(ValueType.PitchMax20m).status == DataStatus.TIMEOUT_ERROR ||
                        hmsOutputData.GetData(ValueType.RollMax20m).status == DataStatus.TIMEOUT_ERROR ||
                        hmsOutputData.GetData(ValueType.SignificantHeaveRate).status == DataStatus.TIMEOUT_ERROR)
                    {
                        // Status: OFF
                        helideckStatus = HelideckStatusType.OFF;
                    }
                    else
                    {
                        switch (helideckStatus)
                        {
                            // Init
                            case HelideckStatusType.OFF:
                                if (IsWithinLimits(ValueType.PitchMax20m) &&
                                    IsWithinLimits(ValueType.RollMax20m) &&
                                    IsWithinLimits(ValueType.InclinationMax20m) &&
                                    IsWithinLimits(ValueType.SignificantHeaveRate))
                                {
                                    helideckStatus = GetMSIWSIState();
                                }
                                else
                                {
                                    helideckStatus = HelideckStatusType.RED;
                                }
                                break;

                            // Blue/amber -> Red
                            case HelideckStatusType.BLUE:
                            case HelideckStatusType.AMBER:
                                if (!IsWithinLimits(ValueType.PitchMax20m) ||
                                    !IsWithinLimits(ValueType.RollMax20m) ||
                                    !IsWithinLimits(ValueType.InclinationMax20m) ||
                                    (!IsWithinLimits(ValueType.SignificantHeaveRate) && hmsProcessingMotion.IsSHR2mMinAboveLimit()))
                                {
                                    helideckStatus = HelideckStatusType.RED;
                                }
                                else
                                {
                                    helideckStatus = GetMSIWSIState();
                                }
                                break;

                            // Red -> Blue/amber
                            case HelideckStatusType.RED:
                                if (IsWithinLimits(ValueType.PitchMax20m) &&
                                    IsWithinLimits(ValueType.RollMax20m) &&
                                    IsWithinLimits(ValueType.InclinationMax20m) &&
                                    /*IsWithinLimits(ValueType.SignificantHeaveRate95pct) && */hmsProcessingMotion.IsSHR10mMeanBelowLimit()) // 95% regelen utgår i CAP 9c
                                {
                                    helideckStatus = GetMSIWSIState();
                                }
                                break;
                        }
                    }
                }
                // ON-DECK page
                else
                {
                    if (hmsOutputData.GetData(ValueType.HelideckWindSpeed2m).status == DataStatus.TIMEOUT_ERROR ||
                        hmsOutputData.GetData(ValueType.RelativeWindDir).status == DataStatus.TIMEOUT_ERROR)
                    {
                        // Status: OFF
                        helideckStatus = HelideckStatusType.OFF;
                    }
                    else
                    {
                        // Status: BLUE / AMBER / RED
                        helideckStatus = hmsProcessingWindHeading.GetRWDLimitState;
                    }
                }
            }

            // Overføre data til output liste
            helideckStatusData.data = (double)helideckStatus;
            helideckStatusData.timestamp = DateTime.UtcNow;

            if (helideckStatus != HelideckStatusType.OFF)
                helideckStatusData.status = DataStatus.OK;
            else
                helideckStatusData.status = DataStatus.TIMEOUT_ERROR;
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
                double maxWSI = Constants.MSIMax - (Constants.MSIMax * (hmsOutputData.GetData(ValueType.MSI).data / Constants.WSIMax));

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
