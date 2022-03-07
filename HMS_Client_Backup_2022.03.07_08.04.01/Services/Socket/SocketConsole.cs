using Telerik.Windows.Data;

namespace HMS_Client
{
    public class SocketConsole
    {
        private RadObservableCollection<SocketConsoleMessage> socketConsoleMessages = new RadObservableCollection<SocketConsoleMessage>();
        private object socketConsoleMessagesLock = new object();

        public SocketConsole()
        {
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

        public RadObservableCollection<SocketConsoleMessage> GetMessages()
        {
            return socketConsoleMessages;
        }

        public void Clear()
        {
            lock (socketConsoleMessagesLock)
                socketConsoleMessages.Clear();
        }
    }

    public class SocketConsoleMessage
    {
        public string text { get; set; }
    }
}