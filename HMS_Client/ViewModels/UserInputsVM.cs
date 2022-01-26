using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Threading;

namespace HMS_Client
{
    public class UserInputsVM : INotifyPropertyChanged
    {
        // Change notification
        public event PropertyChangedEventHandler PropertyChanged;

        // Configuration settings
        private Config config;

        // Server communications modul
        private ServerCom serverCom;

        private AdminSettingsVM adminSettingsVM;
        private HelideckMotionLimitsVM helideckMotionLimitsVM;
        private MainWindowVM mainWindowVM;
        private HelideckRelativeWindLimitsVM relativeWindDirectionLimitsVM;
        private HelideckStabilityLimitsVM helideckStabilityLimitsVM;
        private WindHeadingTrendVM helideckWindHeadingTrendVM;
        private HelideckRelativeWindLimitsVM helideckRelativeWindLimitsVM;
        private WindHeadingVM windHeadingVM;

        private MainWindow.ResetWindDisplayCallback resetWindDisplayCallback;

        private bool isUserInputsSet = false;

        private DispatcherTimer UserInputsSetCheckDispatcher = new DispatcherTimer();

        public void Init(
            AdminSettingsVM adminSettingsVM,
            HelideckMotionLimitsVM helideckMotionLimitsVM,
            Config config,
            MainWindowVM mainWindowVM,
            HelideckRelativeWindLimitsVM relativeWindDirectionLimitsVM,
            HelideckStabilityLimitsVM helideckStabilityLimitsVM,
            WindHeadingTrendVM helideckWindHeadingTrendVM,
            HelideckRelativeWindLimitsVM helideckRelativeWindLimitsVM,
            WindHeadingVM windHeadingVM,
            MainWindow.ResetWindDisplayCallback resetWindDisplayCallback,
            ServerCom serverCom)
        {
            this.adminSettingsVM = adminSettingsVM;
            this.helideckMotionLimitsVM = helideckMotionLimitsVM;
            this.config = config;
            this.mainWindowVM = mainWindowVM;
            this.relativeWindDirectionLimitsVM = relativeWindDirectionLimitsVM;
            this.helideckStabilityLimitsVM = helideckStabilityLimitsVM;
            this.helideckWindHeadingTrendVM = helideckWindHeadingTrendVM;
            this.helideckRelativeWindLimitsVM = helideckRelativeWindLimitsVM;
            this.windHeadingVM = windHeadingVM;
            this.resetWindDisplayCallback = resetWindDisplayCallback;
            this.serverCom = serverCom;

            InitUI();
        }

        private void InitUI()
        {
            // Helicopter Type
            helicopterType = (HelicopterType)Enum.Parse(typeof(HelicopterType), config.ReadWithDefault(ConfigKey.HelicopterType, HelicopterType.AS332.ToString()));

            // Helideck Category
            helideckCategory = (HelideckCategory)Enum.Parse(typeof(HelideckCategory), config.ReadWithDefault(ConfigKey.HelideckCategory, HelideckCategory.Category1.ToString()));

            // Day / Night
            dayNight = (DayNight)Enum.Parse(typeof(DayNight), config.ReadWithDefault(ConfigKey.DayNight, DayNight.Day.ToString()));

            // Display Mode
            displayMode = DisplayMode.PreLanding;

            // Klient er server
            if (adminSettingsVM.clientIsMaster)
            {
                // Sjekk på om user inputs er sent til server
                UserInputsSetCheckDispatcher.Interval = TimeSpan.FromMilliseconds(Constants.UserInputsSetCheckFrequency);
                UserInputsSetCheckDispatcher.Tick += UserInputsSetCheck;
                UserInputsSetCheckDispatcher.Start();

                void UserInputsSetCheck(object sender, EventArgs e)
                {
                    // User Inputs er ikke sent
                    if (!isUserInputsSet)
                    {
                        SendUserInputsToServer();
                    }
                    // User inputs er sent
                    else
                    {
                        // Sjekke om vi har data timeout, dvs server utilgjengelig, kan være restartet, må sende user inputs på nytt
                        if (serverCom.lastDataReceivedTime.AddMilliseconds(config.ReadWithDefault(ConfigKey.DataTimeout, Constants.DataTimeoutDefault)) < DateTime.UtcNow)
                            isUserInputsSet = false;
                    }
                }
            }
        }

