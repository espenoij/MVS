using System;
using System.Windows;
using System.Windows.Threading;
using Telerik.Windows.Controls;

namespace SensorMonitorClient
{
    public class SocketConsole
    {
        private RadObservableCollectionEx<SocketConsoleMessage> socketConsoleMessages = new RadObservableCollectionEx<SocketConsoleMessage>();

        public SocketConsole()
        {
        }

        public void Add(string msg)
        {
            lock (socketConsoleMessages)
            {
                // Legger til en ny melding i socket console listen
                socketConsoleMessages.Add(new SocketConsoleMessage()
                {
                    text = msg
                });

                // Begrenser antallet i listen
                while (socketConsoleMessages.Count > 100)
                    socketConsoleMessages.RemoveAt(0);
            }
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