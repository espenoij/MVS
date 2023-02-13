using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Threading;

namespace HMS_Client
{
    public class HelideckMotionLimitsVM : INotifyPropertyChanged
    {
        // Change notification
        public event PropertyChangedEventHandler PropertyChanged;

        private RegulationStandard regulationStandard;

        private DispatcherTimer UIUpdateTimer = new DispatcherTimer();

        public void Init(Config config, SensorGroupStatus sensorStatus)
        {
            regulationStandard = (RegulationStandard)Enum.Parse(typeof(RegulationStandard), config.ReadWithDefault(ConfigKey.RegulationStandard, RegulationStandard.NOROG.ToString()));

            // Init
            InitMotionLimits();
            InitPitchData();
            InitRollData();
            InitInclinationData();
            InitHeaveData();
            InitSignificantHeaveRateData();
            InitHeavePeriodData();

            // Oppdatere UI
            UIUpdateTimer.Interval = TimeSpan.FromMilliseconds(config.ReadWithDefault(ConfigKey.ClientUpdateFrequencyUI, Constants.ClientUIUpdateFrequencyDefault));
            UIUpdateTimer.Tick += UIUpdate;
            UIUpdateTimer.Start();

            void UIUpdate(object sender, EventArgs e)
            {
                // Sjekke om vi har data timeout
                if (sensorStatus.TimeoutCheck(pitchMax20mData)) OnPropertyChanged(nameof(pitchMax20mString));
                if (sensorStatus.TimeoutCheck(pitchMaxUp20mData)) OnPropertyChanged(nameof(pitchMaxUp20mString));
                if (sensorStatus.TimeoutCheck(pitchMaxDown20mData)) OnPropertyChanged(nameof(pitchMaxDown20mString));

                if (sensorStatus.TimeoutCheck(rollMax20mData)) OnPropertyChanged(nameof(rollMax20mString));
                if (sensorStatus.TimeoutCheck(rollMaxLeft20mData)) OnPropertyChanged(nameof(rollMaxLeft20mString));
                if (sensorStatus.TimeoutCheck(rollMaxRight20mData)) OnPropertyChanged(nameof(rollMaxRight20mString));

                if (sensorStatus.TimeoutCheck(inclinationMax20mData)) OnPropertyChanged(nameof(inclinationMax20mString));

                if (sensorStatus.TimeoutCheck(heaveHeightMax20mData)) OnPropertyChanged(nameof(heaveHeightMax20mString));
                if (sensorStatus.TimeoutCheck(heavePeriodData)) OnPropertyChanged(nameof(heavePeriodString));

                if (sensorStatus.TimeoutCheck(significantHeaveRateData)) OnPropertyChanged(nameof(significantHeaveRateString));

                if (sensorStatus.TimeoutCheck(pitchRollLimit)) OnPropertyChanged(nameof(pitchRollLimitString));
                if (sensorStatus.TimeoutCheck(inclinationLimit)) OnPropertyChanged(nameof(inclinationLimitString));
                if (sensorStatus.TimeoutCheck(heaveHeightLimit)) OnPropertyChanged(nameof(heaveHeightLimitString));
                if (sensorStatus.TimeoutCheck(significantHeaveRateLimit)) OnPropertyChanged(nameof(significantHeaveRateLimitString));
            }
        }

        public void UpdateData(HMSDataCollection hmsDataList)
        {
            // Roll & Pitch
            pitchMax20mData = hmsDataList.GetData(ValueType.PitchMax20m);
            rollMax20mData = hmsDataList.GetData(ValueType.RollMax20m);

            if (regulationStandard == RegulationStandard.CAP)
            {
                pitchMaxUp20mData = hmsDataList.GetData(ValueType.PitchMaxUp20m);
                pitchMaxDown20mData = hmsDataList.GetData(ValueType.PitchMaxDown20m);
                rollMaxLeft20mData = hmsDataList.GetData(ValueType.RollMaxLeft20m);
                rollMaxRight20mData = hmsDataList.GetData(ValueType.RollMaxRight20m);
            }

            // Inclination
            inclinationMax20mData = hmsDataList.GetData(ValueType.InclinationMax20m);

            //// TEST
            //if (hmsDataList.GetData(ValueType.InclinationMax20m).status == DataStatus.OK)
            //    OnPropertyChanged(nameof(inclinationMax20mString));

            // Heave
            heaveHeightMax20mData = hmsDataList.GetData(ValueType.HeaveHeightMax20m);
            heavePeriodData = hmsDataList.GetData(ValueType.HeavePeriodMean);

            // SHR
            significantHeaveRateData = hmsDataList.GetData(ValueType.SignificantHeaveRate);

            // Limits
            pitchRollLimit = hmsDataList.GetData(ValueType.MotionLimitPitchRoll);
            inclinationLimit = hmsDataList.GetData(ValueType.MotionLimitInclination);
            heaveHeightLimit = hmsDataList.GetData(ValueType.MotionLimitHeaveHeight);
            significantHeaveRateLimit = hmsDataList.GetData(ValueType.MotionLimitSignificantHeaveRate);
        }

