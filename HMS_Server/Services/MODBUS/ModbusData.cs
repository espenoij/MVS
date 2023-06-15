using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace HMS_Server
{
    public class ModbusData : INotifyPropertyChanged
    {
        // Change notification
        public event PropertyChangedEventHandler PropertyChanged;

        private int _address { get; set; }
        public int address
        {
            get
            {
                return _address;
            }
            set
            {
                _address = value;
                OnPropertyChanged(nameof(address));
                OnPropertyChanged(nameof(addressString));
            }
        }
        public string addressString
        {
            get
            {
                return _address.ToString();
            }
        }

        private int _data { get; set; }
        public int data
        {
            get
            {
                return _data;
            }
            set
            {
                _data = value;
                OnPropertyChanged(nameof(data));
                OnPropertyChanged(nameof(dataString));
            }
        }
        public string dataString
        {
            get
            {
                return _data.ToString();
            }
        }

        private double _calculatedData { get; set; }
        public double calculatedData
        {
            get
            {
                return _calculatedData;
            }
            set
            {
                _calculatedData = value;
                OnPropertyChanged(nameof(calculatedData));
                OnPropertyChanged(nameof(calculatedDataString));
            }
        }
        public string calculatedDataString
        {
            get
            {
                return _calculatedData.ToString();
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
