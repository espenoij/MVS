using System;
using System.Linq;
using System.Text.Json;
using System.Windows.Threading;
using Telerik.Windows.Data;

namespace HMS_Client
{
    public class ServerCom
    {
        // Configuration settings
        private Config config;

        // TCP/IP Socket console for utskrift av trafikk
        private SocketConsole socketConsole;

        // TCP/IP Data Request Timer
        private DispatcherTimer dataRequestTimer = new DispatcherTimer();
        private DispatcherTimer sensorStatusRequestTimer = new DispatcherTimer();
        //private DispatcherTimer userInputsRequestTimer = new DispatcherTimer();

        // Liste med HMS data
        private HMSDataCollection hmsDataCollection;
        private RadObservableCollection<HMSData> socketHMSDataList = new RadObservableCollection<HMSData>();

        // Liste med sensor status
        private RadObservableCollection<SensorGroup> sensorStatusList;
        private object sensorStatusListLock;
        private RadObservableCollection<SensorGroup> socketSensorStatusList = new RadObservableCollection<SensorGroup>();

        // Socket client callback
        public delegate void SocketCallback();

        // Data Request callback
        private MainWindow.DataRequestCallback dataRequestCallback;
        private MainWindow.ClientDeniedCallback clientDeniedCallback;

        // Tidspunkt for sist mottatt HMS data
        public DateTime lastDataReceivedTime = new DateTime();

        public void Init(
            SocketConsole socketConsole,
            HMSDataCollection hmsDataCollection,
            SensorGroupStatus sensorStatus,
            Config config,
            MainWindow.DataRequestCallback dataRequestCallback,
            MainWindow.ClientDeniedCallback clientDeniedCallback)
        {
            // Socket Console
            this.socketConsole = socketConsole;

            // Data fra server
            this.hmsDataCollection = hmsDataCollection;

            // Sensor Status fra server
            sensorStatusList = sensorStatus.GetSensorGroupList();
            sensorStatusListLock = sensorStatus.GetSensorGroupListLock();

            // Data Request callback
            this.dataRequestCallback = dataRequestCallback;
            this.clientDeniedCallback = clientDeniedCallback;

            // Config
            this.config = config;

            // Init av data update request fra server
            InitDataUpdater();

            // Starte innhenting av data fra server
            Start();
        }

        private void InitDataUpdater()
        {
            // Data Retrieval Timer
            /////////////////////////////////////////////////////////////
            dataRequestTimer.Interval = TimeSpan.FromMilliseconds(config.ReadWithDefault(ConfigKey.DataRequestFrequency, Constants.DataRequestFrequencyDefault));
            dataRequestTimer.Tick += DataRequest;

            void DataRequest(object sender, EventArgs e)
            {
                // Callback funksjon som kalles når socketClient er ferdig med å hente data
                SocketCallback socketCallback = new SocketCallback(ProcessSocketHMSDataList);

                // Start en data fetch mot server
                SocketClient socketClient = new SocketClient(socketConsole, socketCallback, clientDeniedCallback);

                // Sette kommando og parametre
                socketClient.SetParams(
                    PacketCommand.GetDataUpdate,
                    socketHMSDataList);

                socketClient.Start();
            }

            // Sensor Status Retrieval Timer
            /////////////////////////////////////////////////////////////
            sensorStatusRequestTimer.Interval = TimeSpan.FromMilliseconds(config.ReadWithDefault(ConfigKey.SensorStatusRequestFrequency, Constants.SensorStatusRequestFrequencyDefault));
            sensorStatusRequestTimer.Tick += SensorStatusRequest;

            void SensorStatusRequest(object sender, EventArgs e)
            {
                // Callback funksjon som kalles når socketClient er ferdig med å hente data
                SocketCallback socketCallback = new SocketCallback(ProcessSocketSensorStatusList);

                // Start en data fetch mot server
                SocketClient socketClient = new SocketClient(socketConsole, socketCallback, clientDeniedCallback);

                // Sette kommando og parametre
                socketClient.SetParams(
                    PacketCommand.GetSensorStatus,
                    null,
                    socketSensorStatusList);

                socketClient.Start();
            }
        }

        public void SendUserInput(UserInputs userInputs, SocketCallback socketCallback)
        {
            try
            {
                // Start en data sending til server
                SocketClient socketClient = new SocketClient(socketConsole, socketCallback, clientDeniedCallback);

                // Sette kommando og parametre
                socketClient.SetParams(
                    PacketCommand.SetUserInputs,
                    null,
                    null,
                    userInputs);

                socketClient.Start();
            }
            catch (Exception ex)
            {
                if (AdminMode.IsActive)
                    socketConsole.Add(string.Format("SendUserInput: {0}", ex.Message));
            }
        }

        public void ProcessSocketHMSDataList()
        {
            // Prosessere data som kommer fra socketClient
            if (socketHMSDataList != null)
            {
                ProcessSocketHMSData(socketHMSDataList);

                // Ferdig med å hente data fra server
                if (dataRequestCallback != null)
                    dataRequestCallback();
            }

            lastDataReceivedTime = DateTime.UtcNow;
        }

