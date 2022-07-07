using System;
using System.Windows.Controls;
using System.Windows.Threading;
using Telerik.Windows.Controls;
using Telerik.Windows.Controls.GridView;
using Telerik.Windows.Data;

namespace HMS_Server
{
    /// <summary>
    /// Interaction logic for HMSSensorGroupSetup.xaml
    /// </summary>
    public partial class HMSSensorGroupSetup : UserControl
    {
        // Configuration settings
        private Config config;

        private RadObservableCollection<HMSData> hmsDisplayList = new RadObservableCollection<HMSData>();
        private RadObservableCollection<SensorGroup> sensorDisplayList = new RadObservableCollection<SensorGroup>();

        private DispatcherTimer uiTimer = new DispatcherTimer();

        private HMSDataCollection hmsInputData;
        private HMSSensorGroupStatus sensorStatus;

        public HMSSensorGroupSetup()
        {
            InitializeComponent();
        }

        public void Init(
            HMSDataCollection hmsInputData,
            HMSSensorGroupStatus sensorStatus,
            Config config)
        {
            this.hmsInputData = hmsInputData;
            this.sensorStatus = sensorStatus;
            this.config = config;

            // Liste med client sensor data
            gvHMSData.ItemsSource = hmsDisplayList;

            // Liste med sensor ID verdier
            gvSensorID.ItemsSource = sensorDisplayList;

            // Dispatcher som oppdatere UI
            uiTimer.Interval = TimeSpan.FromMilliseconds(config.ReadWithDefault(ConfigKey.ServerUIUpdateFrequency, Constants.ServerUIUpdateFrequencyDefault));
            uiTimer.Tick += runUIInputUpdate;

            void runUIInputUpdate(object sender, EventArgs e)
            {
                if (AdminMode.IsActive)
                {
                    // Overføre fra data lister til display lister
                    DisplayList.Transfer(hmsInputData.GetDataList(), hmsDisplayList);
                    DisplayList.Transfer(sensorStatus.GetSensorList(), sensorDisplayList);
                }
            }
        }

        public void Start()
        {
            uiTimer.Start();
        }

        public void Stop()
        {
            uiTimer.Stop();
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

                // Lagre til HMS input data
                hmsInputData.SetData(hmsData);

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

                // Lagre til sensor group
                sensorStatus.UpdateSensorGroup(sensor);

                // Lagre til fil
                config.SetSensorGroupIDData(sensor);
            }
        }
    }
}