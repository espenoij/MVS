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

            _helideckStatusData = new HMSData();

            OnPropertyChanged(nameof(helideckStatus));

            // Oppdatere UI
            UIUpdateTimer.Interval = TimeSpan.FromMilliseconds(config.Read(ConfigKey.ClientUpdateFrequencyUI, Constants.ClientUpdateFrequencyUIDefault));
            UIUpdateTimer.Tick += UIUpdate;
            UIUpdateTimer.Start();

            void UIUpdate(object sender, EventArgs e)
            {
                // Sjekke om vi har data timeout
                if (sensorStatus.TimeoutCheck(helideckStatusData))
                {
                    OnPropertyChanged(nameof(helideckStatus));
                }

                OnPropertyChanged(nameof(helideckStatusHeader));
            }
        }

        public void UpdateData(HMSDataCollection hmsDataList)
        {
            helideckStatusData = hmsDataList.GetData(ValueType.HelideckStatus);
        }

        /////////////////////////////////////////////////////////////////////////////
        // Helideck Status
        /////////////////////////////////////////////////////////////////////////////
        private HMSData _helideckStatusData { get; set; }
        public HMSData helideckStatusData
        {
            get
            {
                return _helideckStatusData;
            }
            set
            {
                if (value != null)
                {
                    _helideckStatusData.Set(value);

                    OnPropertyChanged(nameof(helideckStatus));
                }
            }
        }

        public HelideckStatusType helideckStatus
        {
            get
            {
                if (_helideckStatusData.status == DataStatus.OK)
                    return (HelideckStatusType)_helideckStatusData.data;
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