        /////////////////////////////////////////////////////////////////////////////
        // Motion Limits
        /////////////////////////////////////////////////////////////////////////////
        private void InitMotionLimits()
        {
            _pitchRollLimit = new HMSData();
            _inclinationLimit = new HMSData();
            _heaveHeightLimit = new HMSData();
            _significantHeaveRateLimit = new HMSData();
        }

        // Pitch & Roll Limit
        private HMSData _pitchRollLimit { get; set; }
        public HMSData pitchRollLimit
        {
            get
            {
                return _pitchRollLimit;
            }
            set
            {
                if (value != null)
                {
                    _pitchRollLimit.Set(value);

                    OnPropertyChanged(nameof(pitchRollLimitString));
                }
            }
        }
        public string pitchRollLimitString
        {
            get
            {
                if (_pitchRollLimit != null)
                {
                    if (_pitchRollLimit.status == DataStatus.OK)
                        return string.Format("{0} °", _pitchRollLimit.data.ToString("0.0"));
                    else
                        return Constants.NotAvailable;
                }
                else
                {
                    return Constants.NotAvailable;
                }
            }
        }

        // Inclination Limit
        private HMSData _inclinationLimit { get; set; }
        public HMSData inclinationLimit
        {
            get
            {
                return _inclinationLimit;
            }
            set
            {
                if (value != null)
                {
                    _inclinationLimit.Set(value);

                    OnPropertyChanged(nameof(inclinationLimitString));
                }
            }
        }
        public string inclinationLimitString
        {
            get
            {
                if (_inclinationLimit != null)
                {
                    if (_inclinationLimit.status == DataStatus.OK)
                        return string.Format("{0} °", _inclinationLimit.data.ToString("0.0"));
                    else
                        return Constants.NotAvailable;
                }
                else
                {
                    return Constants.NotAvailable;
                }
            }
        }

        // Heave Height Limit
        private HMSData _heaveHeightLimit { get; set; }
        public HMSData heaveHeightLimit
        {
            get
            {
                return _heaveHeightLimit;
            }
            set
            {
                if (value != null)
                {
                    _heaveHeightLimit.Set(value);

                    OnPropertyChanged(nameof(heaveHeightLimitString));
                }
            }
        }
        public string heaveHeightLimitString
        {
            get
            {
                if (_heaveHeightLimit != null)
                {
                    if (_heaveHeightLimit.status == DataStatus.OK)
                        return string.Format("{0} m", _heaveHeightLimit.data.ToString("0.0"));
                    else
                        return Constants.NotAvailable;
                }
                else
                {
                    return Constants.NotAvailable;
                }
            }
        }

