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
        private DispatcherTimer lightUpdateTimer = new DispatcherTimer();

        private UserInputsVM userInputsVM;
        private AdminSettingsVM adminSettingsVM;

        public void Init(Config config, SensorGroupStatus sensorStatus, UserInputsVM userInputsVM, AdminSettingsVM adminSettingsVM)
        {
            this.userInputsVM = userInputsVM;
            this.adminSettingsVM = adminSettingsVM;

            OnPropertyChanged(nameof(helideckLightStatus));
            OnPropertyChanged(nameof(landingStatus));
            OnPropertyChanged(nameof(rwdStatus));

            // Oppdatere UI
            UIUpdateTimer.Interval = TimeSpan.FromMilliseconds(config.ReadWithDefault(ConfigKey.ClientUpdateFrequencyUI, Constants.ClientUIUpdateFrequencyDefault));
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

            // Oppdatere light
            UIUpdateTimer.Interval = TimeSpan.FromMilliseconds(100);
            UIUpdateTimer.Tick += lightUpdate;
            UIUpdateTimer.Start();

            void lightUpdate(object sender, EventArgs e)
            {
                OnPropertyChanged(nameof(helideckLightStatus));
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
                HelideckStatusType status = HelideckStatusType.OFF;

                // CAP
                if (adminSettingsVM.regulationStandard == RegulationStandard.CAP)
                {
                    LightsOutputType lightsOutput;

                    if (userInputsVM.displayMode == DisplayMode.PreLanding)
                    {
                        switch ((HelideckStatusType)helideckLightStatusData.data)
                        {
                            case HelideckStatusType.OFF:
                                lightsOutput = LightsOutputType.Off;
                                break;
                            case HelideckStatusType.BLUE:
                                lightsOutput = LightsOutputType.Blue;
                                break;
                            case HelideckStatusType.AMBER:
                                lightsOutput = LightsOutputType.Amber;
                                break;
                            case HelideckStatusType.RED:
                                lightsOutput = LightsOutputType.Red;
                                break;
                            case HelideckStatusType.GREEN:
                                lightsOutput = LightsOutputType.Green;
                                break;
                            default:
                                lightsOutput = LightsOutputType.Off;
                                break;
                        }
                    }
                    else
                    {
                        switch ((HelideckStatusType)helideckLightStatusData.data)
                        {
                            case HelideckStatusType.OFF:
                                lightsOutput = LightsOutputType.Off;
                                break;
                            case HelideckStatusType.BLUE:
                                lightsOutput = LightsOutputType.BlueFlash;
                                break;
                            case HelideckStatusType.AMBER:
                                lightsOutput = LightsOutputType.AmberFlash;
                                break;
                            case HelideckStatusType.RED:
                                lightsOutput = LightsOutputType.RedFlash;
                                break;
                            case HelideckStatusType.GREEN:
                                lightsOutput = LightsOutputType.GreenFlash;
                                break;
                            default:
                                lightsOutput = LightsOutputType.Off;
                                break;
                        }
                    }

                    switch (lightsOutput)
                    {
                        case LightsOutputType.Off:
                            status = HelideckStatusType.OFF;
                            break;

                        case LightsOutputType.Blue:
                            status = HelideckStatusType.BLUE;
                            break;

                        case LightsOutputType.BlueFlash:
                            if ((int)DateTime.Now.Second % 2 == 0) // 30 fpm
                                status = HelideckStatusType.OFF;
                            else
                                status = HelideckStatusType.BLUE;
                            break;

                        case LightsOutputType.Amber:
                            status = HelideckStatusType.AMBER;
                            break;

                        case LightsOutputType.AmberFlash:
                            if (DateTime.Now.Millisecond < 500) // 60 fpm
                                status = HelideckStatusType.OFF;
                            else
                                status = HelideckStatusType.AMBER;
                            break;

                        case LightsOutputType.Red:
                            status = HelideckStatusType.RED;
                            break;

                        case LightsOutputType.RedFlash:
                            if (DateTime.Now.Millisecond < 500) // 60 fpm
                                status = HelideckStatusType.OFF;
                            else
                                status = HelideckStatusType.RED;
                            break;

                        case LightsOutputType.Green:
                            status = HelideckStatusType.GREEN;
                            break;

                        case LightsOutputType.GreenFlash:
                            if ((int)DateTime.Now.Second % 2 == 0) // 30 fpm
                                status = HelideckStatusType.OFF;
                            else
                                status = HelideckStatusType.GREEN;
                            break;
                    }
                }
                // NOROG
                else
                {
                    if (_helideckLightStatusData.status == DataStatus.OK)
                        status = (HelideckStatusType)_helideckLightStatusData.data;
                    else
                        // Dersom vi har data timeout skal lyset slåes av
                        status = HelideckStatusType.OFF;
                }

                return status;
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
                    return "RWD STATUS";
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
