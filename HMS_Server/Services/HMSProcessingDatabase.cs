using System;

namespace HMS_Server
{
    class HMSProcessingDatabase
    {
        private ErrorHandler errorHandler;

        private HMSData databaseStatus = new HMSData();

        public HMSProcessingDatabase(HMSDataCollection hmsOutputData, ErrorHandler errorHandler)
        {
            this.errorHandler = errorHandler;

            // Fyller output listen med HMS Output data
            // NB! Variablene som legges inn i listen her fungerer som pekere: Oppdateres variabelen -> oppdateres listen
            // NB! Dersom nye variabler legges til i hmsOutputDataList må databasen opprettes på nytt

            RadObservableCollectionEx<HMSData> hmsOutputDataList = hmsOutputData.GetDataList();

            hmsOutputDataList.Add(databaseStatus);

            //databaseStatus.id = (int)ValueType.DatabaseStatus;
            databaseStatus.name = "Database Status";
        }

        public void Update()
        {
            // Tar data fra input delen av server og overfører til HMS output delen

            if (errorHandler.IsDatabaseError())
                databaseStatus.status = DataStatus.TIMEOUT_ERROR;
            else
                databaseStatus.status = DataStatus.OK;

            databaseStatus.timestamp = DateTime.UtcNow;
        }
    }
}