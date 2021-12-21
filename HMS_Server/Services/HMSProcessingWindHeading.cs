using System;
using System.Linq;
using System.Collections.Generic;

namespace HMS_Server
{
    class HMSProcessingWindHeading
    {
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

        private HMSData vesselHeading = new HMSData();
        private HMSData vesselSpeed = new HMSData();

        private HMSData helideckHeading = new HMSData();

        private HMSData relativeWindDir = new HMSData();

        private HMSData vesselHeadingDelta = new HMSData();
        private HMSData windDirectionDelta = new HMSData();

        private HMSData helicopterHeading = new HMSData();

        // Data lister for vind-snittberegninger
        private WindAverageData areaWindAverageData2m = new WindAverageData();
        private WindAverageData helideckWindAverageData2m = new WindAverageData();
        private WindAverageData helideckWindAverageData10m = new WindAverageData();

        // WSI
        private HMSData wsiData = new HMSData();

        private AdminSettingsVM adminSettingsVM;
        private UserInputs userInputs;

        public HMSProcessingWindHeading(DataCollection hmsOutputData, AdminSettingsVM adminSettingsVM, UserInputs userInputs, ErrorHandler errorHandler)
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
            helideckWindAverageData2m.minutes = 2;
            helideckWindAverageData10m.minutes = 10;

            areaWindDirection2m.id = (int)ValueType.AreaWindDirection2m;
            areaWindDirection2m.name = "Area Wind Direction (2m)";
            areaWindDirection2m.dbColumnName = "area_wind_direction_2m";

            areaWindSpeed2m.id = (int)ValueType.AreaWindSpeed2m;
            areaWindSpeed2m.name = "Area Wind Speed (2m)";
            areaWindSpeed2m.sensorGroupId = Constants.NO_SENSOR_GROUP_ID;
            areaWindSpeed2m.dbColumnName = "area_wind_speed_2m";

            areaWindGust2m.id = (int)ValueType.AreaWindGust2m;
            areaWindGust2m.name = "Area Wind Gust (2m)";
            areaWindGust2m.sensorGroupId = Constants.NO_SENSOR_GROUP_ID;
            areaWindGust2m.dbColumnName = "area_wind_gust_2m";

            helideckWindDirectionRT.id = (int)ValueType.HelideckWindDirectionRT;
            helideckWindDirectionRT.name = "Helideck Wind Direction (RT)";
            helideckWindDirectionRT.sensorGroupId = Constants.NO_SENSOR_GROUP_ID;
            helideckWindDirectionRT.dbColumnName = "helideck_wind_direction_rt";

            helideckWindDirection2m.id = (int)ValueType.HelideckWindDirection2m;
            helideckWindDirection2m.name = "Helideck Wind Direction (2m)";
            helideckWindDirection2m.sensorGroupId = Constants.NO_SENSOR_GROUP_ID;
            helideckWindDirection2m.dbColumnName = "helideck_wind_direction_2m";

            helideckWindDirection10m.id = (int)ValueType.HelideckWindDirection10m;
            helideckWindDirection10m.name = "Helideck Wind Direction (10m)";
            helideckWindDirection10m.sensorGroupId = Constants.NO_SENSOR_GROUP_ID;
            helideckWindDirection10m.dbColumnName = "helideck_wind_direction_10m";

            helideckWindSpeedRT.id = (int)ValueType.HelideckWindSpeedRT;
            helideckWindSpeedRT.name = "Helideck Wind Speed (RT)";
            helideckWindSpeedRT.sensorGroupId = Constants.NO_SENSOR_GROUP_ID;
            helideckWindSpeedRT.dbColumnName = "helideck_wind_speed_rt";

            helideckWindSpeed2m.id = (int)ValueType.HelideckWindSpeed2m;
            helideckWindSpeed2m.name = "Helideck Wind Speed (2m)";
            helideckWindSpeed2m.sensorGroupId = Constants.NO_SENSOR_GROUP_ID;
            helideckWindSpeed2m.dbColumnName = "helideck_wind_speed_2m";

            helideckWindSpeed10m.id = (int)ValueType.HelideckWindSpeed10m;
            helideckWindSpeed10m.name = "Helideck Wind Speed (10m)";
            helideckWindSpeed10m.sensorGroupId = Constants.NO_SENSOR_GROUP_ID;
            helideckWindSpeed10m.dbColumnName = "helideck_wind_speed_10m";

