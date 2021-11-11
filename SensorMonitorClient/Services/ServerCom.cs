using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Threading;
using Telerik.Windows.Data;

namespace SensorMonitorClient
{
    public class ServerCom
    {
        // Admin Settings
        private AdminSettingsVM adminSettingsVM;

        // Configuration settings
        private Config config;

        // TCP/IP Socket console for utskrift av trafikk
        private SocketConsole socketConsole;

        // TCP/IP Data Request Timer
        private DispatcherTimer dataRequestTimer = new DispatcherTimer();
        private DispatcherTimer sensorStatusRequestTimer = new DispatcherTimer();
        private DispatcherTimer userInputsRequestTimer = new DispatcherTimer();

        // Liste med HMS data
        private RadObservableCollectionEx<HMSData> hmsDataList;
        private RadObservableCollectionEx<HMSData> socketHMSDataList = new RadObservableCollectionEx<HMSData>();

        // Liste med sensor status
        private RadObservableCollectionEx<SensorGroup> sensorStatusList;
        private RadObservableCollectionEx<SensorGroup> socketSensorStatusList = new RadObservableCollectionEx<SensorGroup>();

        // User Inputs
        private UserInputs userInputsReceived = new UserInputs();

        // Socket client callback
        public delegate void SocketCallback();

        // Data Request callback
        private MainWindow.DataRequestCallback dataRequestCallback;

        // User Inputs have been set callback
        //private MainWindow.UserInputsSetCallback userInputsSetCallback;

        private SensorStatusDisplayVM sensorStatusDisplayVM;
        private UserInputsVM userInputsVM;

        // Tidspunkt for sist mottatt HMS data
        public DateTime lastDataReceivedTime = new DateTime();

        public void Init(
            SocketConsole socketConsole,
            HMSDataCollection hmsDataCollection,
            AdminSettingsVM adminSettingsVM,
            SensorGroupStatus sensorStatus,
            SensorStatusDisplayVM sensorStatusVM,
            UserInputsVM userInputsVM,
            MainWindow.DataRequestCallback dataRequestCallback,
            Config config)
        {
            // Socket Console
            this.socketConsole = socketConsole;

            // Data fra server
            hmsDataList = hmsDataCollection.GetDataList();

            // Admin Settings
            this.adminSettingsVM = adminSettingsVM;

            // Sensor Status fra server
            sensorStatusList = sensorStatus.GetSensorList();

            // Oppdatere sensor status på skjerm
            this.sensorStatusDisplayVM = sensorStatusVM;

            // User Inputs VM
            this.userInputsVM = userInputsVM;

            // Data Request callback
            this.dataRequestCallback = dataRequestCallback;

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
            dataRequestTimer.Interval = TimeSpan.FromMilliseconds(config.Read(ConfigKey.DataRequestFrequency, Constants.DataRequestFrequencyDefault));
            dataRequestTimer.Tick += DataRequest;

            void DataRequest(object sender, EventArgs e)
            {
                // Callback funksjon som kalles når socketClient er ferdig med å hente data
                SocketCallback socketCallback = new SocketCallback(ProcessSocketHMSDataList);

                // Start en data fetch mot server
                SocketClient socketClient = new SocketClient(socketConsole, socketCallback);
                
                // Sette kommando og parametre
                socketClient.SetParams(
                    Constants.CommandGetDataUpdate,
                    socketHMSDataList);
                
                socketClient.Start();
            }

            // Sensor Status Retrieval Timer
            /////////////////////////////////////////////////////////////
            sensorStatusRequestTimer.Interval = TimeSpan.FromMilliseconds(config.Read(ConfigKey.SensorStatusRequestFrequency, Constants.SensorStatusRequestFrequencyDefault));
            sensorStatusRequestTimer.Tick += SensorStatusRequest;

            void SensorStatusRequest(object sender, EventArgs e)
            {
                // Callback funksjon som kalles når socketClient er ferdig med å hente data
                SocketCallback socketCallback = new SocketCallback(ProcessSocketSensorStatusList);

                // Start en data fetch mot server
                SocketClient socketClient = new SocketClient(socketConsole, socketCallback, sensorStatusDisplayVM);

                // Sette kommando og parametre
                socketClient.SetParams(
                    Constants.CommandGetSensorStatus,
                    null,
                    socketSensorStatusList);

                socketClient.Start();
            }

            // User Inputs Retrieval Timer
            /////////////////////////////////////////////////////////////
            userInputsRequestTimer.Interval = TimeSpan.FromMilliseconds(Constants.UserInputsSetCheckFrequency);
            userInputsRequestTimer.Tick += userInputsRequest;

            void userInputsRequest(object sender, EventArgs e)
            {
                // Callback funksjon som kalles når socketClient er ferdig med å hente data
                SocketCallback socketCallback = new SocketCallback(ProcessSocketUserInputs);

                // Start en data fetch mot server
                SocketClient socketClient = new SocketClient(socketConsole, socketCallback, sensorStatusDisplayVM);

                // Sette kommando og parametre
                socketClient.SetParams(
                    Constants.CommandGetUserInputs,
                    null,
                    null,
                    userInputsReceived);

                socketClient.Start();
            }
        }

