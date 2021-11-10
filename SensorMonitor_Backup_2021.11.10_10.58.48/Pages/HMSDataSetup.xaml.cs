using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Telerik.Windows.Controls;
using Telerik.Windows.Controls.GridView;

namespace SensorMonitor
{
    /// <summary>
    /// Interaction logic for HMSDataSetup.xaml
    /// </summary>
    public partial class HMSDataSetup : UserControl
    {
        // Configuration settings
        private Config config;

        public HMSDataSetup()
        {
            InitializeComponent();
        }

        public void Init(
            RadObservableCollectionEx<SensorData> serverSensorDataList, 
            HMSDataCollection hmsInputDataList,
            HMSDataCollection hmsOutputDataList,
            Config config)
        {
            this.config = config;

            // Liste med sensor verdier
            gvSensorInputData.ItemsSource = serverSensorDataList;

            // Liste med HMS input data
            gvHMSInputData.ItemsSource = hmsInputDataList.GetDataList();

            // Liste med HMS output data
            gvHMSOutputData.ItemsSource = hmsOutputDataList.GetDataList();
        }

        private void gvClientData_BeginningEdit(object sender, GridViewBeginningEditRoutedEventArgs e)
        {
            gvHMSInputData.BeginEdit();
        }

        private void gvClientData_RowEditEnded(object sender, GridViewRowEditEndedEventArgs e)
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
    }
}