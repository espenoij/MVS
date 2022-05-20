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
        private RadObservableCollectionEx<HMSData> waveBuffer20m = new RadObservableCollectionEx<HMSData>();
        private RadObservableCollectionEx<HMSData> waveHeightBuffer20m = new RadObservableCollectionEx<HMSData>();
        private RadObservableCollectionEx<HMSData> waveMeanPeriodBuffer20m = new RadObservableCollectionEx<HMSData>();

        // 3 timers buffer
        private RadObservableCollectionEx<HMSData> waveBuffer3h = new RadObservableCollectionEx<HMSData>();
        private RadObservableCollectionEx<HMSData> waveHeightBuffer3h = new RadObservableCollectionEx<HMSData>();
        private RadObservableCollectionEx<HMSData> waveMeanPeriodBuffer3h = new RadObservableCollectionEx<HMSData>();

        // 20 minutters grafer
        public RadObservableCollectionEx<HMSData> wave20mList = new RadObservableCollectionEx<HMSData>();
        public RadObservableCollectionEx<HMSData> waveHeight20mList = new RadObservableCollectionEx<HMSData>();
        public RadObservableCollectionEx<HMSData> waveMeanPeriod20mList = new RadObservableCollectionEx<HMSData>();

        // 3 timers grafer
        public RadObservableCollectionEx<HMSData> wave3hList = new RadObservableCollectionEx<HMSData>();
        public RadObservableCollectionEx<HMSData> waveHeight3hList = new RadObservableCollectionEx<HMSData>();
        public RadObservableCollectionEx<HMSData> waveMeanPeriod3hList = new RadObservableCollectionEx<HMSData>();

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

                    // Wave Height
                    sensorStatus.TimeoutCheck(waveData);
                    sensorStatus.TimeoutCheck(waveMax20mData);
                    sensorStatus.TimeoutCheck(waveMax3hData);

                    sensorStatus.TimeoutCheck(waveHeightData);
                    if (sensorStatus.TimeoutCheck(waveHeightMax20mData)) OnPropertyChanged(nameof(waveHeightMax20mString));
                    if (sensorStatus.TimeoutCheck(waveHeightMax3hData)) OnPropertyChanged(nameof(waveHeightMax3hString));

                    sensorStatus.TimeoutCheck(waveMeanPeriodData);
                    if (sensorStatus.TimeoutCheck(waveMeanPeriodMax20mData)) OnPropertyChanged(nameof(waveMeanPeriodMax20mString));
                    if (sensorStatus.TimeoutCheck(waveMeanPeriodMax3hData)) OnPropertyChanged(nameof(waveMeanPeriodMax3hString));

                    // Oppdatere data som skal ut i grafer
                    GraphBuffer.Update(waveData, waveBuffer20m);
                    GraphBuffer.Update(waveData, waveBuffer3h);

                    GraphBuffer.Update(waveHeightData, waveHeightBuffer20m);
                    GraphBuffer.Update(waveHeightData, waveHeightBuffer3h);

                    GraphBuffer.Update(waveMeanPeriodData, waveMeanPeriodBuffer20m);
                    GraphBuffer.Update(waveMeanPeriodData, waveMeanPeriodBuffer3h);

                    // Oppdatere alignment datetime (nåtid) til alle chart (20m og 3h)
                    //alignmentTime = DateTime.UtcNow;
                }

                // Oppdatere trend data i UI: 20 minutter
                ChartDataUpdateTimer20m.Interval = TimeSpan.FromMilliseconds(config.ReadWithDefault(ConfigKey.ChartDataUpdateFrequency20m, Constants.ClientUIUpdateFrequencyDefault));
                ChartDataUpdateTimer20m.Tick += ChartDataUpdate20m;
                ChartDataUpdateTimer20m.Start();

                void ChartDataUpdate20m(object sender, EventArgs e)
                {
                    // Overføre data fra buffer til chart data: 20m
                    GraphBuffer.Transfer(waveBuffer20m, wave20mList);
                    GraphBuffer.Transfer(waveHeightBuffer20m, waveHeight20mList);
                    GraphBuffer.Transfer(waveMeanPeriodBuffer20m, waveMeanPeriod20mList);

                    // Fjerne gamle data fra chart data
                    GraphBuffer.RemoveOldData(wave20mList, Constants.Minutes20 + Constants.ChartTimeCorrMin);
                    GraphBuffer.RemoveOldData(waveHeight20mList, Constants.Minutes20 + Constants.ChartTimeCorrMin);
                    GraphBuffer.RemoveOldData(waveMeanPeriod20mList, Constants.Minutes20 + Constants.ChartTimeCorrMin);

                    // Oppdatere alignment datetime (nåtid) til alle chart (20m og 3h)
                    alignmentTime = DateTime.UtcNow.AddSeconds(Constants.ChartTimeCorrMax);

                    // Oppdatere aksene og farget område på graf
                    OnPropertyChanged(nameof(waveChartAxisMax20m));
                    OnPropertyChanged(nameof(waveChartAxisMin20m));
                    OnPropertyChanged(nameof(waveHeightChartAxisMax20m));
                    OnPropertyChanged(nameof(waveMeanPeriodChartAxisMax20m));
                }

                // Oppdatere trend data i UI: 3 hours
                ChartDataUpdateTimer3h.Interval = TimeSpan.FromMilliseconds(config.ReadWithDefault(ConfigKey.ChartDataUpdateFrequency3h, Constants.ChartUpdateFrequencyUI3hDefault));
                ChartDataUpdateTimer3h.Tick += ChartDataUpdate3h;
                ChartDataUpdateTimer3h.Start();

                void ChartDataUpdate3h(object sender, EventArgs e)
                {
                    // Overføre data fra buffer til chart data: 20m
                    GraphBuffer.Transfer(waveBuffer3h, wave3hList);
                    GraphBuffer.Transfer(waveHeightBuffer3h, waveHeight3hList);

                    // Fjerne gamle data fra chart data
                    GraphBuffer.RemoveOldData(wave3hList, Constants.Hours3 + Constants.ChartTimeCorrMin);
                    GraphBuffer.RemoveOldData(waveHeight3hList, Constants.Hours3 + Constants.ChartTimeCorrMin);

                    // Oppdatere alignment datetime (nåtid) til alle chart (20m og 3h)
                    //alignmentTime = DateTime.UtcNow.AddSeconds(Constants.ChartTimeCorrMax);

                    // Oppdatere aksene og farget område på graf
                    OnPropertyChanged(nameof(waveChartAxisMax3h));
                    OnPropertyChanged(nameof(waveChartAxisMin3h));
                    OnPropertyChanged(nameof(waveHeightChartAxisMax3h));
                }
            }
        }

        public void UpdateData(HMSDataCollection hmsDataCollection)
        {
            // Wave
            waveData = hmsDataCollection.GetData(ValueType.Wave);
            waveMax20mData = hmsDataCollection.GetData(ValueType.WaveMax20m);
            waveMax3hData = hmsDataCollection.GetData(ValueType.WaveMax3h);

            waveHeightData = hmsDataCollection.GetData(ValueType.WaveHeight);
            waveHeightMax20mData = hmsDataCollection.GetData(ValueType.WaveHeightMax20m);
            waveHeightMax3hData = hmsDataCollection.GetData(ValueType.WaveHeightMax3h);

            waveMeanPeriodData = hmsDataCollection.GetData(ValueType.WaveHeight);
            waveMeanPeriodMax20mData = hmsDataCollection.GetData(ValueType.WaveHeightMax20m);
            waveMeanPeriodMax3hData = hmsDataCollection.GetData(ValueType.WaveHeightMax3h);
        }

        public void InitWaveHeightData()
        {
            _waveData = new HMSData();
            _waveMax20mData = new HMSData();
            _waveMax3hData = new HMSData();

            _waveHeightData = new HMSData();
            _waveHeightMax20mData = new HMSData();
            _waveHeightMax3hData = new HMSData();

            _waveMeanPeriodData = new HMSData();
            _waveMeanPeriodMax20mData = new HMSData();
            _waveMeanPeriodMax3hData = new HMSData();

            // Init av chart data
            for (int i = -Constants.Minutes20; i <= 0; i++)
            {
                wave20mList.Add(new HMSData()
                {
                    data = 0,
                    timestamp = DateTime.UtcNow.AddSeconds(i)
                });

                waveHeight20mList.Add(new HMSData()
                {
                    data = 0,
                    timestamp = DateTime.UtcNow.AddSeconds(i)
                });

                waveMeanPeriod20mList.Add(new HMSData()
                {
                    data = 0,
                    timestamp = DateTime.UtcNow.AddSeconds(i)
                });
            }

            for (int i = -Constants.Hours3; i <= 0; i++)
            {
                wave3hList.Add(new HMSData()
                {
                    data = 0,
                    timestamp = DateTime.UtcNow.AddSeconds(i)
                });

                waveHeight3hList.Add(new HMSData()
                {
                    data = 0,
                    timestamp = DateTime.UtcNow.AddSeconds(i)
                });

                waveMeanPeriod3hList.Add(new HMSData()
                {
                    data = 0,
                    timestamp = DateTime.UtcNow.AddSeconds(i)
                });
            }
        }


        private HMSData _waveData { get; set; }
        public HMSData waveData
        {
            get
            {
                return _waveData;
            }
            set
            {
                if (value != null)
                {
                    _waveData.Set(value);
                }
            }
        }

        private HMSData _waveMax20mData { get; set; }
        public HMSData waveMax20mData
        {
            get
            {
                return _waveMax20mData;
            }
            set
            {
                if (value != null)
                {
                    _waveMax20mData.Set(value);
                }
            }
        }

        private HMSData _waveMax3hData { get; set; }
        public HMSData waveMax3hData
        {
            get
            {
                return _waveMax3hData;
            }
            set
            {
                if (value != null)
                {
                    _waveMax3hData.Set(value);
                }
            }
        }

        private HMSData _waveHeightData { get; set; }
        public HMSData waveHeightData
        {
            get
            {
                return _waveHeightData;
            }
            set
            {
                if (value != null)
                {
                    _waveHeightData.Set(value);
                }
            }
        }

        private HMSData _waveHeightMax20mData { get; set; }
        public HMSData waveHeightMax20mData
        {
            get
            {
                return _waveHeightMax20mData;
            }
            set
            {
                if (value != null)
                {
                    OnPropertyChanged(nameof(waveHeightMax20mString));

                    _waveHeightMax20mData.Set(value);
                }
            }
        }
        public string waveHeightMax20mString
        {
            get
            {
                if (waveHeightMax20mData != null)
                {
                    // Sjekke om data er gyldig
                    if (waveHeightMax20mData.status == DataStatus.OK)
                    {
                        return string.Format("{0} °", waveHeightMax20mData.data.ToString("0.0"));
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

        private HMSData _waveHeightMax3hData { get; set; }
        public HMSData waveHeightMax3hData
        {
            get
            {
                return _waveHeightMax3hData;
            }
            set
            {
                if (value != null)
                {
                    OnPropertyChanged(nameof(waveHeightMax3hString));

                    _waveHeightMax3hData.Set(value);
                }
            }
        }
        public string waveHeightMax3hString
        {
            get
            {
                if (waveHeightMax3hData != null)
                {
                    // Sjekke om data er gyldig
                    if (waveHeightMax3hData.status == DataStatus.OK)
                    {
                        return string.Format("{0} °", waveHeightMax3hData.data.ToString("0.0"));
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

        private HMSData _waveMeanPeriodData { get; set; }
        public HMSData waveMeanPeriodData
        {
            get
            {
                return _waveMeanPeriodData;
            }
            set
            {
                if (value != null)
                {
                    _waveMeanPeriodData.Set(value);
                }
            }
        }

        private HMSData _waveMeanPeriodMax20mData { get; set; }
        public HMSData waveMeanPeriodMax20mData
        {
            get
            {
                return _waveMeanPeriodMax20mData;
            }
            set
            {
                if (value != null)
                {
                    OnPropertyChanged(nameof(waveMeanPeriodMax20mString));

                    _waveMeanPeriodMax20mData.Set(value);
                }
            }
        }
        public string waveMeanPeriodMax20mString
        {
            get
            {
                if (waveMeanPeriodMax20mData != null)
                {
                    // Sjekke om data er gyldig
                    if (waveMeanPeriodMax20mData.status == DataStatus.OK)
                    {
                        return string.Format("{0} °", waveMeanPeriodMax20mData.data.ToString("0.0"));
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

        private HMSData _waveMeanPeriodMax3hData { get; set; }
        public HMSData waveMeanPeriodMax3hData
        {
            get
            {
                return _waveMeanPeriodMax3hData;
            }
            set
            {
                if (value != null)
                {
                    OnPropertyChanged(nameof(waveMeanPeriodMax3hString));

                    _waveMeanPeriodMax3hData.Set(value);
                }
            }
        }
        public string waveMeanPeriodMax3hString
        {
            get
            {
                if (waveMeanPeriodMax3hData != null)
                {
                    // Sjekke om data er gyldig
                    if (waveMeanPeriodMax3hData.status == DataStatus.OK)
                    {
                        return string.Format("{0} °", waveMeanPeriodMax3hData.data.ToString("0.0"));
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
        public double waveChartAxisMax20m
        {
            get
            {
                return (int)waveMax20mData.data / Constants.WaveChartIncrements * Constants.WaveChartIncrements + Constants.WaveChartIncrements;
            }
        }

        public double waveChartAxisMax3h
        {
            get
            {
                return (int)waveMax3hData.data / Constants.WaveChartIncrements * Constants.WaveChartIncrements + Constants.WaveChartIncrements;
            }
        }

        public double waveChartAxisMin20m
        {
            get
            {
                return ((int)waveMax20mData.data / Constants.WaveChartIncrements * Constants.WaveChartIncrements + Constants.WaveChartIncrements) * -1;
            }
        }

        public double waveChartAxisMin3h
        {
            get
            {
                return ((int)waveMax3hData.data / Constants.WaveChartIncrements * Constants.WaveChartIncrements + Constants.WaveChartIncrements) * -1;
            }
        }

        public double waveHeightChartAxisMax20m
        {
            get
            {
                return (int)waveHeightMax20mData.data / Constants.WaveChartIncrements * Constants.WaveChartIncrements + Constants.WaveChartIncrements;
            }
        }

        public double waveHeightChartAxisMax3h
        {
            get
            {
                return (int)waveHeightMax3hData.data / Constants.WaveChartIncrements * Constants.WaveChartIncrements + Constants.WaveChartIncrements;
            }
        }

        public double waveMeanPeriodChartAxisMax20m
        {
            get
            {
                return (int)waveMeanPeriodMax20mData.data / Constants.WaveChartIncrements * Constants.WaveChartIncrements + Constants.WaveChartIncrements;
            }
        }

        public double waveMeanPeriodChartAxisMax3h
        {
            get
            {
                return (int)waveMeanPeriodMax3hData.data / Constants.WaveChartIncrements * Constants.WaveChartIncrements + Constants.WaveChartIncrements;
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