            helideckWindGust2m.id = (int)ValueType.HelideckWindGust2m;
            helideckWindGust2m.name = "Helideck Wind Gust (2m)";
            helideckWindGust2m.sensorGroupId = Constants.NO_SENSOR_GROUP_ID;
            helideckWindGust2m.dbColumnName = "helideck_wind_gust_2m";

            helideckWindGust10m.id = (int)ValueType.HelideckWindGust10m;
            helideckWindGust10m.name = "Helideck Wind Gust (10m)";
            helideckWindGust10m.sensorGroupId = Constants.NO_SENSOR_GROUP_ID;
            helideckWindGust10m.dbColumnName = "helideck_wind_gust_10m";

            vesselHeading.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser);
            vesselHeading.AddProcessing(CalculationType.RoundingDecimals, 0);

            vesselSpeed.InitProcessing(errorHandler, ErrorMessageCategory.AdminUser);
            vesselSpeed.AddProcessing(CalculationType.RoundingDecimals, 1);

            relativeWindDir.id = (int)ValueType.RelativeWindDir;
            relativeWindDir.name = "Relative Wind Direction (CAP)";
            relativeWindDir.sensorGroupId = Constants.NO_SENSOR_GROUP_ID;
            relativeWindDir.dbColumnName = "relative_wind_direction";

            helideckHeading.id = (int)ValueType.HelideckHeading;
            helideckHeading.name = "Helideck Heading";
            helideckHeading.sensorGroupId = Constants.NO_SENSOR_GROUP_ID;
            helideckHeading.dbColumnName = "helideck_heading";

            vesselHeadingDelta.id = (int)ValueType.VesselHeadingDelta;
            vesselHeadingDelta.name = "Vessel Heading (Delta) (CAP)";
            vesselHeadingDelta.sensorGroupId = Constants.NO_SENSOR_GROUP_ID;
            vesselHeadingDelta.dbColumnName = "vessel_heading_delta";

            windDirectionDelta.id = (int)ValueType.WindDirectionDelta;
            windDirectionDelta.name = "Wind Direction (Delta) (CAP)";
            windDirectionDelta.sensorGroupId = Constants.NO_SENSOR_GROUP_ID;
            windDirectionDelta.dbColumnName = "wind_direction_delta";

            helicopterHeading.id = (int)ValueType.HelicopterHeading;
            helicopterHeading.name = "Helicopter Heading (CAP)";
            helicopterHeading.sensorGroupId = Constants.NO_SENSOR_GROUP_ID;
            helicopterHeading.dbColumnName = "helicopter_heading";

