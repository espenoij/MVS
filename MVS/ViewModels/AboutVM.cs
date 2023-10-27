using DeviceId;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MVS
{
    public class AboutVM : INotifyPropertyChanged
    {
        // Change notification
        public event PropertyChangedEventHandler PropertyChanged;

        public AboutVM(Version version)
        {
            // Software Name
            softwareName = "MVS - Motion Verification System";

            // Software Version
            softwareVersion = string.Format("{0} {1}",
                version.ToString(),
                Constants.SoftwareVersionPostfix);

            // About Support
            aboutSupport = string.Format("For support and other inquiries, please contact Swire Energy Services - Aviation.");

            // About Copyright
            aboutCopyright = string.Format("Copyright © 2022-{0} Swire Energy Services.", DateTime.UtcNow.Year);
        }

        /////////////////////////////////////////////////////////////////////////////
        // Software Version
        /////////////////////////////////////////////////////////////////////////////
        private string _softwareName { get; set; }
        public string softwareName
        {
            get
            {
                return _softwareName;
            }
            set
            {
                if (value != _softwareName)
                {
                    _softwareName = value;

                    OnPropertyChanged();
                }
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
        // About Support
        /////////////////////////////////////////////////////////////////////////////
        private string _aboutSupport { get; set; }
        public string aboutSupport
        {
            get
            {
                return _aboutSupport;
            }
            set
            {
                if (value != _aboutSupport)
                {
                    _aboutSupport = value;

                    OnPropertyChanged();
                }
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // About Support
        /////////////////////////////////////////////////////////////////////////////
        private string _aboutCopyright { get; set; }
        public string aboutCopyright
        {
            get
            {
                return _aboutCopyright;
            }
            set
            {
                if (value != _aboutCopyright)
                {
                    _aboutCopyright = value;

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

