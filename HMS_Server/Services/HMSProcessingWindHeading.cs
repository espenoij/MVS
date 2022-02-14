using System;

namespace HMS_Server
{
    class HMSProcessingWindHeading
    {
        private HMSData areaWindDirection2m = new HMSData();
        private HMSData areaWindDirection2mNonRounded = new HMSData();
        private HMSData areaWindSpeed2m = new HMSData();
        private HMSData areaWindSpeed2mNonRounded = new HMSData();
        private HMSData areaWindGust2m = new HMSData();

        private HMSData helideckWindDirectionRT = new HMSData();
        private HMSData helideckWindDirection2m = new HMSData();
        private HMSData helideckWindDirection10m = new HMSData();
        private HMSData helideckWindSpeedRT = new HMSData();
        private HMSData helideckWindSpeed2m = new HMSData();
        private HMSData helideckWindSpeed2mNonRounded = new HMSData();
        private HMSData helideckWindSpeed10m = new HMSData();
        private HMSData helideckWindSpeed10mNonRounded = new HMSData();
        private HMSData helideckWindGust2m = new HMSData();
        private HMSData helideckWindGust10m = new HMSData();

        private HMSData vesselHeading = new HMSData();
        private HMSData vesselSpeed = new HMSData();
        private HMSData vesselCOG = new HMSData();
        private HMSData vesselSOG = new HMSData();

        private HMSData helideckHeading = new HMSData();

        private HMSData relativeWindDir = new HMSData();

        private HMSData vesselHeadingDelta = new HMSData();
        private HMSData windDirectionDelta = new HMSData();

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

            RadObservableCollectionEx<HMSData> hmsOutputDataList = hmsOutputData.GetDataList();

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

            hmsOutputDataList.Add(vesselHeading);
            hmsOutputDataList.Add(vesselSpeed);
            hmsOutputDataList.Add(vesselCOG);
            hmsOutputDataList.Add(vesselSOG);

            hmsOutputDataList.Add(helideckHeading);

            hmsOutputDataList.Add(relativeWindDir);

            hmsOutputDataList.Add(vesselHeadingDelta);
            hmsOutputDataList.Add(windDirectionDelta);

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

            areaWindDirection2mNonRounded.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser);
            areaWindDirection2mNonRounded.AddProcessing(CalculationType.TimeAverage, 120); // 2 minutter

            areaWindSpeed2m.id = (int)ValueType.AreaWindSpeed2m;
            areaWindSpeed2m.name = "Area Wind Speed (2m)";
            areaWindSpeed2m.sensorGroupId = Constants.NO_SENSOR_GROUP_ID;
            areaWindSpeed2m.dbColumn = "area_wind_speed_2m";