        public void UpdateData(HMSDataCollection hmsDataList)
        {
            if (!adminSettingsVM.clientIsMaster)
            {
                helicopterType = (HelicopterType)hmsDataList.GetData(ValueType.SettingsHelicopterType).data;
                helideckCategory = (HelideckCategory)hmsDataList.GetData(ValueType.SettingsHelideckCategory).data;
                dayNight = (DayNight)hmsDataList.GetData(ValueType.SettingsDayNight).data;
                displayMode = (DisplayMode)hmsDataList.GetData(ValueType.SettingsDisplayMode).data;

                onDeckTime = DateTime.FromOADate(hmsDataList.GetData(ValueType.SettingsOnDeckTime).data);
                onDeckHelicopterHeading = hmsDataList.GetData(ValueType.SettingsOnDeckHelicopterHeading).data;

                if (hmsDataList.GetData(ValueType.SettingsOnDeckHelicopterHeadingCorrected).data == 1)
                    onDeckHelicopterHeadingIsCorrected = true;
                else
                    onDeckHelicopterHeadingIsCorrected = false;
                onDeckVesselHeading = hmsDataList.GetData(ValueType.SettingsOnDeckVesselHeading).data;
                onDeckWindDirection = hmsDataList.GetData(ValueType.SettingsOnDeckWindDirection).data;

                OnPropertyChanged(nameof(displayModeVisibilityPreLanding));
                OnPropertyChanged(nameof(displayModeVisibilityOnDeck));
                OnPropertyChanged(nameof(helicopterHeadingInfoString1));
                OnPropertyChanged(nameof(helicopterHeadingInfoString2));
            }
        }

