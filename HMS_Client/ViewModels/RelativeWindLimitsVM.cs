using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Threading;

namespace HMS_Client
{
    public class RelativeWindLimitsVM : INotifyPropertyChanged
    {
        // Change notification
        public event PropertyChangedEventHandler PropertyChanged;

        private DispatcherTimer UIUpdateTimer = new DispatcherTimer();

        // Relative Wind graph buffer/data
        public RadObservableCollectionEx<RWDData> relativeWindDir30mDataList = new RadObservableCollectionEx<RWDData>();

        private OnDeckStabilityLimitsVM onDeckStabilityLimitsVM;
        private WindHeadingChangeVM windHeadingChangeVM;

        public void Init(Config config, SensorGroupStatus sensorStatus, OnDeckStabilityLimitsVM onDeckStabilityLimitsVM, WindHeadingChangeVM windHeadingChangeVM)
        {
            this.onDeckStabilityLimitsVM = onDeckStabilityLimitsVM;
            this.windHeadingChangeVM = windHeadingChangeVM;

            // Oppdatere UI
            UIUpdateTimer.Interval = TimeSpan.FromMilliseconds(config.ReadWithDefault(ConfigKey.ClientUpdateFrequencyUI, Constants.ClientUIUpdateFrequencyDefault));
            UIUpdateTimer.Tick += UIUpdate;

            void UIUpdate(object sender, EventArgs e)
            {
                // Sjekke om vi har data timeout
                sensorStatus.TimeoutCheck(windSpd);
                if (sensorStatus.TimeoutCheck(relativeWindDir)) OnPropertyChanged(nameof(relativeWindDirString));

                // Oppdatere data som skal ut i grafer
                if (rwdGraphData.status == DataStatus.OK)
                    GraphBuffer.UpdateWithCull(rwdGraphData, relativeWindDir30mDataList, Constants.GraphCullFrequency30m);

                // Fjerne gamle data fra chart data
                GraphBuffer.RemoveOldData(relativeWindDir30mDataList, Constants.Minutes30);

                OnPropertyChanged(nameof(landingStatusTimeString));
                OnPropertyChanged(nameof(displayModeVisibilityPreLanding));
            }
        }

        public void Start()
        {
            UIUpdateTimer.Start();
        }

        public void Stop()
        {
            UIUpdateTimer.Stop();

            // Slette Graph buffer/data
            GraphBuffer.Clear(relativeWindDir30mDataList);
        }

        public void UpdateData(HMSDataCollection clientSensorList)
        {
            // Wind Data
            windSpd = clientSensorList.GetData(ValueType.HelideckWindSpeed2m);
            relativeWindDir = clientSensorList.GetData(ValueType.RelativeWindDir);

            if (windSpd.status == DataStatus.OK &&
                relativeWindDir.status == DataStatus.OK)
            {
                // Begrenser RWD i grafen til 60 grader
                double rwd = relativeWindDir.data;

                // Begrense vind i grafen til 60 kts
                double wind = windSpd.data;

                rwdGraphData.rwd = rwd;
                rwdGraphData.wind = wind;
                rwdGraphData.status = DataStatus.OK;
                rwdGraphData.timestamp = windSpd.timestamp;

                OnPropertyChanged(nameof(rwdGraphDataX));
                OnPropertyChanged(nameof(rwdGraphDataY));
            }
            else
            {
                rwdGraphData.rwd = 0;
                rwdGraphData.wind = 0;
                rwdGraphData.status = DataStatus.TIMEOUT_ERROR;
                rwdGraphData.timestamp = windSpd.timestamp;

                OnPropertyChanged(nameof(rwdGraphDataX));
                OnPropertyChanged(nameof(rwdGraphDataY));
            }
        }

        public void CorrectRWD(double correction)
        {
            // Løper gjennom hele listen og legger til korreksjon på RWD komponenten
            foreach (var item in relativeWindDir30mDataList)
            {
                // Lagre korrigert data
                item.rwd += correction;
            }

            // Korrigere siste input fra server
            //rwdGraphData.rwd -= newHeading - oldHeading;
            rwdGraphData.status = DataStatus.TIMEOUT_ERROR; // Sikrere å merke data som ubrukelige

            // Oppdatere trend data
            windHeadingChangeVM.CorrectRWDTrend(correction);
        }

