using System;
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
        public RadObservableCollectionEx<HMSData> relativeWindDir20mDataList = new RadObservableCollectionEx<HMSData>();

        // Lagret helicopter heading
        private double savedHelicopterHeading = -1;

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
                // Sjekke om vi har data timeout
                sensorStatus.TimeoutCheck(windSpd);
                if (sensorStatus.TimeoutCheck(relativeWindDir)) OnPropertyChanged(nameof(relativeWindDirString));

                // Oppdatere data som skal ut i grafer
                if (rwdGraphData.status == DataStatus.OK)
                    GraphBuffer.UpdateWithCull(rwdGraphData, relativeWindDir20mDataList, Constants.GraphCullFrequency20m);

                // Fjerne gamle data fra chart data
                GraphBuffer.RemoveOldData(relativeWindDir20mDataList, Constants.Minutes20);
            }
        }

        private void InitUI()
        {
            _windSpd = new HMSData();
            _relativeWindDir = new HMSData();
            _rwdGraphData = new HMSData();
        }

        public void Start()
        {
            UIUpdateTimer.Start();
        }

        public void Stop()
        {
            UIUpdateTimer.Stop();

            // Slette Graph buffer/data
            GraphBuffer.Clear(relativeWindDir20mDataList);
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
                double rwd = Math.Abs(relativeWindDir.data);
                if (rwd < 0)
                    rwd = 0;
                else
                if (rwd > 60)
                    rwd = 60;

                // Begrense vind i grafen til 60 kts
                double wind = windSpd.data;
                if (wind < 0)
                    wind = 0;
                else
                if (wind > 60)
                    wind = 60;

                rwdGraphData.data = rwd;
                rwdGraphData.data2 = wind;
                rwdGraphData.status = DataStatus.OK;
                rwdGraphData.timestamp = windSpd.timestamp;

                OnPropertyChanged(nameof(rwdGraphDataX));
                OnPropertyChanged(nameof(rwdGraphDataY));
            }
            else
            {
                rwdGraphData.data = 0;
                rwdGraphData.data2 = 0;
                rwdGraphData.status = DataStatus.TIMEOUT_ERROR;
                rwdGraphData.timestamp = windSpd.timestamp;

                OnPropertyChanged(nameof(rwdGraphDataX));
                OnPropertyChanged(nameof(rwdGraphDataY));
            }

            // On-deck display?
            if (userInputsVM.displayMode == DisplayMode.OnDeck)
            {
                // Er on-deck helicopter heading korrigert?
                if (userInputsVM.onDeckHelicopterHeading != savedHelicopterHeading)
                {
                    if (savedHelicopterHeading != -1)
                        CorrectRWDGraph(savedHelicopterHeading - userInputsVM.onDeckHelicopterHeading);

                    savedHelicopterHeading = userInputsVM.onDeckHelicopterHeading;
                }
            }
        }

        private void CorrectRWDGraph(double correction)
        {
            // Løper gjennom hele listen og legger til korreksjon på RWD komponenten
            foreach (var item in relativeWindDir20mDataList)
            {
                item.data += correction;
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Wind Calculations: Wind Speed
        /////////////////////////////////////////////////////////////////////////////
        private HMSData _windSpd { get; set; }
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
        private HMSData _relativeWindDir { get; set; }
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
        private HMSData _rwdGraphData { get; set; }
        public HMSData rwdGraphData
        {
            get
            {
                return _rwdGraphData;
            }
            set
            {
                if (value != null)
                {
                    if (_rwdGraphData.data != value.data ||
                        _rwdGraphData.data2 != value.data2)
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
                    return _rwdGraphData.data2;
                else
                    return 0;
            }
        }

        public double rwdGraphDataY
        {
            get
            {
                if (_rwdGraphData?.status == DataStatus.OK)
                    return _rwdGraphData.data;
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