        private void ProcessSocketHMSData(RadObservableCollection<HMSData> socketHMSDataList)
        {
            try
            {
                // Lese data timeout fra config
                double dataTimeout = config.ReadWithDefault(ConfigKey.DataTimeout, Constants.DataTimeoutDefault);

                lock (hmsDataCollection.GetDataListLock())
                {
                    RadObservableCollection<HMSData> hmsDataList = hmsDataCollection.GetDataList();

                    //// TEST
                    //HMSData testData1 = new HMSData();
                    //HMSData testData2 = new HMSData();
                    //HMSData testData3 = new HMSData();
                    //HMSData testData4 = new HMSData();
                    //HMSData testData5 = new HMSData();
                    //HMSData testData6 = new HMSData();
                    //HMSData testData7 = new HMSData();
                    //HMSData testData8 = new HMSData();

                    //// Sjekke om listen vi har er like lang som listen som kommer inn
                    //if (socketHMSDataList.Count() != hmsDataCollection.Count())
                    //    // I så tilfelle sletter vi listen vår og ny blir generert under
                    //    hmsDataList.Clear();

                    //// TEST
                    //if (hmsDataCollection.GetData(ValueType.MSI).status == DataStatus.OK)
                    //{
                    //    testData1.Set(hmsDataCollection.GetData(ValueType.MSI));
                    //}

                    // Løper gjennom HMS data listen fra socket
                    foreach (var socketHMSData in socketHMSDataList.ToList())
                    {
                        if (socketHMSData != null)
                        {
                            // Finne match i HMS data listen
                            var hmsData = hmsDataList.Where(x => x?.id == socketHMSData?.id);

                            // Fant match?
                            if (hmsData?.Count() > 0 && socketHMSData != null)
                            {
                                //// TEST
                                //if (hmsData.First().id == (int)ValueType.MSI)
                                //{
                                //    testData2.Set(hmsData.First());
                                //    testData3.Set(socketHMSData);
                                //}

                                // Overføre data
                                hmsData.First().Set(socketHMSData); // <--- ok blip

                                //// TEST
                                //if (hmsData.First().id == (int)ValueType.MSI)
                                //{
                                //    testData4.Set(hmsData.First());
                                //    testData5.Set(socketHMSData);
                                //}

                                // Sjekke timestamp for data timeout
                                if (hmsData.First().timestamp.AddMilliseconds(dataTimeout) < DateTime.UtcNow)
                                    hmsData.First().status = DataStatus.TIMEOUT_ERROR;

                                //// TEST
                                //if (hmsData.First().id == (int)ValueType.MSI)
                                //{
                                //    testData6.Set(hmsData.First());
                                //    testData7.Set(socketHMSData);
                                //}
                            }
                            // Ikke match?
                            else
                            {
                                // Legg inn i listen
                                hmsDataList.Add(new HMSData(socketHMSData));
                            }
                        }
                    }

                    //// TEST
                    //if (hmsDataCollection.GetData(ValueType.MSI).status == DataStatus.OK)
                    //{
                    //    testData8.Set(hmsDataCollection.GetData(ValueType.MSI));
                    //}
                }
            }
            catch (Exception ex)
            {
                if (AdminMode.IsActive)
                    socketConsole.Add(string.Format("ProcessSocketHMSData: {0}", ex.Message));
            }
        }

        public void ProcessSocketSensorStatusList()
        {
            // Prosessere data som kommer fra socketClient
            if (socketSensorStatusList != null)
            {
                ProcessSocketSensorStatus(socketSensorStatusList);

                // Ferdig med å hente data fra server
                if (dataRequestCallback != null)
                    dataRequestCallback();
            }
        }

        private void ProcessSocketSensorStatus(RadObservableCollection<SensorGroup> socketSensorStatusList)
        {
            // Lese data timeout fra config
            double dataTimeout = config.ReadWithDefault(ConfigKey.DataTimeout, Constants.DataTimeoutDefault);

            lock (sensorStatusListLock)
            {
                //// Sjekke om listen vi har er like lang som listen som kommer inn
                //if (socketSensorStatusList.Count() != sensorStatusList.Count())
                //    // I så tilfelle sletter vi listen vår og ny blir generert under
                //    sensorStatusList.Clear();

                // Løper gjennom sensor group status listen fra socket
                foreach (var socketSensorStatus in socketSensorStatusList.ToList())
                {
                    if (socketSensorStatus != null)
                    {
                        // Finne match i sensor group status listen
                        var sensor = sensorStatusList.Where(x => x?.id == socketSensorStatus.id);

                        // Fant match?
                        if (sensor?.Count() > 0)
                        {
                            // Overføre data
                            sensor.First().Set(socketSensorStatus);
                        }
                        // Ikke match?
                        else
                        {
                            // Legg inn i listen
                            sensorStatusList.Add(new SensorGroup(socketSensorStatus));
                        }
                    }
                }
            }
        }

        public void Start()
        {
            dataRequestTimer.Start();
            sensorStatusRequestTimer.Start();
        }

        public void Stop()
        {
            dataRequestTimer.Stop();
            sensorStatusRequestTimer.Stop();
        }
    }
}
