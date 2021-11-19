using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace HMS_Client
{
    public class MainWindowVM : INotifyPropertyChanged
    {
        // Change notification
        public event PropertyChangedEventHandler PropertyChanged;

        public void Init()
        {
            // Display Mode
            displayMode = DisplayMode.PreLanding;
        }

        /////////////////////////////////////////////////////////////////////////////
        // Display Mode
        /////////////////////////////////////////////////////////////////////////////
        private DisplayMode _displayMode { get; set; }
        public DisplayMode displayMode
        {
            get
            {
                return _displayMode;
            }
            set
            {
                _displayMode = value;
                OnPropertyChanged(nameof(displayModeString));
            }
        }

        public string displayModeString
        {
            get
            {
                switch (_displayMode)
                {
                    case DisplayMode.PreLanding:
                        return "PRE-LANDING";

                    case DisplayMode.OnDeck:
                        return "ON-DECK";

                    default:
                        return "";
                }
            }
        }

        // Variabel oppdatert
        // Dersom navn ikke er satt brukes kallende medlem sitt navn
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
