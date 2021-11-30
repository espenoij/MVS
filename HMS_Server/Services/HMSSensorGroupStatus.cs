using System;
using System.Linq;
using System.Windows.Threading;

namespace HMS_Server
{
    public class HMSSensorGroupStatus
    {
        private RadObservableCollectionEx<SensorGroup> hmsSensorGroupList = new RadObservableCollectionEx<SensorGroup>();
        private DataCollection hmsOutputDataList;

        // Config
        private Config config;

        public HMSSensorGroupStatus(Config config, DataCollection hmsOutputDataList)
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
                    hmsSensorGroupList.Add(sensor);
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
            return hmsSensorGroupList;
        }

        public void SetSensorGroupName(SensorGroup sensor)
        {
            if (sensor.id < hmsSensorGroupList.Count)
            {
                hmsSensorGroupList[sensor.id].name = sensor.name;

                // Lagre til fil
                config.SetSensorGroupIDData(sensor);
            }
        }

        private void UpdateStatus(int id)
        {
            // Finne frem sensoren vi skal oppdatere status for
            var sensorGroup = hmsSensorGroupList.Where(x => x.id == id);
            if (sensorGroup.Count() == 1)
            {
                // Finne sensor verdier knyttet til valgt sensor gruppe ID
                var hmsDataList = hmsOutputDataList.GetDataList();

                var clientData = hmsDataList.Where(x => x.sensorGroupId == id).ToList();

                // Har funnet sensor verdier knyttet til sensor gruppe
                if (clientData.Count() > 0)
                {
                    // Skjekke om noen av sensor verdiene har error status
                    var errorList = clientData.Where(x => x.status == DataStatus.TIMEOUT_ERROR);

                    // En eller flere error statuser funnet
                    if (errorList.Count() > 0)
                    {
                        sensorGroup.First().status = DataStatus.TIMEOUT_ERROR;
                    }
                    // Ingen error status funnet
                    else
                    {
                        sensorGroup.First().status = DataStatus.OK;
                    }
                }
                // Ingen verdier knyttet til denne sensoren -> ingen feilmelding
                else
                {
                    sensorGroup.First().status = DataStatus.OK;
                }
            }
        }
    }
}
