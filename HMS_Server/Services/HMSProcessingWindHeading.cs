using HMS_Server.Services;
using System;
using Telerik.Windows.Data;

namespace HMS_Server
{
    class HMSProcessingWindHeading
    {
        // Input data
        private HMSData inputVesselHeading = new HMSData();
        private HMSData inputVesselSpeed = new HMSData();
        private HMSData inputVesselCOG = new HMSData();
        private HMSData inputVesselSOG = new HMSData();
        private HMSData inputSensorWindDirection = new HMSData();
        private HMSData inputSensorWindSpeed = new HMSData();

        // Heading
        private HMSData vesselHeading = new HMSData();
        private HMSData vesselSpeed = new HMSData();
        private HMSData vesselCOG = new HMSData();
        private HMSData vesselSOG = new HMSData();

        // Wind
        private HMSData apparentWindDirection = new HMSData(); // temp

        private HMSData areaWindDirection2m = new HMSData();
        private HMSData areaWindSpeed2m = new HMSData();
        private HMSData areaWindGust2m = new HMSData();

        private HMSData helideckWindDirectionRT = new HMSData();
        private HMSData helideckWindDirection2m = new HMSData();
        private HMSData helideckWindDirection10m = new HMSData();

        private HMSData helideckWindSpeedRT = new HMSData();
        private HMSData helideckWindSpeed2m = new HMSData();
        private HMSData helideckWindSpeed10m = new HMSData();

        private HMSData helideckWindGust2m = new HMSData();
        private HMSData helideckWindGust10m = new HMSData();

        // EMS Wind
        private HMSData emsWindDirectionRT = new HMSData();
        private HMSData emsWindDirection2m = new HMSData();
        private HMSData emsWindDirection10m = new HMSData();

        private HMSData emsWindSpeedRT = new HMSData();
        private HMSData emsWindSpeed2m = new HMSData();
        private HMSData emsWindSpeed10m = new HMSData();

        private HMSData emsWindGust2m = new HMSData();
        private HMSData emsWindGust10m = new HMSData();

        // RWD & delta change
        private HMSData relativeWindDir = new HMSData();
        private HMSData vesselHeadingDelta = new HMSData();
        private HMSData windDirectionDelta = new HMSData();

        // Helideck & Helicopter
        private HMSData helideckHeading = new HMSData();
        private HMSData helicopterHeading = new HMSData();

        // Data lister for vind-snittberegninger
        private GustData areaWindAverageData2m = new GustData();
        private GustData helideckWindAverageData2m = new GustData();
        private GustData helideckWindAverageData10m = new GustData();
        private GustData emsWindAverageData2m = new GustData();
        private GustData emsWindAverageData10m = new GustData();

        // WSI
        private HMSData wsiData = new HMSData();

        private AdminSettingsVM adminSettingsVM;
        private UserInputs userInputs;

        public HMSProcessingWindHeading(HMSDataCollection hmsOutputData, AdminSettingsVM adminSettingsVM, UserInputs userInputs, ErrorHandler errorHandler)
        {
            this.adminSettingsVM = adminSettingsVM;
            this.userInputs = userInputs;

            // Fyller output listen med HMS Output data
            // NB! Variablene som legges inn i listen her fungerer som pekere: Oppdateres variabelen -> oppdateres listen
            // NB! Dersom nye variabler legges til i hmsOutputDataList må databasen opprettes på nytt

            RadObservableCollection<HMSData> hmsOutputDataList = hmsOutputData.GetDataList();

            hmsOutputDataList.Add(vesselHeading);
            hmsOutputDataList.Add(vesselSpeed);
            hmsOutputDataList.Add(vesselCOG);
            hmsOutputDataList.Add(vesselSOG);

            hmsOutputDataList.Add(areaWindDirection2m);
            hmsOutputDataList.Add(areaWindSpeed2m);
            hmsOutputDataList.Add(areaWindGust2m);

            hmsOutputDataList.Add(helideckWindDirectionRT);
            hmsOutputDataList.Add(helideckWindDirection2m);
            hmsOutputDataList.Add(helideckWindDirection10m);
            hmsOutputDataList.Add(helideckWindSpeedRT);
            hmsOutputDataList.Add(helideckWindSpeed2m);
            hmsOutputDataList.Add(helideckWindSpeed10m);
            hmsOutputDataList.Add(helideckWindGust2m);
            hmsOutputDataList.Add(helideckWindGust10m);

            if (adminSettingsVM.enableEMS)
            {
                hmsOutputDataList.Add(emsWindDirectionRT);
                hmsOutputDataList.Add(emsWindDirection2m);
                hmsOutputDataList.Add(emsWindDirection10m);
                hmsOutputDataList.Add(emsWindSpeedRT);
                hmsOutputDataList.Add(emsWindSpeed2m);
                hmsOutputDataList.Add(emsWindSpeed10m);
                hmsOutputDataList.Add(emsWindGust2m);
                hmsOutputDataList.Add(emsWindGust10m);
            }

            hmsOutputDataList.Add(relativeWindDir);
            hmsOutputDataList.Add(vesselHeadingDelta);
            hmsOutputDataList.Add(windDirectionDelta);

            hmsOutputDataList.Add(helideckHeading);
            hmsOutputDataList.Add(helicopterHeading);

            // NB! Selv om WSI ikke brukes i NOROG må vi legge den inn her
            // slik at database-tabell blir lik for CAP/NOROG.
            // Får database-feil ved bytte mellom CAP/NOROG når tabellene ikke er like.
            hmsOutputDataList.Add(wsiData);

            areaWindAverageData2m.minutes = 2;
            helideckWindAverageData2m.minutes = 2;
            helideckWindAverageData10m.minutes = 10;

            // Vessel Heading
            vesselHeading.id = (int)ValueType.VesselHeading;
            vesselHeading.name = "Vessel Heading";
            vesselHeading.sensorGroupId = Constants.NO_SENSOR_GROUP_ID;
            vesselHeading.dbColumn = "vessel_heading";

            // Wind
            areaWindDirection2m.id = (int)ValueType.AreaWindDirection2m;
            areaWindDirection2m.name = "Area Wind Direction (2m)";
            areaWindDirection2m.sensorGroupId = Constants.NO_SENSOR_GROUP_ID;
            areaWindDirection2m.dbColumn = "area_wind_direction_2m";
            areaWindDirection2m.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser, adminSettingsVM);
            areaWindDirection2m.AddProcessing(CalculationType.WindDirTimeAverage, 120); // 2 minutter

            areaWindSpeed2m.id = (int)ValueType.AreaWindSpeed2m;
            areaWindSpeed2m.name = "Area Wind Speed (2m)";
            areaWindSpeed2m.sensorGroupId = Constants.NO_SENSOR_GROUP_ID;
            areaWindSpeed2m.dbColumn = "area_wind_speed_2m";
            areaWindSpeed2m.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser, adminSettingsVM);
            areaWindSpeed2m.AddProcessing(CalculationType.TimeAverage, 120); // 2 minutter

