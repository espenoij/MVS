using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Threading;

namespace HMS_Client
{
    public class EMSWaveDataVM : INotifyPropertyChanged
    {
        // Change notification
        public event PropertyChangedEventHandler PropertyChanged;

        private DispatcherTimer UIUpdateTimer = new DispatcherTimer();
        private DispatcherTimer ChartDataUpdateTimer20m = new DispatcherTimer();
        private DispatcherTimer ChartDataUpdateTimer3h = new DispatcherTimer();

        // 20 minutters buffer
        private RadObservableCollectionEx<HMSData> waveSWHBuffer20m = new RadObservableCollectionEx<HMSData>();
        private RadObservableCollectionEx<HMSData> waveSWHMaxBuffer20m = new RadObservableCollectionEx<HMSData>();
        private RadObservableCollectionEx<HMSData> wavePeriodBuffer20m = new RadObservableCollectionEx<HMSData>();
        private RadObservableCollectionEx<HMSData> wavePeriodMaxBuffer20m = new RadObservableCollectionEx<HMSData>();

        // 3 timers buffer
        private RadObservableCollectionEx<HMSData> waveSWHBuffer3h = new RadObservableCollectionEx<HMSData>();
        private RadObservableCollectionEx<HMSData> waveSWHMaxBuffer3h = new RadObservableCollectionEx<HMSData>();
        private RadObservableCollectionEx<HMSData> wavePeriodBuffer3h = new RadObservableCollectionEx<HMSData>();
        private RadObservableCollectionEx<HMSData> wavePeriodMaxBuffer3h = new RadObservableCollectionEx<HMSData>();

        // 20 minutters grafer
        public RadObservableCollectionEx<HMSData> waveSWH20mList = new RadObservableCollectionEx<HMSData>();
        public RadObservableCollectionEx<HMSData> waveSWHMax20mList = new RadObservableCollectionEx<HMSData>();
        public RadObservableCollectionEx<HMSData> wavePeriod20mList = new RadObservableCollectionEx<HMSData>();
        public RadObservableCollectionEx<HMSData> wavePeriodMax20mList = new RadObservableCollectionEx<HMSData>();

        // 3 timers grafer
        public RadObservableCollectionEx<HMSData> waveSWH3hList = new RadObservableCollectionEx<HMSData>();
        public RadObservableCollectionEx<HMSData> waveSWHMax3hList = new RadObservableCollectionEx<HMSData>();
        public RadObservableCollectionEx<HMSData> wavePeriod3hList = new RadObservableCollectionEx<HMSData>();
        public RadObservableCollectionEx<HMSData> wavePeriodMax3hList = new RadObservableCollectionEx<HMSData>();

