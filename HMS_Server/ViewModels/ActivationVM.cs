using DeviceId;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace HMS_Server
{
    public class ActivationVM : INotifyPropertyChanged
    {
        // Change notification
        public event PropertyChangedEventHandler PropertyChanged;

        private Config config;

        public ActivationVM(Config config, Version version)
        {
            this.config = config;

            licenseOwner = Encryption.ToInsecureString(Encryption.DecryptString(config.Read(ConfigKey.LicenseOwner)));
            licenseLocation = Encryption.ToInsecureString(Encryption.DecryptString(config.Read(ConfigKey.LicenseLocation)));

            if (int.TryParse(Encryption.ToInsecureString(Encryption.DecryptString(config.Read(ConfigKey.LicenseMaxClients))), Constants.numberStyle, Constants.cultureInfo, out int value))
                licenseMaxClients = value;
            else
                licenseMaxClients = 0;

            deviceID = Encryption.ToInsecureString(Encryption.DecryptString(config.Read(ConfigKey.DeviceID)));

            // Leser device ID fra hardware
            // Bruker hovedkort serienummer som identifikator
            string deviceId = new DeviceIdBuilder()
                .OnWindows(windows => windows
                    .AddMotherboardSerialNumber())
                .ToString();

            // Sammenligne device ID fra hardware med den lagret i config fil
            if (deviceID.CompareTo(deviceId) == 0)
                isActivated = true;
            else
                isActivated = false;

            // Software Version
            softwareVersion = string.Format("{0} {1}",
                version.ToString(),
                Constants.SoftwareVersionPostfix);

            // About Information
            aboutInformation = string.Format("Copyright © 2022-{0} Swire Energy Services\n\nFor support and other inquiries, please contact Swire Energy Services - Aviation.", DateTime.UtcNow.Year);
        }

        // License Owner
        private string _licenseOwner { get; set; }
        public string licenseOwner
        {
            get
            {
                return _licenseOwner;
            }
            set
            {
                _licenseOwner = value;
                config.Write(ConfigKey.LicenseOwner, Encryption.EncryptString(Encryption.ToSecureString(value)));
                OnPropertyChanged();
            }
        }

        // License Location
        private string _licenseLocation { get; set; }
        public string licenseLocation
        {
            get
            {
                return _licenseLocation;
            }
            set
            {
                _licenseLocation = value;
                config.Write(ConfigKey.LicenseLocation, Encryption.EncryptString(Encryption.ToSecureString(value)));
                OnPropertyChanged();
            }
        }

        // License Max Clients
        private int _licenseMaxClients { get; set; }
        public int licenseMaxClients
        {
            get
            {
                return _licenseMaxClients;
            }
            set
            {
                _licenseMaxClients = value;
                config.Write(ConfigKey.LicenseMaxClients, Encryption.EncryptString(Encryption.ToSecureString(value.ToString())));
                OnPropertyChanged();
            }
        }

        // Device ID
        private string _deviceID { get; set; }
        public string deviceID
        {
            get
            {
                return _deviceID;
            }
            set
            {
                _deviceID = value;
                config.Write(ConfigKey.DeviceID, Encryption.EncryptString(Encryption.ToSecureString(value)));
                OnPropertyChanged();
            }
        }

        public bool _isActivated { get; set; }
        public bool isActivated
        {
            get
            {
                return _isActivated;
            }
            set
            {
                _isActivated = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(activationStatusString));
            }
        }

        public string activationStatusString
        {
            get
            {
                if (_isActivated)
                    return "Activated";
                else
                    return "Not activated";
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Software Version
        /////////////////////////////////////////////////////////////////////////////
        private string _softwareVersion { get; set; }
        public string softwareVersion
        {
            get
            {
                return _softwareVersion;
            }
            set
            {
                if (value != _softwareVersion)
                {
                    _softwareVersion = value;

                    OnPropertyChanged();
                }
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // About Information
        /////////////////////////////////////////////////////////////////////////////
        private string _aboutInformation { get; set; }
        public string aboutInformation
        {
            get
            {
                return _aboutInformation;
            }
            set
            {
                if (value != _aboutInformation)
                {
                    _aboutInformation = value;

                    OnPropertyChanged();
                }
            }
        }

        // Variabel oppdatert
        // Dersom navn ikke er satt brukes kallende medlem sitt navn
        protected virtual void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}

