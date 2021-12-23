using System;

namespace HMS_Server
{
    class HMSDatabase
    {
        private DatabaseHandler database;
        private ErrorHandler errorHandler;

        public HMSDatabase(DatabaseHandler database, ErrorHandler errorHandler)
        {
            this.database = database;
            this.errorHandler = errorHandler;
        }

        public void InsertData(DataCollection hmsDataCollection)
        {
            try
            {
                database.Insert(hmsDataCollection);

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

        public void CreateDataTables(DataCollection hmsDataCollection)
        {
            try
            {
                // Oppretter tabellene dersom de ikke eksisterer
                //database.CreateTables(hmsDataCollection.GetDataList());
                database.CreateTables(hmsDataCollection);

                errorHandler.ResetDatabaseError(ErrorHandler.DatabaseErrorType.CreateTablesHMSData);
            }
            catch (Exception ex)
            {
                errorHandler.Insert(
                    new ErrorMessage(
                        DateTime.UtcNow,
                        ErrorMessageType.Database,
                        ErrorMessageCategory.None,
                        string.Format("Database Error (CreateTables HMS)\n\nSystem Message:\n{0}", ex.Message)));

                errorHandler.SetDatabaseError(ErrorHandler.DatabaseErrorType.CreateTablesHMSData);
            }
        }
    }
}