        // Significant Heave Rate Limit
        private HMSData _significantHeaveRateLimit { get; set; }
        public HMSData significantHeaveRateLimit
        {
            get
            {
                return _significantHeaveRateLimit;
            }
            set
            {
                if (value != null)
                {
                    _significantHeaveRateLimit.Set(value);

                    OnPropertyChanged(nameof(significantHeaveRateLimitString));
                }
            }
        }
        public string significantHeaveRateLimitString
        {
            get
            {
                if (_significantHeaveRateLimit != null)
                {
                    if (_significantHeaveRateLimit.status == DataStatus.OK)
                        return string.Format("{0} m/s", _significantHeaveRateLimit.data.ToString("0.0"));
                    else
                        return Constants.NotAvailable;
                }
                else
                {
                    return Constants.NotAvailable;
                }
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Pitch
        /////////////////////////////////////////////////////////////////////////////
        private void InitPitchData()
        {
            _pitchMax20mData = new HMSData();
            _pitchMaxUp20mData = new HMSData();
            _pitchMaxDown20mData = new HMSData();
        }

        private HMSData _pitchMax20mData { get; set; }
        public HMSData pitchMax20mData
        {
            get
            {
                return _pitchMax20mData;
            }
            set
            {
                if (value != null)
                {
                    _pitchMax20mData.Set(value);

                    OnPropertyChanged(nameof(pitchMax20mString));
                }
            }
        }
        public string pitchMax20mString
        {
            get
            {
                if (pitchMax20mData != null)
                {
                    // Sjekke om data er gyldig
                    if (pitchMax20mData.status == DataStatus.OK)
                    {
                        return string.Format("{0} °", pitchMax20mData.data.ToString("0.0"));
                    }
                    else
                    {
                        return Constants.NotAvailable;
                    }
                }
                else
                {
                    return Constants.NotAvailable;
                }
            }
        }

        private HMSData _pitchMaxUp20mData { get; set; }
        public HMSData pitchMaxUp20mData
        {
            get
            {
                return _pitchMaxUp20mData;
            }
            set
            {
                if (value != null)
                {
                    _pitchMaxUp20mData.Set(value);

                    OnPropertyChanged(nameof(pitchMaxUp20mString));
                }
            }
        }
        public string pitchMaxUp20mString
        {
            get
            {
                if (pitchMaxUp20mData != null)
                {
                    // Sjekke om data er gyldig
                    if (pitchMaxUp20mData.status == DataStatus.OK)
                    {
                        double pitch = Math.Abs(pitchMaxUp20mData.data);

                        string dir;
                        if (pitchMaxUp20mData.data > 0)
                            dir = "U";
                        else
                            dir = "D";

                        return string.Format("{0} ° {1}", pitch.ToString("0.0"), dir);
                    }
                    else
                    {
                        return Constants.NotAvailable;
                    }
                }
                else
                {
                    return Constants.NotAvailable;
                }
            }
        }

        private HMSData _pitchMaxDown20mData { get; set; }
        public HMSData pitchMaxDown20mData
        {
            get
            {
                return _pitchMaxDown20mData;
            }
            set
            {
                if (value != null)
                {
                    _pitchMaxDown20mData.Set(value);

                    OnPropertyChanged(nameof(pitchMaxDown20mString));
                }
            }
        }
        public string pitchMaxDown20mString
        {
            get
            {
                if (pitchMaxDown20mData != null)
                {
                    // Sjekke om data er gyldig
                    if (pitchMaxDown20mData.status == DataStatus.OK)
                    {
                        double pitch = Math.Abs(pitchMaxDown20mData.data);

                        string dir;
                        if (pitchMaxDown20mData.data > 0)
                            dir = "U";
                        else
                            dir = "D";
                        
                        return string.Format("{0} ° {1}", pitch.ToString("0.0"), dir);
                    }
                    else
                    {
                        return Constants.NotAvailable;
                    }
                }
                else
                {
                    return Constants.NotAvailable;
                }
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Roll
        /////////////////////////////////////////////////////////////////////////////
        private void InitRollData()
        {
            _rollMax20mData = new HMSData();
            _rollMaxLeft20mData = new HMSData();
            _rollMaxRight20mData = new HMSData();
        }

        private HMSData _rollMax20mData { get; set; }
        public HMSData rollMax20mData
        {
            get
            {
                return _rollMax20mData;
            }
            set
            {
                if (value != null)
                {
                    _rollMax20mData.Set(value);

                    OnPropertyChanged(nameof(rollMax20mString));
                }
            }
        }
        public string rollMax20mString
        {
            get
            {
                if (rollMax20mData != null)
                {
                    // Sjekke om data er gyldig
                    if (rollMax20mData.status == DataStatus.OK)
                    {
                        return string.Format("{0} °", rollMax20mData.data.ToString("0.0"));
                    }
                    else
                    {
                        return Constants.NotAvailable;
                    }
                }
                else
                {
                    return Constants.NotAvailable;
                }
            }
        }

        private HMSData _rollMaxLeft20mData { get; set; }
        public HMSData rollMaxLeft20mData
        {
            get
            {
                return _rollMaxLeft20mData;
            }
            set
            {
                if (value != null)
                {
                    _rollMaxLeft20mData.Set(value);

                    OnPropertyChanged(nameof(rollMaxLeft20mString));
                }
            }
        }
        public string rollMaxLeft20mString
        {
            get
            {
                if (rollMaxLeft20mData != null)
                {
                    // Sjekke om data er gyldig
                    if (rollMaxLeft20mData.status == DataStatus.OK)
                    {
                        double roll = Math.Abs(rollMaxLeft20mData.data);

                        string dir;
                        if (rollMaxLeft20mData.data >= 0)
                            dir = "L";
                        else
                            dir = "R";

                        return string.Format("{0} ° {1}", roll.ToString("0.0"), dir);
                    }
                    else
                    {
                        return Constants.NotAvailable;
                    }
                }
                else
                {
                    return Constants.NotAvailable;
                }
            }
        }

        private HMSData _rollMaxRight20mData { get; set; }
        public HMSData rollMaxRight20mData
        {
            get
            {
                return _rollMaxRight20mData;
            }
            set
            {
                if (value != null)
                {
                    _rollMaxRight20mData.Set(value);

                    OnPropertyChanged(nameof(rollMaxRight20mString));
                }
            }
        }
        public string rollMaxRight20mString
        {
            get
            {
                if (rollMaxRight20mData != null)
                {
                    // Sjekke om data er gyldig
                    if (rollMaxRight20mData.status == DataStatus.OK)
                    {
                        double roll = Math.Abs(rollMaxRight20mData.data);

                        string dir;
                        if (rollMaxRight20mData.data >= 0)
                            dir = "L";
                        else
                            dir = "R";
                        
                        return string.Format("{0} ° {1}", roll.ToString("0.0"), dir);
                    }
                    else
                    {
                        return Constants.NotAvailable;
                    }
                }
                else
                {
                    return Constants.NotAvailable;
                }
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Inclination
        /////////////////////////////////////////////////////////////////////////////
        public void InitInclinationData()
        {
            _inclinationMax20mData = new HMSData();
        }

        private HMSData _inclinationMax20mData { get; set; }
        public HMSData inclinationMax20mData
        {
            get
            {
                return _inclinationMax20mData;
            }
            set
            {
                if (value != null)
                {
                    _inclinationMax20mData.Set(value);

                    OnPropertyChanged(nameof(inclinationMax20mString));
                }
            }
        }

        public string inclinationMax20mString
        {
            get
            {
                if (inclinationMax20mData != null)
                {
                    // Sjekke om data er gyldig
                    if (pitchMax20mData.status == DataStatus.OK &&
                        rollMax20mData.status == DataStatus.OK)
                    {
                        return string.Format("{0} °", inclinationMax20mData.data.ToString("0.0"));
                    }
                    else
                    {
                        return Constants.NotAvailable;
                    }
                }
                else
                {
                    return Constants.NotAvailable;
                }
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Heave Height
        /////////////////////////////////////////////////////////////////////////////
        public void InitHeaveData()
        {
            _heaveHeightMax20mData = new HMSData();
        }

        private HMSData _heaveHeightMax20mData { get; set; }
        public HMSData heaveHeightMax20mData
        {
            get
            {
                return _heaveHeightMax20mData;
            }
            set
            {
                if (value != null)
                {
                    _heaveHeightMax20mData.Set(value);

                    OnPropertyChanged(nameof(heaveHeightMax20mString));
                }
            }
        }

        public string heaveHeightMax20mString
        {
            get
            {
                if (heaveHeightMax20mData != null)
                {
                    // Sjekke om data er gyldig
                    if (heaveHeightMax20mData.status == DataStatus.OK)
                    {
                        return string.Format("{0} m", heaveHeightMax20mData.data.ToString("0.0"));
                    }
                    else
                    {
                        return Constants.NotAvailable;
                    }
                }
                else
                {
                    return Constants.NotAvailable;
                }
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Significant Heave Rate
        /////////////////////////////////////////////////////////////////////////////
        public void InitSignificantHeaveRateData()
        {
            _significantHeaveRateData = new HMSData();
        }

        private HMSData _significantHeaveRateData { get; set; }
        public HMSData significantHeaveRateData
        {
            get
            {
                return _significantHeaveRateData;
            }
            set
            {
                if (value != null)
                {
                    _significantHeaveRateData.Set(value);

                    OnPropertyChanged(nameof(significantHeaveRateString));
                }
            }
        }
        public string significantHeaveRateString
        {
            get
            {
                if (significantHeaveRateData != null)
                {
                    // Sjekke om data er gyldig
                    if (significantHeaveRateData.status == DataStatus.OK)
                    {
                        return string.Format("{0} m/s", significantHeaveRateData.data.ToString("0.0"));
                    }
                    else
                    {
                        return Constants.NotAvailable;
                    }
                }
                else
                {
                    return Constants.NotAvailable;
                }
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Heave Period Mean
        /////////////////////////////////////////////////////////////////////////////
        public void InitHeavePeriodData()
        {
            _heavePeriodData = new HMSData();
        }

        private HMSData _heavePeriodData { get; set; }
        public HMSData heavePeriodData
        {
            get
            {
                return _heavePeriodData;
            }
            set
            {
                if (value != null)
                {
                    _heavePeriodData.Set(value);
                    OnPropertyChanged(nameof(heavePeriodString));
                }
            }
        }
        public string heavePeriodString
        {
            get
            {
                if (heavePeriodData != null)
                {
                    // Sjekke om data er gyldig
                    if (heavePeriodData.status == DataStatus.OK)
                    {
                        return string.Format("{0} s", heavePeriodData.data.ToString("0.0"));
                    }
                    else
                    {
                        return Constants.NotAvailable;
                    }
                }
                else
                {
                    return Constants.NotAvailable;
                }
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
                _displayMode = value;

                OnPropertyChanged(nameof(displayModeVisibilityOnDeck));
            }
        }

        public bool displayModeVisibilityOnDeck
        {
            get
            {
                if (_displayMode == DisplayMode.PreLanding)
                    return true;
                else
                    return false;
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
