using System;
using System.Threading;

namespace HMS_Server
{
    class DatabaseInsertTimer
    {
        // Database Save Frequency timers
        private System.Timers.Timer saveTimer;

        public DatabaseInsertTimer(SensorData sensorData, DatabaseHandler database, Config config, ErrorHandler errorHandler)
        {
            double saveInterval = sensorData.GetSaveFrequency(config);

            saveTimer = new System.Timers.Timer(saveInterval);
            saveTimer.AutoReset = true;
            saveTimer.Elapsed += DatabaseSaveTimer_Run;

            void DatabaseSaveTimer_Run(object sender, EventArgs e)
            {
                if (sensorData.saveToDatabase &&
                    (sensorData.type == SensorType.SerialPort && sensorData.saveFreq != DatabaseSaveFrequency.Sensor) ||
                    ((sensorData.type == SensorType.ModbusRTU ||
                            sensorData.type == SensorType.ModbusASCII ||
                            sensorData.type == SensorType.ModbusTCP) &&
                        sensorData.saveFreq != DatabaseSaveFrequency.Freq_2hz))
                {
                    Thread thread = new Thread(() => DatabaseSaveTimer_Thread());
                    thread.Start();
                }
            }

            void DatabaseSaveTimer_Thread()
            {
                // Sjekke tidsstempel på data slik at vi ikke får duplikater
                if (sensorData.timestamp.AddMilliseconds(saveInterval) >= DateTime.UtcNow)
                {
                    // Legger ikke inn data dersom data ikke er satt
                    if (!double.IsNaN(sensorData.data))
                    {
                        // Lagre
                        try
                        {
                            database.Insert(sensorData);

                            errorHandler.ResetDatabaseError(ErrorHandler.DatabaseErrorType.Insert);
                        }
                        catch (Exception ex)
                        {
                            errorHandler.Insert(
                                new ErrorMessage(
                                    DateTime.UtcNow,
                                    ErrorMessageType.Database,
                                    ErrorMessageCategory.None,
                                    string.Format("Database Error (Insert 1)\n\nSystem Message:\n{0}", ex.Message),
                                    sensorData.id));

                            errorHandler.SetDatabaseError(ErrorHandler.DatabaseErrorType.Insert);
                        }
                    }
                }
            }
        }

        public void Start()
        {
            saveTimer.Start();
        }

        public void Stop()
        {
            saveTimer.Stop();
        }
    }
}
