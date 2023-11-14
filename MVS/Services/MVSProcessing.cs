namespace MVS
{
    // Klasse for prosessering av data fra sensor til HMS output
    public class MVSProcessing
    {
        private MVSProcessingMotion hmsProcessingMotion;

        // Init data prosessering
        public MVSProcessing(
            MVSDataCollection hmsOutputData,
            AdminSettingsVM adminSettingsVM,
            ErrorHandler errorHandler)
        {
            hmsProcessingMotion = new MVSProcessingMotion(hmsOutputData, adminSettingsVM, errorHandler);
        }

        // Kjøre prosessering og oppdatere data
        public void Update(MVSDataCollection hmsInputDataList, MainWindowVM mainWindowVM)
        {
            hmsProcessingMotion.Update(hmsInputDataList, mainWindowVM);
        }

        public void ResetDataCalculations()
        {
            hmsProcessingMotion.ResetDataCalculations();
        }
    }
}
