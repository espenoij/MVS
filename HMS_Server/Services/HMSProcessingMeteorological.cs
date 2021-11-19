using System;

namespace HMS_Server
{
    class HMSProcessingMeteorological
    {
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

            RadObservableCollectionEx<HMSData> hmsOutputDataList = hmsOutputData.GetDataList();

            hmsOutputDataList.Add(airTemperature);
            hmsOutputDataList.Add(airHumidity);

            hmsOutputDataList.Add(airDewPoint);
            airDewPoint.id = (int)ValueType.AirDewPoint;
            airDewPoint.name = "Air Dew Point";
            airDewPoint.sensorGroupId = Constants.NO_SENSOR_GROUP_ID;
            airDewPoint.dbTableName = "air_dew_point";

            hmsOutputDataList.Add(airPressureQFE);
            airPressureQFE.id = (int)ValueType.AirPressureQFE;
            airPressureQFE.name = "Air Pressure QFE";
            airPressureQFE.sensorGroupId = Constants.NO_SENSOR_GROUP_ID;
            airPressureQFE.dbTableName = "air_pressure_qfe";

            hmsOutputDataList.Add(airPressureQNH);
            airPressureQNH.id = (int)ValueType.AirPressureQNH;
            airPressureQNH.name = "Air Pressure QNH";
            airPressureQNH.sensorGroupId = Constants.NO_SENSOR_GROUP_ID;
            airPressureQNH.dbTableName = "air_pressure_qnh";

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

            airTemperature.Set(hmsInputDataList.GetData(ValueType.AirTemperature));
            airHumidity.Set(hmsInputDataList.GetData(ValueType.AirHumidity));
            CalculateDewPoint();

            airPressureQFE.data = Math.Round(CalculateQFE(hmsInputDataList.GetData(ValueType.AirPressure).data, airTemperature.data, adminSettingsVM.airPressureSensorHeight - adminSettingsVM.helideckHeight), 1);
            airPressureQFE.status = hmsInputDataList.GetData(ValueType.AirPressure).status;
            airPressureQFE.timestamp = hmsInputDataList.GetData(ValueType.AirPressure).timestamp;

            airPressureQNH.data = Math.Round(CalculateQNH(airPressureQFE.data, adminSettingsVM.helideckHeight), 1);
            airPressureQNH.status = hmsInputDataList.GetData(ValueType.AirPressure).status;
            airPressureQNH.timestamp = hmsInputDataList.GetData(ValueType.AirPressure).timestamp;

            Visibility.Set(hmsInputDataList.GetData(ValueType.Visibility));

            weatherPhenomena.Set(hmsInputDataList.GetData(ValueType.Weather));

            cloudLayer1Base.Set(hmsInputDataList.GetData(ValueType.CloudLayer1Base));
            cloudLayer1Coverage.Set(hmsInputDataList.GetData(ValueType.CloudLayer1Coverage));
            cloudLayer2Base.Set(hmsInputDataList.GetData(ValueType.CloudLayer2Base));
            cloudLayer2Coverage.Set(hmsInputDataList.GetData(ValueType.CloudLayer2Coverage));
            cloudLayer3Base.Set(hmsInputDataList.GetData(ValueType.CloudLayer3Base));
            cloudLayer3Coverage.Set(hmsInputDataList.GetData(ValueType.CloudLayer3Coverage));
            cloudLayer4Base.Set(hmsInputDataList.GetData(ValueType.CloudLayer4Base));
            cloudLayer4Coverage.Set(hmsInputDataList.GetData(ValueType.CloudLayer4Coverage));
        }

        public void CalculateDewPoint()
        {
            double temp = airTemperature.data;
            double humi = airHumidity.data;

            // Beregne dew point
            airDewPoint.data = Math.Round(temp - (14.55 + 0.114 * temp) * (1 - (0.01 * humi)) - Math.Pow((2.5 + 0.007 * temp) * (1 - (0.01 * humi)), 3) - (15.9 + 0.117 * temp) * Math.Pow(1 - (0.01 * humi), 14), 1);

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
