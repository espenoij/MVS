using System;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Threading;

namespace MVS
{
    /// <summary>
    /// Interaction logic for MVSDataSetup.xaml
    /// </summary>
    public partial class MVSInputSetup : UserControl
    {
        // Configuration settings
        private Config config;

        private ObservableCollectionEx<SensorData> sensorDisplayList = new ObservableCollectionEx<SensorData>();
        private ObservableCollectionEx<HMSData> hmsInputDisplayList = new ObservableCollectionEx<HMSData>();

        private DispatcherTimer uiTimer = new DispatcherTimer();

        private MVSDataCollection hmsInputDataList;

        private ObservableCollectionEx<SensorData> serverSensorDataList;

        private HMSData editingInput;
        private int originalDataId;
        private int cellDataId;

        public MVSInputSetup()
        {
            InitializeComponent();
        }

        public void Init(
            ObservableCollectionEx<SensorData> serverSensorDataList,
            MVSDataCollection hmsInputDataList,
            Config config)
        {
            this.serverSensorDataList = serverSensorDataList;
            this.hmsInputDataList = hmsInputDataList;
            this.config = config;

            // Liste med sensor verdier
            gvSensorInputData.ItemsSource = sensorDisplayList;

            // Liste med HMS input data
            gvHMSInputData.ItemsSource = hmsInputDisplayList;

            // Dispatcher som oppdatere UI
            uiTimer.Interval = TimeSpan.FromMilliseconds(config.ReadWithDefault(ConfigKey.ServerUIUpdateFrequency, Constants.ServerUIUpdateFrequencyDefault));
            uiTimer.Tick += runUIInputUpdate;

            void runUIInputUpdate(object sender, EventArgs e)
            {
                // Slette display liste dersom lengde er ulik input
                if (serverSensorDataList.Count != sensorDisplayList.Count)
                    sensorDisplayList.Clear();

                if (editingInput == null && hmsInputDataList.GetDataList().Count != hmsInputDisplayList.Count)
                    hmsInputDisplayList.Clear();

                // Overføre fra data lister til display lister
                DisplayList.Transfer(serverSensorDataList, sensorDisplayList);
                TransferInputData();
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

        public void UpdateSensorDataList()
        {
            // Overføre fra data lister til display lister
            DisplayList.Transfer(serverSensorDataList, sensorDisplayList);
            TransferInputData();

            ClearSensorStatus(sensorDisplayList);
            ClearSensorStatus(hmsInputDisplayList);
        }

        private void TransferInputData()
        {
            var items = hmsInputDataList.GetDataList().ToList();
            if (editingInput != null)
            {
                var current = items.FirstOrDefault(item => item.id == editingInput.id);
                if (current != null)
                {
                    editingInput.name = current.name;
                    editingInput.data = current.data;
                    editingInput.data2 = current.data2;
                    editingInput.data3 = current.data3;
                    editingInput.timestamp = current.timestamp;
                    editingInput.status = current.status;
                    editingInput.dbColumn = current.dbColumn;
                }
                items.RemoveAll(item => item.id == editingInput.id);
            }
            DisplayList.Transfer(items, hmsInputDisplayList);
        }

        private void ClearSensorStatus(ObservableCollectionEx<SensorData> sensorDisplayList)
        {
            foreach (var item in sensorDisplayList)
                item.portStatus = PortStatus.Closed;
        }

        private void ClearSensorStatus(ObservableCollectionEx<HMSData> hmsInputDisplayList)
        {
            foreach (var item in hmsInputDisplayList)
                item.status = DataStatus.NONE;
        }

        private void gvClientData_BeginningEdit(object sender, DataGridBeginningEditEventArgs e)
        {
            if (e.Row.Item is HMSData input)
            {
                if (!ReferenceEquals(editingInput, input))
                {
                    editingInput = input;
                    originalDataId = input.dataId;
                }
                cellDataId = input.dataId;
            }
        }

        private void gvClientData_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.EditAction == DataGridEditAction.Cancel && e.Row.Item is HMSData input)
                input.dataId = cellDataId;
        }

        private void gvClientData_RowEditEnding(object sender, DataGridRowEditEndingEventArgs e)
        {
            HMSData hmsData = (HMSData)e.Row.Item;
            if (e.EditAction == DataGridEditAction.Cancel)
            {
                if (ReferenceEquals(editingInput, hmsData))
                    hmsData.dataId = originalDataId;
            }
            else
            {
                hmsInputDataList.SetData(hmsData);
                config.SetClientData(hmsData);
            }
            editingInput = null;
        }
    }
}