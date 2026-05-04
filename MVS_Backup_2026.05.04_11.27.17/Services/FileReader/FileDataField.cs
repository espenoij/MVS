using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MVS
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
                OnPropertyChanged(nameof(dataField));
            }
        }

        // Variabel oppdatert
        // Dersom navn ikke settes brukes kallende medlem sitt navn
        protected virtual void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}

