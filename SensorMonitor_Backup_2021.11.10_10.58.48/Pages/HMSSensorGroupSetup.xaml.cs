using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Telerik.Windows.Controls;
using Telerik.Windows.Controls.GridView;

namespace SensorMonitor
{
    /// <summary>
    /// Interaction logic for HMSSensorGroupSetup.xaml
    /// </summary>
    public partial class HMSSensorGroupSetup : UserControl
    {
        // Configuration settings
        private Config config;

        // Sensor Status
        private HMSSensorStatus sensorStatus;

        public HMSSensorGroupSetup()
        {
            InitializeComponent();
        }

        public void Init(
            RadObservableCollectionEx<SensorData> serverSensorDataList, 
            HMSDataCollection clientSensorData, 
            Config config,
            HMSSensorStatus sensorStatus)
        {
            this.config = config;
            this.sensorStatus = sensorStatus;

            // Liste med client sensor data
            gvHMSData.ItemsSource = clientSensorData.GetDataList();

            // Liste med sensor ID verdier
            gvSensorID.ItemsSource = sensorStatus.GetSensorList();
        }

        private void gvHMSData_BeginningEdit(object sender, GridViewBeginningEditRoutedEventArgs e)
        {
            gvHMSData.BeginEdit();
        }

        private void gvHMSData_RowEditEnded(object sender, GridViewRowEditEndedEventArgs e)
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
                config.SetClientData(hmsData);
            }
        }

        private void gvSensorID_BeginningEdit(object sender, GridViewBeginningEditRoutedEventArgs e)
        {
            gvSensorID.BeginEdit();
        }

        private void gvSensorID_RowEditEnded(object sender, GridViewRowEditEndedEventArgs e)
        {
            if (e.EditAction == GridViewEditAction.Cancel)
            {
                return;
            }
            if (e.EditOperationType == GridViewEditOperationType.Edit)
            {
                // Hente oppdaterte data
                SensorGroup sensor = (sender as RadGridView).SelectedItem as SensorGroup;

                // Lagre til fil
                config.SetSensorGroupIDData(sensor);
            }
        }
    }
}