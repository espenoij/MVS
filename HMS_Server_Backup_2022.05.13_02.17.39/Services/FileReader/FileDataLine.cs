using System.ComponentModel;

namespace HMS_Server
{
    public class FileDataLine : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private string _data { get; set; }
        public string data
        {
            get
            {
                return _data;
            }
            set
            {
                _data = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(data)));
            }
        }
    }
}