            if (adminSettingsVM.regulationStandard == RegulationStandard.CAP)
            {
                wsiData.id = (int)ValueType.WSI;
                wsiData.name = "WSI";
                wsiData.sensorGroupId = Constants.NO_SENSOR_GROUP_ID;
                wsiData.dbColumnName = "wsi";
            }
        }

        public void Update(DataCollection hmsInputDataList)
        {
            // Tar data fra input delen av server og overfører til HMS output delen

            // Vind retning
            helideckWindDirectionRT.data = Math.Round(hmsInputDataList.GetData(ValueType.SensorWindDirection).data, 0, MidpointRounding.AwayFromZero);
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
            helideckWindSpeedRT.data = Math.Round(windSpeedCorrectedToHelideck.data, 0, MidpointRounding.AwayFromZero);
            helideckWindSpeedRT.status = hmsInputDataList.GetData(ValueType.SensorWindSpeed).status;
            helideckWindSpeedRT.timestamp = hmsInputDataList.GetData(ValueType.SensorWindSpeed).timestamp;

            // Area Wind: 2-minute data
            ///////////////////////////////////////////////////////////
            UpdateWindAverages(
                hmsInputDataList.GetData(ValueType.SensorWindDirection),
                hmsInputDataList.GetData(ValueType.SensorWindSpeed),
                areaWindAverageData2m);

            areaWindDirection2m.data = Math.Round(areaWindAverageData2m.windDir, 0, MidpointRounding.AwayFromZero);
            areaWindDirection2m.status = hmsInputDataList.GetData(ValueType.SensorWindDirection).status;
            areaWindDirection2m.timestamp = hmsInputDataList.GetData(ValueType.SensorWindDirection).timestamp;
            areaWindDirection2m.sensorGroupId = hmsInputDataList.GetData(ValueType.SensorWindDirection).sensorGroupId;

            areaWindSpeed2m.data = Math.Round(areaWindAverageData2m.windSpeed, 0, MidpointRounding.AwayFromZero);
            areaWindSpeed2m.status = hmsInputDataList.GetData(ValueType.SensorWindSpeed).status;
            areaWindSpeed2m.timestamp = hmsInputDataList.GetData(ValueType.SensorWindSpeed).timestamp;
            areaWindSpeed2m.sensorGroupId = hmsInputDataList.GetData(ValueType.SensorWindSpeed).sensorGroupId;

            areaWindGust2m.data = Math.Round(areaWindAverageData2m.windGust, 0, MidpointRounding.AwayFromZero);
            areaWindGust2m.status = hmsInputDataList.GetData(ValueType.SensorWindSpeed).status;
            areaWindGust2m.timestamp = hmsInputDataList.GetData(ValueType.SensorWindSpeed).timestamp;
            areaWindGust2m.sensorGroupId = hmsInputDataList.GetData(ValueType.SensorWindSpeed).sensorGroupId;

            // Helideck Wind: 2-minute data
            ///////////////////////////////////////////////////////////
            UpdateWindAverages(
                hmsInputDataList.GetData(ValueType.SensorWindDirection),
                windSpeedCorrectedToHelideck,
                helideckWindAverageData2m);

            helideckWindDirection2m.data = Math.Round(helideckWindAverageData2m.windDir, 0, MidpointRounding.AwayFromZero);
            helideckWindDirection2m.status = helideckWindDirectionRT.status;
            helideckWindDirection2m.timestamp = helideckWindDirectionRT.timestamp;

            helideckWindSpeed2m.data = Math.Round(helideckWindAverageData2m.windSpeed, 0, MidpointRounding.AwayFromZero);
            helideckWindSpeed2m.status = helideckWindSpeedRT.status;
            helideckWindSpeed2m.timestamp = helideckWindSpeedRT.timestamp;

            helideckWindGust2m.data = Math.Round(helideckWindAverageData2m.windGust, 0, MidpointRounding.AwayFromZero);
            helideckWindGust2m.status = helideckWindSpeedRT.status;
            helideckWindGust2m.timestamp = helideckWindSpeedRT.timestamp;

            // Helideck Wind: 10-minute data
            ///////////////////////////////////////////////////////////
            UpdateWindAverages(
                hmsInputDataList.GetData(ValueType.SensorWindDirection),
                windSpeedCorrectedToHelideck,
                helideckWindAverageData10m);

            helideckWindDirection10m.data = Math.Round(helideckWindAverageData10m.windDir, 0, MidpointRounding.AwayFromZero);
            helideckWindDirection10m.status = helideckWindDirectionRT.status;
            helideckWindDirection10m.timestamp = helideckWindDirectionRT.timestamp;

            helideckWindSpeed10m.data = Math.Round(helideckWindAverageData10m.windSpeed, 0, MidpointRounding.AwayFromZero);
            helideckWindSpeed10m.status = helideckWindSpeedRT.status;
            helideckWindSpeed10m.timestamp = helideckWindSpeedRT.timestamp;

            helideckWindGust10m.data = Math.Round(helideckWindAverageData10m.windGust, 0, MidpointRounding.AwayFromZero);
            helideckWindGust10m.status = helideckWindSpeedRT.status;
            helideckWindGust10m.timestamp = helideckWindSpeedRT.timestamp;

            // Vessel Heading & Speed
            ///////////////////////////////////////////////////////////
            vesselHeading.Set(hmsInputDataList.GetData(ValueType.VesselHeading)); // Set for å overføre grunnleggende data
            vesselHeading.DoProcessing(hmsInputDataList.GetData(ValueType.VesselHeading)); // DoProcessing for å avrunde til heltall

            vesselSpeed.Set(hmsInputDataList.GetData(ValueType.VesselSpeed)); // Set for å overføre grunnleggende data
            vesselSpeed.DoProcessing(hmsInputDataList.GetData(ValueType.VesselSpeed)); // DoProcessing for å avrunde til en desimal

            double heading = hmsInputDataList.GetData(ValueType.VesselHeading).data + adminSettingsVM.helideckHeadingOffset;
            if (heading >= 360)
                heading -= 360;
            helideckHeading.data = Math.Round(heading, 0, MidpointRounding.AwayFromZero);
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
                    relativeWindDir.data = Math.Round(areaWindDirection2m.data - (hmsInputDataList.GetData(ValueType.VesselHeading).data + (userInputs.onDeckHelicopterHeading - userInputs.onDeckVesselHeading)), 0, MidpointRounding.AwayFromZero);
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
                    vesselHeadingDelta.data = Math.Round(hmsInputDataList.GetData(ValueType.VesselHeading).data - userInputs.onDeckVesselHeading, 0, MidpointRounding.AwayFromZero);
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
                    windDirectionDelta.data = Math.Round(areaWindDirection2m.data - userInputs.onDeckWindDirection, 0, MidpointRounding.AwayFromZero);
                }
                else
                {
                    windDirectionDelta.data = 0;
                }

                windDirectionDelta.status = areaWindDirection2m.status;
                windDirectionDelta.timestamp = areaWindDirection2m.timestamp;

                // Helicopter Heading
                /////////////////////////////////////////////////////////////////////////////////////////
                double heliHdg = userInputs.onDeckHelicopterHeading + (hmsInputDataList.GetData(ValueType.VesselHeading).data - userInputs.onDeckVesselHeading);
                if (heliHdg > 360)
                    heliHdg -= 360;
                if (heliHdg < 0)
                    heliHdg += 360;

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
            ///
            if (helideckWindSpeed10m.status == DataStatus.OK)
            {
                wsiData.status = helideckWindSpeed10m.status;
                wsiData.timestamp = helideckWindSpeed10m.timestamp;
                wsiData.data = Math.Round(helideckWindAverageData10m.windSpeed / adminSettingsVM.GetWSILimit(userInputs.helicopterType) * 100, 0, MidpointRounding.AwayFromZero);
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

            if (adminSettingsVM.dataVerificationEnabled)
            {
                areaWindAverageData2m.Reset();
                helideckWindAverageData2m.Reset();
                helideckWindAverageData10m.Reset();

                // WSI
                wsiData.data = 0;
            }
        }
        
        private void UpdateWindAverages(HMSData windDir, HMSData windSpd, WindAverageData windAverageData)
        {
            // Sjekker status på data først
            if (windDir?.status == DataStatus.OK &&
                windSpd?.status == DataStatus.OK)
            {
                // WIND & HEADING
                //////////////////////////////////////////////////////////////////////////////
                // Lagre i datalisten
                windAverageData.windDataList.Add(new Wind()
                {
                    dir = windDir.data,
                    spd = windSpd.data,
                    timestamp = windDir.timestamp
                });

                // Wind heading & speed: Legg til i total summene for retning og hastighet
                windAverageData.windDataDirTotal += windDir.data;
                windAverageData.windDataSpdTotal += windSpd.data;

                // Sjekke om vi skal fjerne data fra vind data listen
                for (int i = 0; i < windAverageData.windDataList.Count && windAverageData.windDataList.Count > 0; i++)
                {
                    if (windAverageData.windDataList[i]?.timestamp.AddMinutes(windAverageData.minutes) < DateTime.UtcNow)
                    {
                        // Trekke fra i total summene
                        windAverageData.windDataDirTotal -= windAverageData.windDataList[i].dir;
                        windAverageData.windDataSpdTotal -= windAverageData.windDataList[i].spd;

                        windAverageData.windDataList.RemoveAt(i--);
                    }
                }

                // Lagre snittet vind retning, hastighet og gust data
                windAverageData.windDir = windAverageData.windDataDirTotal / windAverageData.windDataList.Count;
                windAverageData.windSpeed = windAverageData.windDataSpdTotal / windAverageData.windDataList.Count;

                // GUST
                //////////////////////////////////////////////////////////////////////////////
                bool findNewMaxGust = false;

                // Gust: NOROG
                //////////////////////////////////////////////
                if (adminSettingsVM.regulationStandard == RegulationStandard.NOROG)
                {
                    // NOROG: 10 minuter gust
                    if (windAverageData.minutes == 10)
                    {
                        // Lagre i 3 sekund-listen
                        windAverageData.gust3SecDataList.Add(new Wind()
                        {
                            spd = windSpd.data,
                            timestamp = windSpd.timestamp
                        });

                        // Sjekke om vi skal fjerne data fra 3-sek gust listen
                        for (int i = 0; i < windAverageData.gust3SecDataList.Count && windAverageData.gust3SecDataList.Count > 0; i++)
                        {
                            if (windAverageData.gust3SecDataList[i]?.timestamp.AddSeconds(3) < DateTime.UtcNow)
                                windAverageData.gust3SecDataList.RemoveAt(i--);
                        }

                        // Høyeste av 3 sek gust verdiene
                        double gust3SecHigh = double.MinValue;
                        foreach (var item in windAverageData.gust3SecDataList)
                            if (item.spd > gust3SecHigh)
                                gust3SecHigh = item.spd;

                        // Lagre i gust listen
                        windAverageData.gustDataList.Add(new Wind()
                        {
                            spd = gust3SecHigh,
                            timestamp = windSpd.timestamp
                        });

                        // Ny max verdi?
                        if (gust3SecHigh > windAverageData.windDataGustMax)
                        {
                            windAverageData.windDataGustMax = gust3SecHigh;
                        }

                        // Sjekke om vi skal fjerne data fra gust listen
                        for (int i = 0; i < windAverageData.gustDataList.Count && windAverageData.gustDataList.Count > 0; i++)
                        {
                            if (windAverageData.gustDataList[i]?.timestamp.AddMinutes(windAverageData.minutes) < DateTime.UtcNow)
                            {
                                // Sjekke om dette var max gust
                                if (windAverageData.gustDataList[i].spd == windAverageData.windDataGustMax)
                                    findNewMaxGust = true;

                                windAverageData.gustDataList.RemoveAt(i--);
                            }
                        }

                        // Finne ny max gust
                        if (findNewMaxGust)
                        {
                            double oldMax = windAverageData.windDataGustMax;
                            windAverageData.windDataGustMax = 0;
                            bool foundNewMax = false;

                            for (int i = 0; i < windAverageData.gustDataList.Count && !foundNewMax; i++)
                            {
                                // Kan avslutte søket dersom vi finne en verdi like den gamle maximumsverdien (ingen er høyere)
                                if (windAverageData.gustDataList[i]?.spd == oldMax)
                                {
                                    windAverageData.windDataGustMax = oldMax;
                                    foundNewMax = true;
                                }
                                else
                                {
                                    // Sjekke om data er større enn største lagret
                                    if (windAverageData.gustDataList[i]?.spd > windAverageData.windDataGustMax)
                                        windAverageData.windDataGustMax = windAverageData.gustDataList[i].spd;
                                }
                            }
                        }

                        // Er max gust 10 knop høyere enn snitt vind?
                        if (windAverageData.windDataGustMax >= windAverageData.windSpeed + 10)
                            windAverageData.windGust = windAverageData.windDataGustMax;
                        else
                            windAverageData.windGust = 0;
                    }
                    // NOROG: 2-minutter gust
                    else
                    {
                        // Lagre i gust listen
                        windAverageData.gustDataList.Add(new Wind()
                        {
                            spd = windSpd.data,
                            timestamp = windSpd.timestamp
                        });

                        // Ny max verdi?
                        if (windSpd.data > windAverageData.windDataGustMax)
                        {
                            windAverageData.windDataGustMax = windSpd.data;
                        }

                        // Sjekke om vi skal fjerne data fra gust listen
                        for (int i = 0; i < windAverageData.gustDataList.Count && windAverageData.gustDataList.Count > 0; i++)
                        {
                            if (windAverageData.gustDataList[i]?.timestamp.AddMinutes(windAverageData.minutes) < DateTime.UtcNow)
                            {
                                // Sjekke om dette var max gust
                                if (windAverageData.gustDataList[i].spd == windAverageData.windDataGustMax)
                                    findNewMaxGust = true;

                                windAverageData.gustDataList.RemoveAt(i--);
                            }
                        }

                        // Finne ny max gust
                        if (findNewMaxGust)
                        {
                            double oldMax = windAverageData.windDataGustMax;
                            windAverageData.windDataGustMax = 0;
                            bool foundNewMax = false;

                            for (int i = 0; i < windAverageData.gustDataList.Count && !foundNewMax; i++)
                            {
                                // Kan avslutte søket dersom vi finne en verdi like den gamle maximumsverdien (ingen er høyere)
                                if (windAverageData.gustDataList[i]?.spd == oldMax)
                                {
                                    windAverageData.windDataGustMax = oldMax;
                                    foundNewMax = true;
                                }
                                else
                                {
                                    // Sjekke om data er større enn største lagret
                                    if (windAverageData.gustDataList[i]?.spd > windAverageData.windDataGustMax)
                                        windAverageData.windDataGustMax = windAverageData.gustDataList[i].spd;
                                }
                            }
                        }

                        // Er max gust 10 knop høyere enn snitt vind?
                        if (windAverageData.windDataGustMax >= windAverageData.windSpeed + 10)
                            windAverageData.windGust = windAverageData.windDataGustMax;
                        else
                            windAverageData.windGust = 0;
                    }
                }
                // Gust: CAP
                //////////////////////////////////////////////
                else
                {
                    // Lagre i 3 sekund-listen
                    windAverageData.gust3SecDataList.Add(new Wind()
                    {
                        spd = windSpd.data,
                        timestamp = windSpd.timestamp
                    });

                    // Sjekke om vi skal fjerne data fra 3-sek gust listen
                    for (int i = 0; i < windAverageData.gust3SecDataList.Count && windAverageData.gust3SecDataList.Count > 0; i++)
                    {
                        if (windAverageData.gust3SecDataList[i]?.timestamp.AddSeconds(3) < DateTime.UtcNow)
                            windAverageData.gust3SecDataList.RemoveAt(i--);
                    }

                    // Sum av gust verdiene
                    double gust3SecSum = 0;
                    foreach (var item in windAverageData.gust3SecDataList)
                        gust3SecSum += item.spd;

                    // Snitt
                    double gust3SecMean = gust3SecSum / windAverageData.gust3SecDataList.Count;

                    // Lagre i gust listen
                    windAverageData.gustDataList.Add(new Wind()
                    {
                        spd = gust3SecMean,
                        timestamp = windSpd.timestamp
                    });

                    // Ny max gust verdi?
                    if (gust3SecMean > windAverageData.windDataGustMax)
                    {
                        windAverageData.windDataGustMax = gust3SecMean;
                    }

                    // Sjekke om vi skal fjerne data fra gust listen
                    for (int i = 0; i < windAverageData.gustDataList.Count && windAverageData.gustDataList.Count > 0; i++)
                    {
                        if (windAverageData.gustDataList[0]?.timestamp.AddMinutes(windAverageData.minutes) < DateTime.UtcNow)
                        {
                            // Sjekke om dette var max gust
                            if (windAverageData.gustDataList[i]?.spd == windAverageData.windDataGustMax)
                                findNewMaxGust = true;

                            windAverageData.gustDataList.RemoveAt(i--);
                        }
                    }

                    // Finne ny max gust
                    if (findNewMaxGust)
                    {
                        double oldMax = windAverageData.windDataGustMax;
                        windAverageData.windDataGustMax = 0;
                        bool foundNewMax = false;

                        for (int i = 0; i < windAverageData.gustDataList.Count && !foundNewMax; i++)
                        {
                            // Kan avslutte søket dersom vi finne en verdi like den gamle maximumsverdien (ingen er høyere)
                            if (windAverageData.gustDataList[i]?.spd == oldMax)
                            {
                                windAverageData.windDataGustMax = oldMax;
                            }
                            else
                            {
                                // Sjekke om data er større enn største lagret
                                if (windAverageData.gustDataList[i]?.spd > windAverageData.windDataGustMax)
                                    windAverageData.windDataGustMax = windAverageData.gustDataList[i].spd;
                            }
                        }
                    }

                    // Sette gust
                    windAverageData.windGust = windAverageData.windDataGustMax;
                }
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
