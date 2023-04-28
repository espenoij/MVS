using System;
using System.Collections.Generic;

namespace HMS_Server
{
    public class FixedValueDataRetrieval
    {
        //// TEST
        //int counter = 0;

        // Database
        private DatabaseHandler database;

        // Config
        private Config config;

        // Fi: List
        private List<SensorData> fixedValueSensorList = new List<SensorData>();
        private List<FixedValueSetup> fixedValueList = new List<FixedValueSetup>();

        // Error Handler
        private ErrorHandler errorHandler;

        // Admin Settings
        private AdminSettingsVM adminSettingsVM;

        // File Reader callback
        public delegate void FixedValueReaderCallback(FixedValueSetup fileReaderData);
        private FixedValueReaderCallback fixedValueReaderCallback;

        public FixedValueDataRetrieval(Config config, DatabaseHandler database, ErrorHandler errorHandler, AdminSettingsVM adminSettingsVM)
        {
            this.config = config;
            this.database = database;
            this.errorHandler = errorHandler;
            this.adminSettingsVM = adminSettingsVM;

            // Callback funksjon som kalles når file reader har lest en linje fra fil
            fixedValueReaderCallback = new FixedValueReaderCallback(DataProcessing);
        }

        public void Load(SensorData sensorData)
        {
            // Legge til sensor i fixed value sensor listen
            fixedValueSensorList.Add(sensorData);

            // Legge inn i file reader listen
            fixedValueList.Add(new FixedValueSetup(sensorData.fixedValue));
        }

        public void Start()
        {
            // Starter lesing for hvert fil
            foreach (var fixedValueReader in fixedValueList)
            {
                fixedValueReader?.StartReader(config, errorHandler, fixedValueReaderCallback);
            }
        }

        public void Stop()
        {
            // Stoppe lesing for hvert fil
            foreach (var fixedValueReader in fixedValueList)
            {
                fixedValueReader?.StopReader();
            }
        }

        private void DataProcessing(FixedValueSetup fixedValueReaderData)
        {

            //  Trinn 1: Finne alle sensorer som er satt opp til å lese fra denne filen
            foreach (var sensorData in fixedValueSensorList)
            {
                // Har vi data?
                if (!string.IsNullOrEmpty(fixedValueReaderData.value))
                {
                    try
                    {
                        // Lagre resultat
                        sensorData.timestamp = fixedValueReaderData.timestamp;
                        sensorData.data = Convert.ToDouble(sensorData.fixedValue.value);

                        // Sette status
                        sensorData.portStatus = fixedValueReaderData.portStatus;

                        // Lagre til databasen
                        if (sensorData.saveFreq == DatabaseSaveFrequency.Sensor)
                        {
                            // Legger ikke inn data dersom data ikke er satt
                            if (!double.IsNaN(sensorData.data))
                            {
                                try
                                {
                                    database.Insert(sensorData);

                                    errorHandler.ResetDatabaseError(ErrorHandler.DatabaseErrorType.Insert2);
                                }
                                catch (Exception ex)
                                {
                                    errorHandler.Insert(
                                        new ErrorMessage(
                                            DateTime.UtcNow,
                                            ErrorMessageType.Database,
                                            ErrorMessageCategory.None,
                                            string.Format("Database Error (Insert 5)\n\nSystem Message:\n{0}", ex.Message),
                                            sensorData.id));

                                    errorHandler.SetDatabaseError(ErrorHandler.DatabaseErrorType.Insert2);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // Stte feilmelding
                        errorHandler.Insert(
                            new ErrorMessage(
                                DateTime.UtcNow,
                                ErrorMessageType.FixedValue,
                                ErrorMessageCategory.AdminUser,
                                string.Format("DataProcessing error, System Message: {0}", ex.Message),
                                sensorData.id));
                    }
                }
                // Ingen data
                else
                {
                    // Sette status
                    sensorData.portStatus = PortStatus.NoData;
                }
            }
        }

        public List<FixedValueSetup> GetFixedValueList()
        {
            return fixedValueList;
        }

        public void Clear()
        {
            fixedValueList.Clear();
        }
    }
}
