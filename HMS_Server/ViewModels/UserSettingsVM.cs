using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace HMS_Server
{
    public class UserSettingsVM : INotifyPropertyChanged
    {
        // Change notification
        public event PropertyChangedEventHandler PropertyChanged;

        // Configuration settings
        private Config config;

        public void Init(Config config)
        {
            this.config = config;

            // Heading
            if (config.ReadWithDefault(ConfigKey.FixedInstallation, "0") == "1")
                fixedInstallation = true;
            else
                fixedInstallation = false;
            fixedHeading = config.ReadWithDefault(ConfigKey.FixedHeading, Constants.HeadingDefault);

            // Jackup height adjustment
            jackupHeight = config.ReadWithDefault(ConfigKey.JackupHeight, Constants.JackupHeightDefault);
        }

        /////////////////////////////////////////////////////////////////////////////
        // Fixed Heading
        /////////////////////////////////////////////////////////////////////////////
        private bool _fixedInstallation { get; set; }
        public bool fixedInstallation
        {
            get
            {
                return _fixedInstallation;
            }
            set
            {
                _fixedInstallation = value;

                if (_fixedInstallation)
                    config.Write(ConfigKey.FixedInstallation, "1");
                else
                    config.Write(ConfigKey.FixedInstallation, "0");

                OnPropertyChanged();
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Fixed Heading Value
        /////////////////////////////////////////////////////////////////////////////
        private double _fixedHeading { get; set; }
        public double fixedHeading
        {
            get
            {
                return _fixedHeading;
            }
            set
            {
                _fixedHeading = value;
                config.Write(ConfigKey.FixedHeading, _fixedHeading.ToString());
                OnPropertyChanged();
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Jackup Right Height Adjustment
        /////////////////////////////////////////////////////////////////////////////
        private double _jackupHeight { get; set; }
        public double jackupHeight
        {
            get
            {
                return _jackupHeight;
            }
            set
            {
                _jackupHeight = value;
                config.Write(ConfigKey.JackupHeight, value.ToString());
                OnPropertyChanged();
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
