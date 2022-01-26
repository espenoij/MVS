using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Threading;

namespace HMS_Server
{
    class SocketConsole
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

    class SocketConsoleMessage : INotifyPropertyChanged
    {
        // Change notification
        public event PropertyChangedEventHandler PropertyChanged;

        private string _text { get; set; }
        public string text
        {
            get
            {
                return _text;
            }
            set
            {
                _text = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(text)));
            }
        }
    }
}