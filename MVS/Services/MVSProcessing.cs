namespace MVS
{
    // Klasse for prosessering av data fra sensor til HMS output
    public class MVSProcessing
    {
        private MVSProcessingMotion mvsProcessingMotion;

        // Init data prosessering
        public MVSProcessing(
            MVSDataCollection mvsOutputData,
            AdminSettingsVM adminSettingsVM,
            ErrorHandler errorHandler)
        {
            mvsProcessingMotion = new MVSProcessingMotion(mvsOutputData, adminSettingsVM, errorHandler);
        }

        // Kjøre prosessering og oppdatere data
        public void Update(MVSDataCollection mvsInputDataList, MainWindowVM mainWindowVM, ProcessingType processingType)
        {
            mvsProcessingMotion.Update(mvsInputDataList, mainWindowVM, processingType);
        }

        public void ResetDataCalculations()
        {
            mvsProcessingMotion.ResetDataCalculations();
        }
    }
}
