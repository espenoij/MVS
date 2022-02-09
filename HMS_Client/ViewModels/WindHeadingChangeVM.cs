using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Threading;

namespace HMS_Client
{
    public class WindHeadingChangeVM : INotifyPropertyChanged
    {
        // Change notification
        public event PropertyChangedEventHandler PropertyChanged;

        private DispatcherTimer UIUpdateTimer = new DispatcherTimer();

        // Graph buffer/data
        public RadObservableCollectionEx<HMSData> vesselHdg20mDataList = new RadObservableCollectionEx<HMSData>();
        private double vesselHdgDeltaAbsMax = 0;

        public RadObservableCollectionEx<HMSData> windDir20mDataList = new RadObservableCollectionEx<HMSData>();
        private double windDirDeltaAbsMax = 0;

        private UserInputsVM userInputsVM;

        public void Init(Config config, SensorGroupStatus sensorStatus, UserInputsVM userInputsVM)
        {
            this.userInputsVM = userInputsVM;

            InitUI();

            // Oppdatere UI
            UIUpdateTimer.Interval = TimeSpan.FromMilliseconds(config.ReadWithDefault(ConfigKey.ClientUpdateFrequencyUI, Constants.ClientUpdateFrequencyUIDefault));
            UIUpdateTimer.Tick += UIUpdate;

            void UIUpdate(object sender, EventArgs e)
            {
                // Disse korreksjonene legges inn for å få tidspunkt-label på X aksen til å vises korrekt
                int chartTimeCorrMin = 4;
                int chartTimeCorrMax = -2;

                // Sjekke om vi har data timeout
                if (sensorStatus.TimeoutCheck(vesselHeadingDelta))
                {
                    OnPropertyChanged(nameof(vesselHeadingDeltaString));
                    OnPropertyChanged(nameof(vesselHeadingAxisMax));
                    OnPropertyChanged(nameof(vesselHeadingAxisMin));
                }
                
                if (sensorStatus.TimeoutCheck(windDirectionDelta))
                {
                    OnPropertyChanged(nameof(deltaWindDirectionString));
                    OnPropertyChanged(nameof(windDirectionAxisMax));
                    OnPropertyChanged(nameof(windDirectionAxisMin));
                }

                // Oppdatere data som skal ut i grafer
                GraphBuffer.UpdateWithCull(vesselHeadingDelta, vesselHdg20mDataList, Constants.GraphCullFrequency30m);
                GraphBuffer.UpdateWithCull(windDirectionDelta, windDir20mDataList, Constants.GraphCullFrequency30m);

                // Fjerne gamle data fra chart data
                GraphBuffer.RemoveOldData(vesselHdg20mDataList, Constants.Minutes30 + chartTimeCorrMin);
                GraphBuffer.RemoveOldData(windDir20mDataList, Constants.Minutes30 + chartTimeCorrMin);

                // Finne absolute max verdi
                vesselHdgDeltaAbsMax = FindAbsMax(vesselHdg20mDataList);
                windDirDeltaAbsMax = FindAbsMax(windDir20mDataList);

                // Oppdatere alignment datetime (nåtid) til begge chart
                if (userInputsVM.onDeckTime.AddSeconds(Constants.Minutes30) > DateTime.UtcNow)
                    alignmentTime = userInputsVM.onDeckTime.AddSeconds(Constants.Minutes30 + chartTimeCorrMax);
                else
                    alignmentTime = DateTime.UtcNow;
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
            GraphBuffer.Clear(vesselHdg20mDataList);
            GraphBuffer.Clear(windDir20mDataList);
        }

        private void InitUI()
        {
            _vesselHeadingDelta = new HMSData();
            _windDirectionDelta = new HMSData();
        }

        public void UpdateData(HMSDataCollection clientSensorList)
        {
            // Heading Data
            vesselHeadingDelta = clientSensorList.GetData(ValueType.VesselHeadingDelta);
            windDirectionDelta = clientSensorList.GetData(ValueType.WindDirectionDelta);
        }

        private double FindAbsMax(RadObservableCollectionEx<HMSData> dataList)
        {
            double max = 0;

            foreach (var item in dataList)
                if (Math.Abs(item.data) > max)
                    max = Math.Abs(item.data);

            return max;
        }

        /////////////////////////////////////////////////////////////////////////////
        // Wind Calculations: Wind Speed
        /////////////////////////////////////////////////////////////////////////////
        public HMSData _vesselHeadingDelta { get; set; }
        public HMSData vesselHeadingDelta
        {
            get
            {
                return _vesselHeadingDelta;
            }
            set
            {
                if (value != null)
                {
                    OnPropertyChanged(nameof(vesselHeadingDeltaString));
                    OnPropertyChanged(nameof(vesselHeadingAxisMax));
                    OnPropertyChanged(nameof(vesselHeadingAxisMin));

                    _vesselHeadingDelta.Set(value);
                }
            }
        }

        public string vesselHeadingDeltaString
        {
            get
            {
                if (vesselHeadingDelta != null)
                {
                    // Sjekke om data er gyldig
                    if (vesselHeadingDelta.status == DataStatus.OK)
                    {
                        if (vesselHeadingDelta.data >= 0)
                            return string.Format("{0}° R", vesselHeadingDelta.data.ToString("0"));
                        else
                            return string.Format("{0}° L", Math.Abs(vesselHeadingDelta.data).ToString("0"));
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
        // Relative Wind Direction
        /////////////////////////////////////////////////////////////////////////////
        public HMSData _windDirectionDelta { get; set; }
        public HMSData windDirectionDelta
        {
            get
            {
                return _windDirectionDelta;
            }
            set
            {
                if (value != null)
                {
                    OnPropertyChanged(nameof(deltaWindDirectionString));
                    OnPropertyChanged(nameof(windDirectionAxisMax));
                    OnPropertyChanged(nameof(windDirectionAxisMin));

                    _windDirectionDelta.Set(value);
                }
            }
        }

        public string deltaWindDirectionString
        {
            get
            {
                if (_windDirectionDelta != null)
                {
                    // Sjekke om data er gyldig
                    if (_windDirectionDelta.status == DataStatus.OK)
                    {
                        if (windDirectionDelta.data >= 0)
                            return string.Format("{0}° R", windDirectionDelta.data.ToString("0"));
                        else
                            return string.Format("{0}° L", Math.Abs(windDirectionDelta.data).ToString("0"));
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
        // Chart Axis Min/Max
        /////////////////////////////////////////////////////////////////////////////
        public double vesselHeadingAxisMax
        {
            get
            {
                int axisMax;

                axisMax = (int)vesselHdgDeltaAbsMax - ((int)vesselHdgDeltaAbsMax % 5) + 5;

                if (axisMax < 10)
                    axisMax = 10;

                return (double)axisMax;
            }
        }

        public double vesselHeadingAxisMin
        {
            get
            {
                int axisMin;

                axisMin = (int)vesselHdgDeltaAbsMax - ((int)vesselHdgDeltaAbsMax % 5) + 5;

                if (axisMin < 10)
                    axisMin = 10;

                return (double)axisMin * -1;
            }
        }

        public double windDirectionAxisMax
        {
            get
            {
                int axisMax;

                axisMax = (int)windDirDeltaAbsMax - ((int)windDirDeltaAbsMax % 5) + 5;

                if (axisMax < 10)
                    axisMax = 10;

                return (double)axisMax;
            }
        }

        public double windDirectionAxisMin
        {
            get
            {
                int axisMin;

                axisMin = (int)windDirDeltaAbsMax - ((int)windDirDeltaAbsMax % 5) + 5;

                if (axisMin < 10)
                    axisMin = 10;

                return (double)axisMin * -1;
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

        /////////////////////////////////////////////////////////////////////////////
        // Chart X axis alignment
        /////////////////////////////////////////////////////////////////////////////
        private DateTime _alignmentTime;
        public DateTime alignmentTime
        {
            get
            {
                return _alignmentTime;
            }
            set
            {
                if (_alignmentTime != value)
                {
                    _alignmentTime = value;

                    OnPropertyChanged(nameof(alignmentTimeMin));
                    OnPropertyChanged(nameof(alignmentTimeMax));
                }
            }
        }

        public DateTime alignmentTimeMin
        {
            get
            {
                if (userInputsVM.onDeckTime.AddSeconds(Constants.Minutes30) > DateTime.UtcNow)
                    return userInputsVM.onDeckTime;
                else
                    return DateTime.UtcNow.AddSeconds(-Constants.Minutes30);
            }
        }

        public DateTime alignmentTimeMax
        {
            get
            {
                if (userInputsVM.onDeckTime.AddSeconds(Constants.Minutes30) > DateTime.UtcNow)
                    return userInputsVM.onDeckTime.AddSeconds(Constants.Minutes30);
                else
                    return DateTime.UtcNow;
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
