using System;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Threading;
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

        private RadObservableCollection<HMSData> hmsOutputDisplayList = new RadObservableCollection<HMSData>();
        private RadObservableCollection<HMSData> testDisplayList = new RadObservableCollection<HMSData>();
        private RadObservableCollection<SensorData> sensorInputDisplayList = new RadObservableCollection<SensorData>();
        private RadObservableCollection<HMSData> referenceDisplayList = new RadObservableCollection<HMSData>();

        public DataVerificationSetup()
        {
            InitializeComponent();
        }

        public void Init(
            HMSDataCollection hmsOutputDataList,
            HMSDataCollection testDataList,
            SensorDataRetrieval serverSensorDataList,
            HMSDataCollection referenceDataList,
            Config config)
        {
            this.config = config;

            // Liste med HMS output data
            gvHMSOutputData.ItemsSource = hmsOutputDisplayList;

            // Liste med test data
            gvTestData.ItemsSource = testDisplayList;

            // Liste med sensor verdier
            gvSensorInputData.ItemsSource = sensorInputDisplayList;

            // Liste med referanse verdier
            gvReferenceData.ItemsSource = referenceDisplayList;


            // Dispatcher som oppdatere UI
            DispatcherTimer uiTimer = new DispatcherTimer();
            uiTimer.Interval = TimeSpan.FromMilliseconds(config.ReadWithDefault(ConfigKey.ServerUIUpdateFrequency, Constants.ServerUIUpdateFrequencyDefault));
            uiTimer.Tick += runUIInputUpdate;
            uiTimer.Start();

            void runUIInputUpdate(object sender, EventArgs e)
            {
                if (AdminMode.IsActive)
                {
                    // Overføre fra data lister til display lister
                    DisplayList.Transfer(hmsOutputDataList.GetDataList(), hmsOutputDisplayList);
                    DisplayList.Transfer(testDataList.GetDataList(), testDisplayList);
                    DisplayList.Transfer(serverSensorDataList.GetSensorDataList(), sensorInputDisplayList);
                    DisplayList.Transfer(referenceDataList.GetDataList(), referenceDisplayList);
                }
            }
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