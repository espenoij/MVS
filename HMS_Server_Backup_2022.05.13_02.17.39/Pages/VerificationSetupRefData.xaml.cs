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
    /// Interaction logic for VerificationSetupRefData.xaml
    /// </summary>
    partial class VerificationSetupRefData : UserControl
    {
        // Configuration settings
        private Config config;

        private RadObservableCollection<SensorData> sensorInputDisplayList = new RadObservableCollection<SensorData>();

        public VerificationSetupRefData()
        {
            InitializeComponent();
        }

        public void Init(
            SensorDataRetrieval serverSensorDataList,
            HMSDataCollection referenceDataList,
            Config config)
        {
            this.config = config;

            // Liste med sensor verdier
            gvSensorInputData.ItemsSource = sensorInputDisplayList;

            // Liste med referanse verdier
            gvReferenceData.ItemsSource = referenceDataList.GetDataList();


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
                    DisplayList.Transfer(serverSensorDataList.GetSensorDataList(), sensorInputDisplayList);
                }
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