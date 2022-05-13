using System;
using System.Linq;
using System.Windows.Threading;
using Telerik.Windows.Data;

namespace HMS_Server
{
    public class HMSSensorGroupStatus
    {
        private RadObservableCollection<SensorGroup> hmsSensorGroupList = new RadObservableCollection<SensorGroup>();
        private HMSDataCollection hmsInputDataList;

        // Config
        private Config config;

        // Database
        private DatabaseHandler database;

        // Error handler
        private ErrorHandler errorHandler;

        // Update Sensor Status
        DispatcherTimer timer = new DispatcherTimer();

        public HMSSensorGroupStatus(Config config, DatabaseHandler database, ErrorHandler errorHandler)
        {
            this.config = config;
            this.database = database;
            this.errorHandler = errorHandler;

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
            timer.Interval = TimeSpan.FromMilliseconds(config.ReadWithDefault(ConfigKey.ServerUIUpdateFrequency, Constants.ServerUIUpdateFrequencyDefault));
            timer.Tick += UpdateSensorStatus;

            void UpdateSensorStatus(object sender, EventArgs e)
            {
                // Løper gjennom alle sensorene...
                for (int i = 0; i < Constants.MaxSensors; i++)
                {
                    //  ...og sjekker om de tilknyttede verdiene har error status
                    UpdateStatus(i);
                }

                // Lagre til databasen
                SaveToDatabase();
            }
        }

        public void Start(HMSDataCollection hmsInputDataList)
        {
            this.hmsInputDataList = hmsInputDataList;

            timer.Start();
        }

        public void Stop()
        {
            timer.Stop();
        }

        public RadObservableCollection<SensorGroup> GetSensorList()
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
            if (hmsInputDataList != null)
            {
                // Finne frem sensoren vi skal oppdatere status for
                var sensorGroup = hmsSensorGroupList.Where(x => x.id == id);
                if (sensorGroup.Count() == 1)
                {
                    // Finne sensor verdier knyttet til valgt sensor gruppe ID
                    var hmsDataList = hmsInputDataList.GetDataList().ToList();

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


        private void SaveToDatabase()
        {
            try
            {
                // Lagre til databasen
                database.Insert(hmsSensorGroupList);

                errorHandler.ResetDatabaseError(ErrorHandler.DatabaseErrorType.Insert6);
            }
            catch (Exception ex)
            {
                errorHandler.Insert(
                    new ErrorMessage(
                        DateTime.UtcNow,
                        ErrorMessageType.Database,
                        ErrorMessageCategory.None,
                        string.Format("Database Error (InsertData SensorStatus)\n\nSystem Message:\n{0}", ex.Message)));

                errorHandler.SetDatabaseError(ErrorHandler.DatabaseErrorType.Insert6);
            }
        }

        public void CreateDataTables()
        {
            try
            {
                // Oppretter tabellene dersom de ikke eksisterer
                database.CreateTables(hmsSensorGroupList);

                errorHandler.ResetDatabaseError(ErrorHandler.DatabaseErrorType.CreateTablesSensorStatus);
            }
            catch (Exception ex)
            {
                errorHandler.Insert(
                    new ErrorMessage(
                        DateTime.UtcNow,
                        ErrorMessageType.Database,
                        ErrorMessageCategory.None,
                        string.Format("Database Error (CreateTables SensorStatus)\n\nSystem Message:\n{0}", ex.Message)));

                errorHandler.SetDatabaseError(ErrorHandler.DatabaseErrorType.CreateTablesSensorStatus);
            }
        }
    }
}
