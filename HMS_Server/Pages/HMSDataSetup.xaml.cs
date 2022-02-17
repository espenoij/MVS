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
    public partial class HMSDataSetup : UserControl
    {
        // Configuration settings
        private Config config;

        private RadObservableCollection<SensorData> sensorDisplayList = new RadObservableCollection<SensorData>();
        private RadObservableCollection<HMSData> hmsInputDisplayList = new RadObservableCollection<HMSData>();
        private RadObservableCollection<HMSData> hmsOutputDisplayList = new RadObservableCollection<HMSData>();

        public HMSDataSetup()
        {
            InitializeComponent();
        }

        public void Init(
            RadObservableCollection<SensorData> serverSensorDataList,
            HMSDataCollection hmsInputDataList,
            HMSDataCollection hmsOutputDataList,
            Config config)
        {
            this.config = config;

            // Liste med sensor verdier
            gvSensorInputData.ItemsSource = sensorDisplayList;

            // Liste med HMS input data
            gvHMSInputData.ItemsSource = hmsInputDisplayList;

            // Liste med HMS output data
            gvHMSOutputData.ItemsSource = hmsOutputDisplayList;

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
                    DisplayList.Transfer(serverSensorDataList, sensorDisplayList);
                    DisplayList.Transfer(hmsInputDataList.GetDataList(), hmsInputDisplayList);
                    DisplayList.Transfer(hmsOutputDataList.GetDataList(), hmsOutputDisplayList);
                }
            }
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