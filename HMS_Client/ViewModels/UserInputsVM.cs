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
        private OnDeckStabilityLimitsVM onDeckStabilityLimitsVM;
        private WindHeadingChangeVM windHeadingTrendVM;
        private RelativeWindLimitsVM relativeWindLimitsVM;
        private LandingStatusTrendVM landingStatusTrendVM;
        private WindHeadingVM windHeadingVM;

        private MainWindow.SetDefaultWindMeasurementCallback setDefaultWindMeasurementCallback;

        private bool isUserInputsSet = false;

        private double onDeckHelicopterHeadingPrev = -1;

        private DispatcherTimer UserInputsSetCheckDispatcher = new DispatcherTimer();

        public void Init(
            AdminSettingsVM adminSettingsVM,
            HelideckMotionLimitsVM helideckMotionLimitsVM,
            Config config,
            MainWindowVM mainWindowVM,
            OnDeckStabilityLimitsVM onDeckStabilityLimitsVM,
            WindHeadingChangeVM windHeadingTrendVM,
            RelativeWindLimitsVM relativeWindLimitsVM,
            LandingStatusTrendVM landingStatusTrendVM,
            WindHeadingVM windHeadingVM,
            MainWindow.SetDefaultWindMeasurementCallback setDefaultWindMeasurementCallback,
            ServerCom serverCom)
        {
            this.adminSettingsVM = adminSettingsVM;
            this.helideckMotionLimitsVM = helideckMotionLimitsVM;
            this.config = config;
            this.mainWindowVM = mainWindowVM;
            this.onDeckStabilityLimitsVM = onDeckStabilityLimitsVM;
            this.windHeadingTrendVM = windHeadingTrendVM;
            this.relativeWindLimitsVM = relativeWindLimitsVM;
            this.landingStatusTrendVM = landingStatusTrendVM;
            this.windHeadingVM = windHeadingVM;
            this.setDefaultWindMeasurementCallback = setDefaultWindMeasurementCallback;
            this.serverCom = serverCom;

            InitUI();
        }

        private void InitUI()
        {
            // Helicopter Type
            if (adminSettingsVM.regulationStandard == RegulationStandard.NOROG)
                helicopterType = (HelicopterType)Enum.Parse(typeof(HelicopterType), config.ReadWithDefault(ConfigKey.HelicopterType, HelicopterType.Default.ToString()));
            else
                helicopterType = HelicopterType.Default;

            // Helideck Category
            helideckCategory = (HelideckCategory)Enum.Parse(typeof(HelideckCategory), config.ReadWithDefault(ConfigKey.HelideckCategory, HelideckCategory.Category1.ToString()));

            // Sjekker helideck category mot tillatte kategorier (kan bli endret i admin settings)
            if (adminSettingsVM.regulationStandard == RegulationStandard.CAP)
            {
                switch (adminSettingsVM.helideckCategory)
                {
                    case HelideckCategory.Category1:
                        if (helideckCategory != HelideckCategory.Category1)
                            helideckCategory = HelideckCategory.Category1;
                        break;
                    case HelideckCategory.Category1_Semisub:
                        if (helideckCategory != HelideckCategory.Category1_Semisub)
                            helideckCategory = HelideckCategory.Category1_Semisub;
                        break;
                    case HelideckCategory.Category2:
                        if (helideckCategory != HelideckCategory.Category2)
                            helideckCategory = HelideckCategory.Category2;
                        break;
                    case HelideckCategory.Category2_or_3:
                        if (helideckCategory != HelideckCategory.Category2 &&
                            helideckCategory != HelideckCategory.Category3)
                            helideckCategory = HelideckCategory.Category2;
                        break;
                    default:
                        helideckCategory = HelideckCategory.Category1;
                        break;
                }
            }

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
            // Setter ikke user input fra server dersom klient er master
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
                OnPropertyChanged(nameof(helicopterHeadingInfoString));
            }
            else
            {
                // Dersomm klient er master, setter vi likevel vessel heading og wind direction ved landing,
                // dersom disse to variablene ikke var tilgjengelig da on-deck mode ble valgt.
                if (onDeckVesselHeading == -1)
                    onDeckVesselHeading = hmsDataList.GetData(ValueType.SettingsOnDeckVesselHeading).data;

                if (onDeckWindDirection == -1)
                    onDeckWindDirection = hmsDataList.GetData(ValueType.SettingsOnDeckWindDirection).data;
            }

            if (displayMode != DisplayMode.PreLanding)
            {
                // Er helikopter heading korrigert?
                if (onDeckHelicopterHeadingIsCorrected)
                {
                    // Er heading endret?
                    if (onDeckHelicopterHeadingPrev != onDeckHelicopterHeading)
                    {
                        // Korrigere RWD
                        // Trekker gammel heading fra ny heading
                        relativeWindLimitsVM.CorrectRWD(onDeckHelicopterHeadingPrev - onDeckHelicopterHeading);

                        // Resette forrige-verdi
                        onDeckHelicopterHeadingPrev = onDeckHelicopterHeading;
                    }
                }
                else
                {
                    // Resette forrige-verdi
                    onDeckHelicopterHeadingPrev = onDeckHelicopterHeading;
                }
            }
            else
            {
                // Reset
                onDeckHelicopterHeadingPrev = -1;
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
                    relativeWindLimitsVM.displayMode = _displayMode;
                    helideckMotionLimitsVM.displayMode = _displayMode;
                    onDeckStabilityLimitsVM.displayMode = _displayMode;
                    windHeadingTrendVM.displayMode = _displayMode;

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

                        // Korreksjon ikke aktiv
                        onDeckHelicopterHeadingIsCorrected = false;

                        // Start oppdatering av grafer
                        windHeadingTrendVM.Start();
                        relativeWindLimitsVM.Start();
                    }
                    else
                    {
                        // Stopp oppdatering av grafer
                        windHeadingTrendVM.Stop();
                        relativeWindLimitsVM.Stop();
                    }

                    // Sette Wind & Heading til å vise default mean vind
                    setDefaultWindMeasurementCallback();

                    OnPropertyChanged(nameof(displayModeVisibilityPreLanding));
                    OnPropertyChanged(nameof(displayModeVisibilityOnDeck));
                    OnPropertyChanged(nameof(helicopterHeadingInfoString));

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

        public bool onDeckHelicopterHeadingIsCorrected { get; set; }

        public double onDeckVesselHeading { get; set; }
        public double onDeckWindDirection { get; set; }

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
                    // Setter ny heading
                    // Ved corrected helicopter heading må vi korrigere for endring i vessel heading
                    _onDeckHelicopterHeading = _displayMode == DisplayMode.PreLanding ? value : value + onDeckVesselHeading - windHeadingVM.vesselHeading.data;

                    onDeckHelicopterHeadingIsCorrected = true;
                    OnPropertyChanged(nameof(helicopterHeadingInfoString));

                    SendUserInputsToServer();
                }
            }
        }

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
        public string helicopterHeadingInfoString
        {
            get
            {
                if (!onDeckHelicopterHeadingIsCorrected)
                    return string.Format("On-Deck Helicopter Heading {0}° M at {1} UTC",
                                onDeckHelicopterHeading.ToString("000"),
                                onDeckTime.ToShortTimeString());
                else
                return string.Format("Corrected Helicopter Heading {0}° M at {1} UTC",
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
