using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Threading;

namespace HMS_Client
{
    public class WindHeadingTrendVM : INotifyPropertyChanged
    {
        // Change notification
        public event PropertyChangedEventHandler PropertyChanged;

        private DispatcherTimer UIUpdateTimer = new DispatcherTimer();
        private DispatcherTimer ChartDataUpdateTimer = new DispatcherTimer();

        // Graph buffer/data
        private RadObservableCollectionEx<HMSData> vesselHdg20mBuffer = new RadObservableCollectionEx<HMSData>();
        public RadObservableCollectionEx<HMSData> vesselHdg20mDataList = new RadObservableCollectionEx<HMSData>();
        private double vesselHdgDeltaAbsMax = 0;

        private RadObservableCollectionEx<HMSData> windDir20mBuffer = new RadObservableCollectionEx<HMSData>();
        public RadObservableCollectionEx<HMSData> windDir20mDataList = new RadObservableCollectionEx<HMSData>();
        private double windDirDeltaAbsMax = 0;

        private UserInputsVM userInputsVM;

        public void Init(Config config, SensorGroupStatus sensorStatus, UserInputsVM userInputsVM)
        {
            this.userInputsVM = userInputsVM;

            InitUI();

            // Oppdatere UI
            UIUpdateTimer.Interval = TimeSpan.FromMilliseconds(config.Read(ConfigKey.ClientUpdateFrequencyUI, Constants.ClientUpdateFrequencyUIDefault));
            UIUpdateTimer.Tick += UIUpdate;

            void UIUpdate(object sender, EventArgs e)
            {
                // Sjekke om vi har data timeout
                sensorStatus.TimeoutCheck(vesselHeadingDelta);
                sensorStatus.TimeoutCheck(windDirectionDelta);

                // Oppdatere data som skal ut i grafer
                UpdateChartBuffer(vesselHeadingDelta, vesselHdg20mBuffer);
                UpdateChartBuffer(windDirectionDelta, windDir20mBuffer);
            }

            // Oppdatere trend data i UI: 20 minutter
            ChartDataUpdateTimer.Interval = TimeSpan.FromMilliseconds(config.Read(ConfigKey.ChartDataUpdateFrequency20m, Constants.ChartUpdateFrequencyUI20mDefault));
            ChartDataUpdateTimer.Tick += ChartDataUpdate;

            void ChartDataUpdate(object sender, EventArgs e)
            {
                // Disse korreksjonene legges inn for å få tidspunkt-label på X aksen til å vises korrekt
                int chartTimeCorrMin = 4;
                int chartTimeCorrMax = -2;

                // Overføre data fra buffer til chart data: 20m
                TransferBuffer(vesselHdg20mBuffer, vesselHdg20mDataList);
                TransferBuffer(windDir20mBuffer, windDir20mDataList);

                // Slette buffer
                ClearBuffer(vesselHdg20mBuffer);
                ClearBuffer(windDir20mBuffer);

                //Fjerne gamle data fra chart data
                RemoveOldData(vesselHdg20mDataList, Constants.Minutes30 + chartTimeCorrMin);
                RemoveOldData(windDir20mDataList, Constants.Minutes30 + chartTimeCorrMin);

                // Finne absolute max verdi
                vesselHdgDeltaAbsMax = FindAbsMax(vesselHdg20mDataList);
                windDirDeltaAbsMax = FindAbsMax(windDir20mDataList);

                // Oppdatere alignment datetime (nåtid) til alle chart
                alignmentTime = DateTime.UtcNow.AddSeconds(Constants.Minutes30 + chartTimeCorrMax);

                //OnPropertyChanged(nameof(helideckTrendTimeString));
            }
        }

        public void Start()
        {
            UIUpdateTimer.Start();
            ChartDataUpdateTimer.Start();
        }

        public void Stop()
        {
            UIUpdateTimer.Stop();
            ChartDataUpdateTimer.Stop();

            // Slette Graph buffer/data
            ClearBuffer(vesselHdg20mBuffer);
            ClearBuffer(vesselHdg20mDataList);
            ClearBuffer(windDir20mBuffer);
            ClearBuffer(windDir20mDataList);
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

        public void UpdateChartBuffer(HMSData data, RadObservableCollectionEx<HMSData> buffer)
        {
            // NB! Når vi har data tilgjengelig fores dette inn i grafene.
            // Når vi ikke har data tilgjengelig legges 0 data inn i grafene for å holde de gående.

            // Grunne til at vi buffrer data først er pga ytelsesproblemer dersom vi kjører data rett ut i grafene på skjerm.
            // Det takler ikke grafene fra Telerik. Buffrer data først og så oppdaterer vi grafene med jevne passende mellomrom.

            if (data?.status == DataStatus.OK)
            {
                // Lagre data i buffer
                buffer?.Add(new HMSData(data));
            }
            else
            {
                // Lagre 0 data
                buffer?.Add(new HMSData() { data = 0, timestamp = DateTime.UtcNow });
            }
        }

        public void TransferBuffer(RadObservableCollectionEx<HMSData> buffer, RadObservableCollectionEx<HMSData> dataList)
        {
            // Overfører alle data fra buffer til dataList
            dataList.AddRange(buffer);
        }

        public void ClearBuffer(RadObservableCollectionEx<HMSData> buffer)
        {
            // Sletter alle data fra buffer
            buffer.Clear();
        }

        public void RemoveOldData(RadObservableCollectionEx<HMSData> dataList, double timeInterval)
        {
            for (int i = 0; i < dataList.Count && i >= 0; i++)
            {
                if (dataList[i]?.timestamp < DateTime.UtcNow.AddSeconds(-timeInterval))
                    dataList.RemoveAt(i--);
                else
                    break;
            }
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
                if (_vesselHeadingDelta != null)
                {
                    // Sjekke om data er gyldig
                    if (_vesselHeadingDelta.status == DataStatus.OK)
                    {
                        if (_vesselHeadingDelta.data >= 0)
                            return string.Format("{0}° R", _vesselHeadingDelta.data.ToString("0"));
                        else
                            return string.Format("{0}° L", Math.Abs(_vesselHeadingDelta.data).ToString("0"));
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
                    _alignmentTime = value;
            }
        }

        private DateTime _alignmentTimeMin { get; set; }
        public DateTime alignmentTimeMin
        {
            get
            {
                return userInputsVM.onDeckTime;
            }
        }

        public DateTime alignmentTimeMax
        {
            get
            {
                return userInputsVM.onDeckTime.AddSeconds(Constants.Minutes30);
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Helideck Trend Time
        /////////////////////////////////////////////////////////////////////////////
        //public string helideckTrendTimeString
        //{
        //    get
        //    {
        //        if (vesselHdg20mDataList.Count > 0)
        //        {
        //            return string.Format("30-minute Trend ({0} - {1} UTC)",
        //                vesselHdg20mDataList[0].timestamp.ToShortTimeString(),
        //                vesselHdg20mDataList[0].timestamp.AddSeconds(Constants.Minutes30).ToShortTimeString());
        //        }
        //        else
        //        {
        //            return "30-minute Trend (--:-- - --:-- UTC)";
        //        }
        //    }
        //}

        // Variabel oppdatert
        // Dersom navn ikke er satt brukes kallende medlem sitt navn
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
