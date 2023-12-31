﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace SensorMonitor
{
    public class HMSSensorStatus
    {
        private RadObservableCollectionEx<SensorGroup> hmsSensorList = new RadObservableCollectionEx<SensorGroup>();
        private HMSDataCollection hmsOutputDataList;

        // Config
        private Config config;

        public HMSSensorStatus(Config config, HMSDataCollection hmsOutputDataList)
        {
            this.config = config;
            this.hmsOutputDataList = hmsOutputDataList;

            // Hente liste med data fra fil
            SensorGroupIDConfigCollection sensorIDConfigCollection = config.GetSensorGroupIDDataList();

            if (sensorIDConfigCollection != null)
            {
                // Legge alle data inn i listview for visning på skjerm
                foreach (SensorGroupIDConfig item in sensorIDConfigCollection)
                {
                    SensorGroup sensor = new SensorGroup(item);
                    hmsSensorList.Add(sensor);
                }
            }

            // Update Sensor Status
            DispatcherTimer timer = new DispatcherTimer();

            timer.Interval = TimeSpan.FromMilliseconds(Constants.ServerUpdateFrequencyUI);
            timer.Tick += UpdateSensorStatus;
            timer.Start();

            void UpdateSensorStatus(object sender, EventArgs e)
            {
                // Løper gjennom alle sensorene...
                for (int i = 0; i < Constants.MaxSensors; i++)
                    //  ...og sjekker om de tilknyttede verdiene har error status
                    UpdateStatus(i);
            }
        }

        public RadObservableCollectionEx<SensorGroup> GetSensorList()
        {
            return hmsSensorList;
        }

        public void SetSensorName(SensorGroup sensor)
        {
            if (sensor.id < hmsSensorList.Count)
            {
                hmsSensorList[sensor.id].name = sensor.name;

                // Lagre til fil
                config.SetSensorGroupIDData(sensor);
            }
        }

        private void UpdateStatus(int id)
        {
            // Finne frem sensoren vi skal oppdatere status for
            var sensor = hmsSensorList.Where(x => x.id == id);
            if (sensor.Count() == 1)
            {
                // Finne sensor verdier knyttet til valgt sensor ID
                var hmsDataList = hmsOutputDataList.GetDataList();
                lock (hmsDataList)
                {
                    var clientData = hmsDataList.Where(x => x.sensorGroupId == id);

                    // Har funnet verdier knyttet til sensor
                    if (clientData.Count() > 0)
                    {
                        // Skjekke om noen av sensor verdiene har error status
                        var errorList = clientData.Where(x => x.dataStatus == DataStatus.TIMEOUT_ERROR);

                        // En eller flere error statuser funnet
                        if (errorList.Count() > 0)
                            sensor.First().status = DataStatus.TIMEOUT_ERROR;
                        // Ingen error status funnet
                        else
                            sensor.First().status = DataStatus.OK;
                    }
                    // Ingen verdier knyttet til denne sensoren -> ingen feilmelding
                    else
                    {
                        sensor.First().status = DataStatus.OK;
                    }
                }
            }
        }

        public DataStatus GetStatus(int id)
        {
            var sensor = hmsSensorList.Where(x => x.id == id);
            if (sensor.Count() == 1)
            {
                return sensor.First().status;
            }
            else
            {
                return DataStatus.TIMEOUT_ERROR;
            }
        }

        public bool StatusChanged(int id)
        {
            // Sjekk på om status er endret
            var sensor = hmsSensorList.Where(x => x.id == id);
            if (sensor.Count() == 1)
            {
                if (sensor.First().previousStatus != sensor.First().status)
                    return true;
                else
                    return false;
            }
            else
            {
                return false;
            }
        }

        public void StatusChangedReset(int id)
        {
            var sensor = hmsSensorList.Where(x => x.id == id);
            if (sensor.Count() == 1)
            {
                sensor.First().previousStatus = sensor.First().status;
            }
        }

        public bool IsActive(int id)
        {
            var sensor = hmsSensorList.Where(x => x.id == id);
            if (sensor.Count() == 1)
                return sensor.First().active;
            else
                return false;
        }
    }
}
