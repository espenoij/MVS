using System.Windows.Controls;
using Telerik.Windows.Controls;
using Telerik.Windows.Controls.GridView;
using Telerik.Windows.Data;

namespace HMS_Server
{
    /// <summary>
    /// Interaction logic for DataVerificationSetup.xaml
    /// </summary>
    partial class DataVerificationSetup : UserControl
    {
        // Configuration settings
        private Config config;

        public DataVerificationSetup()
        {
            InitializeComponent();
        }

        public void Init(
            HMSDataCollection hmsOutputDataList,
            HMSDataCollection testDataList,
            RadObservableCollection<SensorData> serverSensorDataList,
            HMSDataCollection referenceDataList,
            Config config)
        {
            this.config = config;

            // Liste med HMS output data
            gvHMSOutputData.ItemsSource = hmsOutputDataList.GetDataList();

            // Liste med test data
            gvTestData.ItemsSource = testDataList.GetDataList();

            // Liste med sensor verdier
            gvSensorInputData.ItemsSource = serverSensorDataList;

            // Liste med referanse verdier
            gvReferenceData.ItemsSource = referenceDataList.GetDataList();
        }

        private void gvTestData_BeginningEdit(object sender, GridViewBeginningEditRoutedEventArgs e)
        {
            gvTestData.BeginEdit();
        }

        private void gvTestData_RowEditEnded(object sender, GridViewRowEditEndedEventArgs e)
        {
            if (e.EditAction == GridViewEditAction.Cancel)
            {
                return;
            }
            if (e.EditOperationType == GridViewEditOperationType.Edit)
            {
                // Hente oppdaterte data
                HMSData hmsData = (sender as RadGridView).SelectedItem as HMSData;

                // Lagre til fil
                config.SetTestData(hmsData);
            }
        }

        private void gvReferenceData_BeginningEdit(object sender, GridViewBeginningEditRoutedEventArgs e)
        {
            gvReferenceData.BeginEdit();
        }

        private void gvReferenceData_RowEditEnded(object sender, GridViewRowEditEndedEventArgs e)
        {
            if (e.EditAction == GridViewEditAction.Cancel)
            {
                return;
            }
            if (e.EditOperationType == GridViewEditOperationType.Edit)
            {
                // Hente oppdaterte data
                HMSData hmsData = (sender as RadGridView).SelectedItem as HMSData;

                // Lagre til fil
                config.SetReferenceData(hmsData);
            }
        }
    }
}