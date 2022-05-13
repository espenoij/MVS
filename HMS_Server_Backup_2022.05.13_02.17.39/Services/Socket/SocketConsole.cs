using System.ComponentModel;
using System.Windows.Data;
using Telerik.Windows.Data;

namespace HMS_Server
{
    class SocketConsole
    {
        private RadObservableCollection<SocketConsoleMessage> socketConsoleMessages = new RadObservableCollection<SocketConsoleMessage>();
        private object socketConsoleMessagesLock = new object();

        public SocketConsole()
        {
            socketConsoleMessages = new RadObservableCollection<SocketConsoleMessage>();
            BindingOperations.EnableCollectionSynchronization(socketConsoleMessages, socketConsoleMessagesLock);
        }

        public void Add(string msg)
        {
            // Legger til en ny melding i socket console listen
            lock (socketConsoleMessagesLock)
            {
                socketConsoleMessages.Add(new SocketConsoleMessage()
                {
                    text = msg
                });

                // Begrenser antallet i listen
                while (socketConsoleMessages.Count > Constants.MaxErrorMessages)
                    socketConsoleMessages.RemoveAt(0);
            }
        }

        public RadObservableCollection<SocketConsoleMessage> Messages()
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