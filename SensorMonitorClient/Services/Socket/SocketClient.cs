using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Telerik.Windows.Data;

namespace SensorMonitorClient
{
    class SocketClient
    {
        // ManualResetEvent instances signal completion.  
        private static ManualResetEvent connectDone = new ManualResetEvent(false);
        private static ManualResetEvent sendDone    = new ManualResetEvent(false);
        private static ManualResetEvent receiveDone = new ManualResetEvent(false);

        // The response from the remote device.  
        private static string response = string.Empty;

        private RadObservableCollectionEx<HMSData> hmsDataList;
        private RadObservableCollectionEx<Sensor> sensorStatusList;
        private UserInputs userInputs;

        // Socker Client worker
        private BackgroundWorker socketWorker;

        private SocketConsole socketConsole;

        private string serverAddress;
        private int serverPort;

        // Packet Parametre
        // En packet består av:
        // 1. Command
        // 2. Payload
        // 3. EOF
        private string command;
        private string payload;

        // Configuration
        Config config;

        private ServerCom.SocketCallback socketCallback;

        private SensorStatusVM sensorStatusVM;

        public SocketClient(SocketConsole socketConsole, ServerCom.SocketCallback socketCallback = null, SensorStatusVM sensorStatusVM = null)
        {
            this.socketConsole = socketConsole;
            this.socketCallback = socketCallback;
            this.sensorStatusVM = sensorStatusVM;

            config = new Config();

            try
            {
                serverAddress = config.Read(ConfigKey.ServerAddress);
                serverPort = config.Read(ConfigKey.ServerPort, Constants.ServerPortDefault);
            }
            catch (Exception) { } // Ingenting

            // Socket Listener Worker for kontinuerlig innhenting av data (data update)
            socketWorker = new BackgroundWorker();
            socketWorker.DoWork += DoSocketWork;
        }

        public void SetParams(string command, RadObservableCollectionEx<HMSData> hmsDataList = null, RadObservableCollectionEx<Sensor> sensorStatusList = null, UserInputs userInputs = null, string payload = "")
        {
            this.command = command;
            this.hmsDataList = hmsDataList;
            this.sensorStatusList = sensorStatusList;
            this.userInputs = userInputs;
            this.payload = payload;
        }

        public void DoSocketWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                // Forberede pakken som skal sendes
                string packet;

                switch (command)
                {
                    // Get Data Update
                    case Constants.CommandGetDataUpdate:

                        // Bare kommando med eof
                        packet = $"{command}{Constants.EOF}";

                        if (AdminMode.IsActive)
                            socketConsole?.Add("Requesting data update...");

                        break;

                    // Get Sensor Status
                    case Constants.CommandGetSensorStatus:

                        // Bare kommando med eof
                        packet = $"{command}{Constants.EOF}";

                        if (AdminMode.IsActive)
                            socketConsole?.Add("Requesting sensor status...");

                        break;

                    // Sende User Inputs
                    case Constants.CommandSetUserInputs:

                        // Kommando + user inputs + eof
                        packet = $"{command}{payload}{Constants.EOF}";

                        if (AdminMode.IsActive)
                            socketConsole?.Add("Setting user inputs...");

                        break;

                    // Get User Inputs
                    case Constants.CommandGetUserInputs:

                        // Bare kommando med eof
                        packet = $"{command}{Constants.EOF}";

                        if (AdminMode.IsActive)
                            socketConsole?.Add("Requesting user inputs...");

                        break;

                    default:
                        packet = string.Empty;
                        break;
                }

                // Init
                connectDone.Reset();
                sendDone.Reset();
                receiveDone.Reset();
                response = string.Empty;

                // Sette opp endpoint for socket forbindelsen 
                IPHostEntry ipHostInfo = Dns.GetHostEntry(serverAddress);
                IPAddress ipAddress = ipHostInfo.AddressList[0];
                IPEndPoint remoteIP = new IPEndPoint(ipAddress, serverPort);

                // Opprette TCP/IP socket.  
                Socket client = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                client.SendTimeout = 1000;
                client.ReceiveTimeout = 1000;

