using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Threading;

namespace HMS_Client
{
    public class HelideckStatusVM : INotifyPropertyChanged
    {
        // Change notification
        public event PropertyChangedEventHandler PropertyChanged;

        private DispatcherTimer UIUpdateTimer = new DispatcherTimer();

        private UserInputsVM userInputsVM;

        public void Init(Config config, SensorGroupStatus sensorStatus, UserInputsVM userInputsVM)
        {
            this.userInputsVM = userInputsVM;

            OnPropertyChanged(nameof(helideckLightStatus));
            OnPropertyChanged(nameof(landingStatus));
            OnPropertyChanged(nameof(rwdStatus));

            // Oppdatere UI
            UIUpdateTimer.Interval = TimeSpan.FromMilliseconds(config.ReadWithDefault(ConfigKey.ClientUpdateFrequencyUI, Constants.ClientUpdateFrequencyUIDefault));
            UIUpdateTimer.Tick += UIUpdate;
            UIUpdateTimer.Start();

            void UIUpdate(object sender, EventArgs e)
            {
                // Sjekke om vi har data timeout
                if (sensorStatus.TimeoutCheck(helideckLightStatusData)) OnPropertyChanged(nameof(helideckLightStatus)); 
                if (sensorStatus.TimeoutCheck(landingStatusData)) OnPropertyChanged(nameof(landingStatus));
                if (sensorStatus.TimeoutCheck(rwdStatusData)) OnPropertyChanged(nameof(rwdStatus));

                OnPropertyChanged(nameof(helideckStatusHeader));
            }
        }

        public void UpdateData(HMSDataCollection hmsDataList)
        {
            helideckLightStatusData = hmsDataList.GetData(ValueType.HelideckLightStatus);
            landingStatusData = hmsDataList.GetData(ValueType.LandingStatus);
            rwdStatusData = hmsDataList.GetData(ValueType.RWDStatus);
        }

        /////////////////////////////////////////////////////////////////////////////
        // Helideck Light Status
        /////////////////////////////////////////////////////////////////////////////
        private HMSData _helideckLightStatusData { get; set; } = new HMSData();
        public HMSData helideckLightStatusData
        {
            get
            {
                return _helideckLightStatusData;
            }
            set
            {
                if (value != null)
                {
                    _helideckLightStatusData.Set(value);

                    OnPropertyChanged(nameof(helideckLightStatus));
                }
            }
        }

        public HelideckStatusType helideckLightStatus
        {
            get
            {
                if (_helideckLightStatusData.status == DataStatus.OK)
                    return (HelideckStatusType)_helideckLightStatusData.data;
                else
                    // Dersom vi har data timeout skal lyset slåes av
                    return HelideckStatusType.OFF;
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Landing Status
        /////////////////////////////////////////////////////////////////////////////
        private HMSData _landingStatusData { get; set; } = new HMSData();
        public HMSData landingStatusData
        {
            get
            {
                return _landingStatusData;
            }
            set
            {
                if (value != null)
                {
                    _landingStatusData.Set(value);

                    OnPropertyChanged(nameof(landingStatus));
                }
            }
        }

        public HelideckStatusType landingStatus
        {
            get
            {
                if (_landingStatusData.status == DataStatus.OK)
                    return (HelideckStatusType)_landingStatusData.data;
                else
                    // Dersom vi har data timeout skal lyset slåes av
                    return HelideckStatusType.OFF;
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // RWD Status
        /////////////////////////////////////////////////////////////////////////////
        private HMSData _rwdStatusData { get; set; } = new HMSData();
        public HMSData rwdStatusData
        {
            get
            {
                return _rwdStatusData;
            }
            set
            {
                if (value != null)
                {
                    _rwdStatusData.Set(value);

                    OnPropertyChanged(nameof(rwdStatus));
                }
            }
        }

        public HelideckStatusType rwdStatus
        {
            get
            {
                if (_rwdStatusData.status == DataStatus.OK)
                    return (HelideckStatusType)_rwdStatusData.data;
                else
                    // Dersom vi har data timeout skal lyset slåes av
                    return HelideckStatusType.OFF;
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Helideck Status Header
        /////////////////////////////////////////////////////////////////////////////
        public string helideckStatusHeader
        {
            get
            {
                if (userInputsVM.displayMode == DisplayMode.PreLanding)
                    return "LANDING STATUS";
                else
                    return "TAKEOFF STATUS";
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
