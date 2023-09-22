﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace HMS_Server
{
    public class FixedValueDataRetrieval
    {
        //// TEST
        //int counter = 0;

        // Database
        private DatabaseHandler database;

        // Fi: List
        private List<SensorData> fixedValueSensorList = new List<SensorData>();
        private List<FixedValueSetup> fixedValueList = new List<FixedValueSetup>();

        // Error Handler
        private ErrorHandler errorHandler;

        // File Reader callback
        public delegate void FixedValueReaderCallback(FixedValueSetup fileReaderData);
        private FixedValueReaderCallback fixedValueReaderCallback;

        public FixedValueDataRetrieval(DatabaseHandler database, ErrorHandler errorHandler)
        {
            this.database = database;
            this.errorHandler = errorHandler;

            // Callback funksjon som kalles når fixed value har lest inn en ny verdi med angitt frekvens
            fixedValueReaderCallback = new FixedValueReaderCallback(DataProcessing);
        }

        public void Load(SensorData sensorData)
        {
            // Legge til sensor i fixed value sensor listen
            fixedValueSensorList.Add(sensorData);

            // Legge inn i file reader listen
            fixedValueList.Add(new FixedValueSetup(sensorData));
        }

        public void Start()
        {
            // Starter lesing for hver fixed value
            foreach (var fixedValueData in fixedValueList)
            {
                fixedValueData?.StartReader(errorHandler, fixedValueReaderCallback);
            }
        }

        public void Stop()
        {
            // Stoppe lesing for hvert fixed value
            foreach (var fixedValueData in fixedValueList)
            {
                fixedValueData?.StopReader();
            }
        }

        private void DataProcessing(FixedValueSetup fixedValueData)
        {
            //  Trinn 1: Finne sensoren som tilhører fixed value i input
            var sensorData = fixedValueSensorList.ToList().Where(x => x.id == fixedValueData.id);

            // Har vi data?
            if (sensorData.Count() > 0)
            {
                try
                {
                    // Lagre resultat
                    sensorData.First().timestamp = fixedValueData.timestamp;
                    sensorData.First().data = Convert.ToDouble(fixedValueData.value);

                    // Sette status
                    sensorData.First().portStatus = fixedValueData.portStatus;

                    // Lagre til databasen
                    if (sensorData.First().saveFreq == DatabaseSaveFrequency.Sensor)
                    {
                        // Legger ikke inn data dersom data ikke er satt
                        if (!double.IsNaN(sensorData.First().data))
                        {
                            try
                            {
                                database.Insert(sensorData.First());

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
                                        sensorData.First().id));

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
                            fixedValueData.id));
                }
            }
        }

        public List<FixedValueSetup> GetFixedValueList()
        {
            return fixedValueList;
        }

        public void Clear()
        {
            fixedValueSensorList.Clear();
            fixedValueList.Clear();
        }
    }
}