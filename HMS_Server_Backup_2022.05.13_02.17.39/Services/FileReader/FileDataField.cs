using System.ComponentModel;

namespace HMS_Server
{
    public class FileDataFields : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private string[] _dataField { get; set; } = new string[Constants.PacketDataFields];
        public string[] dataField
        {
            get
            {
                return _dataField;
            }
            set
            {
                _dataField = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(dataField)));
            }
        }
    }
}