            areaWindSpeed2mNonRounded.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser);
            areaWindSpeed2mNonRounded.AddProcessing(CalculationType.TimeAverage, 120); // 2 minutter

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

            helideckWindSpeed2mNonRounded.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser);
            helideckWindSpeed2mNonRounded.AddProcessing(CalculationType.TimeAverage, 120); // 2 minutter

            helideckWindSpeed10m.id = (int)ValueType.HelideckWindSpeed10m;
            helideckWindSpeed10m.name = "Helideck Wind Speed (10m)";
            helideckWindSpeed10m.sensorGroupId = Constants.NO_SENSOR_GROUP_ID;
            helideckWindSpeed10m.dbColumn = "helideck_wind_speed_10m";

            helideckWindSpeed10mNonRounded.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser);
            helideckWindSpeed10mNonRounded.AddProcessing(CalculationType.TimeAverage, 600); // 10 minutter

            helideckWindGust2m.id = (int)ValueType.HelideckWindGust2m;
            helideckWindGust2m.name = "Helideck Wind Gust (2m)";
            helideckWindGust2m.sensorGroupId = Constants.NO_SENSOR_GROUP_ID;
            helideckWindGust2m.dbColumn = "helideck_wind_gust_2m";

            helideckWindGust10m.id = (int)ValueType.HelideckWindGust10m;
            helideckWindGust10m.name = "Helideck Wind Gust (10m)";
            helideckWindGust10m.sensorGroupId = Constants.NO_SENSOR_GROUP_ID;
            helideckWindGust10m.dbColumn = "helideck_wind_gust_10m";

            vesselHeading.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser);
            vesselHeading.AddProcessing(CalculationType.RoundingDecimals, 0);

            vesselSpeed.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser);
            vesselSpeed.AddProcessing(CalculationType.RoundingDecimals, 1);

            vesselCOG.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser);
            vesselCOG.AddProcessing(CalculationType.RoundingDecimals, 1);

            vesselSOG.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser);
            vesselSOG.AddProcessing(CalculationType.RoundingDecimals, 1);

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

            if (adminSettingsVM.regulationStandard == RegulationStandard.CAP)
            {
                wsiData.id = (int)ValueType.WSI;
                wsiData.name = "WSI";
                wsiData.sensorGroupId = Constants.NO_SENSOR_GROUP_ID;
                wsiData.dbColumn = "wsi";
            }
        }

        public void Update(HMSDataCollection hmsInputDataList)
        {
            // Tar data fra input delen av server og overfører til HMS output delen

            // Vind retning
            helideckWindDirectionRT.data = Math.Round(hmsInputDataList.GetData(ValueType.SensorWindDirection).data, 1, MidpointRounding.AwayFromZero);
            helideckWindDirectionRT.status = hmsInputDataList.GetData(ValueType.SensorWindDirection).status;
            helideckWindDirectionRT.timestamp = hmsInputDataList.GetData(ValueType.SensorWindDirection).timestamp;

            // Vind hastighet
            // Korrigerer for høyde
            // (Midlertidig variabel som brukes i beregninger -> ikke rund av!)
            HMSData windSpeedCorrectedToHelideck = new HMSData();
            windSpeedCorrectedToHelideck.data = WindSpeedHeightCorrection(hmsInputDataList.GetData(ValueType.SensorWindSpeed).data);
            windSpeedCorrectedToHelideck.status = hmsInputDataList.GetData(ValueType.SensorWindSpeed).status;
            windSpeedCorrectedToHelideck.timestamp = hmsInputDataList.GetData(ValueType.SensorWindSpeed).timestamp;

            // Vind hastighet
            helideckWindSpeedRT.data = Math.Round(windSpeedCorrectedToHelideck.data, 1, MidpointRounding.AwayFromZero);
            helideckWindSpeedRT.status = hmsInputDataList.GetData(ValueType.SensorWindSpeed).status;
            helideckWindSpeedRT.timestamp = hmsInputDataList.GetData(ValueType.SensorWindSpeed).timestamp;

            // Area Wind: 2-minute data
            ///////////////////////////////////////////////////////////
            areaWindDirection2mNonRounded.DoProcessing(hmsInputDataList.GetData(ValueType.SensorWindDirection));
            areaWindDirection2m.data = Math.Round(areaWindDirection2mNonRounded.data, 1, MidpointRounding.AwayFromZero);
            areaWindDirection2m.status = areaWindDirection2mNonRounded.status;
            areaWindDirection2m.timestamp = areaWindDirection2mNonRounded.timestamp;
            areaWindDirection2m.sensorGroupId = hmsInputDataList.GetData(ValueType.SensorWindDirection).sensorGroupId;

            areaWindSpeed2mNonRounded.DoProcessing(hmsInputDataList.GetData(ValueType.SensorWindSpeed));
            areaWindSpeed2m.data = Math.Round(areaWindSpeed2mNonRounded.data, 1, MidpointRounding.AwayFromZero);
            areaWindSpeed2m.status = areaWindSpeed2mNonRounded.status;
            areaWindSpeed2m.timestamp = areaWindSpeed2mNonRounded.timestamp;
            areaWindSpeed2m.sensorGroupId = areaWindSpeed2mNonRounded.sensorGroupId;

            UpdateGustData(
                hmsInputDataList.GetData(ValueType.SensorWindSpeed),
                areaWindSpeed2mNonRounded,
                areaWindAverageData2m,
                areaWindGust2m);

            // Helideck Wind: 2-minute data
            ///////////////////////////////////////////////////////////
            helideckWindDirection2m.DoProcessing(hmsInputDataList.GetData(ValueType.SensorWindDirection));

            helideckWindSpeed2mNonRounded.DoProcessing(windSpeedCorrectedToHelideck);

            helideckWindSpeed2m.data = Math.Round(helideckWindSpeed2mNonRounded.data, 1, MidpointRounding.AwayFromZero);
            helideckWindSpeed2m.status = helideckWindSpeed2mNonRounded.status;
            helideckWindSpeed2m.timestamp = helideckWindSpeed2mNonRounded.timestamp;
            helideckWindSpeed2m.sensorGroupId = helideckWindSpeed2mNonRounded.sensorGroupId;

            UpdateGustData(
                windSpeedCorrectedToHelideck,
                helideckWindSpeed2mNonRounded,
                helideckGustData2m,
                helideckWindGust2m);

            // Helideck Wind: 10-minute data
            ///////////////////////////////////////////////////////////
            helideckWindDirection10m.DoProcessing(hmsInputDataList.GetData(ValueType.SensorWindDirection));

            helideckWindSpeed10mNonRounded.DoProcessing(windSpeedCorrectedToHelideck);

            helideckWindSpeed10m.data = Math.Round(helideckWindSpeed10mNonRounded.data, 1, MidpointRounding.AwayFromZero);
            helideckWindSpeed10m.status = helideckWindSpeed10mNonRounded.status;
            helideckWindSpeed10m.timestamp = helideckWindSpeed10mNonRounded.timestamp;
            helideckWindSpeed10m.sensorGroupId = helideckWindSpeed10mNonRounded.sensorGroupId;

            UpdateGustData(
                windSpeedCorrectedToHelideck,
                helideckWindSpeed10mNonRounded,
                helideckWindAverageData10m,
                helideckWindGust10m);

            // Vessel Heading & Speed
            ///////////////////////////////////////////////////////////
            vesselHeading.Set(hmsInputDataList.GetData(ValueType.VesselHeading)); // Set for å overføre grunnleggende data
            vesselHeading.DoProcessing(hmsInputDataList.GetData(ValueType.VesselHeading)); // DoProcessing for å avrunde til heltall

            vesselSpeed.Set(hmsInputDataList.GetData(ValueType.VesselSpeed)); // Set for å overføre grunnleggende data
            vesselSpeed.DoProcessing(hmsInputDataList.GetData(ValueType.VesselSpeed)); // DoProcessing for å avrunde til en desimal

            vesselCOG.Set(hmsInputDataList.GetData(ValueType.VesselCOG));
            vesselCOG.DoProcessing(hmsInputDataList.GetData(ValueType.VesselCOG));

            vesselSOG.Set(hmsInputDataList.GetData(ValueType.VesselSOG));
            vesselSOG.DoProcessing(hmsInputDataList.GetData(ValueType.VesselSOG));

            double heading = hmsInputDataList.GetData(ValueType.VesselHeading).data + adminSettingsVM.helideckHeadingOffset;
            if (heading > Constants.HeadingMax)
                heading -= Constants.HeadingMax;
            helideckHeading.data = Math.Round(heading, 1, MidpointRounding.AwayFromZero);
            helideckHeading.status = hmsInputDataList.GetData(ValueType.VesselHeading).status;
            helideckHeading.timestamp = hmsInputDataList.GetData(ValueType.VesselHeading).timestamp;

            if (adminSettingsVM.regulationStandard == RegulationStandard.CAP &&
                userInputs.displayMode == DisplayMode.OnDeck)
            {
                // Relative Wind Direction
                /////////////////////////////////////////////////////////////////////////////////////////
                if (areaWindDirection2m.status == DataStatus.OK &&
                    hmsInputDataList.GetData(ValueType.VesselHeading).status == DataStatus.OK)
                {
                    relativeWindDir.data = Math.Round(areaWindDirection2mNonRounded.data - (hmsInputDataList.GetData(ValueType.VesselHeading).data + (userInputs.onDeckHelicopterHeading - userInputs.onDeckVesselHeading)), 1, MidpointRounding.AwayFromZero);

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
                    vesselHeadingDelta.data = Math.Round(userInputs.onDeckVesselHeading - Math.Round(hmsInputDataList.GetData(ValueType.VesselHeading).data, 1, MidpointRounding.AwayFromZero), 1, MidpointRounding.AwayFromZero);
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
                    userInputs.onDeckTime != DateTime.MinValue &&
                    userInputs.onDeckWindDirection != -1)
                {
                    windDirectionDelta.data = Math.Round(userInputs.onDeckWindDirection - areaWindDirection2mNonRounded.data, 1, MidpointRounding.AwayFromZero);
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

                helicopterHeading.data = Math.Round(heliHdg, 1, MidpointRounding.AwayFromZero);
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
                helideckWindSpeed10mNonRounded.status == DataStatus.OK)
            {
                wsiData.status = helideckWindSpeed10mNonRounded.status;
                wsiData.timestamp = helideckWindSpeed10mNonRounded.timestamp;
                wsiData.data = Math.Round(helideckWindSpeed10mNonRounded.data / adminSettingsVM.GetWSILimit(userInputs.helicopterType) * 100, 1, MidpointRounding.AwayFromZero);
            }
            else
            {
                wsiData.status = DataStatus.TIMEOUT_ERROR;
                wsiData.timestamp = DateTime.UtcNow;
                wsiData.data = 0;
            }
        }

        // Resette dataCalculations
        public void ResetDataCalculations()
        {
            // Strengt tatt ikke nødvendig da disse kalkulasjonen ikke bruker lagrede lister
            vesselHeading.ResetDataCalculations();
            vesselSpeed.ResetDataCalculations();
            vesselCOG.ResetDataCalculations();
            vesselSOG.ResetDataCalculations();

            if (adminSettingsVM.dataVerificationEnabled)
            {
                areaWindAverageData2m.Reset();
                helideckGustData2m.Reset();
                helideckWindAverageData10m.Reset();

                // WSI
                wsiData.data = 0;
            }
        }
        
        private void UpdateGustData(HMSData windSpd, HMSData windSpdMean, GustData gustData, HMSData outputGust)
        {
            // Sjekker status på data først
            if (windSpd?.status == DataStatus.OK &&             // Status OK
                windSpd?.timestamp != gustData.lastTimeStamp)   // Unngå duplikate data
            {
                DateTime newTimestamp = windSpd.timestamp;
                bool findNewMaxGust = false;

                // Oppdatere siste timestamp
                gustData.lastTimeStamp = windSpd.timestamp;

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
                            spd = windSpd.data,
                            timestamp = windSpd.timestamp
                        });

                        // Sjekke om vi skal fjerne data fra 3-sek gust listen
                        for (int i = 0; i < gustData.gust3SecDataList.Count && gustData.gust3SecDataList.Count > 0; i++)
                        {
                            if (gustData.gust3SecDataList[i]?.timestamp.AddSeconds(3) < newTimestamp)
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
                            timestamp = windSpd.timestamp
                        });

                        // Ny max verdi?
                        if (gust3SecLow > gustData.windDataGustMax)
                        {
                            gustData.windDataGustMax = gust3SecLow;
                        }

                        // Sjekke om vi skal fjerne data fra gust listen
                        for (int i = 0; i < gustData.gustDataList.Count && gustData.gustDataList.Count > 0; i++)
                        {
                            if (gustData.gustDataList[i]?.timestamp.AddMinutes(gustData.minutes) < newTimestamp)
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
                            spd = windSpd.data,
                            timestamp = windSpd.timestamp
                        });

                        // Ny max verdi?
                        if (windSpd.data > gustData.windDataGustMax)
                        {
                            gustData.windDataGustMax = windSpd.data;
                        }

                        // Sjekke om vi skal fjerne data fra gust listen
                        for (int i = 0; i < gustData.gustDataList.Count && gustData.gustDataList.Count > 0; i++)
                        {
                            if (gustData.gustDataList[i]?.timestamp.AddMinutes(gustData.minutes) < newTimestamp)
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
                        spd = windSpd.data,
                        timestamp = windSpd.timestamp
                    });

                    // Sjekke om vi skal fjerne data fra 3-sek gust listen
                    for (int i = 0; i < gustData.gust3SecDataList.Count && gustData.gust3SecDataList.Count > 0; i++)
                    {
                        if (gustData.gust3SecDataList[i]?.timestamp.AddSeconds(3) < newTimestamp)
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
                        timestamp = windSpd.timestamp
                    });

                    // Ny max gust verdi?
                    if (gust3SecMean > gustData.windDataGustMax)
                    {
                        gustData.windDataGustMax = gust3SecMean;
                    }

                    // Sjekke om vi skal fjerne data fra gust listen
                    for (int i = 0; i < gustData.gustDataList.Count && gustData.gustDataList.Count > 0; i++)
                    {
                        if (gustData.gustDataList[0]?.timestamp.AddMinutes(gustData.minutes) < newTimestamp)
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
                outputGust.status = windSpd.status;
                outputGust.timestamp = windSpd.timestamp;
                outputGust.sensorGroupId = windSpd.sensorGroupId;                
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Relative Wind Direction Limit State
        /////////////////////////////////////////////////////////////////////////////
        public HelideckStatusType GetRWDLimitState
        {
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

        private double WindSpeedHeightCorrection(double windSpeed)
        {
            // Power law approximation of a marine atmospheric boundary layer (0.13 er en konstant i denne formelen)
            // Justert til X m over helideck
            if (adminSettingsVM.windSensorHeight != 0)
                return windSpeed * Math.Pow((adminSettingsVM.helideckHeight + Constants.WindAdjustmentAboveHelideck) / adminSettingsVM.windSensorHeight, 0.13);
            else
                return windSpeed;
        }
    }
}