        private void SendUserInputsToServer()
        {
            if (adminSettingsVM.clientIsMaster)
            {
                UserInputs userInputs = new UserInputs();

                userInputs.helicopterType = helicopterType;
                userInputs.helideckCategory = helideckCategory;
                userInputs.dayNight = dayNight;
                userInputs.displayMode = displayMode;
                userInputs.onDeckTime = onDeckTime;
                userInputs.onDeckHelicopterHeading = onDeckHelicopterHeading;
                userInputs.onDeckHelicopterHeadingIsCorrected = onDeckHelicopterHeadingIsCorrected;
                userInputs.onDeckVesselHeading = onDeckVesselHeading;
                userInputs.onDeckWindDirection = onDeckWindDirection;

                ServerCom.SocketCallback socketCallback = new ServerCom.SocketCallback(UserInputsSet);

                serverCom.SendUserInput(userInputs, socketCallback);

                void UserInputsSet()
                {
                    isUserInputsSet = true;
                }
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Helicopter Type
        /////////////////////////////////////////////////////////////////////////////
        private HelicopterType _helicopterType { get; set; }
        public HelicopterType helicopterType
        {
            get
            {
                return _helicopterType;
            }
            set
            {
                if (value != _helicopterType)
                {
                    _helicopterType = value;
                    config.Write(ConfigKey.HelicopterType, _helicopterType.ToString());
                    SendUserInputsToServer();
                }

                OnPropertyChanged(nameof(helicopterTypeString));
            }
        }

        public string helicopterTypeString
        {
            get
            {
                return helicopterType.ToString();
            }
        }

        // Helicopter Category
        public HelicopterCategory helicopterCategory
        {
            get
            {
                switch (helicopterType)
                {
                    // Heavy / A
                    case HelicopterType.AS332:
                    case HelicopterType.EC225:
                    case HelicopterType.AW189:
                    case HelicopterType.S61:
                    case HelicopterType.S92:
                        return HelicopterCategory.Heavy;

                    // Medium / B
                    case HelicopterType.AS365:
                    case HelicopterType.EC135:
                    case HelicopterType.EC155:
                    case HelicopterType.EC175:
                    case HelicopterType.AW139:
                    case HelicopterType.AW169:
                    case HelicopterType.S76:
                    case HelicopterType.B212:
                    case HelicopterType.B412:
                    case HelicopterType.B525:
                    case HelicopterType.H145:
                    case HelicopterType.H175:
                        return HelicopterCategory.Medium;

                    default:
                        return HelicopterCategory.Heavy;
                }
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Helideck Category
        /////////////////////////////////////////////////////////////////////////////
        private HelideckCategory _helideckCategory { get; set; }
        public HelideckCategory helideckCategory
        {
            get
            {
                return _helideckCategory;
            }
            set
            {
                if (value != _helideckCategory)
                {
                    _helideckCategory = value;
                    config.Write(ConfigKey.HelideckCategory, _helideckCategory.ToString());
                    OnPropertyChanged(nameof(helideckCategoryString));
                    SendUserInputsToServer();
                }
            }
        }
        public string helideckCategoryString
        {
            get
            {
                return _helideckCategory.GetDescription();
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Day / Night
        /////////////////////////////////////////////////////////////////////////////
        private DayNight _dayNight { get; set; }
        public DayNight dayNight
        {
            get
            {
                return _dayNight;
            }
            set
            {
                if (value != _dayNight)
                {
                    _dayNight = value;
                    config.Write(ConfigKey.DayNight, _dayNight.ToString());
                    OnPropertyChanged(nameof(dayNightString));
                    SendUserInputsToServer();
                }
            }
        }

        public string dayNightString
        {
            get
            {
                return _dayNight.ToString();
            }
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
                if (_displayMode != value)
                {
                    _displayMode = value;

                    // Sette display mode
                    mainWindowVM.displayMode = _displayMode;
                    relativeWindDirectionLimitsVM.displayMode = _displayMode;
                    helideckMotionLimitsVM.displayMode = _displayMode;
                    helideckStabilityLimitsVM.displayMode = _displayMode;
                    helideckWindHeadingTrendVM.displayMode = _displayMode;

                    if (_displayMode == DisplayMode.OnDeck)
                    {
                        // Sette on-deck variabler
                        onDeckTime = DateTime.UtcNow;

                        if (windHeadingVM.vesselHeading.status == DataStatus.OK)
                            onDeckVesselHeading = windHeadingVM.vesselHeading.data;
                        else
                            onDeckVesselHeading = -1;

                        if (windHeadingVM.windDirection2m.status == DataStatus.OK)
                            onDeckWindDirection = windHeadingVM.windDirection2m.data;
                        else
                            onDeckWindDirection = -1;

                        // Korreksjon aktiv
                        onDeckHelicopterHeadingIsCorrected = false;

                        // Start oppdatering av grafer
                        helideckWindHeadingTrendVM.Start();
                        helideckRelativeWindLimitsVM.Start();

                        // Sette Wind & Heading til å vise 2-min mean vind
                        resetWindDisplayCallback();
                    }
                    else
                    {
                        // Stopp oppdatering av grafer
                        helideckWindHeadingTrendVM.Stop();
                        helideckRelativeWindLimitsVM.Stop();
                    }

                    OnPropertyChanged(nameof(displayModeVisibilityPreLanding));
                    OnPropertyChanged(nameof(displayModeVisibilityOnDeck));
                    OnPropertyChanged(nameof(helicopterHeadingInfoString1));
                    OnPropertyChanged(nameof(helicopterHeadingInfoString2));

                    SendUserInputsToServer();
                }
            }
        }

        public bool displayModeVisibilityPreLanding
        {
            get
            {
                if (_displayMode == DisplayMode.PreLanding)
                    return true;
                else
                    return false;
            }
        }

        public bool displayModeVisibilityOnDeck
        {
            get
            {
                if (_displayMode == DisplayMode.OnDeck)
                    return true;
                else
                    return false;
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // On-deck variabler
        /////////////////////////////////////////////////////////////////////////////
        public DateTime _onDeckTime { get; set; }
        public DateTime onDeckTime
        {
            get
            {
                return _onDeckTime;
            }
            set
            {
                _onDeckTime = value;
            }
        }

        private double _onDeckHelicopterHeading { get; set; }
        public double onDeckHelicopterHeading
        {
            get
            {
                return _onDeckHelicopterHeading;
            }
            set
            {
                if (value >= Constants.HeadingMin && value <= Constants.HeadingMax)
                {
                    _onDeckHelicopterHeading = value;

                    onDeckHelicopterRelativeHeading = value - windHeadingVM.vesselHeading.data;

                    onDeckTime = DateTime.UtcNow;
                    onDeckHelicopterHeadingIsCorrected = true;
                    OnPropertyChanged(nameof(helicopterHeadingInfoString1));
                    OnPropertyChanged(nameof(helicopterHeadingInfoString2));

                    SendUserInputsToServer();
                }
            }
        }
        public double onDeckHelicopterRelativeHeading { get; set; }


        public bool onDeckHelicopterHeadingIsCorrected { get; set; }

        public double onDeckVesselHeading { get; set; }
        public double onDeckWindDirection { get; set; }

        /////////////////////////////////////////////////////////////////////////////
        // Helicopter Landed Dialog
        /////////////////////////////////////////////////////////////////////////////
        private double _helicopterLandedHeading { get; set; }
        public double helicopterLandedHeading
        {
            get
            {
                return _helicopterLandedHeading;
            }
            set
            {
                _helicopterLandedHeading = value;

                OnPropertyChanged(nameof(helicopterLandedHeadingDialogString));
            }
        }

        public string helicopterLandedHeadingDialogString
        {
            get
            {
                return string.Format("Helicopter Heading: {0}° (M)", _helicopterLandedHeading.ToString("000"));
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Helicopter Heading / Corrected Heading text
        /////////////////////////////////////////////////////////////////////////////
        public string helicopterHeadingInfoString1
        {
            get
            {
                if (!onDeckHelicopterHeadingIsCorrected)
                    return "On-Deck Helicopter Heading";
                else
                    return "Corrected Helicopter Heading";
            }
        }

        public string helicopterHeadingInfoString2
        {
            get
            {
                return string.Format("{0}° M at {1} UTC",
                    onDeckHelicopterHeading.ToString("000"),
                    onDeckTime.ToShortTimeString());
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Display Mode
        /////////////////////////////////////////////////////////////////////////////
        public DisplayMode onDeckTimeString
        {
            get
            {
                return _displayMode;
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