        /////////////////////////////////////////////////////////////////////////////
        // Helideck Status
        /////////////////////////////////////////////////////////////////////////////
        private HelideckStatusType _helideckStatus { get; set; }
        public HelideckStatusType helideckStatus
        {
            set
            {
                if (_helideckStatus != value)
                {
                    _helideckStatus = value;
                }
            }
            get
            {
                return _helideckStatus;
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Helideck Status Trend Time
        /////////////////////////////////////////////////////////////////////////////
        public string landingStatusTimeString
        {
            get
            {
                if (relativeWindDir30mDataList.Count > 0)
                {
                    return string.Format("30-minute Trend ({1} - {2} UTC)",
                        relativeWindDir30mDataList[0].timestamp.ToShortTimeString(),
                        relativeWindDir30mDataList[relativeWindDir30mDataList.Count - 1].timestamp.ToShortTimeString());
                }
                else
                {
                    return string.Format("30-minute Trend (--:-- - --:-- UTC)");
                }
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Pre-Landing Visibility
        /////////////////////////////////////////////////////////////////////////////
        public bool displayModeVisibilityPreLanding
        {
            get
            {
                return onDeckStabilityLimitsVM.displayModeVisibilityPreLanding;
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Wind Calculations: Wind Speed
        /////////////////////////////////////////////////////////////////////////////
        private HMSData _windSpd { get; set; } = new HMSData();
        public HMSData windSpd
        {
            get
            {
                return _windSpd;
            }
            set
            {
                if (value != null)
                {
                    if (value.data != _windSpd.data ||
                        value.timestamp != _windSpd.timestamp)
                    {
                        _windSpd.Set(value);
                    }
                }
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Relative Wind Direction
        /////////////////////////////////////////////////////////////////////////////
        private HMSData _relativeWindDir { get; set; } = new HMSData();
        public HMSData relativeWindDir
        {
            get
            {
                return _relativeWindDir;
            }
            set
            {
                if (value != null)
                {
                    _relativeWindDir.Set(value);

                    OnPropertyChanged(nameof(relativeWindDirString));
                }
            }
        }

        public string relativeWindDirString
        {
            get
            {
                if (relativeWindDir != null)
                {
                    // Sjekke om data er gyldig
                    if (relativeWindDir.status == DataStatus.OK)
                    {
                        if (relativeWindDir.data >= 0)
                            return string.Format("{0}° R", relativeWindDir.data.ToString("0"));
                        else
                            return string.Format("{0}° L", Math.Abs(relativeWindDir.data).ToString("0"));
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
        // Relative Wind Direction Graf data
        /////////////////////////////////////////////////////////////////////////////
        private RWDData _rwdGraphData { get; set; } = new RWDData();
        public RWDData rwdGraphData
        {
            get
            {
                return _rwdGraphData;
            }
            set
            {
                if (value != null)
                {
                    if (_rwdGraphData.rwd != value.rwd ||
                        _rwdGraphData.wind != value.wind)
                    {
                        OnPropertyChanged(nameof(rwdGraphDataX));
                        OnPropertyChanged(nameof(rwdGraphDataY));
                    }

                    _rwdGraphData.Set(value);
                }
            }
        }

        public double rwdGraphDataX
        {
            get
            {
                if (_rwdGraphData?.status == DataStatus.OK)
                    return _rwdGraphData.wind;
                else
                    return 0;
            }
        }

        public double rwdGraphDataY
        {
            get
            {
                if (_rwdGraphData?.status == DataStatus.OK)
                {
                    double val = Math.Abs(_rwdGraphData.rwd);
                    if (val > 60)
                        val = 60;
                    return val;
                }
                else
                    return 0;
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
                if (_displayMode == DisplayMode.OnDeck)
                    return true;
                else
                    return false;
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
