using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using Telerik.Windows.Controls;
using Telerik.Windows.Data;

namespace HMS_Server
{
    public class ErrorHandler
    {
        public const bool ShowMessageBox = true;
        public const bool HideMessageBox = false;

        // Database
        private DatabaseHandler database;

        // Latest Error Messages
        private RadObservableCollectionEx<ErrorMessage> errorMessageList = new RadObservableCollectionEx<ErrorMessage>();

        // Database Errors
        public enum DatabaseErrorType
        {
            NoError,

            CreateTablesSensorData1,
            CreateTablesSensorData2,
            CreateTablesStartServer,
            CreateTablesHMSData,
            StatusCheck,
            RemoveUnusedTables,
            DeleteAllData,
            Insert1,
            Insert2,
            Insert3,
            Insert4,
            Insert5,
            DatabaseMaintenance,
            InsertErrorMessage,
            GetLastErrorMessages,
            DeleteErrorMessageData,

            TotalErrorTypes
        }
        private List<bool> databaseErrorList = new List<bool>();

        public ErrorHandler(DatabaseHandler database)
        {
            this.database = database;

            // Database Errors
            for (int i = 0; i < (int)DatabaseErrorType.TotalErrorTypes; i++)
                databaseErrorList.Add(false);
        }

        public void Insert(ErrorMessage errorMessage)
        {
            // Legge feilmelding inn i databasen
            database.InsertErrorMessage(errorMessage);

            lock (errorMessageList)
            {
                // Legge feilmelring inn i feilmeldinglisten
                errorMessageList.Add(errorMessage);

                // Begrenser antallet i feilmeldingslisten
                if (errorMessageList.Count > Constants.MaxErrorMessages)
                    errorMessageList.RemoveAt(0);
            }

            // Vise melding på skjerm?
            switch (errorMessage.category)
            {
                case ErrorMessageCategory.None:
                    break;

                case ErrorMessageCategory.Admin:

                    if (AdminMode.IsActive)
                    {
                        Application.Current.Dispatcher.BeginInvoke(
                        DispatcherPriority.Normal,
                        new Action(() =>
                        {
                            {
                                RadWindow.Alert(TextHelper.Wrap(errorMessage.message));
                            }
                        }));
                    }
                    break;

                case ErrorMessageCategory.User:

                    Application.Current.Dispatcher.BeginInvoke(
                    DispatcherPriority.Normal,
                    new Action(() =>
                    {
                        {
                            RadWindow.Alert(TextHelper.Wrap(errorMessage.message));
                        }
                    }));

                    break;

                case ErrorMessageCategory.AdminUser:
                    break;
            }
        }

        public ErrorMessage GetErrorMessage(int id)
        {
            // NB! Returnerer null dersom melding med angitt id ikke ble funnet
            return errorMessageList?.ToList().Where(x => x?.id == id)?.FirstOrDefault();
        }

        public RadObservableCollectionEx<ErrorMessage> GetErrorMessageList()
        {
            return errorMessageList;
        }

        public List<ErrorMessage> ReadLast(ErrorMessageType type, int number)
        {
            try
            {
                return database.GetLastErrorMessages(type, number);
            }
            catch (Exception ex)
            {
                Insert(
                    new ErrorMessage(
                        DateTime.UtcNow,
                        ErrorMessageType.Database,
                        ErrorMessageCategory.AdminUser,
                        string.Format("Database Error (GetLastErrorMessages)\n\nSystem Message:\n{0}", ex.Message)));
            }

            return null;
        }

        public void DeleteErrorMessageData()
        {
            try
            {
                database.DeleteErrorMessageData();
            }
            catch (Exception ex)
            {
                Insert(
                    new ErrorMessage(
                        DateTime.UtcNow,
                        ErrorMessageType.Database,
                        ErrorMessageCategory.AdminUser,
                        string.Format("Database Error (DeleteErrorMessageData)\n\nSystem Message:\n{0}", ex.Message)));
            }
        }

        public void ClearAllMessages()
        {
            errorMessageList?.Clear();
        }

        public void SetDatabaseError(DatabaseErrorType type)
        {
            databaseErrorList[(int)type] = true;
        }

        public void ResetDatabaseError(DatabaseErrorType type)
        {
            databaseErrorList[(int)type] = false;
        }

        public bool IsDatabaseError()
        {
            bool value = false;

            foreach (var error in databaseErrorList.ToList())
                if (error)
                    value = true;

            return value;
        }

        public DatabaseErrorType GetDatabaseErrorType()
        {
            DatabaseErrorType errorType = DatabaseErrorType.NoError;

            for (int i = 0; i < (int)DatabaseErrorType.TotalErrorTypes && errorType == DatabaseErrorType.NoError; i++)
            {
                if (databaseErrorList[i])
                    errorType = (DatabaseErrorType)i;
            }

            return errorType;
        }
    }
}
