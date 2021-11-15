using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        // Data lister for vind-snittberegninger
        private WindAverageData areaWindAverageData2m = new WindAverageData();
        private WindAverageData areaWindAverageData10m = new WindAverageData();
        private WindAverageData helideckWindAverageData2m = new WindAverageData();
        private WindAverageData helideckWindAverageData10m = new WindAverageData();

        private AdminSettingsVM adminSettingsVM;
        private UserInputs userInputs;

        public HMSProcessingWindHeading(HMSDataCollection hmsOutputData, AdminSettingsVM adminSettingsVM, UserInputs userInputs)
        {
            this.adminSettingsVM = adminSettingsVM;
            this.userInputs = userInputs;

            // Fyller output listen med HMS Output data
            // NB! Variablene som legges inn i listen her fungerer som pekere: Oppdateres variabelen -> oppdateres listen

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

            areaWindAverageData2m.minutes = 2;
            areaWindAverageData10m.minutes = 10;
            helideckWindAverageData2m.minutes = 2;
            helideckWindAverageData10m.minutes = 10;

            areaWindDirection2m.id = (int)ValueType.AreaWindDirection2m;
            areaWindDirection2m.name = "Area Wind Direction (2m)";
            areaWindDirection2m.dbTableName = "area_wind_direction_2m";

            areaWindSpeed2m.id = (int)ValueType.AreaWindSpeed2m;
            areaWindSpeed2m.name = "Area Wind Speed (2m)";
            areaWindSpeed2m.sensorGroupId = Constants.NO_SENSOR_GROUP_ID;
            areaWindSpeed2m.dbTableName = "area_wind_speed_2m";

            areaWindGust2m.id = (int)ValueType.AreaWindGust2m;
            areaWindGust2m.name = "Area Wind Gust (2m)";
            areaWindGust2m.sensorGroupId = Constants.NO_SENSOR_GROUP_ID;
            areaWindGust2m.dbTableName = "area_wind_gust_2m";

            helideckWindDirectionRT.id = (int)ValueType.HelideckWindDirectionRT;
            helideckWindDirectionRT.name = "Helideck Wind Direction (RT)";
            helideckWindDirectionRT.sensorGroupId = Constants.NO_SENSOR_GROUP_ID;
            helideckWindDirectionRT.dbTableName = "helideck_wind_direction_rt";

            helideckWindDirection2m.id = (int)ValueType.HelideckWindDirection2m;
            helideckWindDirection2m.name = "Helideck Wind Direction (2m)";
            helideckWindDirection2m.sensorGroupId = Constants.NO_SENSOR_GROUP_ID;
            helideckWindDirection2m.dbTableName = "helideck_wind_direction_2m";

            helideckWindDirection10m.id = (int)ValueType.HelideckWindDirection10m;
            helideckWindDirection10m.name = "Helideck Wind Direction (10m)";
            helideckWindDirection10m.sensorGroupId = Constants.NO_SENSOR_GROUP_ID;
            helideckWindDirection10m.dbTableName = "helideck_wind_direction_10m";

            helideckWindSpeedRT.id = (int)ValueType.HelideckWindSpeedRT;
            helideckWindSpeedRT.name = "Helideck Wind Speed (RT)";
            helideckWindSpeedRT.sensorGroupId = Constants.NO_SENSOR_GROUP_ID;
            helideckWindSpeedRT.dbTableName = "helideck_wind_speed_rt";

            helideckWindSpeed2m.id = (int)ValueType.HelideckWindSpeed2m;
            helideckWindSpeed2m.name = "Helideck Wind Speed (2m)";
            helideckWindSpeed2m.sensorGroupId = Constants.NO_SENSOR_GROUP_ID;
            helideckWindSpeed2m.dbTableName = "helideck_wind_speed_2m";

            helideckWindSpeed10m.id = (int)ValueType.HelideckWindSpeed10m;
            helideckWindSpeed10m.name = "Helideck Wind Speed (10m)";
            helideckWindSpeed10m.sensorGroupId = Constants.NO_SENSOR_GROUP_ID;
            helideckWindSpeed10m.dbTableName = "helideck_wind_speed_10m";

            helideckWindGust2m.id = (int)ValueType.HelideckWindGust2m;
            helideckWindGust2m.name = "Helideck Wind Gust (2m)";
            helideckWindGust2m.sensorGroupId = Constants.NO_SENSOR_GROUP_ID;
            helideckWindGust2m.dbTableName = "helideck_wind_gust_2m";

            helideckWindGust10m.id = (int)ValueType.HelideckWindGust10m;
            helideckWindGust10m.name = "Helideck Wind Gust (10m)";
            helideckWindGust10m.sensorGroupId = Constants.NO_SENSOR_GROUP_ID;
            helideckWindGust10m.dbTableName = "helideck_wind_gust_10m";


            relativeWindDir.id = (int)ValueType.RelativeWindDir;
            relativeWindDir.name = "Relative Wind Direction (CAP)";
            relativeWindDir.sensorGroupId = Constants.NO_SENSOR_GROUP_ID;
            relativeWindDir.dbTableName = "relative_wind_direction";

            helideckHeading.id = (int)ValueType.HelideckHeading;
            helideckHeading.name = "Helideck Heading";
            helideckHeading.sensorGroupId = Constants.NO_SENSOR_GROUP_ID;
            helideckHeading.dbTableName = "helideck_heading";

            vesselHeadingDelta.id = (int)ValueType.VesselHeadingDelta;
            vesselHeadingDelta.name = "Vessel Heading (Delta) (CAP)";
            vesselHeadingDelta.sensorGroupId = Constants.NO_SENSOR_GROUP_ID;
            vesselHeadingDelta.dbTableName = "vessel_heading_delta";

            windDirectionDelta.id = (int)ValueType.WindDirectionDelta;
            windDirectionDelta.name = "Wind Direction (Delta) (CAP)";
            windDirectionDelta.sensorGroupId = Constants.NO_SENSOR_GROUP_ID;
            windDirectionDelta.dbTableName = "wind_direction_delta";
        }

        public void Update(HMSDataCollection hmsInputDataList)
        {
            // Tar data fra input delen av server og overfører til HMS output delen

            helideckWindDirectionRT.data = hmsInputDataList.GetData(ValueType.AreaWindDirectionRT).data;
            helideckWindDirectionRT.status = hmsInputDataList.GetData(ValueType.AreaWindDirectionRT).status;
            helideckWindDirectionRT.timestamp = hmsInputDataList.GetData(ValueType.AreaWindDirectionRT).timestamp;

            // Korrigerer for høyde
            helideckWindSpeedRT.data = Math.Round(WindSpeedCorrection(hmsInputDataList.GetData(ValueType.AreaWindSpeedRT).data), 1);
            helideckWindSpeedRT.status = hmsInputDataList.GetData(ValueType.AreaWindSpeedRT).status;
            helideckWindSpeedRT.timestamp = hmsInputDataList.GetData(ValueType.AreaWindSpeedRT).timestamp;

            // 2-minute data
            ///////////////////////////////////////////////////////////
            UpdateWindAverages(
                hmsInputDataList.GetData(ValueType.AreaWindDirectionRT),
                hmsInputDataList.GetData(ValueType.AreaWindSpeedRT),
                areaWindAverageData2m);

            areaWindDirection2m.data = areaWindAverageData2m.windDir;
            areaWindDirection2m.status = hmsInputDataList.GetData(ValueType.AreaWindDirectionRT).status;
            areaWindDirection2m.timestamp = hmsInputDataList.GetData(ValueType.AreaWindDirectionRT).timestamp;
            areaWindDirection2m.sensorGroupId = hmsInputDataList.GetData(ValueType.AreaWindDirectionRT).sensorGroupId;


            areaWindSpeed2m.data = Math.Round(areaWindAverageData2m.windSpeed, 1);
            areaWindSpeed2m.status = hmsInputDataList.GetData(ValueType.AreaWindSpeedRT).status;
            areaWindSpeed2m.timestamp = hmsInputDataList.GetData(ValueType.AreaWindSpeedRT).timestamp;
            areaWindSpeed2m.sensorGroupId = hmsInputDataList.GetData(ValueType.AreaWindDirectionRT).sensorGroupId;

            areaWindGust2m.data = areaWindAverageData2m.windGust;
            areaWindGust2m.status = hmsInputDataList.GetData(ValueType.AreaWindSpeedRT).status;
            areaWindGust2m.timestamp = hmsInputDataList.GetData(ValueType.AreaWindSpeedRT).timestamp;
            areaWindGust2m.sensorGroupId = hmsInputDataList.GetData(ValueType.AreaWindDirectionRT).sensorGroupId;

            UpdateWindAverages(
                helideckWindDirectionRT,
                helideckWindSpeedRT,
                helideckWindAverageData2m);

            helideckWindDirection2m.data = helideckWindAverageData2m.windDir;
            helideckWindDirection2m.status = helideckWindDirectionRT.status;
            helideckWindDirection2m.timestamp = helideckWindDirectionRT.timestamp;

            helideckWindSpeed2m.data = Math.Round(helideckWindAverageData2m.windSpeed, 1);
            helideckWindSpeed2m.status = helideckWindSpeedRT.status;
            helideckWindSpeed2m.timestamp = helideckWindSpeedRT.timestamp;

            helideckWindGust2m.data = Math.Round(helideckWindAverageData2m.windGust, 1);
            helideckWindGust2m.status = helideckWindSpeedRT.status;
            helideckWindGust2m.timestamp = helideckWindSpeedRT.timestamp;

            // 10-minute data
            ///////////////////////////////////////////////////////////
            UpdateWindAverages(
                helideckWindDirectionRT,
                helideckWindSpeedRT,
                helideckWindAverageData10m);

            helideckWindDirection10m.data = helideckWindAverageData10m.windDir;
            helideckWindDirection10m.status = helideckWindDirectionRT.status;
            helideckWindDirection10m.timestamp = helideckWindDirectionRT.timestamp;

            helideckWindSpeed10m.data = Math.Round(helideckWindAverageData10m.windSpeed, 1);
            helideckWindSpeed10m.status = helideckWindSpeedRT.status;
            helideckWindSpeed10m.timestamp = helideckWindSpeedRT.timestamp;

            helideckWindGust10m.data = Math.Round(helideckWindAverageData10m.windGust, 1);
            helideckWindGust10m.status = helideckWindSpeedRT.status;
            helideckWindGust10m.timestamp = helideckWindSpeedRT.timestamp;


            vesselHeading.Set(hmsInputDataList.GetData(ValueType.VesselHeading));
            vesselSpeed.Set(hmsInputDataList.GetData(ValueType.VesselSpeed));

            double heading = vesselHeading.data + adminSettingsVM.helideckHeadingOffset;
            if (heading >= 360)
                heading -= 360;
            helideckHeading.data = heading;
            helideckHeading.status = vesselHeading.status;
            helideckHeading.timestamp = vesselHeading.timestamp;

            if (adminSettingsVM.regulationStandard == RegulationStandard.CAP &&
                userInputs.displayMode == DisplayMode.OnDeck)
            {
                // Relative Wind Direction
                /////////////////////////////////////////////////////////////////////////////////////////
                if (areaWindDirection2m.status == DataStatus.OK &&
                    vesselHeading.status == DataStatus.OK)
                {
                    relativeWindDir.data = areaWindDirection2m.data - (vesselHeading.data + (userInputs.onDeckHelicopterHeading - userInputs.onDeckVesselHeading));
                }
                else
                {
                    relativeWindDir.data = 0;
                }

                relativeWindDir.status = areaWindDirection2m.status;
                relativeWindDir.timestamp = areaWindDirection2m.timestamp;

                // Vessel Heading Delta
                /////////////////////////////////////////////////////////////////////////////////////////
                if (vesselHeading.status == DataStatus.OK &&
                    userInputs.onDeckTime != DateTime.MinValue &&
                    userInputs.onDeckVesselHeading != -1)
                {
                    vesselHeadingDelta.data = vesselHeading.data - userInputs.onDeckVesselHeading;
                }
                else
                {
                    vesselHeadingDelta.data = 0;
                }

                vesselHeadingDelta.status = vesselHeading.status;
                vesselHeadingDelta.timestamp = vesselHeading.timestamp;

                // Wind Direction Delta
                /////////////////////////////////////////////////////////////////////////////////////////
                if (areaWindDirection2m.status == DataStatus.OK &&
                    userInputs.onDeckTime != DateTime.MinValue &&
                    userInputs.onDeckWindDirection != -1)
                {
                    windDirectionDelta.data = areaWindDirection2m.data - userInputs.onDeckWindDirection;
                }
                else
                {
                    windDirectionDelta.data = 0;
                }

                windDirectionDelta.status = areaWindDirection2m.status;
                windDirectionDelta.timestamp = areaWindDirection2m.timestamp;
            }
            else
            {
                //  Dersom disse dataene ikke trenger å beregnes, men status og timestamp må likevel settes slik at det ikke ser ut som feil
                relativeWindDir.data = 0;
                relativeWindDir.status = areaWindDirection2m.status;
                relativeWindDir.timestamp = areaWindDirection2m.timestamp;

                vesselHeadingDelta.data = 0;
                vesselHeadingDelta.status = vesselHeading.status;
                vesselHeadingDelta.timestamp = vesselHeading.timestamp;

                windDirectionDelta.data = 0;
                windDirectionDelta.status = areaWindDirection2m.status;
                windDirectionDelta.timestamp = areaWindDirection2m.timestamp;
            }
        }

        private void UpdateWindAverages(HMSData windDir, HMSData windSpd, WindAverageData windAverageData)
        {
            // Sjekker status på data først
            if (windDir?.status == DataStatus.OK &&
                windSpd?.status == DataStatus.OK)
            {
                // Lagre i datalisten
                windAverageData.windDataList.Add(new Wind()
                {
                    dir = windDir.data,
                    spd = windSpd.data,
                    timestamp = windDir.timestamp
                });

                // Legg til i total summene for retning og hastighet
                windAverageData.windDataDirTotal += windDir.data;
                windAverageData.windDataSpdTotal += windSpd.data;

                // Gust
                if (windSpd.data > windAverageData.windDataSpdMax)
                {
                    windAverageData.windDataSpdMax = windSpd.data;
                }

                // Sjekke om vi skal fjerne data fra data listen
                bool done = false;
                bool findNewMaxGust = false;

                for (int i = 0; i < windAverageData.windDataList.Count && i >= 0 && !done; i++)
                {
                    if (windAverageData.windDataList[i]?.timestamp.AddMinutes(windAverageData.minutes) < DateTime.UtcNow)
                    {
                        // Trekke fra i total summene
                        windAverageData.windDataDirTotal -= windAverageData.windDataList[i].dir;
                        windAverageData.windDataSpdTotal -= windAverageData.windDataList[i].spd;

                        // Sjekke om dette var max gust
                        if (windAverageData.windDataList[i].spd == windAverageData.windDataSpdMax)
                            findNewMaxGust = true;

                        windAverageData.windDataList.RemoveAt(i--);
                    }
                    else
                    {
                        done = true;
                    }
                }

                // Finne ny max gust
                if (findNewMaxGust)
                {
                    double oldMax = windAverageData.windDataSpdMax;
                    windAverageData.windDataSpdMax = 0;

                    for (int j = 0; j < windAverageData.windDataList.Count && !done; j++)
                    {
                        // Kan avslutte søket dersom vi finne en verdi like den gamle maximumsverdien (ingen er høyere)
                        if (windAverageData.windDataList[j]?.spd == oldMax)
                        {
                            windAverageData.windDataSpdMax = oldMax;
                            done = true;
                        }
                        else
                        {
                            // Sjekke om data er større enn største lagret
                            if (windAverageData.windDataList[j]?.spd > windAverageData.windDataSpdMax)
                                windAverageData.windDataSpdMax = windAverageData.windDataList[j].spd;
                        }
                    }
                }

                // Lagre snittet vind retning, hastighet og gust data
                windAverageData.windDir = windAverageData.windDataDirTotal / windAverageData.windDataList.Count;
                windAverageData.windSpeed = windAverageData.windDataSpdTotal / windAverageData.windDataList.Count;

                if (windAverageData.windDataSpdMax >= windAverageData.windSpeed + 10)
                    windAverageData.windGust = windAverageData.windDataSpdMax;
                else
                    windAverageData.windGust = 0;
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

        private double WindSpeedCorrection(double windSpeed)
        {
            // Power law approximation of a marine atmospheric boundary layer
            if (adminSettingsVM.windSensorHeight != 0)
                return windSpeed * Math.Pow((adminSettingsVM.helideckHeight + 10) / adminSettingsVM.windSensorHeight, 0.13);
            else
                return windSpeed;
        }
    }
}
