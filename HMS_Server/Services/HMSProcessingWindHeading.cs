using System;
using Telerik.Windows.Data;

namespace HMS_Server
{
    class HMSProcessingWindHeading
    {
        private HMSData vesselHeading = new HMSData();
        private HMSData vesselSpeed = new HMSData();
        private HMSData vesselCOG = new HMSData();
        private HMSData vesselSOG = new HMSData();

        private HMSData sensorWindDirectionCorrected = new HMSData(); // temp

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

        private HMSData relativeWindDir = new HMSData();
        private HMSData vesselHeadingDelta = new HMSData();
        private HMSData windDirectionDelta = new HMSData();

        private HMSData helideckHeading = new HMSData();
        private HMSData helicopterHeading = new HMSData();

        // Data lister for vind-snittberegninger
        private GustData areaWindAverageData2m = new GustData();
        private GustData helideckGustData2m = new GustData();
        private GustData helideckWindAverageData10m = new GustData();

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
            helideckGustData2m.minutes = 2;
            helideckWindAverageData10m.minutes = 10;

            areaWindDirection2m.id = (int)ValueType.AreaWindDirection2m;
            areaWindDirection2m.name = "Area Wind Direction (2m)";
            areaWindDirection2m.sensorGroupId = Constants.NO_SENSOR_GROUP_ID;
            areaWindDirection2m.dbColumn = "area_wind_direction_2m";
            areaWindDirection2m.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser);
            areaWindDirection2m.AddProcessing(CalculationType.TimeAverage, 120); // 2 minutter

