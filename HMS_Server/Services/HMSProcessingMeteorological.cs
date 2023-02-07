﻿using System;
using Telerik.Windows.Data;

namespace HMS_Server
{
    class HMSProcessingMeteorological
    {
        // Sensor Data
        private HMSData sensorAirTemperature = new HMSData();
        private HMSData sensorAirHumidity = new HMSData();
        private HMSData sensorAirPressure = new HMSData();
        private HMSData sensorVisibility = new HMSData();
        private HMSData sensorWeather = new HMSData();
        private HMSData sensorCloudLayer1Base = new HMSData();
        private HMSData sensorCloudLayer2Base = new HMSData();
        private HMSData sensorCloudLayer3Base = new HMSData();
        private HMSData sensorCloudLayer4Base = new HMSData();
        private HMSData sensorCloudLayer1Coverage = new HMSData();
        private HMSData sensorCloudLayer2Coverage = new HMSData();
        private HMSData sensorCloudLayer3Coverage = new HMSData();
        private HMSData sensorCloudLayer4Coverage = new HMSData();

        private HMSData airTemperature = new HMSData();
        private HMSData airHumidity = new HMSData();
        private HMSData airDewPoint = new HMSData();
        private HMSData airPressureQFE = new HMSData();
        private HMSData airPressureQNH = new HMSData();

        private HMSData Visibility = new HMSData();

        private HMSData weatherPhenomena = new HMSData();

        private HMSData cloudLayer1Base = new HMSData();
        private HMSData cloudLayer1Coverage = new HMSData();
        private HMSData cloudLayer2Base = new HMSData();
        private HMSData cloudLayer2Coverage = new HMSData();
        private HMSData cloudLayer3Base = new HMSData();
        private HMSData cloudLayer3Coverage = new HMSData();
        private HMSData cloudLayer4Base = new HMSData();
        private HMSData cloudLayer4Coverage = new HMSData();

        private AdminSettingsVM adminSettingsVM;

        public HMSProcessingMeteorological(HMSDataCollection hmsOutputData, AdminSettingsVM adminSettingsVM)
        {
            this.adminSettingsVM = adminSettingsVM;

            // Fyller output listen med HMS Output data
            // NB! Variablene som legges inn i listen her fungerer som pekere: Oppdateres variabelen (Update funksjonen under) -> oppdateres listen
            // NB! Dersom nye variabler legges til i hmsOutputDataList må databasen opprettes på nytt

            RadObservableCollection<HMSData> hmsOutputDataList = hmsOutputData.GetDataList();

            hmsOutputDataList.Add(airTemperature);
            hmsOutputDataList.Add(airHumidity);

            hmsOutputDataList.Add(airDewPoint);
            airDewPoint.id = (int)ValueType.AirDewPoint;
            airDewPoint.name = "Air Dew Point";
            airDewPoint.sensorGroupId = Constants.NO_SENSOR_GROUP_ID;
            airDewPoint.dbColumn = "air_dew_point";

            hmsOutputDataList.Add(airPressureQFE);
            airPressureQFE.id = (int)ValueType.AirPressureQFE;
            airPressureQFE.name = "Air Pressure QFE";
            airPressureQFE.sensorGroupId = Constants.NO_SENSOR_GROUP_ID;
            airPressureQFE.dbColumn = "air_pressure_qfe";

            hmsOutputDataList.Add(airPressureQNH);
            airPressureQNH.id = (int)ValueType.AirPressureQNH;
            airPressureQNH.name = "Air Pressure QNH";
            airPressureQNH.sensorGroupId = Constants.NO_SENSOR_GROUP_ID;
            airPressureQNH.dbColumn = "air_pressure_qnh";

            hmsOutputDataList.Add(Visibility);

            hmsOutputDataList.Add(weatherPhenomena);

            hmsOutputDataList.Add(cloudLayer1Base);
            hmsOutputDataList.Add(cloudLayer1Coverage);
            hmsOutputDataList.Add(cloudLayer2Base);
            hmsOutputDataList.Add(cloudLayer2Coverage);
            hmsOutputDataList.Add(cloudLayer3Base);
            hmsOutputDataList.Add(cloudLayer3Coverage);
            hmsOutputDataList.Add(cloudLayer4Base);
            hmsOutputDataList.Add(cloudLayer4Coverage);
        }

