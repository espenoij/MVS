using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace SensorMonitorClient
{
    public class SensorStatus
    {
        private RadObservableCollectionEx<Sensor> sensorList = new RadObservableCollectionEx<Sensor>();
        private HMSDataCollection clientSensorData;

        private double dataTimeout;

        public SensorStatus(Config config, HMSDataCollection clientSensorData)
        {
            this.clientSensorData = clientSensorData;

            // Lese data timeout fra config
            dataTimeout = config.Read(ConfigKey.DataTimeout, Constants.DataTimeoutDefault);

            // Update Sensor Status
            DispatcherTimer timer = new DispatcherTimer();

            timer.Interval = TimeSpan.FromMilliseconds(config.Read(ConfigKey.ClientUpdateFrequencyUI, Constants.ClientUpdateFrequencyUIDefault));
            timer.Tick += DoSensorStatusUpdate;
            timer.Start();

            void DoSensorStatusUpdate(object sender, EventArgs e)
            {
                UpdateSensorStatus();
            }
        }

        private void UpdateSensorStatus()
        {
            // Løper gjennom alle sensorene...
            for (int i = 0; i < sensorList.Count; i++)
            {
                Sensor sensor = sensorList[i];

                // Finne sensor verdier knyttet til valgt sensor ID
                var clientDataList = clientSensorData.GetDataList();
                lock (clientDataList)
                {
                    var clientData = clientDataList.Where(x => x.sensorGroupId == i);

                    // Har funnet verdier knyttet til sensor
                    if (clientData.Count() > 0)
                    {
                        // Skjekke om noen av sensor verdiene har error status
                        var errorList = clientData.Where(x => x.dataStatus == DataStatus.TIMEOUT_ERROR);

                        // En eller flere error statuser funnet
                        if (errorList.Count() > 0)
                        {
                            sensor.dataStatus = DataStatus.TIMEOUT_ERROR;
                        }
                        // Ingen error status funnet
                        else
                        {
                            sensor.dataStatus = DataStatus.OK;
                        }
                    }
                    // Ingen verdier knyttet til denne sensoren -> ingen feilmelding
                    else
                    {
                        sensor.dataStatus = DataStatus.OK;
                    }
                }
            }
        }

        public RadObservableCollectionEx<Sensor> GetSensorList()
        {
            return sensorList;
        }

        public DataStatus GetStatus(int id)
        {
            var sensor = sensorList.Where(x => x.id == id);
            if (sensor.Count() == 1)
            {
                return sensor.First().dataStatus;
            }
            else
            {
                return DataStatus.TIMEOUT_ERROR;
            }
        }

        public bool StatusChanged(int id)
        {
            // Sjekk på om status er endret
            var sensor = sensorList.Where(x => x.id == id);
            if (sensor.Count() == 1)
            {
                if (sensor.First().previousStatus != sensor.First().dataStatus)
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
            var sensor = sensorList.Where(x => x.id == id);
            if (sensor.Count() == 1)
            {
                sensor.First().previousStatus = sensor.First().dataStatus;
            }
        }

        public bool TimeoutCheck(HMSData sensorData)
        {
            bool ret = false;

            if (sensorData != null)
            {
                // Sjekker data timeout
                if (sensorData.timestamp.AddMilliseconds(dataTimeout) < DateTime.UtcNow)
                {
                    if (sensorData.dataStatus != DataStatus.TIMEOUT_ERROR)
                    {
                        sensorData.dataStatus = DataStatus.TIMEOUT_ERROR;
                        ret = true;
                    }
                }
                else
                {
                    if (sensorData.dataStatus != DataStatus.OK)
                    {
                        sensorData.dataStatus = DataStatus.OK;
                        ret = true;
                    }
                }

                // Oppdatere data i klient sensor data listen også
                var clientDataList = clientSensorData.GetDataList();
                lock (clientDataList)
                {
                    var clientData = clientDataList?.Where(x => x?.id == sensorData.id);

                    // Fant sensor
                    if (clientData.Count() == 1)
                    {
                        clientData.First().dataStatus = sensorData.dataStatus;
                    }
                }
            }

            return ret;
        }

        public bool IsActive(int id)
        {
            var sensor = sensorList.Where(x => x.id == id);
            if (sensor.Count() > 0)
                return sensor.First().active;
            else
                return false;
        }
    }
}
