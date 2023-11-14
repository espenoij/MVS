using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Threading;
using Telerik.Windows.Controls;
using Telerik.Windows.Data;

namespace MVS
{
    public class ErrorHandler
    {
        // Database
        private DatabaseHandler database;

        // Latest Error Messages
        private RadObservableCollection<ErrorMessage> errorMessageList;
        private object errorMessageListLock = new object();

        // Selected type
        private ErrorMessageType selectedType = ErrorMessageType.All;

        // Database Errors
        public enum DatabaseErrorType
        {
            NoError,

            CreateTablesSensorData1,
            CreateTablesSensorData2,
            CreateTablesStartServer,
            CreateTablesHMSData,
            CreateTablesSensorStatus,
            StatusCheck,
            RemoveUnusedTables,
            DeleteAllData,
            Insert1,
            Insert2,
            Insert3,
            Insert4,
            Insert5,
            Insert6,
            DatabaseMaintenance,
            InsertErrorMessage,
            GetLastErrorMessages,
            DeleteErrorMessageData,

            InsertMVSDataSet,
            UpdateMVSDataSet,
            DeleteMVSDataSet,
            DeleteMVSData,
            CreateTablesMVSData,
            CreateTablesMVSDataSet,
            GetTimestamps,
            SaveMVSData,
            CreateErrorMessagesTables,
            GetAllMVSData,
            InsertMVSSession,

            TotalErrorTypes
        }

        private List<bool> databaseErrorList = new List<bool>();

        public ErrorHandler(DatabaseHandler database)
        {
            this.database = database;

            errorMessageList = new RadObservableCollection<ErrorMessage>();
            BindingOperations.EnableCollectionSynchronization(errorMessageList, errorMessageListLock);

            // Database Errors
            for (int i = 0; i < (int)DatabaseErrorType.TotalErrorTypes; i++)
                databaseErrorList.Add(false);
        }

        public void Insert(ErrorMessage errorMessage)
        {
            // Legge feilmelding inn i databasen
            database.InsertErrorMessage(errorMessage);

            // Legge inn i live view listen
            // Brukes også til å vise feilmeldinger på de individuelle sensorene på status siden
            if (errorMessage.type == selectedType ||
                selectedType == ErrorMessageType.All)
            {
                // Legge feilmelding inn i feilmeldingslisten
                try
                {
                    lock (errorMessageListLock)
                    {
                        errorMessageList.Add(errorMessage);

                        while (errorMessageList.Count > Constants.MaxErrorMessages)
                            errorMessageList.RemoveAt(0);
                    }
                }
                catch (Exception ex)
                {
                    DialogHandler.Warning("An error occured when inserting an error message into live view display.", ex.Message);
                }
            }

            // Vise melding på skjerm?
            switch (errorMessage.category)
            {
                case ErrorMessageCategory.None:
                    break;

                case ErrorMessageCategory.Admin:
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
            lock (errorMessageListLock)
                return errorMessageList?.ToList().Where(x => x?.id == id)?.DefaultIfEmpty(null).First();
        }

        public RadObservableCollection<ErrorMessage> GetErrorMessageList()
        {
            return errorMessageList;
        }

        public object GetErrorMessageListLock()
        {
            return errorMessageListLock;
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
            lock (errorMessageListLock)
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

        public void ResetDatabaseError()
        {
            for (int i = 0; i < databaseErrorList.Count; i++)
                databaseErrorList[i] = false;
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

        public void SetSelectedType(ErrorMessageType type)
        {
            selectedType = type;
        }
    }
}
