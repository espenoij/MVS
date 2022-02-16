using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Windows.Threading;

namespace HMS_Server
{
    class SocketListener
    {
        // Thread signal.  
        public static ManualResetEvent allDone = new ManualResetEvent(false);

        // Socket
        private Socket listener;

        // Socker listener worker
        private BackgroundWorker socketWorker;

        // Activation
        private ActivationVM activationVM;

        // Socket Console - utskrift av socket meldinger for debug
        SocketConsole socketConsole;

        // Liste med sensor data
        private RadObservableCollectionEx<HMSData> hmsOutputDataList;

        // Liste med sensor status
        private RadObservableCollectionEx<SensorGroup> sensorStatusOutputList;

        // Server Port
        private int serverPort;

        // Socket Listener satt på pause
        private bool socketListenerPaused = true;
        // Socket Listener exit
        private bool socketExit = false;

        // Configuration
        private Config config;

        // Klient ID liste
        private List<string> clientIDList = new List<string>();

        private MainWindow.UserInputsCallback userInputCallback;

        public SocketListener(
            HMSDataCollection hmsOutputData,
            HMSSensorGroupStatus sensorStatusOutput,
            SocketConsole socketConsole, 
            ActivationVM activationVM, 
            MainWindow.UserInputsCallback userInputCallback)
        {
            config = new Config();
            serverPort = config.ReadWithDefault(ConfigKey.ServerPort, Constants.ServerPortDefault);

            this.hmsOutputDataList = hmsOutputData.GetDataList();
            this.sensorStatusOutputList = sensorStatusOutput.GetSensorList();
            this.socketConsole = socketConsole;
            this.activationVM = activationVM;
            this.userInputCallback = userInputCallback;

            // Socket Listener Worker
            socketWorker = new BackgroundWorker();
            socketWorker.DoWork += StartSocketListener;
            socketWorker.RunWorkerCompleted += StopSocketListener;

            // Starte socket
            StartSocketWorker();

            // Socket Listener Check
            // Starter socket igjen dersom noe har skjedd slik at den har stoppet
            // Socket skal alltid kjøre!
            DispatcherTimer socketListenerCheck = new DispatcherTimer();
            socketListenerCheck.Interval = TimeSpan.FromMilliseconds(2000);
            socketListenerCheck.Tick += checkSocketListener;
            socketListenerCheck.Start();

            void checkSocketListener(object sender, EventArgs e)
            {
                // Kjører socket worker thread
                if (!socketWorker.IsBusy)
                {
                    // Socket opprettet?
                    if (listener != null)
                    {
                        // Er socket ikke tilkoblet?
                        if (!SocketConnected(listener))
                        {
                            // Restarte socket
                            StartSocketWorker();
                        }
                    }
                }
            }

            // Sjekk om socket er oppkoblet
            bool SocketConnected(Socket s)
            {
                bool part1 = s.Poll(1000, SelectMode.SelectRead);
                bool part2 = (s.Available == 0);
                if (part1 && part2)
                    return false;
                else
                    return true;
            }

            // Starter socket worker
            void StartSocketWorker()
            {
                try
                {
                    socketWorker.RunWorkerAsync();
                }
                catch (Exception ex)
                {
                    if (AdminMode.IsActive)
                        socketConsole.Add(string.Format("RunWorkerAsync (2):\n\n{0}", ex.Message));
                }
            }
        }

        public void Start()
        {
            socketListenerPaused = false;
        }

        public void Stop()
        {
            socketListenerPaused = true;
            //allDone.Reset();
        }

        public void Exit()
        {
            socketExit = true;
        }

