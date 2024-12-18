﻿using MVS.Models;
using System;
using System.Collections.Generic;
using System.Data;
using Telerik.Charting;
using Telerik.Windows.Data;
using static MVS.DialogImport;

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

        public void CreateDataSetTables()
        {
            try
            {
                // Oppretter tabellene dersom de ikke eksisterer
                database.CreateSessionTables();

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

        public void CreateDataTables(Project dataSet, MVSDataCollection mvsDataCollection)
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

        public void Insert(Project dataSet, MVSDataCollection hmsDataCollection)
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

        public List<Project> GetAllProjects()
        {
            try
            {
                List<Project> dataSets = database.GetAllProjects();

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

        public int Insert(Project dataSet)
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

        public void Update(Project dataSet)
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

        public void Remove(Project dataSet)
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

        public void DeleteData(Project dataSet)
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

        public void LoadTimestamps(Project dataSet)
        {
            try
            {
                // Eksisterer database tabell for dataset?
                if (database.TableExists(dataSet))
                {
                    database.LoadTimestamps(dataSet);
                }
                // ...dersom tabell ikke eksisterer
                else
                {
                    // Sette timestamp 0
                    dataSet.StartTime = System.Data.SqlTypes.SqlDateTime.MinValue.Value;
                    dataSet.EndTime = System.Data.SqlTypes.SqlDateTime.MinValue.Value;
                }

                errorHandler.ResetDatabaseError(ErrorHandler.DatabaseErrorType.LoadTimestamps);
            }
            catch (Exception ex)
            {
                errorHandler.Insert(
                    new ErrorMessage(
                        DateTime.UtcNow,
                        ErrorMessageType.Database,
                        ErrorMessageCategory.None,
                        string.Format("Database Error (GetTimestamps)\n\nSystem Message:\n{0}", ex.Message)));

                errorHandler.SetDatabaseError(ErrorHandler.DatabaseErrorType.LoadTimestamps);
            }
        }

        public void LoadSessionData(Project dataSet, RadObservableCollection<ProjectData> dataList)
        {
            try
            {
                // Slette gamle data i listen
                dataList.Clear();

                database.LoadSessionData(dataSet, dataList);

                errorHandler.ResetDatabaseError(ErrorHandler.DatabaseErrorType.LoadSessionData);
            }
            catch (Exception ex)
            {
                errorHandler.Insert(
                    new ErrorMessage(
                        DateTime.UtcNow,
                        ErrorMessageType.Database,
                        ErrorMessageCategory.None,
                        string.Format("Database Error (LoadSessionData)\n\nSystem Message:\n{0}", ex.Message)));

                errorHandler.SetDatabaseError(ErrorHandler.DatabaseErrorType.LoadSessionData);
            }
        }

        public ImportResult ImportHMSData(Project selectedSession, ReportProgressDelegate reportProgress)
        {
            ImportResult result = new ImportResult();

            try
            {
                result = database.ImportHMSData(selectedSession, reportProgress);

                errorHandler.ResetDatabaseError(ErrorHandler.DatabaseErrorType.ImportHMSData);
            }
            catch (Exception ex)
            {
                errorHandler.Insert(
                    new ErrorMessage(
                        DateTime.UtcNow,
                        ErrorMessageType.Database,
                        ErrorMessageCategory.None,
                        string.Format("Database Error (ImportHMSData)\n\nSystem Message:\n{0}", ex.Message)));

                errorHandler.SetDatabaseError(ErrorHandler.DatabaseErrorType.ImportHMSData);

                // Sende feilmelding videre
                result.code = ImportResultCode.DatabaseError;
                result.message = ex.Message;
            }

            return result;
        }
    }
}