                // Opprette forbindelse
                IAsyncResult result = client.BeginConnect(remoteIP, new AsyncCallback(ConnectCallback), client);
                connectDone.WaitOne(Constants.SocketTimeout, true);

                if (client.Connected && packet != string.Empty)
                {
                    // Sende data/forespørsel
                    Send(client, packet);
                    sendDone.WaitOne(Constants.SocketTimeout, true);

                    // Motta svar/data
                    Receive(client);
                    receiveDone.WaitOne(Constants.SocketTimeout, true);

                    // Avslutte/Release socket
                    client.Shutdown(SocketShutdown.Both);
                    client.Disconnect(true);

                    if (AdminMode.IsActive)
                        socketConsole?.Add(string.Format("Response received : {0}", response));

                    // Prosessere mottatt data
                    ProcessReceivedData(response, command);
                }
                else
                {
                    if (AdminMode.IsActive)
                        socketConsole?.Add("No responce from server");

                    client.Disconnect(true);
                }
            }
            catch (ObjectDisposedException odx)
            {
                if (AdminMode.IsActive)
                    socketConsole?.Add(string.Format("RequestData ObjectDisposedException: {0}", odx.Message));
            }
            catch (SocketException sx)
            {
                if (AdminMode.IsActive)
                    socketConsole?.Add(string.Format("RequestData SocketException: {0}", sx.Message));
            }
            catch (Exception ex)
            {
                if (AdminMode.IsActive)
                    socketConsole?.Add(string.Format("RequestData: {0}", ex.Message));
            }
        }

        private void ConnectCallback(IAsyncResult ar)
        {
            try
            {
                // Hente socket fra state object
                Socket client = (Socket)ar.AsyncState;

                // Ferdigstille forbindelsen 
                client.EndConnect(ar);

                if (AdminMode.IsActive)
                {
                    socketConsole?.Add(string.Format("Server address {0}:{1}", serverAddress, serverPort));

                    socketConsole?.Add(string.Format("Remote end-point {0}:{1}",
                        (client.RemoteEndPoint as IPEndPoint).Address.MapToIPv4().ToString(),
                        (client.RemoteEndPoint as IPEndPoint).Port.ToString()));
                }

                // Signalisert at forbindelsen er satt 
                connectDone.Set();
            }
            catch (ObjectDisposedException)
            {
                // Ingen melding, socket lukket
            }
            catch (SocketException sx)
            {
                //if (sx.SocketErrorCode == SocketError.ConnectionRefused)
                if (AdminMode.IsActive)
                    socketConsole?.Add(string.Format("ConnectCallback SocketException: {0}", sx.Message));
            }
            catch (Exception ex)
            {
                if (AdminMode.IsActive)
                    socketConsole?.Add(string.Format("ConnectCallback: {0}", ex.Message));
            }
        }

        private void Receive(Socket client)
        {
            try
            {
                // Opprette socket state object 
                SocketState state = new SocketState();
                state.workSocket = client;

                // Start mottak av data 
                client.BeginReceive(state.buffer, 0, SocketState.BufferSize, 0, new AsyncCallback(ReceiveCallback), state);
            }
            catch (ObjectDisposedException odx)
            {
                // Ingen melding, socket lukket
                if (AdminMode.IsActive)
                    socketConsole?.Add(string.Format("Receive ObjectDisposedException: {0}", odx.Message));
            }
            catch (SocketException sx)
            {
                //if (sx.SocketErrorCode == SocketError.ConnectionRefused)
                if (AdminMode.IsActive)
                    socketConsole?.Add(string.Format("Receive SocketException: {0}", sx.Message));
            }
            catch (Exception ex)
            {
                if (AdminMode.IsActive)
                    socketConsole?.Add(string.Format("Receive: {0}", ex.Message));
            }
        }

        private void ReceiveCallback(IAsyncResult ar)
        {
            try
            {
                // Hente socket state object og client socket fra det asynkrone socket state object
                SocketState state = (SocketState)ar.AsyncState;
                Socket client = state.workSocket;

                // Lese data
                int bytesRead = client.EndReceive(ar);

                if (bytesRead > 0)
                {
                    // Lagrer det vi har mottatt, men det kan komme mer data 
                    state.sb.Append(Encoding.ASCII.GetString(state.buffer, 0, bytesRead));

                    // Hente resten av dataene 
                    client.BeginReceive(state.buffer, 0, SocketState.BufferSize, 0, new AsyncCallback(ReceiveCallback), state);
                }
                else
                {
                    // Alle dataene er mottatt, legg de i response
                    if (state.sb.Length > 1)
                    {
                        response = state.sb.ToString();
                    }

                    // Signaliser at alle dataene er mottatt
                    receiveDone.Set();
                }
            }
            catch (ObjectDisposedException odx)
            {
                // Ingen melding, socket lukket
                if (AdminMode.IsActive)
                    socketConsole?.Add(string.Format("ReceiveCallback ObjectDisposedException: {0}", odx.Message));
            }
            catch (SocketException sx)
            {
                //if (sx.SocketErrorCode == SocketError.ConnectionRefused)
                if (AdminMode.IsActive)
                    socketConsole?.Add(string.Format("ReceiveCallback SocketException: {0}", sx.Message));
            }
            catch (Exception ex)
            {
                if (AdminMode.IsActive)
                    socketConsole?.Add(string.Format("ReceiveCallback: {0}", ex.Message));
            }
        }

        private void Send(Socket client, string packet)
        {
            try
            {
                // Konverterer string data til byte data ved bruk av ASCII encoding
                byte[] byteData = Encoding.ASCII.GetBytes(packet);

                // Start sending 
                client.BeginSend(byteData, 0, byteData.Length, 0, new AsyncCallback(SendCallback), client);
            }
            catch (ObjectDisposedException)
            {
                // Ingen melding, socket lukket
            }
            catch (SocketException sx)
            {
                //if (sx.SocketErrorCode == SocketError.ConnectionRefused)
                if (AdminMode.IsActive)
                    socketConsole?.Add(string.Format("Send SocketException: {0}", sx.Message));
            }
            catch (Exception ex)
            {
                if (AdminMode.IsActive)
                    socketConsole?.Add(string.Format("Send: {0}", ex.Message));
            }
        }

        private void SendCallback(IAsyncResult ar)
        {
            try
            {
                // Hente socket fra socket state object.  
                Socket client = (Socket)ar.AsyncState;

                // Ferdigstill sending av data 
                int bytesSent = client.EndSend(ar);

                if (AdminMode.IsActive)
                    socketConsole?.Add(string.Format("Sent {0} bytes to server.", bytesSent));

                // Signaliser at alle data har blitt sendt 
                sendDone.Set();
            }
            catch (ObjectDisposedException odx)
            {
                // Ingen melding, socket lukket
                if (AdminMode.IsActive)
                    socketConsole?.Add(string.Format("Send ObjectDisposedException: {0}", odx.Message));
            }
            catch (SocketException sx)
            {
                //if (sx.SocketErrorCode == SocketError.ConnectionRefused)
                if (AdminMode.IsActive)
                    socketConsole?.Add(string.Format("Send SocketException: {0}", sx.Message));
            }
            catch (Exception ex)
            {
                if (AdminMode.IsActive)
                    socketConsole?.Add(string.Format("Send: {0}", ex.Message));
            }
        }

        public void Start()
        {
            socketWorker.RunWorkerAsync();
        }

        private void ProcessReceivedData(string receivedData, string command)
        {
            string payload;

            try
            {
                switch (command)
                {
                    // Get Data Update
                    //////////////////////////
                    case Constants.CommandGetDataUpdate:

                        // Fjerne command
                        payload = receivedData.Substring(receivedData.IndexOf(command) + command.Length);

                        // Fjerne end-of-file
                        payload = payload.Substring(0, payload.LastIndexOf(Constants.EOF));

                        if (payload.CompareTo(string.Empty) != 0)
                        {
                            // De-serialisere fra JSON
                            List<HMSData> dataList = JsonSerializer.Deserialize<List<HMSData>>(payload);

                            // Overføre mottatt data til lagringsplass
                            TransferReceivedData(dataList);

                            // Ferdig med å hente data fra socket -> si i fra at vi er ferdig og prosessere data
                            if (socketCallback != null)
                                socketCallback();
                        }
                        break;

                    // Get Sensor Status
                    //////////////////////////
                    case Constants.CommandGetSensorStatus:

                        // Fjerne command
                        payload = receivedData.Substring(receivedData.IndexOf(command) + command.Length);

                        // Fjerne end-of-file
                        payload = payload.Substring(0, payload.LastIndexOf(Constants.EOF));

                        if (payload.CompareTo(string.Empty) != 0)
                        {
                            // De-serialisere fra JSON
                            List<Sensor> sensorStatusList = JsonSerializer.Deserialize<List<Sensor>>(payload);

                            // Overføre mottatt data til lagringsplass
                            TransferReceivedSensorStatus(sensorStatusList);

                            // Ferdig med å hente data fra socket -> si i fra at vi er ferdig og prosessere data
                            if (socketCallback != null)
                                socketCallback();
                        }
                        break;

                    // Set User Inputs
                    //////////////////////////
                    case Constants.CommandSetUserInputs:

                        // Når vi mottar denne kommandoen i klienten betyr det at server verifiserer at den har mottatt kommandoen.
                        // Det kommer ingen data som skal behandles.
                        // Fungerer som en handshake på mottatt user inputs.

                        if (socketCallback != null)
                            socketCallback();

                        break;

                    // Get User Inputs
                    //////////////////////////
                    case Constants.CommandGetUserInputs:

                        // Fjerne command
                        payload = receivedData.Substring(receivedData.IndexOf(command) + command.Length);

                        // Fjerne end-of-file
                        payload = payload.Substring(0, payload.LastIndexOf(Constants.EOF));

                        if (payload.CompareTo(string.Empty) != 0)
                        {
                            // De-serialisere fra JSON
                            UserInputs userInputsReceived = JsonSerializer.Deserialize<UserInputs>(payload);

                            // Overføre mottatt data til lagringsplass
                            TransferReceivedUserInputs(userInputsReceived);

                            // Ferdig med å hente data fra socket -> si i fra at vi er ferdig og prosessere data
                            if (socketCallback != null)
                                socketCallback();
                        }
                        break;

                }
            }
            catch (Exception ex)
            {
                if (AdminMode.IsActive)
                    socketConsole?.Add(string.Format("ProcessReceivedData: {0}", ex.Message));
            }
        }

        private void TransferReceivedData(List<HMSData> dataList)
        {
            // NB! Samme problem under!
            // Gjør en del sjekker, søk og kopiering av data her. Hvorfor ikke bare slette alle data og så kopiere alt?
            // Får i skjeldne tilfeller problem med lesing av data i kode som kjører fra andre tråder. Kan krasje klient programmet.
            // Sletting av hele data listen bør derfor unngåes om mulig.
            // Endring i listen som krever at hele listen må slettes vil nå kun være aktuelt når admin modus er
            // aktivt og operatør driver å endrer og setter opp sensorer i server.
            // 2021.06.22: Tror kanskje det over er et tilbakelagt stadium.
            // 2021.08.16: Har ikke hatt krasj her siden sist. Suksess!

            bool listResetRequired = false;

            // Sjekker først om lengden på innkommende data liste er like lagret data liste
            if (dataList.Count() == hmsDataList?.Count())
            {
                // Starte overføring av data

                // Legge nye data inn i lagringsliste
                foreach (var item in dataList)
                {
                    // Finne igjen ID i lagringslisten
                    var sensorData = hmsDataList.Where(x => x.id == item.id);
                    if (sensorData.Count() > 0)
                    {
                        // Lagre data
                        sensorData.First().Set(item);

                        if (AdminMode.IsActive)
                            socketConsole?.Add(string.Format("ProcessReceivedData: id:{0}, data:{1}, timestamp:{2}", item.id, item.data, item.timestamp.ToString()));
                    }
                    else
                    {
                        // Dersom vi ikke fant ID betyr det at listen er endret av server
                        listResetRequired = true;
                    }
                }
            }
            else
            {
                // Ulike lengde på listene -> må legge inn alt på nytt
                listResetRequired = true;
            }

            // Data listen fra server er endret og hele listen på klient siden må legges inn på nytt
            if (listResetRequired)
            {
                // Fjerne alle tidligere data fra liste
                hmsDataList?.Clear();

                // Legge inn alle data på nytt
                foreach (var item in dataList)
                {
                    hmsDataList?.Add(item);

                    if (AdminMode.IsActive)
                        socketConsole?.Add(string.Format("ProcessReceivedData: id:{0}, data:{1}, timestamp:{2}", item.id, item.data, item.timestamp.ToString()));
                }
            }
        }

        private void TransferReceivedSensorStatus(List<Sensor> dataList)
        {
            // NB! Samme problem som over!
            // Gjør en del sjekker, søk og kopiering av data her. Hvorfor ikke bare slette alle data og så kopiere alt?
            // Får i skjeldne tilfeller problem med lesing av data i kode som kjører fra andre tråder. Kan krasje klient programmet.
            // Sletting av hele data listen bør derfor unngåes om mulig.
            // Endring i listen som krever at hele listen må slettes vil nå kun være aktuelt når admin modus er
            // aktivt og operatør driver å endrer og setter opp sensorer i server.
            // 22.06.2021: Tror kanskje det over er et tilbakelagt stadium.
            // 2021.08.16: Har ikke hatt krasj her siden sist. Suksess!

            bool listResetRequired = false;

            // Sjekker først om lengden på innkommende data liste er lik lagret data liste
            if (dataList.Count() == sensorStatusList?.Count())
            {
                // Starte overføring av data

                // Legge nye data inn i lagringsliste
                foreach (var item in dataList)
                {
                    // Finne igjen ID i lagringslisten
                    var sensorData = sensorStatusList.Where(x => x.id == item.id);
                    if (sensorData.Count() > 0)
                    {
                        // Lagre data
                        sensorData.First().Set(item);

                        if (AdminMode.IsActive)
                            socketConsole?.Add(string.Format("ProcessReceivedData: id:{0}, data:{1}, timestamp:{2}", item.id, item.name, item.active.ToString()));
                    }
                    else
                    {
                        // Dersom vi ikke fant ID betyr det at listen er endret av server
                        listResetRequired = true;
                    }
                }
            }
            else
            {
                // Ulike lengde på listene -> må legge inn alt på nytt
                listResetRequired = true;
            }

            // Data listen fra server er endret og hele listen på klient siden må legges inn på nytt
            if (listResetRequired)
            {
                // Fjerne alle tidligere data fra liste
                sensorStatusList?.Clear();

                // Legge inn alle data på nytt
                foreach (var item in dataList)
                {
                    sensorStatusList?.Add(item);

                    if (AdminMode.IsActive)
                        socketConsole?.Add(string.Format("ProcessReceivedData: id:{0}, data:{1}, timestamp:{2}", item.id, item.name, item.active.ToString()));
                }
            }

            // Oppdatere navn i SensorStatusVM
            sensorStatusVM?.Update();
        }

        private void TransferReceivedUserInputs(UserInputs userInputsReceived)
        {
            userInputs.Set(userInputsReceived);
        }

        public static IPAddress GetIPAddress()
        {
            IPAddress[] hostAddresses = Dns.GetHostAddresses("");

            foreach (IPAddress hostAddress in hostAddresses)
            {
                if (hostAddress.AddressFamily == AddressFamily.InterNetwork &&
                    !IPAddress.IsLoopback(hostAddress) &&                       // Ignore loopback addresses
                    !hostAddress.ToString().StartsWith("169.254."))             // Ignore link-local addresses
                    return hostAddress;
            }
            return IPAddress.None;
        }
    }
}