        public void SendUserInput(UserInputs userInputs, SocketCallback socketCallback)
        {
            try
            {
                // Start en data sending til server
                SocketClient socketClient = new SocketClient(socketConsole, socketCallback);

                // Serialiserer data til JSON objekt
                string payload = JsonSerializer.Serialize(userInputs);

                // Sette kommando og parametre
                socketClient.SetParams(
                    Constants.CommandSetUserInputs,
                    null,
                    null,
                    null,
                    payload);

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

        private void ProcessSocketHMSData(RadObservableCollectionEx<HMSData> socketHMSDataList)
        {
            // Lese data timeout fra config
            double dataTimeout = config.Read(ConfigKey.DataTimeout, Constants.DataTimeoutDefault);

            lock (hmsDataList)
            {
                // Sjekke om listen vi har er like lang som listen som kommer inn
                if (socketHMSDataList.Count() != hmsDataList.Count())
                    // I så tilfelle sletter vi listen vår og ny blir generert under
                    hmsDataList.Clear();

                // Løper gjennom HMS data listen fra socket
                foreach (var socketHMSData in socketHMSDataList.ToList())
                {
                    // Finne match i HMS data listen
                    var hmsData = hmsDataList.Where(x => x?.id == socketHMSData?.id);

                    // Fant match?
                    if (hmsData?.Count() > 0 && socketHMSData != null)
                    {
                        // Overføre data
                        hmsData.First().data = socketHMSData.data;

                        // Overføre time stamp
                        hmsData.First().timestamp = socketHMSData.timestamp;

                        // Name (dersom admin modus er aktiv på server)
                        hmsData.First().name = socketHMSData.name;

                        // Sjekke timestamp for data timeout
                        if (hmsData.First().timestamp.AddMilliseconds(dataTimeout) < DateTime.UtcNow)
                            hmsData.First().status = DataStatus.TIMEOUT_ERROR;
                        else
                            hmsData.First().status = DataStatus.OK;
                    }
                    // Ikke match?
                    else
                    {
                        // Legg inn i listen
                        hmsDataList.Add(socketHMSData);
                    }
                }
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

        private void ProcessSocketSensorStatus(RadObservableCollectionEx<SensorGroup> socketSensorStatusList)
        {
            // Lese data timeout fra config
            double dataTimeout = config.Read(ConfigKey.DataTimeout, Constants.DataTimeoutDefault);

            lock (sensorStatusList)
            {
                // Sjekke om listen vi har er like lang som listen som kommer inn
                if (socketSensorStatusList.Count() != sensorStatusList.Count())
                    // I så tilfelle sletter vi listen vår og ny blir generert under
                    sensorStatusList.Clear();

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
                            sensorStatusList.Add(socketSensorStatus);
                        }
                    }
                }
            }
        }

        public void ProcessSocketUserInputs()
        {
            // Prosessere data som kommer fra socketClient
            if (socketSensorStatusList != null)
            {
                // Flytte data til UserInputsVM
                userInputsVM.UserInputsReceived(userInputsReceived);

                // Ferdig med å hente data fra server
                if (dataRequestCallback != null)
                    dataRequestCallback();
            }
        }

        public void Start()
        {
            dataRequestTimer.Start();
            sensorStatusRequestTimer.Start();

            if (!adminSettingsVM.clientIsMaster)
                userInputsRequestTimer.Start();
        }

        public void Stop()
        {
            dataRequestTimer.Stop();
            sensorStatusRequestTimer.Stop();
            userInputsRequestTimer.Stop();
        }

        public void StartUserInputsRequest()
        {
            userInputsRequestTimer.Start();
        }

        public void StopUserInputsRequest()
        {
            userInputsRequestTimer.Stop();
        }
    }
}
