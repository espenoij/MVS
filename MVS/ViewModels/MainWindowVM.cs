using DeviceId;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MVS
{
    public class MainWindowVM : INotifyPropertyChanged
    {
        // Change notification
        public event PropertyChangedEventHandler PropertyChanged;

        public MainWindowVM( Version version)
        {
            // Software Version
            softwareVersion = string.Format("{0} {1}",
                version.ToString(),
                Constants.SoftwareVersionPostfix);

            // About Information
            aboutInformation = string.Format("For support and other inquiries, please contact Swire Energy Services - Aviation.\n\nCopyright © 2023-{0} Swire Energy Services", DateTime.UtcNow.Year);
        }

        /////////////////////////////////////////////////////////////////////////////
        // Selected Motion Data Set
        /////////////////////////////////////////////////////////////////////////////
        private MotionDataSet _selectedMotionDataSet { get; set; }
        public MotionDataSet SelectedMotionDataSet
        {
            get
            {
                return _selectedMotionDataSet;
            }
            set
            {
                if (value != _selectedMotionDataSet)
                {
                    _selectedMotionDataSet = value;

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