        public void Update(HMSDataCollection hmsInputDataList)
        {
            // Tar data fra input delen av server og overfører til HMS output delen

            // Hente sensor data
            sensorAirTemperature.Set(hmsInputDataList.GetData(ValueType.AirTemperature));
            sensorAirHumidity.Set(hmsInputDataList.GetData(ValueType.AirHumidity));
            sensorAirPressure.Set(hmsInputDataList.GetData(ValueType.AirPressure));
            sensorVisibility.Set(hmsInputDataList.GetData(ValueType.Visibility));
            sensorWeather.Set(hmsInputDataList.GetData(ValueType.Weather));
            sensorCloudLayer1Base.Set(hmsInputDataList.GetData(ValueType.CloudLayer1Base));
            sensorCloudLayer2Base.Set(hmsInputDataList.GetData(ValueType.CloudLayer2Base));
            sensorCloudLayer3Base.Set(hmsInputDataList.GetData(ValueType.CloudLayer3Base));
            sensorCloudLayer4Base.Set(hmsInputDataList.GetData(ValueType.CloudLayer4Base));
            sensorCloudLayer1Coverage.Set(hmsInputDataList.GetData(ValueType.CloudLayer1Coverage));
            sensorCloudLayer2Coverage.Set(hmsInputDataList.GetData(ValueType.CloudLayer2Coverage));
            sensorCloudLayer3Coverage.Set(hmsInputDataList.GetData(ValueType.CloudLayer3Coverage));
            sensorCloudLayer4Coverage.Set(hmsInputDataList.GetData(ValueType.CloudLayer4Coverage));

            if (sensorAirTemperature.TimeStampCheck ||
                sensorAirHumidity.TimeStampCheck ||
                sensorAirPressure.TimeStampCheck ||
                sensorVisibility.TimeStampCheck ||
                sensorWeather.TimeStampCheck ||
                sensorCloudLayer1Base.TimeStampCheck ||
                sensorCloudLayer2Base.TimeStampCheck ||
                sensorCloudLayer3Base.TimeStampCheck ||
                sensorCloudLayer4Base.TimeStampCheck ||
                sensorCloudLayer1Coverage.TimeStampCheck ||
                sensorCloudLayer2Coverage.TimeStampCheck ||
                sensorCloudLayer3Coverage.TimeStampCheck ||
                sensorCloudLayer4Coverage.TimeStampCheck)
            {

                // Temperature & Humidity
                airTemperature.Set(sensorAirTemperature);
                airHumidity.Set(sensorAirHumidity);
                CalculateDewPoint();

                // Air pressure
                airPressureQFE.data = Math.Round(CalculateQFE(sensorAirPressure.data, airTemperature.data, adminSettingsVM.airPressureSensorHeight - adminSettingsVM.helideckHeight), 1, MidpointRounding.AwayFromZero);
                airPressureQFE.status = sensorAirPressure.status;
                airPressureQFE.timestamp = sensorAirPressure.timestamp;

                airPressureQNH.data = Math.Round(CalculateQNH(airPressureQFE.data, adminSettingsVM.helideckHeight), 1, MidpointRounding.AwayFromZero);
                airPressureQNH.status = sensorAirPressure.status;
                airPressureQNH.timestamp = sensorAirPressure.timestamp;

                // Visibility
                Visibility.Set(sensorVisibility);

                // Weather
                weatherPhenomena.Set(sensorWeather);

                // Clouds
                cloudLayer1Base.Set(sensorCloudLayer1Base);
                cloudLayer1Coverage.Set(sensorCloudLayer1Coverage);
                cloudLayer2Base.Set(sensorCloudLayer2Base);
                cloudLayer2Coverage.Set(sensorCloudLayer2Coverage);
                cloudLayer3Base.Set(sensorCloudLayer3Base);
                cloudLayer3Coverage.Set(sensorCloudLayer3Coverage);
                cloudLayer4Base.Set(sensorCloudLayer4Base);
                cloudLayer4Coverage.Set(sensorCloudLayer4Coverage);

                // Sjekke data timeout
                if (airTemperature.timestamp.AddMilliseconds(adminSettingsVM.dataTimeout) < DateTime.UtcNow)
                    airTemperature.status = DataStatus.TIMEOUT_ERROR;

                if (airHumidity.timestamp.AddMilliseconds(adminSettingsVM.dataTimeout) < DateTime.UtcNow)
                    airHumidity.status = DataStatus.TIMEOUT_ERROR;

                if (airPressureQFE.timestamp.AddMilliseconds(adminSettingsVM.dataTimeout) < DateTime.UtcNow)
                    airPressureQFE.status = DataStatus.TIMEOUT_ERROR;

                if (airPressureQNH.timestamp.AddMilliseconds(adminSettingsVM.dataTimeout) < DateTime.UtcNow)
                    airPressureQNH.status = DataStatus.TIMEOUT_ERROR;

                if (Visibility.timestamp.AddMilliseconds(adminSettingsVM.dataTimeout) < DateTime.UtcNow)
                    Visibility.status = DataStatus.TIMEOUT_ERROR;

                if (weatherPhenomena.timestamp.AddMilliseconds(adminSettingsVM.dataTimeout) < DateTime.UtcNow)
                    weatherPhenomena.status = DataStatus.TIMEOUT_ERROR;

                if (cloudLayer1Base.timestamp.AddMilliseconds(adminSettingsVM.dataTimeout) < DateTime.UtcNow)
                    cloudLayer1Base.status = DataStatus.TIMEOUT_ERROR;

                if (cloudLayer1Coverage.timestamp.AddMilliseconds(adminSettingsVM.dataTimeout) < DateTime.UtcNow)
                    cloudLayer1Coverage.status = DataStatus.TIMEOUT_ERROR;

                if (cloudLayer2Base.timestamp.AddMilliseconds(adminSettingsVM.dataTimeout) < DateTime.UtcNow)
                    cloudLayer2Base.status = DataStatus.TIMEOUT_ERROR;

                if (cloudLayer2Coverage.timestamp.AddMilliseconds(adminSettingsVM.dataTimeout) < DateTime.UtcNow)
                    cloudLayer2Coverage.status = DataStatus.TIMEOUT_ERROR;

                if (cloudLayer3Base.timestamp.AddMilliseconds(adminSettingsVM.dataTimeout) < DateTime.UtcNow)
                    cloudLayer3Base.status = DataStatus.TIMEOUT_ERROR;

                if (cloudLayer3Coverage.timestamp.AddMilliseconds(adminSettingsVM.dataTimeout) < DateTime.UtcNow)
                    cloudLayer3Coverage.status = DataStatus.TIMEOUT_ERROR;

                if (cloudLayer4Base.timestamp.AddMilliseconds(adminSettingsVM.dataTimeout) < DateTime.UtcNow)
                    cloudLayer4Base.status = DataStatus.TIMEOUT_ERROR;

                if (cloudLayer4Coverage.timestamp.AddMilliseconds(adminSettingsVM.dataTimeout) < DateTime.UtcNow)
                    cloudLayer4Coverage.status = DataStatus.TIMEOUT_ERROR;
            }
        }