        private void StartSocketListener(object sender, DoWorkEventArgs e)
        {
            // Starte opp socket og lytte etter innkommende kommunikasjon
            try
            {
                // Sette maskinens DNS som local endpoint
                IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
                IPAddress ipAddress = ipHostInfo.AddressList[0];
                IPEndPoint localEndPoint = new IPEndPoint(ipAddress, serverPort);

                // Opprette TCP/IP socket 
                listener = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                // Socket Options
                listener.SendTimeout = 1000;
                listener.ReceiveTimeout = 1000;

                // Binde socket til endpoint
                listener.Bind(localEndPoint);
                listener.Listen(10);

                while (!socketExit)
                {
                    if (!socketListenerPaused)
                    {
                        // Resette "ferdig" signalet
                        allDone.Reset();

                        if (AdminMode.IsActive)
                            socketConsole.Add("Waiting for a connection...");

                        // Starter en asynkron socket for å lytte etter innkommende kommunikasjon
                        listener.BeginAccept(new AsyncCallback(AcceptCallback), listener);

                        // Venter til kommunikasjonen er ferdig før vi går videre
                        allDone.WaitOne();
                    }
                    else
                    {
                        // Kommer vi her er serveren stoppet
                        Thread.Sleep(500);
                    }
                }
            }
            catch (ObjectDisposedException ex)
            {
                // Ingen melding, socket lukket OK
                if (AdminMode.IsActive)
                    socketConsole.Add(string.Format("ObjectDisposedException:\n\n{0}", ex.Message));
            }
            catch (Exception ex)
            {
                if (AdminMode.IsActive)
                    socketConsole.Add(string.Format("StartSocketListener:\n\n{0}", ex.Message));
            }
        }

        private void StopSocketListener(object sender, RunWorkerCompletedEventArgs e)
        {
            listener.Shutdown(SocketShutdown.Both);
            listener.Disconnect(true);
        }

        public void AcceptCallback(IAsyncResult ar)
        {
            try
            {
                // Signal til hoved/listener tråden om å fortsette 
                allDone.Set();

                // Socket som håndterer kommunikasjonen
                Socket listener = (Socket)ar.AsyncState;
                Socket handler = listener.EndAccept(ar);

                // Opprette socket state object
                SocketState state = new SocketState();
                state.workSocket = handler;

                // Starte mottak av data
                handler.BeginReceive(state.buffer, 0, SocketState.BufferSize, 0, new AsyncCallback(ReadCallback), state);
            }
            catch (ObjectDisposedException)
            {
                // Ingen melding, socket lukket
            }
            catch (Exception ex)
            {
                if (AdminMode.IsActive)
                    socketConsole.Add(string.Format("AcceptCallback: {0}", ex.Message));
            }
        }

        public void ReadCallback(IAsyncResult ar)
        {
            try
            {
                string data = string.Empty;

                // Hente socket og socket state objekt som skal håndtere lesing
                SocketState state = (SocketState)ar.AsyncState;
                Socket handler = state.workSocket;

                // Antall leste byte
                int bytesRead = handler.EndReceive(ar);

                if (bytesRead > 0)
                {
                    // I tilfelle der er mer data på vei, så lagrer vi det vi har fått så langt
                    state.sb.Append(Encoding.ASCII.GetString(state.buffer, 0, bytesRead));

                    // Sjekker etter end-of-file tag.
                    // Dersom ingen EOF tag, les mer...
                    data = state.sb.ToString();
                    if (data.IndexOf(Constants.EOF) > -1)
                    {
                        // Alle data mottat

                        if (AdminMode.IsActive)
                            socketConsole.Add(string.Format("Read {0} bytes from socket. Data : {1}", data.Length, data));

                        // Hvilken forespørsel har vi mottatt?
                        CommandRequestHandler(handler, data);
                    }
                    else
                    {
                        // Har ikke mottatt alle data ennå, les mer...
                        handler.BeginReceive(state.buffer, 0, SocketState.BufferSize, 0, new AsyncCallback(ReadCallback), state);
                    }
                }
            }
            catch (ObjectDisposedException)
            {
                // Ingen melding, socket lukket
            }
            catch (Exception ex)
            {
                if (AdminMode.IsActive)
                    socketConsole.Add(string.Format("ReadCallback: {0}", ex.Message));
            }
        }

