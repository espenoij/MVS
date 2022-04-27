using System;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using Telerik.Windows.Controls;
using Telerik.Windows.Data;

namespace HMS_Client
{
    /// <summary>
    /// Interaction logic for SocketConsoleWindow.xaml
    /// </summary>
    public partial class SocketConsoleWindow : RadWindow
    {
        private SocketConsole socketConsole;

        private RadObservableCollection<SocketConsoleMessage> socketConsoleDisplayList = new RadObservableCollection<SocketConsoleMessage>();
        private DispatcherTimer socketConsoleUpdater;

        private RadObservableCollectionEx<HMSData> hmsDataList = new RadObservableCollectionEx<HMSData>();

        public SocketConsoleWindow(SocketConsole socketConsole, Config config)
        {
            InitializeComponent();

            // Socket console hvor vi mottar meldinger fra socket listener
            this.socketConsole = socketConsole;

            // Listview source binding
            lvClientSocketConsole.ItemsSource = socketConsoleDisplayList;

            // Dispatcher for å oppdatere meldingene i socket console
            socketConsoleUpdater = new DispatcherTimer();

            socketConsoleUpdater.Interval = TimeSpan.FromMilliseconds(config.ReadWithDefault(ConfigKey.ClientUpdateFrequencyUI, Constants.ClientUIUpdateFrequencyDefault));
            socketConsoleUpdater.Tick += UpdateConsoleMessages;
            socketConsoleUpdater.Start();

            void UpdateConsoleMessages(object sender, EventArgs e)
            {
                TransferNewMessages(socketConsoleDisplayList, socketConsole.GetMessages());
            }
        }

        private void TransferNewMessages(RadObservableCollection<SocketConsoleMessage> displayList, RadObservableCollection<SocketConsoleMessage> newMessages)
        {
            // Gå gjennom meldingslisten
            foreach (var item in newMessages.ToList())
            {
                if (item != null)
                {
                    // Finne ut om melding ligger inne fra før
                    // Dersom den ikke ligger inne -> legg den inn, ellers gjør vi ingenting
                    if (displayList.Where(x => x.text == item.text).Count() == 0)
                    {
                        // Legge inn my melding
                        displayList.Add(new SocketConsoleMessage()
                        {
                            text = item.text
                        });

                        // Slette første når listen blir for lang
                        while (displayList.Count > Constants.MaxErrorMessages)
                            displayList.RemoveAt(0);
                    }
                }
            }
        }

        private void btnGetData_Click(object sender, RoutedEventArgs e)
        {
            // Start en data fetch mot server
            SocketClient socketClient = new SocketClient(socketConsole);

            // Sette kommando og parametre
            socketClient.SetParams(
                PacketCommand.GetDataUpdate,
                hmsDataList);

            socketClient.Start();
        }

        private void chkSocketComClear_Click(object sender, RoutedEventArgs e)
        {
            socketConsole.Clear();
            socketConsoleDisplayList.Clear();
        }
    }

    class SocketConsoleLine
    {
        public string text { get; set; }
    }
}