        public void CalculateDewPoint()
        {
            double temp = airTemperature.data;
            double humi = airHumidity.data;

            // Beregne dew point
            airDewPoint.data = Math.Round(temp - (14.55 + 0.114 * temp) * (1 - (0.01 * humi)) - Math.Pow((2.5 + 0.007 * temp) * (1 - (0.01 * humi)), 3) - (15.9 + 0.117 * temp) * Math.Pow(1 - (0.01 * humi), 14), 1, MidpointRounding.AwayFromZero);

            // Timestamp og status
            airDewPoint.timestamp = airTemperature.timestamp;
            airDewPoint.status = airTemperature.status;
        }

        private double CalculateQFE(double measuredPressure, double measuredTemperature, double heightAboveStation)
        {
            // Formler funnet her: https://www.metpod.co.uk/calculators/pressure/
            // NB! Har brukt mer nøyaktige konstanter for gravitasjonsakselerasjon og spesifikk gass konstant for tørr luft

            double qfe;
            double temperatureKelvin = measuredTemperature + Constants.KelvinZero;

            if (temperatureKelvin != 0)
                qfe = measuredPressure * (1 + ((Constants.GravityAcceleration * heightAboveStation) / (Constants.SpecificGasConstantDryAir * temperatureKelvin)));
            else
                qfe = measuredPressure;

            return qfe;
        }

        private double CalculateQNH(double qfe, double heightStation)
        {
            // Formler funnet her: https://www.metpod.co.uk/calculators/pressure/
            // NB! Har brukt mer nøyaktige konstanter for gravitasjonsakselerasjon og spesifikk gass konstant for tørr luft

            double qnh;
            double heightISA = 44330.77 - (11880.32 * Math.Pow(qfe, 0.190263));

            qnh = Constants.StandardPressure * Math.Pow(1 - (Constants.MeanAdiabaticLapseRate * ((heightISA - heightStation) / Constants.SpecificGasConstantDryAir)), 5.25588);

            return qnh;
        }
    }
}
