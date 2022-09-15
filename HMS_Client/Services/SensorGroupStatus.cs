using System;
using System.Linq;
using System.Windows.Data;
using System.Windows.Threading;
using Telerik.Windows.Data;

namespace HMS_Client
{
    public class SensorGroupStatus
    {
        private RadObservableCollection<SensorGroup> sensorGroupList;
        private object sensorGroupListLock = new object();
        private HMSDataCollection clientSensorDataCollection;

        private double dataTimeout;

        public SensorGroupStatus(Config config, HMSDataCollection clientSensorDataCollection)
        {
            this.clientSensorDataCollection = clientSensorDataCollection;

            sensorGroupList = new RadObservableCollection<SensorGroup>();
            BindingOperations.EnableCollectionSynchronization(sensorGroupList, sensorGroupListLock);

            // Lese data timeout fra config
            dataTimeout = config.ReadWithDefault(ConfigKey.DataTimeout, Constants.DataTimeoutDefault);

            // Update Sensor Status
            DispatcherTimer timer = new DispatcherTimer();

            timer.Interval = TimeSpan.FromMilliseconds(config.ReadWithDefault(ConfigKey.ClientUpdateFrequencyUI, Constants.ClientUIUpdateFrequencyDefault));
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
            lock (sensorGroupListLock)
            {
                // Løper gjennom alle sensorene...
                for (int sensorGroupId = 0; sensorGroupId < sensorGroupList.Count; sensorGroupId++)
                {
                    SensorGroup sensorGroup = sensorGroupList[sensorGroupId];

                    // Finne sensor verdier knyttet til valgt sensor gruppe
                    var clientDataList = clientSensorDataCollection.GetDataList(sensorGroupId);
                    if (clientDataList != null)
                    {
                        var clientData = clientDataList?.Where(x => x.sensorGroupId == sensorGroupId);

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
                        // Ingen verdier knyttet til denne sensor gruppen
                        else
                        {
                            //sensorGroup.status = DataStatus.OK;
                        }
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
                    if (sensorData.status == DataStatus.OK ||
                        sensorData.status == DataStatus.OK_NA)
                    {
                        sensorData.status = DataStatus.TIMEOUT_ERROR;
                        ret = true;
                    }
                }
                //else
                //{
                //    if (sensorData.status == DataStatus.TIMEOUT_ERROR)
                //    {
                //        sensorData.status = DataStatus.OK;
                //        ret = true;
                //    }
                //}

                // Oppdatere data i klient sensor data listen også
                var clientSensorDataList = clientSensorDataCollection.GetDataList().ToList();
                var clientSensorData = clientSensorDataList?.Where(x => x?.id == sensorData.id);

                // Fant sensor
                if (clientSensorData?.Count() == 1)
                {
                    clientSensorData.First().status = sensorData.status;
                }
            }

            return ret;
        }

        public RadObservableCollection<SensorGroup> GetSensorGroupList()
        {
            return sensorGroupList;
        }

        public object GetSensorGroupListLock()
        {
            return sensorGroupListLock;
        }
    }
}
