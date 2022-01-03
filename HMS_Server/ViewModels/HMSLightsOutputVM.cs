using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Threading;

namespace HMS_Server
{
    public class HMSLightsOutputVM : INotifyPropertyChanged
    {
        // Change notification
        public event PropertyChangedEventHandler PropertyChanged;

        private DispatcherTimer statusUpdateTimer = new DispatcherTimer();

        private UserInputs userInputs;

        public void Init(DataCollection hmsOutputDataList, Config config, UserInputs userInputs)
        {
            this.userInputs = userInputs;

            testMode = false;
            testModeStatus = HelideckStatusType.OFF;
            testModeDisplayMode = DisplayMode.PreLanding;

            // Oppdatere UI
            statusUpdateTimer.Interval = TimeSpan.FromMilliseconds(500);
            statusUpdateTimer.Tick += UIUpdate;
            statusUpdateTimer.Start();

            void UIUpdate(object sender, EventArgs e)
            {
                helideckStatusData = hmsOutputDataList?.GetData(ValueType.HelideckStatus);
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Helideck Status
        /////////////////////////////////////////////////////////////////////////////
        private HMSData _helideckStatusData { get; set; } = new HMSData();
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
                    OnPropertyChanged(nameof(HMSLightsOutput));
                    OnPropertyChanged(nameof(HMSLightsOutputToDisplay));
                }
            }
        }

        public HelideckStatusType helideckStatus
        {
            get
            {
                HelideckStatusType status;

                if (_helideckStatusData.status == DataStatus.OK)
                    status = (HelideckStatusType)_helideckStatusData.data;
                else
                    // Dersom vi har data timeout skal lyset slåes av
                    status = HelideckStatusType.OFF;

                return status;
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Helideck Lights Output
        /////////////////////////////////////////////////////////////////////////////
        public LightsOutputType HMSLightsOutput
        {
            get
            {
                LightsOutputType lightsOutput = LightsOutputType.Off;

                if (!testMode)
                {
                    if (userInputs.displayMode == DisplayMode.PreLanding)
                    {
                        switch (helideckStatus)
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
                        switch (helideckStatus)
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
                }
                else
                {
                    if (testModeDisplayMode == DisplayMode.PreLanding)
                    {
                        switch (testModeStatus)
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
                        switch (testModeStatus)
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
                }

                return lightsOutput;
            }
        }

        public HelideckStatusType HMSLightsOutputToDisplay
        {
            // Her legges blinking på

            get
            {
                HelideckStatusType status = HelideckStatusType.OFF; 

                switch (HMSLightsOutput)
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

                return status;
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Helideck Lights Output Test Mode
        /////////////////////////////////////////////////////////////////////////////
        private bool _testMode { get; set; }
        public bool testMode
        {
            get
            {
                return _testMode;
            }
            set
            {
                _testMode = value;
                OnPropertyChanged(nameof(HMSLightsOutput));
                OnPropertyChanged(nameof(HMSLightsOutputToDisplay));
            }
        }

        private HelideckStatusType _testModeStatus { get; set; }
        public HelideckStatusType testModeStatus
        {
            get
            {
                return _testModeStatus;
            }
            set
            {
                _testModeStatus = value;
                OnPropertyChanged(nameof(HMSLightsOutput));
                OnPropertyChanged(nameof(HMSLightsOutputToDisplay));
            }
        }

        private DisplayMode _testModeDisplayMode { get; set; }
        public DisplayMode testModeDisplayMode
        {
            get
            {
                return _testModeDisplayMode;
            }
            set
            {
                _testModeDisplayMode = value;
                OnPropertyChanged(nameof(HMSLightsOutput));
                OnPropertyChanged(nameof(HMSLightsOutputToDisplay));
            }
        }

        // Variabel oppdatert
        // Dersom navn ikke er satt brukes kallende medlem sitt navn
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    public enum LightsOutputType
    {
        Off,
        Blue,
        BlueFlash,
        Amber,
        AmberFlash,
        Red,
        RedFlash,
        Green,
        GreenFlash
    }
}
