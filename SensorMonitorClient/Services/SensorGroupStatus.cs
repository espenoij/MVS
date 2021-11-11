using System;
using System.Linq;
using System.Windows.Threading;

namespace SensorMonitorClient
{
    public class SensorGroupStatus
    {
        private RadObservableCollectionEx<SensorGroup> sensorGroupList = new RadObservableCollectionEx<SensorGroup>();
        private HMSDataCollection clientSensorDataCollection;

        private double dataTimeout;

        public SensorGroupStatus(Config config, HMSDataCollection clientSensorDataCollection)
        {
            this.clientSensorDataCollection = clientSensorDataCollection;

            // Lese data timeout fra config
            dataTimeout = config.Read(ConfigKey.DataTimeout, Constants.DataTimeoutDefault);

            // Update Sensor Status
            DispatcherTimer timer = new DispatcherTimer();

            timer.Interval = TimeSpan.FromMilliseconds(config.Read(ConfigKey.ClientUpdateFrequencyUI, Constants.ClientUpdateFrequencyUIDefault));
            timer.Tick += DoSensorStatusUpdate;
            timer.Start();

            void DoSensorStatusUpdate(object sender, EventArgs e)
            {
                // Klienten sjekker status for sensor gruppene selv, selv om status kommer fra server også.
                // Dersom server slutter å sende data eller har mistet kontakten med klient, da må klienten selv sette status.
                UpdateSensorStatus();
            }
        }

        private void UpdateSensorStatus()
        {
            // Løper gjennom alle sensorene...
            for (int i = 0; i < sensorGroupList.Count; i++)
            {
                SensorGroup sensorGroup = sensorGroupList[i];

                // Finne sensor verdier knyttet til valgt sensor gruppe
                var clientDataList = clientSensorDataCollection.GetDataList();
                lock (clientDataList)
                {
                    var clientData = clientDataList.Where(x => x.sensorGroupId == i);

                    // Har funnet sensor verdier knyttet til sensor gruppe
                    if (clientData.Count() > 0)
                    {
                        // Skjekke om noen av sensor verdiene har error status
                        var errorList = clientData.Where(x => x.status == DataStatus.TIMEOUT_ERROR);

                        // En eller flere error statuser funnet
                        if (errorList.Count() > 0)
                        {
                            sensorGroup.status = DataStatus.TIMEOUT_ERROR;
                        }
                        // Ingen error status funnet
                        else
                        {
                            sensorGroup.status = DataStatus.OK;
                        }
                    }
                    // Ingen verdier knyttet til denne sensor gruppen -> ingen feilmelding
                    else
                    {
                        sensorGroup.status = DataStatus.OK;
                    }
                }
            }
        }

        // Timeout sjekk på sensor data
        // NB! Dersom kommunikasjon med server stopper må klienten selv finne ut om vi har data timeout.
        // Kan ikke regne med at server opplyser om korrekt status til en hver tid.
        public bool TimeoutCheck(HMSData sensorData)
        {
            bool ret = false;

            if (sensorData != null)
            {
                // Sjekker data timeout
                if (sensorData.timestamp.AddMilliseconds(dataTimeout) < DateTime.UtcNow)
                {
                    if (sensorData.status == DataStatus.OK)
                    {
                        sensorData.status = DataStatus.TIMEOUT_ERROR;
                        ret = true;
                    }
                }
                else
                {
                    if (sensorData.status == DataStatus.TIMEOUT_ERROR)
                    {
                        sensorData.status = DataStatus.OK;
                        ret = true;
                    }
                }

                // Oppdatere data i klient sensor data listen også
                var clientSensorDataList = clientSensorDataCollection.GetDataList();
                lock (clientSensorDataList)
                {
                    var clientSensorData = clientSensorDataList?.Where(x => x?.id == sensorData.id);

                    // Fant sensor
                    if (clientSensorData.Count() == 1)
                    {
                        clientSensorData.First().status = sensorData.status;
                    }
                }
            }

            return ret;
        }

        public RadObservableCollectionEx<SensorGroup> GetSensorList()
        {
            return sensorGroupList;
        }
    }
}
