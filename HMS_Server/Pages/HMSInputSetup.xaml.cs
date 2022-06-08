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
    /// Interaction logic for HMSDataSetup.xaml
    /// </summary>
    public partial class HMSInputSetup : UserControl
    {
        // Configuration settings
        private Config config;

        private RadObservableCollection<SensorData> sensorDisplayList = new RadObservableCollection<SensorData>();
        private RadObservableCollection<HMSData> hmsInputDisplayList = new RadObservableCollection<HMSData>();

        private DispatcherTimer uiTimer = new DispatcherTimer();

        private HMSDataCollection hmsInputDataList;

        public HMSInputSetup()
        {
            InitializeComponent();
        }

        public void Init(
            RadObservableCollection<SensorData> serverSensorDataList,
            HMSDataCollection hmsInputDataList,
            Config config)
        {
            this.hmsInputDataList = hmsInputDataList;
            this.config = config;

            // Liste med sensor verdier
            gvSensorInputData.ItemsSource = sensorDisplayList;

            // Liste med HMS input data
            gvHMSInputData.ItemsSource = hmsInputDisplayList;

            // Overføre fra data lister til display lister
            DisplayList.Transfer(serverSensorDataList, sensorDisplayList);
            DisplayList.Transfer(hmsInputDataList.GetDataList(), hmsInputDisplayList);

            ClearSensorStatus(sensorDisplayList);
            ClearSensorStatus(hmsInputDisplayList);

            // Dispatcher som oppdatere UI
            uiTimer.Interval = TimeSpan.FromMilliseconds(config.ReadWithDefault(ConfigKey.ServerUIUpdateFrequency, Constants.ServerUIUpdateFrequencyDefault));
            uiTimer.Tick += runUIInputUpdate;

            void runUIInputUpdate(object sender, EventArgs e)
            {
                if (AdminMode.IsActive)
                {
                    // Overføre fra data lister til display lister
                    DisplayList.Transfer(serverSensorDataList, sensorDisplayList);
                    DisplayList.Transfer(hmsInputDataList.GetDataList(), hmsInputDisplayList);
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

            ClearSensorStatus(sensorDisplayList);
            ClearSensorStatus(hmsInputDisplayList);
        }

        private void ClearSensorStatus(RadObservableCollection<SensorData> sensorDisplayList)
        {
            foreach (var item in sensorDisplayList)
                item.portStatus = PortStatus.Closed;
        }

        private void ClearSensorStatus(RadObservableCollection<HMSData> hmsInputDisplayList)
        {
            foreach (var item in hmsInputDisplayList)
                item.status = DataStatus.NONE;
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

                // Lagre til data samling
                hmsInputDataList.SetData(hmsData);

                // Lagre til fil
                config.SetClientData(hmsData);
            }
        }
    }
}