            areaWindGust2m.id = (int)ValueType.AreaWindGust2m;
            areaWindGust2m.name = "Area Wind Gust (2m)";
            areaWindGust2m.sensorGroupId = Constants.NO_SENSOR_GROUP_ID;
            areaWindGust2m.dbColumn = "area_wind_gust_2m";

            helideckWindDirectionRT.id = (int)ValueType.HelideckWindDirectionRT;
            helideckWindDirectionRT.name = "Helideck Wind Direction (RT)";
            helideckWindDirectionRT.sensorGroupId = Constants.NO_SENSOR_GROUP_ID;
            helideckWindDirectionRT.dbColumn = "helideck_wind_direction_rt";

            helideckWindDirection2m.id = (int)ValueType.HelideckWindDirection2m;
            helideckWindDirection2m.name = "Helideck Wind Direction (2m)";
            helideckWindDirection2m.sensorGroupId = Constants.NO_SENSOR_GROUP_ID;
            helideckWindDirection2m.dbColumn = "helideck_wind_direction_2m";
            helideckWindDirection2m.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser, adminSettingsVM);
            helideckWindDirection2m.AddProcessing(CalculationType.WindDirTimeAverage, 120); // 2 minutter
            helideckWindDirection2m.AddProcessing(CalculationType.RoundingDecimals, 0);

            helideckWindDirection10m.id = (int)ValueType.HelideckWindDirection10m;
            helideckWindDirection10m.name = "Helideck Wind Direction (10m)";
            helideckWindDirection10m.sensorGroupId = Constants.NO_SENSOR_GROUP_ID;
            helideckWindDirection10m.dbColumn = "helideck_wind_direction_10m";
            helideckWindDirection10m.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser, adminSettingsVM);
            helideckWindDirection10m.AddProcessing(CalculationType.WindDirTimeAverage, 600); // 10 minutter
            helideckWindDirection10m.AddProcessing(CalculationType.RoundingDecimals, 0);

            helideckWindSpeedRT.id = (int)ValueType.HelideckWindSpeedRT;
            helideckWindSpeedRT.name = "Helideck Wind Speed (RT)";
            helideckWindSpeedRT.sensorGroupId = Constants.NO_SENSOR_GROUP_ID;
            helideckWindSpeedRT.dbColumn = "helideck_wind_speed_rt";

            helideckWindSpeed2m.id = (int)ValueType.HelideckWindSpeed2m;
            helideckWindSpeed2m.name = "Helideck Wind Speed (2m)";
            helideckWindSpeed2m.sensorGroupId = Constants.NO_SENSOR_GROUP_ID;
            helideckWindSpeed2m.dbColumn = "helideck_wind_speed_2m";
            helideckWindSpeed2m.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser, adminSettingsVM);
            helideckWindSpeed2m.AddProcessing(CalculationType.TimeAverage, 120); // 2 minutter

            helideckWindSpeed10m.id = (int)ValueType.HelideckWindSpeed10m;
            helideckWindSpeed10m.name = "Helideck Wind Speed (10m)";
            helideckWindSpeed10m.sensorGroupId = Constants.NO_SENSOR_GROUP_ID;
            helideckWindSpeed10m.dbColumn = "helideck_wind_speed_10m";
            helideckWindSpeed10m.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser, adminSettingsVM);
            helideckWindSpeed10m.AddProcessing(CalculationType.TimeAverage, 600); // 10 minutter

            helideckWindGust2m.id = (int)ValueType.HelideckWindGust2m;
            helideckWindGust2m.name = "Helideck Wind Gust (2m)";
            helideckWindGust2m.sensorGroupId = Constants.NO_SENSOR_GROUP_ID;
            helideckWindGust2m.dbColumn = "helideck_wind_gust_2m";

            helideckWindGust10m.id = (int)ValueType.HelideckWindGust10m;
            helideckWindGust10m.name = "Helideck Wind Gust (10m)";
            helideckWindGust10m.sensorGroupId = Constants.NO_SENSOR_GROUP_ID;
            helideckWindGust10m.dbColumn = "helideck_wind_gust_10m";

            // EMS Wind
            if (adminSettingsVM.enableEMS)
            {
                emsWindDirectionRT.id = (int)ValueType.EMSWindDirectionRT;
                emsWindDirectionRT.name = "EMS Wind Direction (RT)";
                emsWindDirectionRT.sensorGroupId = Constants.NO_SENSOR_GROUP_ID;

                emsWindDirection2m.id = (int)ValueType.EMSWindDirection2m;
                emsWindDirection2m.name = "EMS Wind Direction (2m)";
                emsWindDirection2m.sensorGroupId = Constants.NO_SENSOR_GROUP_ID;
                emsWindDirection2m.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser, adminSettingsVM);
                emsWindDirection2m.AddProcessing(CalculationType.WindDirTimeAverage, 120); // 2 minutter
                emsWindDirection2m.AddProcessing(CalculationType.RoundingDecimals, 0);

                emsWindDirection10m.id = (int)ValueType.EMSWindDirection10m;
                emsWindDirection10m.name = "EMS Wind Direction (10m)";
                emsWindDirection10m.sensorGroupId = Constants.NO_SENSOR_GROUP_ID;
                emsWindDirection10m.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser, adminSettingsVM);
                emsWindDirection10m.AddProcessing(CalculationType.WindDirTimeAverage, 600); // 10 minutter
                emsWindDirection10m.AddProcessing(CalculationType.RoundingDecimals, 0);

                emsWindSpeedRT.id = (int)ValueType.EMSWindSpeedRT;
                emsWindSpeedRT.name = "EMS Wind Speed (RT)";
                emsWindSpeedRT.sensorGroupId = Constants.NO_SENSOR_GROUP_ID;

                emsWindSpeed2m.id = (int)ValueType.EMSWindSpeed2m;
                emsWindSpeed2m.name = "EMS Wind Speed (2m)";
                emsWindSpeed2m.sensorGroupId = Constants.NO_SENSOR_GROUP_ID;
                emsWindSpeed2m.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser, adminSettingsVM);
                emsWindSpeed2m.AddProcessing(CalculationType.TimeAverage, 120); // 2 minutter

                emsWindSpeed10m.id = (int)ValueType.EMSWindSpeed10m;
                emsWindSpeed10m.name = "EMS Wind Speed (10m)";
                emsWindSpeed10m.sensorGroupId = Constants.NO_SENSOR_GROUP_ID;
                emsWindSpeed10m.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser, adminSettingsVM);
                emsWindSpeed10m.AddProcessing(CalculationType.TimeAverage, 600); // 10 minutter

                emsWindGust2m.id = (int)ValueType.EMSWindGust2m;
                emsWindGust2m.name = "EMS Wind Gust (2m)";
                emsWindGust2m.sensorGroupId = Constants.NO_SENSOR_GROUP_ID;

                emsWindGust10m.id = (int)ValueType.EMSWindGust10m;
                emsWindGust10m.name = "EMS Wind Gust (10m)";
                emsWindGust10m.sensorGroupId = Constants.NO_SENSOR_GROUP_ID;
            }

            // RWD & delta change
            relativeWindDir.id = (int)ValueType.RelativeWindDir;
            relativeWindDir.name = "Relative Wind Direction (CAP)";
            relativeWindDir.sensorGroupId = Constants.NO_SENSOR_GROUP_ID;
            relativeWindDir.dbColumn = "relative_wind_direction";

            helideckHeading.id = (int)ValueType.HelideckHeading;
            helideckHeading.name = "Helideck Heading";
            helideckHeading.sensorGroupId = Constants.NO_SENSOR_GROUP_ID;
            helideckHeading.dbColumn = "helideck_heading";

            vesselHeadingDelta.id = (int)ValueType.VesselHeadingDelta;
            vesselHeadingDelta.name = "Vessel Heading (Delta) (CAP)";
            vesselHeadingDelta.sensorGroupId = Constants.NO_SENSOR_GROUP_ID;
            vesselHeadingDelta.dbColumn = "vessel_heading_delta";

            windDirectionDelta.id = (int)ValueType.WindDirectionDelta;
            windDirectionDelta.name = "Wind Direction (Delta) (CAP)";
            windDirectionDelta.sensorGroupId = Constants.NO_SENSOR_GROUP_ID;
            windDirectionDelta.dbColumn = "wind_direction_delta";

            // Helicopter
            helicopterHeading.id = (int)ValueType.HelicopterHeading;
            helicopterHeading.name = "Helicopter Heading (CAP)";
            helicopterHeading.sensorGroupId = Constants.NO_SENSOR_GROUP_ID;
            helicopterHeading.dbColumn = "helicopter_heading";

            wsiData.id = (int)ValueType.WSI;
            wsiData.name = "WSI";
            wsiData.sensorGroupId = Constants.NO_SENSOR_GROUP_ID;
            wsiData.dbColumn = "wsi";
        }

        public void Update(HMSDataCollection hmsInputDataList)
        {
            // Overføre input data vi skal bruke
            if (adminSettingsVM.fixedInstallation)
            {
                // Vessel Heading
                hmsInputDataList.GetData(ValueType.VesselHeading).data = adminSettingsVM.fixedHeading;
                hmsInputDataList.GetData(ValueType.VesselHeading).status = DataStatus.OK;
                hmsInputDataList.GetData(ValueType.VesselHeading).timestamp = DateTime.UtcNow;

                hmsInputDataList.GetData(ValueType.VesselSpeed).data = adminSettingsVM.fixedHeading;
                hmsInputDataList.GetData(ValueType.VesselSpeed).status = DataStatus.OK;
                hmsInputDataList.GetData(ValueType.VesselSpeed).timestamp = DateTime.UtcNow;

                // Vessel Speed
                hmsInputDataList.GetData(ValueType.VesselSpeed).data = 0;
                hmsInputDataList.GetData(ValueType.VesselSpeed).status = DataStatus.OK;
                hmsInputDataList.GetData(ValueType.VesselSpeed).timestamp = DateTime.UtcNow;

                // Vessel COG
                hmsInputDataList.GetData(ValueType.VesselCOG).data = adminSettingsVM.fixedHeading;
                hmsInputDataList.GetData(ValueType.VesselCOG).status = DataStatus.OK;
                hmsInputDataList.GetData(ValueType.VesselCOG).timestamp = DateTime.UtcNow;

                // Vessel SOG
                hmsInputDataList.GetData(ValueType.VesselSOG).data = 0;
                hmsInputDataList.GetData(ValueType.VesselSOG).status = DataStatus.OK;
                hmsInputDataList.GetData(ValueType.VesselSOG).timestamp = DateTime.UtcNow;
            }

            inputVesselHeading.Set(hmsInputDataList.GetData(ValueType.VesselHeading));
            inputVesselSpeed.Set(hmsInputDataList.GetData(ValueType.VesselSpeed));
            inputVesselCOG.Set(hmsInputDataList.GetData(ValueType.VesselCOG));
            inputVesselSOG.Set(hmsInputDataList.GetData(ValueType.VesselSOG));

            inputSensorWindDirection.Set(hmsInputDataList.GetData(ValueType.SensorWindDirection));
            inputSensorWindSpeed.Set(hmsInputDataList.GetData(ValueType.SensorWindSpeed));


            // Sjekke status: Heading (Gyro)
            if (adminSettingsVM.statusGyroEnabled && hmsInputDataList.GetData(ValueType.SensorGyro).data != 1)
            {
                inputVesselHeading.status = DataStatus.TIMEOUT_ERROR;
            }
            else
            {
                // Sjekke data timeout
                if (inputVesselHeading.timestamp.AddMilliseconds(adminSettingsVM.dataTimeout) < DateTime.UtcNow)
                    inputVesselHeading.status = DataStatus.TIMEOUT_ERROR;
            }


            // Sjekke status: Wind
            if ((adminSettingsVM.statusWindEnabled && hmsInputDataList.GetData(ValueType.SensorWind).data != 1) ||
                (adminSettingsVM.statusGyroEnabled && hmsInputDataList.GetData(ValueType.SensorGyro).data != 1 && adminSettingsVM.windDirRef == DirectionReference.VesselHeading))
            {
                inputSensorWindDirection.status = DataStatus.TIMEOUT_ERROR;
                inputSensorWindSpeed.status = DataStatus.TIMEOUT_ERROR;
            }
            else
            {
                if (inputSensorWindDirection.timestamp.AddMilliseconds(adminSettingsVM.dataTimeout) < DateTime.UtcNow)
                    inputSensorWindDirection.status = DataStatus.TIMEOUT_ERROR;

                if (inputSensorWindSpeed.timestamp.AddMilliseconds(adminSettingsVM.dataTimeout) < DateTime.UtcNow)
                    inputSensorWindSpeed.status = DataStatus.TIMEOUT_ERROR;
            }

            // Sjekke status: SOG/COG
            if (adminSettingsVM.statusSOGCOGEnabled && hmsInputDataList.GetData(ValueType.SensorSOGCOG)?.data != 1)
            {
                inputVesselCOG.status = DataStatus.TIMEOUT_ERROR;
                inputVesselSOG.status = DataStatus.TIMEOUT_ERROR;
            }
            else
            {
                if (inputVesselCOG.timestamp.AddMilliseconds(adminSettingsVM.dataTimeout) < DateTime.UtcNow)
                    inputVesselCOG.status = DataStatus.TIMEOUT_ERROR;

                if (inputVesselSOG.timestamp.AddMilliseconds(adminSettingsVM.dataTimeout) < DateTime.UtcNow)
                    inputVesselSOG.status = DataStatus.TIMEOUT_ERROR;
            }

            // Sjekke data timeout: Vessel Speed
            if (inputVesselSpeed.timestamp.AddMilliseconds(adminSettingsVM.dataTimeout) < DateTime.UtcNow)
                inputVesselSpeed.status = DataStatus.TIMEOUT_ERROR;


            // Vessel Heading & Speed
            ///////////////////////////////////////////////////////////
            vesselHeading.Set(inputVesselHeading);
            vesselCOG.Set(inputVesselCOG);

            switch (adminSettingsVM.vesselHdgRef)
            {
                case DirectionReference.MagneticNorth:
                    // Trenger ikke gjør korrigeringer
                    break;

                case DirectionReference.TrueNorth:
                    vesselHeading.data = inputVesselHeading.data - adminSettingsVM.magneticDeclination;
                    break;

                default:
                    break;
            }

            vesselSpeed.Set(inputVesselSpeed);
            vesselSOG.Set(inputVesselSOG);

            double heading = vesselHeading.data + adminSettingsVM.helideckHeadingOffset;
            if (heading > Constants.HeadingMax)
                heading -= Constants.HeadingMax;
            helideckHeading.data = heading;
            helideckHeading.status = vesselHeading.status;
            helideckHeading.timestamp = vesselHeading.timestamp;

            // Korrigere vind retning ihht referanse retning
            apparentWindDirection.Set(inputSensorWindDirection);

            // -> gjøre om til magnetisk retning
            switch (adminSettingsVM.windDirRef)
            {
                case DirectionReference.VesselHeading:
                    apparentWindDirection.data = vesselHeading.data + inputSensorWindDirection.data;
                    if (apparentWindDirection.data < 0)
                        apparentWindDirection.data += 360;
                    if (apparentWindDirection.data >= 360)
                        apparentWindDirection.data -= 360;
                    break;

                case DirectionReference.MagneticNorth:
                    break;

                case DirectionReference.TrueNorth:
                    apparentWindDirection.data = inputSensorWindDirection.data - adminSettingsVM.magneticDeclination;
                    if (apparentWindDirection.data < 0)
                        apparentWindDirection.data += 360;
                    if (apparentWindDirection.data >= 360)
                        apparentWindDirection.data -= 360;
                    break;

                default:
                    break;
            }

            // Tar data fra input delen av server og overfører til HMS output delen

            // Korrigerer for høyde
            // (Midlertidig variabel som brukes i beregninger -> ikke rund av!)
            HMSData windSpeedCorrectedToHelideck = new HMSData();
            HMSData windSpeedCorrectedTo10mAboveMSL = new HMSData();
            HMSData windDirectionCorrectedToHelideck = new HMSData();
            HMSData windDirectionCorrectedTo10mAboveMSL = new HMSData();

            if (inputSensorWindSpeed.status == DataStatus.OK &&
                vesselSOG.status == DataStatus.OK &&
                vesselCOG.status == DataStatus.OK)
            {
                // Korrigert til helidekk (+10m)
                WindVector windHelideck = WindSpeedHeightCorrection(
                                    inputSensorWindSpeed.data,
                                    apparentWindDirection.data + adminSettingsVM.magneticDeclination,
                                    vesselSOG.data, 
                                    vesselCOG.data,
                                    adminSettingsVM.windSensorHeight, 
                                    adminSettingsVM.helideckHeight + Constants.WindAdjustmentAboveHelideck);

                // Korrigert til 10m over havet MSL (for EMS)
                WindVector wind10mAboveMSL = WindSpeedHeightCorrection(
                                    inputSensorWindSpeed.data,
                                    apparentWindDirection.data + adminSettingsVM.magneticDeclination,
                                    vesselSOG.data,
                                    vesselCOG.data,
                                    adminSettingsVM.windSensorHeight,
                                    10);

                // Vind retning
                double dir = windHelideck.dir - adminSettingsVM.magneticDeclination;
                while (dir < 0)
                    dir += 360;
                while (dir >= 360)
                    dir -= 360;

                windDirectionCorrectedToHelideck.data = dir;
                windDirectionCorrectedToHelideck.status = apparentWindDirection.status;
                windDirectionCorrectedToHelideck.timestamp = apparentWindDirection.timestamp;

                dir = wind10mAboveMSL.dir - adminSettingsVM.magneticDeclination;
                while (dir < 0)
                    dir += 360;
                while (dir >= 360)
                    dir -= 360;

                windDirectionCorrectedTo10mAboveMSL.data = dir;
                windDirectionCorrectedTo10mAboveMSL.status = apparentWindDirection.status;
                windDirectionCorrectedTo10mAboveMSL.timestamp = apparentWindDirection.timestamp;

                // Vind hastighet
                windSpeedCorrectedToHelideck.data = windHelideck.spd;
                windSpeedCorrectedToHelideck.status = inputSensorWindSpeed.status;
                windSpeedCorrectedToHelideck.timestamp = inputSensorWindSpeed.timestamp;

                windSpeedCorrectedTo10mAboveMSL.data = wind10mAboveMSL.spd;
                windSpeedCorrectedTo10mAboveMSL.status = inputSensorWindSpeed.status;
                windSpeedCorrectedTo10mAboveMSL.timestamp = inputSensorWindSpeed.timestamp;
            }
            else
            {
                // Vind retning
                windDirectionCorrectedToHelideck.data = apparentWindDirection.data;
                windDirectionCorrectedToHelideck.status = apparentWindDirection.status;
                windDirectionCorrectedToHelideck.timestamp = apparentWindDirection.timestamp;

                windDirectionCorrectedTo10mAboveMSL.data = apparentWindDirection.data;
                windDirectionCorrectedTo10mAboveMSL.status = apparentWindDirection.status;
                windDirectionCorrectedTo10mAboveMSL.timestamp = apparentWindDirection.timestamp;

                // Vind hastighet
                windSpeedCorrectedToHelideck.data = 0;
                windSpeedCorrectedToHelideck.status = DataStatus.TIMEOUT_ERROR;
                windSpeedCorrectedToHelideck.timestamp = DateTime.MinValue;

                windSpeedCorrectedTo10mAboveMSL.data = 0;
                windSpeedCorrectedTo10mAboveMSL.status = DataStatus.TIMEOUT_ERROR;
                windSpeedCorrectedTo10mAboveMSL.timestamp = DateTime.MinValue;
            }

            // Vind hastighet
            helideckWindSpeedRT.data = windSpeedCorrectedToHelideck.data;
            helideckWindSpeedRT.status = windSpeedCorrectedToHelideck.status;
            helideckWindSpeedRT.timestamp = windSpeedCorrectedToHelideck.timestamp;

            emsWindSpeedRT.data = windSpeedCorrectedToHelideck.data;
            emsWindSpeedRT.status = windSpeedCorrectedToHelideck.status;
            emsWindSpeedRT.timestamp = windSpeedCorrectedToHelideck.timestamp;

            // Vind retning
            helideckWindDirectionRT.data = windDirectionCorrectedToHelideck.data;
            helideckWindDirectionRT.status = windDirectionCorrectedToHelideck.status;
            helideckWindDirectionRT.timestamp = windDirectionCorrectedToHelideck.timestamp;

            if (adminSettingsVM.enableEMS)
            {
                emsWindDirectionRT.data = windDirectionCorrectedTo10mAboveMSL.data;
                emsWindDirectionRT.status = windDirectionCorrectedTo10mAboveMSL.status;
                emsWindDirectionRT.timestamp = windDirectionCorrectedTo10mAboveMSL.timestamp;
            }

            // Wind Samples in buffer
            ///////////////////////////////////////////////////////////
            double windSamplesInBuffer2m = Constants.WindBufferFill95Pct2m;
            double windSamplesInBuffer10m = Constants.WindBufferFill95Pct10m;

            // Area Wind: 2-minute data
            ///////////////////////////////////////////////////////////
            areaWindDirection2m.DoProcessing(apparentWindDirection);

            if (adminSettingsVM.regulationStandard == RegulationStandard.CAP && !adminSettingsVM.overrideWindBuffer)
                areaWindDirection2m.BufferFillCheck(0, windSamplesInBuffer2m);

            areaWindSpeed2m.DoProcessing(inputSensorWindSpeed);

            if (adminSettingsVM.regulationStandard == RegulationStandard.CAP && !adminSettingsVM.overrideWindBuffer)
                areaWindSpeed2m.BufferFillCheck(0, windSamplesInBuffer2m);

            UpdateGustData(
                inputSensorWindSpeed,
                areaWindSpeed2m,
                areaWindAverageData2m,
                areaWindGust2m);

            // Helideck Wind: 2-minute data
            ///////////////////////////////////////////////////////////
            helideckWindDirection2m.DoProcessing(helideckWindDirectionRT);
            
            if (adminSettingsVM.enableEMS)
                emsWindDirection2m.DoProcessing(emsWindDirectionRT);

            if (adminSettingsVM.regulationStandard == RegulationStandard.CAP && !adminSettingsVM.overrideWindBuffer)
            {
                helideckWindDirection2m.BufferFillCheck(0, windSamplesInBuffer2m);

                if (adminSettingsVM.enableEMS)
                    emsWindDirection2m.BufferFillCheck(0, windSamplesInBuffer2m);
            }

            helideckWindSpeed2m.DoProcessing(windSpeedCorrectedToHelideck);

            if (adminSettingsVM.enableEMS)
                emsWindSpeed2m.DoProcessing(windSpeedCorrectedTo10mAboveMSL);

            if (adminSettingsVM.regulationStandard == RegulationStandard.CAP && !adminSettingsVM.overrideWindBuffer)
            {
                helideckWindSpeed2m.BufferFillCheck(0, windSamplesInBuffer2m);

                if (adminSettingsVM.enableEMS)
                    emsWindSpeed2m.BufferFillCheck(0, windSamplesInBuffer2m);
            }

            UpdateGustData(
                windSpeedCorrectedToHelideck,
                helideckWindSpeed2m,
                helideckWindAverageData2m,
                helideckWindGust2m);

            if (adminSettingsVM.enableEMS)
            {
                UpdateGustData(
                    windSpeedCorrectedTo10mAboveMSL,
                    emsWindSpeed2m,
                    emsWindAverageData2m,
                    emsWindGust2m);
            }

            // Helideck Wind: 10-minute data
            ///////////////////////////////////////////////////////////
            helideckWindDirection10m.DoProcessing(helideckWindDirectionRT);

            if (adminSettingsVM.enableEMS)
                emsWindDirection10m.DoProcessing(emsWindDirectionRT);

            if (adminSettingsVM.regulationStandard == RegulationStandard.CAP && !adminSettingsVM.overrideWindBuffer)
            {
                helideckWindDirection10m.BufferFillCheck(0, windSamplesInBuffer10m);
                if (adminSettingsVM.enableEMS)
                    emsWindDirection10m.BufferFillCheck(0, windSamplesInBuffer10m);
            }

            helideckWindSpeed10m.DoProcessing(windSpeedCorrectedToHelideck);

            if (adminSettingsVM.enableEMS)
                emsWindSpeed10m.DoProcessing(windSpeedCorrectedToHelideck);

            if (adminSettingsVM.regulationStandard == RegulationStandard.CAP && !adminSettingsVM.overrideWindBuffer)
            {
                helideckWindSpeed10m.BufferFillCheck(0, windSamplesInBuffer10m);
                if (adminSettingsVM.enableEMS)
                    emsWindSpeed10m.BufferFillCheck(0, windSamplesInBuffer10m);
            }

            UpdateGustData(
                windSpeedCorrectedToHelideck,
                helideckWindSpeed10m,
                helideckWindAverageData10m,
                helideckWindGust10m);

            if (adminSettingsVM.enableEMS)
            {
                UpdateGustData(
                    windSpeedCorrectedTo10mAboveMSL,
                    emsWindSpeed10m,
                    emsWindAverageData10m,
                    emsWindGust10m);
            }

            // RWD, Delta heading/Wind
            /////////////////////////////////////////////////////////////////////////////////////////
            if (adminSettingsVM.regulationStandard == RegulationStandard.CAP &&
                userInputs.displayMode == DisplayMode.OnDeck)
            {
                // Relative Wind Direction
                /////////////////////////////////////////////////////////////////////////////////////////
                if (areaWindDirection2m.status == DataStatus.OK &&
                    inputVesselHeading.status == DataStatus.OK)
                {
                    relativeWindDir.data = areaWindDirection2m.data - (inputVesselHeading.data + (userInputs.onDeckHelicopterHeading - userInputs.onDeckVesselHeading));

                    if (relativeWindDir.data > 180)
                        relativeWindDir.data = 360 - relativeWindDir.data;
                    else
                        if (relativeWindDir.data < -180)
                        relativeWindDir.data = relativeWindDir.data + 360;

                    relativeWindDir.status = areaWindDirection2m.status;
                    relativeWindDir.timestamp = areaWindDirection2m.timestamp;
                }
                else
                {
                    relativeWindDir.data = 0;
                    relativeWindDir.status = DataStatus.TIMEOUT_ERROR;
                    relativeWindDir.timestamp = DateTime.MinValue;
                }

                // Vessel Heading Delta
                /////////////////////////////////////////////////////////////////////////////////////////
                if (inputVesselHeading.status == DataStatus.OK &&
                    userInputs.onDeckTime != DateTime.MinValue &&
                    userInputs.onDeckVesselHeading != -1)
                {
                    // Dersom vi gikk til on-deck display før vind data buffer ble fyllt opp, kan vi komme
                    // her uten en utgangs-vind-retning å beregne mot.
                    if (userInputs.onDeckVesselHeading == -1)
                        userInputs.onDeckVesselHeading = inputVesselHeading.data;

                    vesselHeadingDelta.data = inputVesselHeading.data - userInputs.onDeckVesselHeading;
                }
                else
                {
                    vesselHeadingDelta.data = 0;
                }

                vesselHeadingDelta.status = inputVesselHeading.status;
                vesselHeadingDelta.timestamp = inputVesselHeading.timestamp;

                // Wind Direction Delta
                /////////////////////////////////////////////////////////////////////////////////////////
                if (areaWindDirection2m.status == DataStatus.OK &&
                    userInputs.onDeckTime != DateTime.MinValue)
                {
                    // Dersom vi gikk til on-deck display før vind data buffer ble fyllt opp, kan vi komme
                    // her uten en utgangs-vind-retning å beregne mot.
                    if (userInputs.onDeckWindDirection == -1)
                        userInputs.onDeckWindDirection = areaWindDirection2m.data;

                    windDirectionDelta.data = areaWindDirection2m.data - userInputs.onDeckWindDirection;

                    if (windDirectionDelta.data > 180.0)
                        windDirectionDelta.data -= 360.0;
                    else
                    if (windDirectionDelta.data < -180.0)
                        windDirectionDelta.data += 360.0;
                }
                else
                {
                    windDirectionDelta.data = 0;
                }

                windDirectionDelta.status = areaWindDirection2m.status;
                windDirectionDelta.timestamp = areaWindDirection2m.timestamp;

                // Helicopter Heading
                /////////////////////////////////////////////////////////////////////////////////////////
                double heliHdg = userInputs.onDeckHelicopterHeading + (vesselHeading.data - userInputs.onDeckVesselHeading);
                if (heliHdg > Constants.HeadingMax)
                    heliHdg -= Constants.HeadingMax;
                if (heliHdg < Constants.HeadingMin)
                    heliHdg += Constants.HeadingMax;

                helicopterHeading.data = heliHdg;
                helicopterHeading.status = inputVesselHeading.status;
                helicopterHeading.timestamp = inputVesselHeading.timestamp;
            }
            else
            {
                //  Disse dataene trenger vi ikke å beregne, men status og timestamp må likevel settes slik at det ikke ser ut som feil
                relativeWindDir.data = 0;
                relativeWindDir.status = areaWindDirection2m.status;
                relativeWindDir.timestamp = areaWindDirection2m.timestamp;

                vesselHeadingDelta.data = 0;
                vesselHeadingDelta.status = inputVesselHeading.status;
                vesselHeadingDelta.timestamp = inputVesselHeading.timestamp;

                windDirectionDelta.data = 0;
                windDirectionDelta.status = areaWindDirection2m.status;
                windDirectionDelta.timestamp = areaWindDirection2m.timestamp;

                helicopterHeading.data = 0;
                helicopterHeading.status = inputVesselHeading.status;
                helicopterHeading.timestamp = inputVesselHeading.timestamp;
            }

            // WSI
            /////////////////////////////////////////////////////////////////////////////////////////
            if (adminSettingsVM.regulationStandard == RegulationStandard.CAP &&
                (helideckWindSpeed10m.status == DataStatus.OK || helideckWindSpeed10m.status == DataStatus.OK_NA))
            {
                wsiData.status = helideckWindSpeed10m.status;
                wsiData.timestamp = helideckWindSpeed10m.timestamp;
                wsiData.data = helideckWindSpeed10m.data / adminSettingsVM.GetWSILimit(userInputs.helicopterType) * 100;
            }
            else
            {
                wsiData.status = DataStatus.TIMEOUT_ERROR;
                wsiData.timestamp = DateTime.MinValue;
                wsiData.data = 0;
            }

            // Avrunding
            /////////////////////////////////////////////////////////////////////////////////////////
            vesselHeading.data = Math.Round(vesselHeading.data, 0, MidpointRounding.AwayFromZero);
            vesselSpeed.data = Math.Round(vesselSpeed.data, 1, MidpointRounding.AwayFromZero);
            vesselCOG.data = Math.Round(vesselCOG.data, 1, MidpointRounding.AwayFromZero);
            vesselSOG.data = Math.Round(vesselSOG.data, 1, MidpointRounding.AwayFromZero);

            areaWindDirection2m.data = Math.Round(areaWindDirection2m.data, 0, MidpointRounding.AwayFromZero);
            areaWindSpeed2m.data = Math.Round(areaWindSpeed2m.data, 1, MidpointRounding.AwayFromZero);
            areaWindGust2m.data = Math.Round(areaWindGust2m.data, 1, MidpointRounding.AwayFromZero);

            helideckWindDirectionRT.data = Math.Round(helideckWindDirectionRT.data, 0, MidpointRounding.AwayFromZero);
            helideckWindDirection2m.data = Math.Round(helideckWindDirection2m.data, 0, MidpointRounding.AwayFromZero);
            helideckWindDirection10m.data = Math.Round(helideckWindDirection10m.data, 0, MidpointRounding.AwayFromZero);

            helideckWindSpeedRT.data = Math.Round(helideckWindSpeedRT.data, 1, MidpointRounding.AwayFromZero);
            helideckWindSpeed2m.data = Math.Round(helideckWindSpeed2m.data, 1, MidpointRounding.AwayFromZero);
            helideckWindSpeed10m.data = Math.Round(helideckWindSpeed10m.data, 1, MidpointRounding.AwayFromZero);

            helideckWindGust2m.data = Math.Round(helideckWindGust2m.data, 1, MidpointRounding.AwayFromZero);
            helideckWindGust10m.data = Math.Round(helideckWindGust10m.data, 1, MidpointRounding.AwayFromZero);

            if (adminSettingsVM.enableEMS)
            {
                emsWindDirectionRT.data = Math.Round(emsWindDirectionRT.data, 0, MidpointRounding.AwayFromZero);
                emsWindDirection2m.data = Math.Round(emsWindDirection2m.data, 0, MidpointRounding.AwayFromZero);
                emsWindDirection10m.data = Math.Round(emsWindDirection10m.data, 0, MidpointRounding.AwayFromZero);

                emsWindSpeedRT.data = Math.Round(emsWindSpeedRT.data, 1, MidpointRounding.AwayFromZero);
                emsWindSpeed2m.data = Math.Round(emsWindSpeed2m.data, 1, MidpointRounding.AwayFromZero);
                emsWindSpeed10m.data = Math.Round(emsWindSpeed10m.data, 1, MidpointRounding.AwayFromZero);

                emsWindGust2m.data = Math.Round(emsWindGust2m.data, 1, MidpointRounding.AwayFromZero);
                emsWindGust10m.data = Math.Round(emsWindGust10m.data, 1, MidpointRounding.AwayFromZero);
            }

            relativeWindDir.data = Math.Round(relativeWindDir.data, 1, MidpointRounding.AwayFromZero);
            vesselHeadingDelta.data = Math.Round(vesselHeadingDelta.data, 1, MidpointRounding.AwayFromZero);
            windDirectionDelta.data = Math.Round(windDirectionDelta.data, 1, MidpointRounding.AwayFromZero);

            helideckHeading.data = Math.Round(helideckHeading.data, 0, MidpointRounding.AwayFromZero);
            helicopterHeading.data = Math.Round(helicopterHeading.data, 0, MidpointRounding.AwayFromZero);

            wsiData.data = Math.Round(wsiData.data, 1, MidpointRounding.AwayFromZero);
        }

        // Resette dataCalculations
        public void ResetDataCalculations()
        {
            areaWindDirection2m.ResetDataCalculations();
            areaWindSpeed2m.ResetDataCalculations();
            helideckWindDirection2m.ResetDataCalculations();
            helideckWindSpeed2m.ResetDataCalculations();
            helideckWindDirection10m.ResetDataCalculations();
            helideckWindSpeed10m.ResetDataCalculations();

            // Strengt tatt ikke nødvendig for disse ettersom kalkulasjonen ikke bruker lagrede lister
            vesselHeading.ResetDataCalculations();
            vesselSpeed.ResetDataCalculations();
            vesselCOG.ResetDataCalculations();
            vesselSOG.ResetDataCalculations();

            areaWindAverageData2m.Reset();
            helideckWindAverageData2m.Reset();
            helideckWindAverageData10m.Reset();

            // WSI
            wsiData.data = 0;
        }
        
        private void UpdateGustData(HMSData newWindSpd, HMSData windSpdMean, GustData gustData, HMSData outputGust)
        {
            // Sjekker status på data først
            if (newWindSpd?.status == DataStatus.OK &&             // Status OK
                newWindSpd?.timestamp != gustData.lastTimeStamp)   // Unngå duplikate data
            {
                bool findNewMaxGust = false;

                // Oppdatere siste timestamp
                gustData.lastTimeStamp = newWindSpd.timestamp;

                // Gust: NOROG
                //////////////////////////////////////////////
                if (adminSettingsVM.regulationStandard == RegulationStandard.NOROG)
                {
                    // NOROG: 10 minuter gust
                    if (gustData.minutes == 10)
                    {
                        // Lagre i 3 sekund-listen
                        gustData.gust3SecDataList.Add(new Wind()
                        {
                            spd = newWindSpd.data,
                            timestamp = newWindSpd.timestamp
                        });

                        // Sjekke om vi skal fjerne data fra 3-sek gust listen
                        for (int i = 0; i < gustData.gust3SecDataList.Count && gustData.gust3SecDataList.Count > 0; i++)
                        {
                            if (gustData.gust3SecDataList[i]?.timestamp.AddSeconds(3) < newWindSpd.timestamp)
                                gustData.gust3SecDataList.RemoveAt(i--);
                        }

                        // Laveste av 3 sek gust verdiene
                        // (Målet er å finne høyeste gust verdi som har vart i 3 sek eller mer)
                        double gust3SecLow = double.MaxValue;
                        foreach (var item in gustData.gust3SecDataList)
                            if (item.spd < gust3SecLow)
                                gust3SecLow = item.spd;

                        // Lagre i gust listen
                        gustData.gustDataList.Add(new Wind()
                        {
                            spd = gust3SecLow,
                            timestamp = newWindSpd.timestamp
                        });

                        // Ny max verdi?
                        if (gust3SecLow > gustData.windDataGustMax)
                        {
                            gustData.windDataGustMax = gust3SecLow;
                        }

                        // Sjekke om vi skal fjerne data fra gust listen
                        for (int i = 0; i < gustData.gustDataList.Count && gustData.gustDataList.Count > 0; i++)
                        {
                            if (gustData.gustDataList[i]?.timestamp.AddMinutes(gustData.minutes) < newWindSpd.timestamp)
                            {
                                // Sjekke om dette var max gust
                                if (gustData.gustDataList[i].spd == gustData.windDataGustMax)
                                    findNewMaxGust = true;

                                gustData.gustDataList.RemoveAt(i--);
                            }
                        }

                        // Finne ny max gust
                        if (findNewMaxGust)
                        {
                            double oldMax = gustData.windDataGustMax;
                            gustData.windDataGustMax = 0;
                            bool foundNewMax = false;

                            for (int i = 0; i < gustData.gustDataList.Count && !foundNewMax; i++)
                            {
                                // Kan avslutte søket dersom vi finne en verdi like den gamle maximumsverdien (ingen er høyere)
                                if (gustData.gustDataList[i]?.spd == oldMax)
                                {
                                    gustData.windDataGustMax = oldMax;
                                    foundNewMax = true;
                                }
                                else
                                {
                                    // Sjekke om data er større enn største lagret
                                    if (gustData.gustDataList[i]?.spd > gustData.windDataGustMax)
                                        gustData.windDataGustMax = gustData.gustDataList[i].spd;
                                }
                            }
                        }

                        // Er max gust 10 knop høyere enn snitt vind?
                        if (gustData.windDataGustMax >= windSpdMean.data + 10)
                            gustData.windGust = gustData.windDataGustMax;
                        else
                            gustData.windGust = 0;
                    }
                    // NOROG: 2-minutter gust
                    else
                    {
                        // Lagre i gust listen
                        gustData.gustDataList.Add(new Wind()
                        {
                            spd = newWindSpd.data,
                            timestamp = newWindSpd.timestamp
                        });

                        // Ny max verdi?
                        if (newWindSpd.data > gustData.windDataGustMax)
                        {
                            gustData.windDataGustMax = newWindSpd.data;
                        }

                        // Sjekke om vi skal fjerne data fra gust listen
                        for (int i = 0; i < gustData.gustDataList.Count && gustData.gustDataList.Count > 0; i++)
                        {
                            if (gustData.gustDataList[i]?.timestamp.AddMinutes(gustData.minutes) < newWindSpd.timestamp)
                            {
                                // Sjekke om dette var max gust
                                if (gustData.gustDataList[i].spd == gustData.windDataGustMax)
                                    findNewMaxGust = true;

                                gustData.gustDataList.RemoveAt(i--);
                            }
                        }

                        // Finne ny max gust
                        if (findNewMaxGust)
                        {
                            double oldMax = gustData.windDataGustMax;
                            gustData.windDataGustMax = 0;
                            bool foundNewMax = false;

                            for (int i = 0; i < gustData.gustDataList.Count && !foundNewMax; i++)
                            {
                                // Kan avslutte søket dersom vi finne en verdi like den gamle maximumsverdien (ingen er høyere)
                                if (gustData.gustDataList[i]?.spd == oldMax)
                                {
                                    gustData.windDataGustMax = oldMax;
                                    foundNewMax = true;
                                }
                                else
                                {
                                    // Sjekke om data er større enn største lagret
                                    if (gustData.gustDataList[i]?.spd > gustData.windDataGustMax)
                                        gustData.windDataGustMax = gustData.gustDataList[i].spd;
                                }
                            }
                        }

                        // Er max gust 10 knop høyere enn snitt vind?
                        if (gustData.windDataGustMax >= windSpdMean.data + 10)
                            gustData.windGust = gustData.windDataGustMax;
                        else
                            gustData.windGust = 0;
                    }
                }
                // Gust: CAP
                //////////////////////////////////////////////
                else
                {
                    // Lagre i 3 sekund-listen
                    gustData.gust3SecDataList.Add(new Wind()
                    {
                        spd = newWindSpd.data,
                        timestamp = newWindSpd.timestamp
                    });

                    // Sjekke om vi skal fjerne data fra 3-sek gust listen
                    for (int i = 0; i < gustData.gust3SecDataList.Count && gustData.gust3SecDataList.Count > 0; i++)
                    {
                        if (gustData.gust3SecDataList[i]?.timestamp.AddSeconds(3) < newWindSpd.timestamp)
                            gustData.gust3SecDataList.RemoveAt(i--);
                    }

                    // Sum av gust verdiene
                    double gust3SecSum = 0;
                    foreach (var item in gustData.gust3SecDataList)
                        gust3SecSum += item.spd;

                    // Snitt
                    double gust3SecMean;
                    if (gustData.gust3SecDataList.Count > 0)
                        gust3SecMean = gust3SecSum / gustData.gust3SecDataList.Count;
                    else
                        gust3SecMean = 0;

                    // Lagre i gust listen
                    gustData.gustDataList.Add(new Wind()
                    {
                        spd = gust3SecMean,
                        timestamp = newWindSpd.timestamp
                    });

                    // Ny max gust verdi?
                    if (gust3SecMean > gustData.windDataGustMax)
                    {
                        gustData.windDataGustMax = gust3SecMean;
                    }

                    // Sjekke om vi skal fjerne data fra gust listen
                    for (int i = 0; i < gustData.gustDataList.Count && gustData.gustDataList.Count > 0; i++)
                    {
                        if (gustData.gustDataList[0]?.timestamp.AddMinutes(gustData.minutes) < newWindSpd.timestamp)
                        {
                            // Sjekke om dette var max gust
                            if (gustData.gustDataList[i]?.spd == gustData.windDataGustMax)
                                findNewMaxGust = true;

                            gustData.gustDataList.RemoveAt(i--);
                        }
                    }

                    // Finne ny max gust
                    if (findNewMaxGust)
                    {
                        double oldMax = gustData.windDataGustMax;
                        gustData.windDataGustMax = 0;
                        bool foundNewMax = false;

                        for (int i = 0; i < gustData.gustDataList.Count && !foundNewMax; i++)
                        {
                            // Kan avslutte søket dersom vi finne en verdi like den gamle maximumsverdien (ingen er høyere)
                            if (gustData.gustDataList[i]?.spd == oldMax)
                            {
                                gustData.windDataGustMax = oldMax;
                            }
                            else
                            {
                                // Sjekke om data er større enn største lagret
                                if (gustData.gustDataList[i]?.spd > gustData.windDataGustMax)
                                    gustData.windDataGustMax = gustData.gustDataList[i].spd;
                            }
                        }
                    }

                    // Sette gust
                    gustData.windGust = gustData.windDataGustMax;
                }

                // Return data
                outputGust.data = Math.Round(gustData.windGust, 1, MidpointRounding.AwayFromZero);

                if (newWindSpd.status == DataStatus.OK && windSpdMean.status == DataStatus.OK_NA)
                    outputGust.status = DataStatus.OK_NA;
                else
                    outputGust.status = newWindSpd.status;

                outputGust.timestamp = newWindSpd.timestamp;
                outputGust.sensorGroupId = newWindSpd.sensorGroupId;                
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Relative Wind Direction Limit State
        /////////////////////////////////////////////////////////////////////////////
        public HelideckStatusType GetRWDLimitState
        {
            // NB! Samme kode som i klient (GetRWDLimitState)
            get
            {
                double wind = helideckWindSpeed2m.data;
                double rwd = Math.Abs(relativeWindDir.data);

                if (wind < 15 || rwd < 25)
                {
                    return HelideckStatusType.BLUE;
                }
                else
                {
                    if (rwd >= 45)
                    {
                        if (wind < 20)
                            return HelideckStatusType.AMBER;
                        else
                            return HelideckStatusType.RED;
                    }
                    else
                    if (wind >= 35)
                    {
                        if (rwd < 30)
                            return HelideckStatusType.AMBER;
                        else
                            return HelideckStatusType.RED;
                    }
                    else
                    {
                        double maxWindRed = 20 + (45 - rwd);

                        if (wind >= maxWindRed)
                        {
                            return HelideckStatusType.RED;
                        }
                        else
                        {
                            double maxWindAmber = 15 + (45 - rwd);

                            if (wind >= maxWindAmber)
                                return HelideckStatusType.AMBER;
                            else
                                return HelideckStatusType.BLUE;
                        }
                    }
                }
            }
        }

        private WindVector WindSpeedHeightCorrection(double apparentWindSpeed, double apparentWindDirTrue, double sog, double cog, double sensorHeight, double adjustedHeight)
        {
            WindVector VOG = new WindVector();
            WindVector WAh = new WindVector();
            WindVector WGh = new WindVector();
            WindVector WGH = new WindVector();
            WindVector WAH = new WindVector();

            // VOG
            if (double.IsNaN(sog) ||
                double.IsNaN(cog) || 
                sog < 2)
            {
                VOG.x = 0;
                VOG.y = 0;
            }
            else
            {
                // Legger på 180 for å få samme retning som vind (from vs to)
                VOG.x = Math.Cos(HMSCalc.ToRadians(cog + 180)) * sog;
                VOG.y = Math.Sin(HMSCalc.ToRadians(cog + 180)) * sog;
            }

            // WAh (apparent wind at sensor)
            WAh.x = Math.Cos(HMSCalc.ToRadians(apparentWindDirTrue)) * apparentWindSpeed;
            WAh.y = Math.Sin(HMSCalc.ToRadians(apparentWindDirTrue)) * apparentWindSpeed;

            // WGh (true wind at sensor, målt sensor vind komponent + vind som følge av fartøyets hastighet (komponent) over bakken)
            WGh.x = WAh.x + VOG.x;
            WGh.y = WAh.y + VOG.y;

            WGh.spd = Math.Sqrt(Math.Pow(WGh.x, 2) + Math.Pow(WGh.y, 2));
            WGh.dir = HMSCalc.ToDegrees(Math.Atan2(WGh.y, WGh.x));

            // Power law approximation of a marine atmospheric boundary layer (0.13 er en konstant i denne formelen)
            // Justert til X m over helideck
            if (sensorHeight != 0)
                WGH.spd = Math.Pow(adjustedHeight / sensorHeight, 0.13) * WGh.spd;
            else
                WGH.spd = WGh.spd;

            WGH.dir = WGh.dir;

            WGH.x = Math.Cos(HMSCalc.ToRadians(WGH.dir)) * WGH.spd;
            WGH.y = Math.Sin(HMSCalc.ToRadians(WGH.dir)) * WGH.spd;

            // Legge på fartøyets hastighet (komponent) over bakken igjen
            WAH.x = WGH.x - VOG.x;
            WAH.y = WGH.y - VOG.y;

            // Kalkulere true wind at helideck height / adjustedHeight
            WAH.spd = Math.Sqrt(Math.Pow(WAH.x, 2) + Math.Pow(WAH.y, 2));
            WAH.dir = HMSCalc.ToDegrees(Math.Atan2(WAH.y, WAH.x));

            while (WAH.dir < 0)
                WAH.dir += 360;
            while (WAH.dir >= 360)
                WAH.dir -= 360;

            return WAH;
        }
    }
}