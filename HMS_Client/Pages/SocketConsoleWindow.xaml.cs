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

        private RadObservableCollectionEx<SocketConsoleMessage> socketConsoleMessages = new RadObservableCollectionEx<SocketConsoleMessage>();
        private DispatcherTimer socketConsoleUpdater;

        private RadObservableCollectionEx<HMSData> hmsDataList = new RadObservableCollectionEx<HMSData>();

        public SocketConsoleWindow(SocketConsole socketConsole)
        {
            InitializeComponent();

            // Socket console hvor vi mottar meldinger fra socket listener
            this.socketConsole = socketConsole;

            // Listview source binding
            lvClientSocketConsole.ItemsSource = socketConsoleMessages;

            // Dispatcher for å oppdatere meldingene i socket console
            socketConsoleUpdater = new DispatcherTimer();

            socketConsoleUpdater.Interval = TimeSpan.FromMilliseconds(1000);
            socketConsoleUpdater.Tick += UpdateConsoleMessages;
            socketConsoleUpdater.Start();

            void UpdateConsoleMessages(object sender, EventArgs e)
            {
                InsertNewMessages(socketConsoleMessages, socketConsole.GetMessages());
            }
        }

        private void InsertNewMessages(RadObservableCollectionEx<SocketConsoleMessage> socketConsoleMessages, RadObservableCollection<SocketConsoleMessage> newMessages)
        {
            // Gå gjennom meldingslisten
            foreach (var item in newMessages.ToList())
            {
                if (item != null)
                {
                    // Finne ut om melding ligger inne fra før
                    // Dersom den ikke ligger inne -> legg den inn, ellers gjør vi ingenting
                    if (socketConsoleMessages.Where(x => x.text == item.text).Count() == 0)
                    {
                        // Legge inn my melding
                        socketConsoleMessages.Add(item);
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
            socketConsoleMessages.Clear();
        }
    }

    class SocketConsoleLine
    {
        public string text { get; set; }
    }
}
