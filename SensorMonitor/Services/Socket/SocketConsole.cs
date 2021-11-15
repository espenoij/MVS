using System.ComponentModel;

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
            lock (socketConsoleMessages)
            {
                socketConsoleMessages.Add(new SocketConsoleMessage()
                {
                    text = msg
                });

                // Begrenser antallet i listen
                while (socketConsoleMessages.Count > 100)
                {
                    socketConsoleMessages.RemoveAt(0);
                }
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