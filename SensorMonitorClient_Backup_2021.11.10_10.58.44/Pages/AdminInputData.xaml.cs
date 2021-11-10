using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Telerik.Windows.Controls;
using Telerik.Windows.Controls.GridView;

namespace SensorMonitorClient
{
    /// <summary>
    /// Interaction logic for AdminInputData.xaml
    /// </summary>
    public partial class AdminInputData : UserControl
    {
        // Configuration settings
        private Config config;

        // Data Request modul
        private ServerCom serverCom;

        // Sensor Status
        private SensorStatusVM sensorStatusVM;

        public AdminInputData()
        {
            InitializeComponent();
        }

        public void Init(
            HMSDataCollection hmsDataCollection, 
            Config config,
            ServerCom serverCom,
            SensorStatus sensorStatus,
            SensorStatusVM sensorStatusVM,
            RadTabItem tabAdminDataFlow)
        {
            this.config = config;
            this.serverCom = serverCom;
            this.sensorStatusVM = sensorStatusVM;

            // Liste med sensor verdier
            gvServerData.ItemsSource = hmsDataCollection.GetDataList();

            // Liste med sensor ID verdier
            gvSensorID.ItemsSource = sensorStatus.GetSensorList();
        }

        public void UIUpdateServerRunning(bool state)
        {
            //btnDataRequestStart.IsEnabled = !state;
            //btnDataRequestStop.IsEnabled = state;
        }

        private void btnDataRequestStart_Click(object sender, RoutedEventArgs e)
        {
            serverCom.Start();

            UIUpdateServerRunning(true);
        }

        private void btnDataRequestStop_Click(object sender, RoutedEventArgs e)
        {
            serverCom.Stop();

            UIUpdateServerRunning(false);
        }
    }
}