        private void Send(Socket handler, string data)
        {
            try
            {
                // Konverterer fra string data to byte data ved hjelp av ASCII encoding
                byte[] byteData = Encoding.ASCII.GetBytes(data);

                // Begin sending the data to the remote device.  
                handler.BeginSend(byteData, 0, byteData.Length, 0, new AsyncCallback(SendCallback), handler);
            }
            catch (ObjectDisposedException)
            {
                // Ingen melding, socket lukket
            }
            catch (Exception ex)
            {
                if (AdminMode.IsActive)
                    socketConsole.Add(string.Format("Send: {0}", ex.Message));
            }
        }

        private void SendCallback(IAsyncResult ar)
        {
            try
            {
                // Hente socket fra socket state objekt 
                Socket handler = (Socket)ar.AsyncState;
                if (handler != null)
                {
                    // Avslutte sending til klient
                    int bytesSent = handler.EndSend(ar);

                    if (AdminMode.IsActive)
                        socketConsole.Add(string.Format("Sent {0} bytes to client.", bytesSent));

                    // Avslutte socket
                    handler.Shutdown(SocketShutdown.Both);
                    handler.Disconnect(false);
                }
            }
            catch (ObjectDisposedException)
            {
                // Ingen melding, socket lukket
            }
            catch (Exception ex)
            {
                if (AdminMode.IsActive)
                    socketConsole.Add(string.Format("SendCallback: {0}", ex.Message));
            }
        }

        private void CommandRequestHandler(Socket handler, string inPacket)
        {
            try
            {
                if (!string.IsNullOrEmpty(inPacket) &&          // Har vi data?
                    inPacket.LastIndexOf(Constants.EOF) > 0)    // Har vi EOF?
                {
                    SocketPacket outPacket = new SocketPacket();

                    // Fjerne end-of-file
                    inPacket = inPacket.Substring(0, inPacket.LastIndexOf(Constants.EOF));

                    // De-serialisere packet
                    SocketPacket packet = JsonSerializer.Deserialize<SocketPacket>(inPacket);

                    // Sjekke klient ID
                    // Server aksepterer bare et gitt antall klienter (spesifisert i lisensen til kunden)
                    bool clientIDCheckOK = true;

                    lock (clientIDList)
                    {
                        var foundClientID = clientIDList.Find(x => x == packet.clientID);
                        if (foundClientID == null)
                        {
                            // Fant ikke klient -> sjekke om vi har plass til å legge den inn i listen over aksepterte klienter
                            if (clientIDList.Count < activationVM.licenseMaxClients)
                            {
                                // Legge inn klient ID
                                clientIDList.Add(packet.clientID);
                            }
                            // Klient ID ligger ikke i listen og vi har ikke plass til flere
                            else
                            {
                                clientIDCheckOK = false;
                            }
                        }
                    }

                    // Klient ID sjekk ok - normal operasjon
                    if (clientIDCheckOK)
                    {
                        switch (packet.command)
                        {
                            // Command: Get Data Update
                            ////////////////////////////////////////////////////////////////
                            case PacketCommand.GetDataUpdate:
                                {
                                    // Serialisere HMS data og lage data pakke
                                    outPacket.command = packet.command;
                                    outPacket.payload = SerializeHMSData();

                                    if (AdminMode.IsActive)
                                        socketConsole.Add(string.Format("Send: jsonData: {0}", outPacket));
                                }
                                break;

                            // Command: Get Sensor Status
                            ////////////////////////////////////////////////////////////////
                            case PacketCommand.GetSensorStatus:
                                {
                                    // Serialisere sensor status data og lage data pakke
                                    outPacket.command = packet.command;
                                    outPacket.payload = SerializeSensorStatus();

                                    if (AdminMode.IsActive)
                                        socketConsole.Add(string.Format("Send: jsonData: {0}", outPacket));
                                }
                                break;

                            // Command: Set User Inputs
                            ////////////////////////////////////////////////////////////////
                            case PacketCommand.SetUserInputs:
                                {
                                    // De-serialisere payload
                                    UserInputs userInputs = JsonSerializer.Deserialize<UserInputs>(packet.payload);

                                    // Callback funksjon for å behandle user inputs
                                    userInputCallback(userInputs);

                                    // Sende svar til klient om at user inputs er mottatt
                                    outPacket.command = packet.command; // Ingen payload data

                                    if (AdminMode.IsActive)
                                        socketConsole.Add(string.Format("Send: jsonData: {0}", outPacket));
                                }
                                break;

                            default:
                                break;
                        }
                    }
                    // Klient ID ikke ok
                    else
                    {
                        // Send client denied melding og få klienten til å stoppe videre forspørsler/avslutte
                        outPacket.command = PacketCommand.ClientDenied;

                        if (AdminMode.IsActive)
                            socketConsole.Add(string.Format("Send: jsonData: {0}", outPacket));
                    }

                    // Kommando/data som skal sendes
                    // Må ha EOF til slutt
                    string sendData = $"{JsonSerializer.Serialize(outPacket)}{Constants.EOF}";

                    // Sende kommando/data til klient
                    Send(handler, sendData);
                }
            }
            catch (Exception ex)
            {
                if (AdminMode.IsActive)
                    socketConsole.Add(string.Format("CommandRequestHandler: {0}", ex.Message));
            }
        }

