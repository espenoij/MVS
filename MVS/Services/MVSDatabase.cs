using System;
using System.Collections.Generic;

namespace MVS
{
    public class MVSDatabase
    {
        private DatabaseHandler database;
        private ErrorHandler errorHandler;

        public MVSDatabase(DatabaseHandler database, ErrorHandler errorHandler)
        {
            this.database = database;
            this.errorHandler = errorHandler;
        }

        public void Insert(RecordingSession dataSet, MVSDataCollection hmsDataCollection)
        {
            try
            {
                database.Insert(dataSet, hmsDataCollection);

                errorHandler.ResetDatabaseError(ErrorHandler.DatabaseErrorType.Insert3);
            }
            catch (Exception ex)
            {
                errorHandler.Insert(
                    new ErrorMessage(
                        DateTime.UtcNow,
                        ErrorMessageType.Database,
                        ErrorMessageCategory.None,
                        string.Format("Database Error (InsertData HMS)\n\nSystem Message:\n{0}", ex.Message)));

                errorHandler.SetDatabaseError(ErrorHandler.DatabaseErrorType.Insert3);
            }
        }

        public List<RecordingSession> GetAllSessions()
        {
            try
            {
                List<RecordingSession> dataSets = database.GetAllSessions();

                errorHandler.ResetDatabaseError(ErrorHandler.DatabaseErrorType.GetAllMVSData);

                return dataSets;
            }
            catch (Exception ex)
            {
                errorHandler.Insert(
                    new ErrorMessage(
                        DateTime.UtcNow,
                        ErrorMessageType.Database,
                        ErrorMessageCategory.None,
                        string.Format("Database Error (GetAll)\n\nSystem Message:\n{0}", ex.Message)));

                errorHandler.SetDatabaseError(ErrorHandler.DatabaseErrorType.GetAllMVSData);

                return null;
            }
        }

        public int Insert(RecordingSession dataSet)
        {
            int id = 0;

            try
            {
                id = database.Insert(dataSet);

                errorHandler.ResetDatabaseError(ErrorHandler.DatabaseErrorType.InsertMVSDataSet);
            }
            catch (Exception ex)
            {
                errorHandler.Insert(
                    new ErrorMessage(
                        DateTime.UtcNow,
                        ErrorMessageType.Database,
                        ErrorMessageCategory.None,
                        string.Format("Database Error (InsertData HMS)\n\nSystem Message:\n{0}", ex.Message)));

                errorHandler.SetDatabaseError(ErrorHandler.DatabaseErrorType.InsertMVSDataSet);
            }

            return id;
        }

        public void Update(RecordingSession dataSet)
        {
            try
            {
                database.Update(dataSet);

                errorHandler.ResetDatabaseError(ErrorHandler.DatabaseErrorType.UpdateMVSDataSet);
            }
            catch (Exception ex)
            {
                errorHandler.Insert(
                    new ErrorMessage(
                        DateTime.UtcNow,
                        ErrorMessageType.Database,
                        ErrorMessageCategory.None,
                        string.Format("Database Error (InsertData HMS)\n\nSystem Message:\n{0}", ex.Message)));

                errorHandler.SetDatabaseError(ErrorHandler.DatabaseErrorType.UpdateMVSDataSet);
            }
        }

        public void Remove(RecordingSession dataSet)
        {
            try
            {
                database.DeleteDataSet(dataSet);
                database.DeleteData(dataSet);

                errorHandler.ResetDatabaseError(ErrorHandler.DatabaseErrorType.DeleteMVSDataSet);
            }
            catch (Exception ex)
            {
                errorHandler.Insert(
                    new ErrorMessage(
                        DateTime.UtcNow,
                        ErrorMessageType.Database,
                        ErrorMessageCategory.None,
                        string.Format("Database Error (InsertData HMS)\n\nSystem Message:\n{0}", ex.Message)));

                errorHandler.SetDatabaseError(ErrorHandler.DatabaseErrorType.DeleteMVSDataSet);
            }
        }

        public void DeleteData(RecordingSession dataSet)
        {
            try
            {
                database.DeleteData(dataSet);

                errorHandler.ResetDatabaseError(ErrorHandler.DatabaseErrorType.DeleteMVSData);
            }
            catch (Exception ex)
            {
                errorHandler.Insert(
                    new ErrorMessage(
                        DateTime.UtcNow,
                        ErrorMessageType.Database,
                        ErrorMessageCategory.None,
                        string.Format("Database Error (InsertData HMS)\n\nSystem Message:\n{0}", ex.Message)));

                errorHandler.SetDatabaseError(ErrorHandler.DatabaseErrorType.DeleteMVSData);
            }
        }

        public void LoadTimestamps(RecordingSession dataSet)
        {
            try
            {
                database.LoadTimestamps(dataSet);

                errorHandler.ResetDatabaseError(ErrorHandler.DatabaseErrorType.GetTimestamps);
            }
            catch (Exception ex)
            {
                errorHandler.Insert(
                    new ErrorMessage(
                        DateTime.UtcNow,
                        ErrorMessageType.Database,
                        ErrorMessageCategory.None,
                        string.Format("Database Error (GetTimestamps)\n\nSystem Message:\n{0}", ex.Message)));

                errorHandler.SetDatabaseError(ErrorHandler.DatabaseErrorType.GetTimestamps);
            }
        }

        public void CreateDataSetTables()
        {
            try
            {
                // Oppretter tabellene dersom de ikke eksisterer
                database.CreateDataSetTables();

                errorHandler.ResetDatabaseError(ErrorHandler.DatabaseErrorType.CreateTablesMVSDataSet);
            }
            catch (Exception ex)
            {
                errorHandler.Insert(
                    new ErrorMessage(
                        DateTime.UtcNow,
                        ErrorMessageType.Database,
                        ErrorMessageCategory.None,
                        string.Format("Database Error (CreateTables Data Sets)\n\nSystem Message:\n{0}", ex.Message)));

                errorHandler.SetDatabaseError(ErrorHandler.DatabaseErrorType.CreateTablesMVSDataSet);
            }
        }

        public void CreateDataTables(RecordingSession dataSet, MVSDataCollection mvsDataCollection)
        {
            try
            {
                // Oppretter tabellene dersom de ikke eksisterer
                database.CreateDataTables(dataSet, mvsDataCollection);

                errorHandler.ResetDatabaseError(ErrorHandler.DatabaseErrorType.CreateTablesMVSData);
            }
            catch (Exception ex)
            {
                errorHandler.Insert(
                    new ErrorMessage(
                        DateTime.UtcNow,
                        ErrorMessageType.Database,
                        ErrorMessageCategory.None,
                        string.Format("Database Error (CreateTables Data)\n\nSystem Message:\n{0}", ex.Message)));

                errorHandler.SetDatabaseError(ErrorHandler.DatabaseErrorType.CreateTablesMVSData);
            }
        }

        public void CreateErrorMessagesTables()
        {
            try
            {
                // Oppretter feilmeldingstabell dersom den ikke eksisterer
                database.CreateErrorMessagesTables();

                errorHandler.ResetDatabaseError(ErrorHandler.DatabaseErrorType.CreateErrorMessagesTables);
            }
            catch (Exception ex)
            {
                errorHandler.Insert(
                    new ErrorMessage(
                        DateTime.UtcNow,
                        ErrorMessageType.Database,
                        ErrorMessageCategory.None,
                        string.Format("Database Error (CreateTables Data Sets)\n\nSystem Message:\n{0}", ex.Message)));

                errorHandler.SetDatabaseError(ErrorHandler.DatabaseErrorType.CreateErrorMessagesTables);
            }
        }
    }
}
