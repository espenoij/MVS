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
        public AdminInputData()
        {
            InitializeComponent();
        }

        public void Init(
            HMSDataCollection hmsDataCollection, 
            SensorGroupStatus sensorStatus)
        {
            // Liste med sensor verdier
            gvServerData.ItemsSource = hmsDataCollection.GetDataList();

            // Liste med sensor ID verdier
            gvSensorID.ItemsSource = sensorStatus.GetSensorList();
        }
    }
}