        private string SerializeHMSData()
        {
            try
            {
                // Liste med data som skal sendes
                List<HMSData> sendData = new List<HMSData>();

                lock (hmsOutputDataList)
                {
                    // Plukke data fra HMS data listen
                    foreach (var hmsOutputData in hmsOutputDataList)
                    {
                        //if (AdminMode.IsActive)
                        //    socketConsole.Add(string.Format("SerializeSensorData: id:{0}, data:{1}, timestamp:{2}", sensorData.id, sensorData.calculatedData, sensorData.timeStampString));

                        HMSData hmsData = new HMSData();

                        // Kopiere alle data
                        hmsData.Set(hmsOutputData);

                        // JSON håndterer ikke NaN
                        // Sender derfor 0 i data og timestamp settes til minimumsverdi for å indikere ubrukelige data
                        if (double.IsNaN(hmsData.data))
                        {
                            hmsData.data = 0;
                            hmsData.timestamp = DateTime.MinValue;
                        }

                        if (double.IsNaN(hmsData.data2))
                            hmsData.data2 = 0;

                        // Fjerner navnet på sensor data under vanlig kjøring for å redusere data mengden som sendes
                        if (!AdminMode.IsActive)
                            hmsData.name = string.Empty;

                        // Legge til i listen som skal sendes
                        sendData.Add(hmsData);
                    }
                }

                // Serialiserer data til JSON objekt
                return JsonSerializer.Serialize(sendData);
            }
            catch (Exception ex)
            {
                if (AdminMode.IsActive)
                    socketConsole.Add(string.Format("SerializeHMSData: {0}", ex.Message));
            }

            return string.Empty;
        }

        private string SerializeSensorStatus()
        {
            try
            {
                // Liste med data som skal sendes
                List<SensorGroup> sendData = new List<SensorGroup>();

                lock (sensorStatusOutputList)
                {
                    // Plukke data fra HMS data listen
                    foreach (var sensorStatusOutputData in sensorStatusOutputList)
                    {
                        // Kopiere alle data
                        // Legge til i listen som skal sendes
                        sendData.Add(new SensorGroup(sensorStatusOutputData));
                    }
                }

                // Serialiserer data til JSON objekt
                return JsonSerializer.Serialize(sendData);
            }
            catch (Exception ex)
            {
                if (AdminMode.IsActive)
                    socketConsole.Add(string.Format("SerializeSensorStatus: {0}", ex.Message));
            }

            return string.Empty;
        }

        //private string SerializeUserInputs()
        //{
        //    try
        //    {
        //        // Serialiserer data til JSON objekt
        //        return JsonSerializer.Serialize(userInputs);
        //    }
        //    catch (Exception ex)
        //    {
        //        if (AdminMode.IsActive)
        //            socketConsole.Add(string.Format("SerializeUserInputs: {0}", ex.Message));
        //    }

        //    return string.Empty;
        //}
    }
}