            areaWindSpeed2m.id = (int)ValueType.AreaWindSpeed2m;
            areaWindSpeed2m.name = "Area Wind Speed (2m)";
            areaWindSpeed2m.sensorGroupId = Constants.NO_SENSOR_GROUP_ID;
            areaWindSpeed2m.dbColumn = "area_wind_speed_2m";
            areaWindSpeed2m.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser);
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
            helideckWindDirection2m.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser);
            helideckWindDirection2m.AddProcessing(CalculationType.TimeAverage, 120); // 2 minutter
            helideckWindDirection2m.AddProcessing(CalculationType.RoundingDecimals, 0);

            helideckWindDirection10m.id = (int)ValueType.HelideckWindDirection10m;
            helideckWindDirection10m.name = "Helideck Wind Direction (10m)";
            helideckWindDirection10m.sensorGroupId = Constants.NO_SENSOR_GROUP_ID;
            helideckWindDirection10m.dbColumn = "helideck_wind_direction_10m";
            helideckWindDirection10m.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser);
            helideckWindDirection10m.AddProcessing(CalculationType.TimeAverage, 600); // 10 minutter
            helideckWindDirection10m.AddProcessing(CalculationType.RoundingDecimals, 0);

            helideckWindSpeedRT.id = (int)ValueType.HelideckWindSpeedRT;
            helideckWindSpeedRT.name = "Helideck Wind Speed (RT)";
            helideckWindSpeedRT.sensorGroupId = Constants.NO_SENSOR_GROUP_ID;
            helideckWindSpeedRT.dbColumn = "helideck_wind_speed_rt";

            helideckWindSpeed2m.id = (int)ValueType.HelideckWindSpeed2m;
            helideckWindSpeed2m.name = "Helideck Wind Speed (2m)";
            helideckWindSpeed2m.sensorGroupId = Constants.NO_SENSOR_GROUP_ID;
            helideckWindSpeed2m.dbColumn = "helideck_wind_speed_2m";
            helideckWindSpeed2m.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser);
            helideckWindSpeed2m.AddProcessing(CalculationType.TimeAverage, 120); // 2 minutter

            helideckWindSpeed10m.id = (int)ValueType.HelideckWindSpeed10m;
            helideckWindSpeed10m.name = "Helideck Wind Speed (10m)";
            helideckWindSpeed10m.sensorGroupId = Constants.NO_SENSOR_GROUP_ID;
            helideckWindSpeed10m.dbColumn = "helideck_wind_speed_10m";
            helideckWindSpeed10m.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser);
            helideckWindSpeed10m.AddProcessing(CalculationType.TimeAverage, 600); // 10 minutter

            helideckWindGust2m.id = (int)ValueType.HelideckWindGust2m;
            helideckWindGust2m.name = "Helideck Wind Gust (2m)";
            helideckWindGust2m.sensorGroupId = Constants.NO_SENSOR_GROUP_ID;
            helideckWindGust2m.dbColumn = "helideck_wind_gust_2m";

            helideckWindGust10m.id = (int)ValueType.HelideckWindGust10m;
            helideckWindGust10m.name = "Helideck Wind Gust (10m)";
            helideckWindGust10m.sensorGroupId = Constants.NO_SENSOR_GROUP_ID;
            helideckWindGust10m.dbColumn = "helideck_wind_gust_10m";

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
            // Vessel Heading & Speed
            ///////////////////////////////////////////////////////////
            vesselHeading.Set(hmsInputDataList.GetData(ValueType.VesselHeading));
            vesselCOG.Set(hmsInputDataList.GetData(ValueType.VesselCOG));

            switch (adminSettingsVM.vesselHdgRef)
            {
                case DirectionReference.MagneticNorth:
                    // Trenger ikke gjør korrigeringer
                    break;

                case DirectionReference.TrueNorth:
                    vesselHeading.data = hmsInputDataList.GetData(ValueType.VesselHeading).data - adminSettingsVM.magneticDeclination;
                    vesselCOG.data = hmsInputDataList.GetData(ValueType.VesselCOG).data - adminSettingsVM.magneticDeclination;
                    break;

                default:
                    break;
            }

            vesselSpeed.Set(hmsInputDataList.GetData(ValueType.VesselSpeed));
            vesselSOG.Set(hmsInputDataList.GetData(ValueType.VesselSOG));

            double heading = vesselHeading.data + adminSettingsVM.helideckHeadingOffset;
            if (heading > Constants.HeadingMax)
                heading -= Constants.HeadingMax;
            helideckHeading.data = heading;
            helideckHeading.status = vesselHeading.status;
            helideckHeading.timestamp = vesselHeading.timestamp;

            // Korrigere vind retning ihht referanse retning
            sensorWindDirectionCorrected.Set(hmsInputDataList.GetData(ValueType.SensorWindDirection));

            switch (adminSettingsVM.windDirRef)
            {
                case DirectionReference.VesselHeading:
                    sensorWindDirectionCorrected.data = vesselHeading.data + hmsInputDataList.GetData(ValueType.SensorWindDirection).data;
                    if (sensorWindDirectionCorrected.data > 360)
                        sensorWindDirectionCorrected.data -= 360;
                    if (sensorWindDirectionCorrected.data < 1)
                        sensorWindDirectionCorrected.data += 360;
                    break;

                case DirectionReference.MagneticNorth:
                    break;

                case DirectionReference.TrueNorth:
                    sensorWindDirectionCorrected.data = hmsInputDataList.GetData(ValueType.SensorWindDirection).data - adminSettingsVM.magneticDeclination;
                    if (sensorWindDirectionCorrected.data > 360)
                        sensorWindDirectionCorrected.data -= 360;
                    if (sensorWindDirectionCorrected.data < 1)
                        sensorWindDirectionCorrected.data += 360;
                    break;

                default:
                    break;
            }

            // Tar data fra input delen av server og overfører til HMS output delen

            // Vind retning
            helideckWindDirectionRT.Set(sensorWindDirectionCorrected);

            // Vind hastighet
            // Korrigerer for høyde
            // (Midlertidig variabel som brukes i beregninger -> ikke rund av!)
            HMSData windSpeedCorrectedToHelideck = new HMSData();

            if (hmsInputDataList.GetData(ValueType.SensorWindSpeed).status == DataStatus.OK &&
                vesselSOG.status == DataStatus.OK)
            {
                windSpeedCorrectedToHelideck.data = WindSpeedHeightCorrection(hmsInputDataList.GetData(ValueType.SensorWindSpeed).data, vesselSOG.data);
                windSpeedCorrectedToHelideck.status = hmsInputDataList.GetData(ValueType.SensorWindSpeed).status;
                windSpeedCorrectedToHelideck.timestamp = hmsInputDataList.GetData(ValueType.SensorWindSpeed).timestamp;
            }
            else
            {
                windSpeedCorrectedToHelideck.data = 0;
                windSpeedCorrectedToHelideck.status = DataStatus.TIMEOUT_ERROR;
                windSpeedCorrectedToHelideck.timestamp = DateTime.UtcNow;
            }

            // Vind hastighet
            helideckWindSpeedRT.Set(windSpeedCorrectedToHelideck);

            // Area Wind: 2-minute data
            ///////////////////////////////////////////////////////////
            areaWindDirection2m.DoProcessing(sensorWindDirectionCorrected);

            if (adminSettingsVM.regulationStandard == RegulationStandard.CAP)
                areaWindDirection2m.BufferFillCheck(Constants.WindBufferFill95Pct2m);

            areaWindSpeed2m.DoProcessing(hmsInputDataList.GetData(ValueType.SensorWindSpeed));

            if (adminSettingsVM.regulationStandard == RegulationStandard.CAP)
                areaWindSpeed2m.BufferFillCheck(Constants.WindBufferFill95Pct2m);

            UpdateGustData(
                hmsInputDataList.GetData(ValueType.SensorWindSpeed),
                areaWindSpeed2m,
                areaWindAverageData2m,
                areaWindGust2m);

            // Helideck Wind: 2-minute data
            ///////////////////////////////////////////////////////////
            helideckWindDirection2m.DoProcessing(sensorWindDirectionCorrected);

            if (adminSettingsVM.regulationStandard == RegulationStandard.CAP)
                helideckWindDirection2m.BufferFillCheck(Constants.WindBufferFill95Pct2m);

            helideckWindSpeed2m.DoProcessing(windSpeedCorrectedToHelideck);

            if (adminSettingsVM.regulationStandard == RegulationStandard.CAP)
                helideckWindSpeed2m.BufferFillCheck(Constants.WindBufferFill95Pct2m);

            UpdateGustData(
                windSpeedCorrectedToHelideck,
                helideckWindSpeed2m,
                helideckGustData2m,
                helideckWindGust2m);

            // Helideck Wind: 10-minute data
            ///////////////////////////////////////////////////////////
            helideckWindDirection10m.DoProcessing(sensorWindDirectionCorrected);

            if (adminSettingsVM.regulationStandard == RegulationStandard.CAP)
                helideckWindDirection10m.BufferFillCheck(Constants.WindBufferFill95Pct10m);

            helideckWindSpeed10m.DoProcessing(windSpeedCorrectedToHelideck);

            if (adminSettingsVM.regulationStandard == RegulationStandard.CAP)
                helideckWindSpeed10m.BufferFillCheck(Constants.WindBufferFill95Pct10m);

            UpdateGustData(
                windSpeedCorrectedToHelideck,
                helideckWindSpeed10m,
                helideckWindAverageData10m,
                helideckWindGust10m);

            // RWD, Delta heading/Wind
            /////////////////////////////////////////////////////////////////////////////////////////
            if (adminSettingsVM.regulationStandard == RegulationStandard.CAP &&
                userInputs.displayMode == DisplayMode.OnDeck)
            {
                // Relative Wind Direction
                /////////////////////////////////////////////////////////////////////////////////////////
                if (areaWindDirection2m.status == DataStatus.OK &&
                    hmsInputDataList.GetData(ValueType.VesselHeading).status == DataStatus.OK)
                {
                    relativeWindDir.data = areaWindDirection2m.data - (hmsInputDataList.GetData(ValueType.VesselHeading).data + (userInputs.onDeckHelicopterHeading - userInputs.onDeckVesselHeading));

                    if (relativeWindDir.data > 180)
                        relativeWindDir.data -= 180;
                    else
                        if (relativeWindDir.data < -180)
                        relativeWindDir.data += 180;
                }
                else
                {
                    relativeWindDir.data = 0;
                }

                relativeWindDir.status = areaWindDirection2m.status;
                relativeWindDir.timestamp = areaWindDirection2m.timestamp;

                // Vessel Heading Delta
                /////////////////////////////////////////////////////////////////////////////////////////
                if (hmsInputDataList.GetData(ValueType.VesselHeading).status == DataStatus.OK &&
                    userInputs.onDeckTime != DateTime.MinValue &&
                    userInputs.onDeckVesselHeading != -1)
                {
                    // Dersom vi gikk til on-deck display før vind data buffer ble fyllt opp, kan vi komme
                    // her uten en utgangs-vind-retning å beregne mot.
                    if (userInputs.onDeckVesselHeading == -1)
                        userInputs.onDeckVesselHeading = hmsInputDataList.GetData(ValueType.VesselHeading).data;

                    vesselHeadingDelta.data = userInputs.onDeckVesselHeading - hmsInputDataList.GetData(ValueType.VesselHeading).data;
                }
                else
                {
                    vesselHeadingDelta.data = 0;
                }

                vesselHeadingDelta.status = hmsInputDataList.GetData(ValueType.VesselHeading).status;
                vesselHeadingDelta.timestamp = hmsInputDataList.GetData(ValueType.VesselHeading).timestamp;

                // Wind Direction Delta
                /////////////////////////////////////////////////////////////////////////////////////////
                if (areaWindDirection2m.status == DataStatus.OK &&
                    userInputs.onDeckTime != DateTime.MinValue)
                {
                    // Dersom vi gikk til on-deck display før vind data buffer ble fyllt opp, kan vi komme
                    // her uten en utgangs-vind-retning å beregne mot.
                    if (userInputs.onDeckWindDirection == -1)
                        userInputs.onDeckWindDirection = areaWindDirection2m.data;

                    windDirectionDelta.data = userInputs.onDeckWindDirection - areaWindDirection2m.data;
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
                helicopterHeading.status = hmsInputDataList.GetData(ValueType.VesselHeading).status;
                helicopterHeading.timestamp = hmsInputDataList.GetData(ValueType.VesselHeading).timestamp;
            }
            else
            {
                //  Disse dataene trenger vi ikke å beregne, men status og timestamp må likevel settes slik at det ikke ser ut som feil
                relativeWindDir.data = 0;
                relativeWindDir.status = areaWindDirection2m.status;
                relativeWindDir.timestamp = areaWindDirection2m.timestamp;

                vesselHeadingDelta.data = 0;
                vesselHeadingDelta.status = hmsInputDataList.GetData(ValueType.VesselHeading).status;
                vesselHeadingDelta.timestamp = hmsInputDataList.GetData(ValueType.VesselHeading).timestamp;

                windDirectionDelta.data = 0;
                windDirectionDelta.status = areaWindDirection2m.status;
                windDirectionDelta.timestamp = areaWindDirection2m.timestamp;

                helicopterHeading.data = 0;
                helicopterHeading.status = hmsInputDataList.GetData(ValueType.VesselHeading).status;
                helicopterHeading.timestamp = hmsInputDataList.GetData(ValueType.VesselHeading).timestamp;
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
                wsiData.timestamp = DateTime.UtcNow;
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
            helideckGustData2m.Reset();
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

                if (wind <= 15 || rwd <= 25)
                {
                    return HelideckStatusType.BLUE;
                }
                else
                {
                    if (rwd > 45)
                    {
                        if (wind <= 20)
                            return HelideckStatusType.AMBER;
                        else
                            return HelideckStatusType.RED;
                    }
                    else
                    if (wind > 35)
                    {
                        if (rwd <= 30)
                            return HelideckStatusType.AMBER;
                        else
                            return HelideckStatusType.RED;
                    }
                    else
                    {
                        double maxWindRed = 20 + (45 - rwd);

                        if (wind > maxWindRed)
                        {
                            return HelideckStatusType.RED;
                        }
                        else
                        {
                            double maxWindAmber = 15 + (45 - rwd);

                            if (wind > maxWindAmber)
                                return HelideckStatusType.AMBER;
                            else
                                return HelideckStatusType.BLUE;
                        }
                    }
                }
            }
        }

        private double WindSpeedHeightCorrection(double anemometerWindSpeed, double vesselSpeed)
        {
            double windSpeed;

            // Trekker av fartøyets hastighet over bakken
            windSpeed = anemometerWindSpeed - vesselSpeed;

            // Power law approximation of a marine atmospheric boundary layer (0.13 er en konstant i denne formelen)
            // Justert til X m over helideck
            if (adminSettingsVM.windSensorHeight != 0)
                windSpeed *= Math.Pow((adminSettingsVM.helideckHeight + Constants.WindAdjustmentAboveHelideck) / adminSettingsVM.windSensorHeight, 0.13);

            // Legget tilbake fartøyets hastighet over bakken
            windSpeed += vesselSpeed;

            return windSpeed;
        }
    }
}
