using System;
using System.Windows;
using System.Windows.Threading;

namespace HMS_Client
{
    public class SocketConsole
    {
        private RadObservableCollectionEx<SocketConsoleMessage> socketConsoleMessages = new RadObservableCollectionEx<SocketConsoleMessage>();

        public SocketConsole()
        {
        }

        public void Add(string msg)
        {
            // Legger til en ny melding i socket console listen
            Application.Current.Dispatcher.BeginInvoke(
                DispatcherPriority.Normal,
                new Action(() =>
                {
                    socketConsoleMessages.Add(new SocketConsoleMessage()
                    {
                        text = msg
                    });

                    // Begrenser antallet i listen
                    while (socketConsoleMessages.Count > Constants.MaxErrorMessages)
                        socketConsoleMessages.RemoveAt(0);
                }));
        }

        public RadObservableCollectionEx<SocketConsoleMessage> Messages()
        {
            return socketConsoleMessages;
        }

        public void Clear()
        {
            socketConsoleMessages.Clear();
        }
    }

    public class SocketConsoleMessage
    {
        public string text { get; set; }
    }
}