        public void Init(AdminSettingsVM adminSettingsVM, Config config, SensorGroupStatus sensorStatus)
        {
            InitWaveHeightData();

            if (adminSettingsVM.enableEMS)
            {
                // Oppdatere UI
                UIUpdateTimer.Interval = TimeSpan.FromMilliseconds(config.ReadWithDefault(ConfigKey.ClientUpdateFrequencyUI, Constants.ClientUIUpdateFrequencyDefault));
                UIUpdateTimer.Tick += UIUpdate;
                UIUpdateTimer.Start();

                void UIUpdate(object sender, EventArgs e)
                {
                    // Sjekke om vi har data timeout
                    if (sensorStatus.TimeoutCheck(waveSWHData)) OnPropertyChanged(nameof(waveSWHString));
                    if (sensorStatus.TimeoutCheck(waveSWHMax20mData)) OnPropertyChanged(nameof(waveSWHMax20mString));
                    if (sensorStatus.TimeoutCheck(waveSWHMax3hData)) OnPropertyChanged(nameof(waveSWHMax3hString));

                    if (sensorStatus.TimeoutCheck(wavePeriodData)) OnPropertyChanged(nameof(wavePeriodString));
                    if (sensorStatus.TimeoutCheck(wavePeriodMax20mData)) OnPropertyChanged(nameof(wavePeriodMax20mString));
                    if (sensorStatus.TimeoutCheck(wavePeriodMax3hData)) OnPropertyChanged(nameof(wavePeriodMax3hString));

                    // Oppdatere data som skal ut i grafer
                    GraphBuffer.Update(waveSWHData, waveSWHBuffer20m);
                    GraphBuffer.Update(waveSWHData, waveSWHBuffer3h);

                    GraphBuffer.Update(waveSWHMax20mData, waveSWHMaxBuffer20m);
                    GraphBuffer.Update(waveSWHMax3hData, waveSWHMaxBuffer3h);

                    GraphBuffer.Update(wavePeriodData, wavePeriodBuffer20m);
                    GraphBuffer.Update(wavePeriodData, wavePeriodBuffer3h);

                    GraphBuffer.Update(wavePeriodMax20mData, wavePeriodMaxBuffer20m);
                    GraphBuffer.Update(wavePeriodMax3hData, wavePeriodMaxBuffer3h);
                }

                // Oppdatere trend data i UI: 20 minutter
                ChartDataUpdateTimer20m.Interval = TimeSpan.FromMilliseconds(config.ReadWithDefault(ConfigKey.ChartDataUpdateFrequency20m, Constants.ClientUIUpdateFrequencyDefault));
                ChartDataUpdateTimer20m.Tick += ChartDataUpdate20m;
                ChartDataUpdateTimer20m.Start();

                void ChartDataUpdate20m(object sender, EventArgs e)
                {
                    // Overføre data fra buffer til chart data: 20m
                    GraphBuffer.Transfer(waveSWHBuffer20m, waveSWH20mList);
                    GraphBuffer.Transfer(waveSWHMaxBuffer20m, waveSWHMax20mList);
                    GraphBuffer.Transfer(wavePeriodBuffer20m, wavePeriod20mList);
                    GraphBuffer.Transfer(wavePeriodMaxBuffer20m, wavePeriodMax20mList);

                    // Fjerne gamle data fra chart data
                    GraphBuffer.RemoveOldData(waveSWH20mList, Constants.Minutes20 + Constants.ChartTimeCorrMin);
                    GraphBuffer.RemoveOldData(waveSWHMax20mList, Constants.Minutes20 + Constants.ChartTimeCorrMin);
                    GraphBuffer.RemoveOldData(wavePeriod20mList, Constants.Minutes20 + Constants.ChartTimeCorrMin);
                    GraphBuffer.RemoveOldData(wavePeriodMax20mList, Constants.Minutes20 + Constants.ChartTimeCorrMin);

                    // Oppdatere alignment datetime (nåtid) til alle chart (20m og 3h)
                    alignmentTime = DateTime.UtcNow.AddSeconds(Constants.ChartTimeCorrMax);

                    // Oppdatere aksene og farget område på graf
                    OnPropertyChanged(nameof(waveSWHChartAxisMax20m));
                    OnPropertyChanged(nameof(wavePeriodChartAxisMax20m));
                }

                // Oppdatere trend data i UI: 3 hours
                ChartDataUpdateTimer3h.Interval = TimeSpan.FromMilliseconds(config.ReadWithDefault(ConfigKey.ChartDataUpdateFrequency3h, Constants.ChartUpdateFrequencyUI3hDefault));
                ChartDataUpdateTimer3h.Tick += ChartDataUpdate3h;
                ChartDataUpdateTimer3h.Start();

                void ChartDataUpdate3h(object sender, EventArgs e)
                {
                    // Overføre data fra buffer til chart data: 20m
                    GraphBuffer.Transfer(waveSWHBuffer3h, waveSWH3hList);
                    GraphBuffer.Transfer(waveSWHMaxBuffer3h, waveSWHMax3hList);
                    GraphBuffer.Transfer(wavePeriodBuffer3h, wavePeriod3hList);
                    GraphBuffer.Transfer(wavePeriodMaxBuffer3h, wavePeriodMax3hList);

                    // Fjerne gamle data fra chart data
                    GraphBuffer.RemoveOldData(waveSWH3hList, Constants.Hours3 + Constants.ChartTimeCorrMin);
                    GraphBuffer.RemoveOldData(waveSWHMax3hList, Constants.Hours3 + Constants.ChartTimeCorrMin);
                    GraphBuffer.RemoveOldData(wavePeriod3hList, Constants.Hours3 + Constants.ChartTimeCorrMin);
                    GraphBuffer.RemoveOldData(wavePeriodMax3hList, Constants.Hours3 + Constants.ChartTimeCorrMin);

                    // Oppdatere alignment datetime (nåtid) til alle chart (20m og 3h)
                    alignmentTime = DateTime.UtcNow.AddSeconds(Constants.ChartTimeCorrMax);

                    // Oppdatere aksene og farget område på graf
                    OnPropertyChanged(nameof(wavePeriodChartAxisMax3h));
                    OnPropertyChanged(nameof(waveSWHChartAxisMax3h));
                }
            }
        }

