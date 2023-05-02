using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;

namespace HMS_Server
{
    public class FileReaderDataRetrieval
    {
        //// TEST
        //int counter = 0;

        // Database
        private DatabaseHandler database;

        // Config
        private Config config;

        // File Reader: List
        private List<SensorData> fileReaderSensorList = new List<SensorData>();
        private List<FileReaderSetup> fileReaderList = new List<FileReaderSetup>();

        // Error Handler
        private ErrorHandler errorHandler;

        // Admin Settings
        private AdminSettingsVM adminSettingsVM;

        // File Reader callback
        public delegate void FileReaderCallback(FileReaderSetup fileReaderData);
        private FileReaderCallback fileReaderCallback;

        public FileReaderDataRetrieval(Config config, DatabaseHandler database, ErrorHandler errorHandler, AdminSettingsVM adminSettingsVM)
        {
            this.config = config;
            this.database = database;
            this.errorHandler = errorHandler;
            this.adminSettingsVM = adminSettingsVM;

            // Callback funksjon som kalles når file reader har lest en linje fra fil
            fileReaderCallback = new FileReaderCallback(DataProcessing);
        }

        public void Load(SensorData sensorData)
        {
            // Legge til sensor i file reader sensor listen
            fileReaderSensorList.Add(sensorData);

            // Sjekke om file er lagt inn/åpnet fra før
            FileReaderSetup fileReaderData = fileReaderList.Find(x => x.fileFolder == sensorData.fileReader.fileFolder && x.fileName == sensorData.fileReader.fileName);

            // Dersom den ikke eksisterer -> så legger vi den inn i file reader fil listen
            if (fileReaderData == null)
            {
                // Legge inn i file reader listen
                fileReaderList.Add(new FileReaderSetup(sensorData.fileReader));
            }
        }

        public void Start()
        {
            // Starter lesing for hvert fil
            foreach (var fileReader in fileReaderList)
            {
                fileReader?.StartReader(config, errorHandler, fileReaderCallback);
            }
        }

        public void Stop()
        {
            // Stoppe lesing for hvert fil
            foreach (var fileReader in fileReaderList)
            {
                fileReader?.StopReader();
            }
        }

        private void DataProcessing(FileReaderSetup fileReaderData)
        {
            //  Trinn 1: Finne alle sensorer som er satt opp til å lese fra denne filen
            foreach (var sensorData in fileReaderSensorList)
            {
                // Har vi data?
                if (!string.IsNullOrEmpty(fileReaderData.dataLine))
                {
                    if (sensorData.fileReader.fileFolder == fileReaderData.fileFolder &&
                        sensorData.fileReader.fileName == fileReaderData.fileName)
                    {
                        // Data Processing
                        FileReaderProcessing process = new FileReaderProcessing();

                        try
                        {
                            //// TEST
                            //if (sensorData.id == 66)
                            //    counter++;

                            // Init processing
                            process.delimiter = sensorData.fileReader.delimiter;
                            process.fixedPosData = sensorData.fileReader.fixedPosData;
                            process.fixedPosStart = sensorData.fileReader.fixedPosStart;
                            process.fixedPosTotal = sensorData.fileReader.fixedPosTotal;
                            process.dataField = Convert.ToInt32(sensorData.fileReader.dataField);
                            process.decimalSeparator = sensorData.fileReader.decimalSeparator;
                            process.autoExtractValue = sensorData.fileReader.autoExtractValue;

                            // Dele data linje opp i individuelle data felt
                            //////////////////////////////////////////////////////////////////////////////
                            FileDataFields dataFields = process.SplitDataLine(fileReaderData.dataLine);

                            // Finne valgt datafelt
                            //////////////////////////////////////////////////////////////////////////////
                            SelectedDataField selectedData = process.FindSelectedData(dataFields);

                            // Utføre kalkulasjoner på utvalgt data
                            //////////////////////////////////////////////////////////////////////////////
                            CalculatedData calculatedData = process.ApplyCalculationsToSelectedData(
                                                                        selectedData, 
                                                                        sensorData.dataCalculations,
                                                                        fileReaderData.timestamp, 
                                                                        errorHandler, 
                                                                        ErrorMessageCategory.Admin,
                                                                        adminSettingsVM);

                            // Lagre resultat
                            sensorData.timestamp = fileReaderData.timestamp;
                            sensorData.data = calculatedData.data;

                            // Sette status
                            sensorData.portStatus = fileReaderData.portStatus;

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
                                                string.Format("Database Error (Insert 4)\n\nSystem Message:\n{0}", ex.Message),
                                                sensorData.id));

                                        errorHandler.SetDatabaseError(ErrorHandler.DatabaseErrorType.Insert2);
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            // Sette feilmelding
                            errorHandler.Insert(
                                new ErrorMessage(
                                    DateTime.UtcNow,
                                    ErrorMessageType.SerialPort,
                                    ErrorMessageCategory.AdminUser,
                                    string.Format("DataProcessing error, System Message: {0}", ex.Message),
                                    sensorData.id));
                        }
                    }
                }
                // Ingen data
                else
                {
                    // Sette status
                    sensorData.portStatus = fileReaderData.portStatus;
                }
            }
        }

        public List<FileReaderSetup> GetFileReaderList()
        {
            return fileReaderList;
        }

        public void Clear()
        {
            fileReaderSensorList.Clear();
            fileReaderList.Clear();
        }
    }
}
