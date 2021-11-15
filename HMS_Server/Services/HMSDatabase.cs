using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        public void InsertData(HMSDataCollection hmsDataCollection)
        {
            try
            {
                foreach (var hmsData in hmsDataCollection.GetDataList())
                {
                    database.Insert(hmsData);
                }
            }
            catch (Exception ex)
            {
                errorHandler.Insert(
                    new ErrorMessage(
                        DateTime.UtcNow,
                        ErrorMessageType.Database,
                        ErrorMessageCategory.None,
                        string.Format("Database Error (InsertData HMS)\n\nSystem Message:\n{0}", ex.Message)));

                errorHandler.SetDatabaseError(ErrorHandler.DatabaseErrorType.CreateTables);
            }
        }

        public void CreateHMSDataTables(HMSDataCollection hmsDataCollection)
        {
            try
            {
                // Oppretter tabellene dersom de ikke eksisterer
                database.CreateTables(hmsDataCollection.GetDataList());

                errorHandler.ResetDatabaseError(ErrorHandler.DatabaseErrorType.CreateTables);
            }
            catch (Exception ex)
            {
                errorHandler.Insert(
                    new ErrorMessage(
                        DateTime.UtcNow,
                        ErrorMessageType.Database,
                        ErrorMessageCategory.None,
                        string.Format("Database Error (CreateTables HMS)\n\nSystem Message:\n{0}", ex.Message)));

                errorHandler.SetDatabaseError(ErrorHandler.DatabaseErrorType.CreateTables);
            }
        }
    }
}