        public void UpdateData(HMSDataCollection hmsDataCollection)
        {
            // Significant Wave Height
            waveSWHData = hmsDataCollection.GetData(ValueType.SignificantWaveHeight);
            waveSWHMax20mData = hmsDataCollection.GetData(ValueType.SignificantWaveHeightMax20m);
            waveSWHMax3hData = hmsDataCollection.GetData(ValueType.SignificantWaveHeightMax3h);

            // Wave Period
            wavePeriodData = hmsDataCollection.GetData(ValueType.WavePeriod);
            wavePeriodMax20mData = hmsDataCollection.GetData(ValueType.WavePeriodMax20m);
            wavePeriodMax3hData = hmsDataCollection.GetData(ValueType.WavePeriodMax3h);
        }

        public void InitWaveHeightData()
        {
            _wavePeriodData = new HMSData();
            _wavePeriodMax20mData = new HMSData();
            _wavePeriodMax3hData = new HMSData();

            _waveSWHData = new HMSData();
            _waveSWHMax20mData = new HMSData();
            _waveSWHMax3hData = new HMSData();

            // Init av chart data
            for (int i = -Constants.Minutes20; i <= 0; i++)
            {
                waveSWH20mList.Add(new HMSData()
                {
                    data = 0,
                    timestamp = DateTime.UtcNow.AddSeconds(i)
                });

                waveSWHMax20mList.Add(new HMSData()
                {
                    data = 0,
                    timestamp = DateTime.UtcNow.AddSeconds(i)
                });

                wavePeriod20mList.Add(new HMSData()
                {
                    data = 0,
                    timestamp = DateTime.UtcNow.AddSeconds(i)
                });

                wavePeriodMax20mList.Add(new HMSData()
                {
                    data = 0,
                    timestamp = DateTime.UtcNow.AddSeconds(i)
                });
            }

            for (int i = -Constants.Hours3; i <= 0; i++)
            {
                waveSWH3hList.Add(new HMSData()
                {
                    data = 0,
                    timestamp = DateTime.UtcNow.AddSeconds(i)
                });

                waveSWHMax3hList.Add(new HMSData()
                {
                    data = 0,
                    timestamp = DateTime.UtcNow.AddSeconds(i)
                });

                wavePeriod3hList.Add(new HMSData()
                {
                    data = 0,
                    timestamp = DateTime.UtcNow.AddSeconds(i)
                });

                wavePeriodMax3hList.Add(new HMSData()
                {
                    data = 0,
                    timestamp = DateTime.UtcNow.AddSeconds(i)
                });
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Wave Period
        /////////////////////////////////////////////////////////////////////////////
        private HMSData _wavePeriodData { get; set; }
        public HMSData wavePeriodData
        {
            get
            {
                return _wavePeriodData;
            }
            set
            {
                if (value != null)
                {
                    OnPropertyChanged(nameof(wavePeriodString));
                    _wavePeriodData.Set(value);
                }
            }
        }
        public string wavePeriodString
        {
            get
            {
                if (wavePeriodData != null)
                {
                    // Sjekke om data er gyldig
                    if (wavePeriodData.status == DataStatus.OK)
                    {
                        return string.Format("{0} s", wavePeriodData.data.ToString("0.0"));
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

        private HMSData _wavePeriodMax20mData { get; set; }
        public HMSData wavePeriodMax20mData
        {
            get
            {
                return _wavePeriodMax20mData;
            }
            set
            {
                if (value != null)
                {
                    OnPropertyChanged(nameof(wavePeriodMax20mString));
                    _wavePeriodMax20mData.Set(value);
                }
            }
        }
        public string wavePeriodMax20mString
        {
            get
            {
                if (wavePeriodMax20mData != null)
                {
                    // Sjekke om data er gyldig
                    if (wavePeriodMax20mData.status == DataStatus.OK)
                    {
                        return string.Format("{0} s", wavePeriodMax20mData.data.ToString("0.0"));
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

        private HMSData _wavePeriodMax3hData { get; set; }
        public HMSData wavePeriodMax3hData
        {
            get
            {
                return _wavePeriodMax3hData;
            }
            set
            {
                if (value != null)
                {
                    OnPropertyChanged(nameof(wavePeriodMax3hString));

                    _wavePeriodMax3hData.Set(value);
                }
            }
        }
        public string wavePeriodMax3hString
        {
            get
            {
                if (wavePeriodMax3hData != null)
                {
                    // Sjekke om data er gyldig
                    if (wavePeriodMax3hData.status == DataStatus.OK)
                    {
                        return string.Format("{0} s", wavePeriodMax3hData.data.ToString("0.0"));
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
        // Significant Wave Height
        /////////////////////////////////////////////////////////////////////////////
        private HMSData _waveSWHData { get; set; }
        public HMSData waveSWHData
        {
            get
            {
                return _waveSWHData;
            }
            set
            {
                if (value != null)
                {
                    OnPropertyChanged(nameof(waveSWHString));

                    _waveSWHData.Set(value);
                }
            }
        }
        public string waveSWHString
        {
            get
            {
                if (waveSWHData != null)
                {
                    // Sjekke om data er gyldig
                    if (waveSWHData.status == DataStatus.OK)
                    {
                        return string.Format("{0} m", waveSWHData.data.ToString("0.0"));
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

        private HMSData _waveSWHMax20mData { get; set; }
        public HMSData waveSWHMax20mData
        {
            get
            {
                return _waveSWHMax20mData;
            }
            set
            {
                if (value != null)
                {
                    OnPropertyChanged(nameof(waveSWHMax20mString));

                    _waveSWHMax20mData.Set(value);
                }
            }
        }
        public string waveSWHMax20mString
        {
            get
            {
                if (waveSWHMax20mData != null)
                {
                    // Sjekke om data er gyldig
                    if (waveSWHMax20mData.status == DataStatus.OK)
                    {
                        return string.Format("{0} m", waveSWHMax20mData.data.ToString("0.0"));
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

        private HMSData _waveSWHMax3hData { get; set; }
        public HMSData waveSWHMax3hData
        {
            get
            {
                return _waveSWHMax3hData;
            }
            set
            {
                if (value != null)
                {
                    OnPropertyChanged(nameof(waveSWHMax3hString));

                    _waveSWHMax3hData.Set(value);
                }
            }
        }
        public string waveSWHMax3hString
        {
            get
            {
                if (waveSWHMax3hData != null)
                {
                    // Sjekke om data er gyldig
                    if (waveSWHMax3hData.status == DataStatus.OK)
                    {
                        return string.Format("{0} m", waveSWHMax3hData.data.ToString("0.0"));
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
        // Chart X axis alignment
        /////////////////////////////////////////////////////////////////////////////
        private DateTime _alignmentTime { get; set; }
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

                    OnPropertyChanged();
                    OnPropertyChanged(nameof(alignmentTimeMin20m));
                    OnPropertyChanged(nameof(alignmentTimeMax));
                }
            }
        }

        public DateTime alignmentTimeMin20m
        {
            get
            {
                return DateTime.UtcNow.AddSeconds(-Constants.Minutes20);
            }
        }

        public DateTime alignmentTimeMin3h
        {
            get
            {
                return DateTime.UtcNow.AddSeconds(-Constants.Hours3);
            }
        }

        public DateTime alignmentTimeMax
        {
            get
            {
                return alignmentTime;
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Chart Axis Min/Max
        /////////////////////////////////////////////////////////////////////////////
        public double wavePeriodChartAxisMax20m
        {
            get
            {
                return (int)wavePeriodMax20mData.data / Constants.WaveChartPeriodInc * Constants.WaveChartPeriodInc + Constants.WaveChartPeriodInc;
            }
        }

        public double wavePeriodChartAxisMax3h
        {
            get
            {
                return (int)wavePeriodMax3hData.data / Constants.WaveChartPeriodInc * Constants.WaveChartPeriodInc + Constants.WaveChartPeriodInc;
            }
        }

        public double waveSWHChartAxisMax20m
        {
            get
            {
                return (int)waveSWHMax20mData.data / Constants.WaveChartSWHInc * Constants.WaveChartSWHInc + Constants.WaveChartSWHInc;
            }
        }

        public double waveSWHChartAxisMax3h
        {
            get
            {
                return (int)waveSWHMax3hData.data / Constants.WaveChartSWHInc * Constants.WaveChartSWHInc + Constants.WaveChartSWHInc;
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Helideck Status Trend Time
        /////////////////////////////////////////////////////////////////////////////
        private string _landingStatusTimeString { get; set; }
        public string landingStatusTimeString
        {
            get
            {
                return _landingStatusTimeString;
            }
            set
            {
                _landingStatusTimeString = value;
                OnPropertyChanged();
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
