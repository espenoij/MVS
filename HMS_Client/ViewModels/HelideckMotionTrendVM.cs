using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Threading;

namespace HMS_Client
{
    public class HelideckMotionTrendVM : INotifyPropertyChanged
    {
        // Change notification
        public event PropertyChangedEventHandler PropertyChanged;

        private DispatcherTimer UIUpdateTimer = new DispatcherTimer();
        private DispatcherTimer ChartDataUpdateTimer20m = new DispatcherTimer();
        private DispatcherTimer ChartDataUpdateTimer3h = new DispatcherTimer();

        // 20 minutters buffer
        private RadObservableCollectionEx<HMSData> pitchBuffer20m = new RadObservableCollectionEx<HMSData>();
        private RadObservableCollectionEx<HMSData> rollBuffer20m = new RadObservableCollectionEx<HMSData>();      
        private RadObservableCollectionEx<HMSData> inclinationBuffer20m = new RadObservableCollectionEx<HMSData>();
        private RadObservableCollectionEx<HMSData> heaveAmplitudeBuffer20m = new RadObservableCollectionEx<HMSData>();
        private RadObservableCollectionEx<HMSData> significantHeaveRateBuffer20m = new RadObservableCollectionEx<HMSData>();

        // 3 timers buffer
        private RadObservableCollectionEx<HMSData> pitchBuffer3h = new RadObservableCollectionEx<HMSData>();
        private RadObservableCollectionEx<HMSData> rollBuffer3h = new RadObservableCollectionEx<HMSData>();
        private RadObservableCollectionEx<HMSData> inclinationBuffer3h = new RadObservableCollectionEx<HMSData>();
        private RadObservableCollectionEx<HMSData> heaveAmplitudeBuffer3h = new RadObservableCollectionEx<HMSData>();
        private RadObservableCollectionEx<HMSData> significantHeaveRateBuffer3h = new RadObservableCollectionEx<HMSData>();

        // 20 minutters grafer
        public RadObservableCollectionEx<HMSData> pitch20mList = new RadObservableCollectionEx<HMSData>();
        public RadObservableCollectionEx<HMSData> pitchMaxUp20mList = new RadObservableCollectionEx<HMSData>();
        public RadObservableCollectionEx<HMSData> pitchMaxDown20mList = new RadObservableCollectionEx<HMSData>();

        public RadObservableCollectionEx<HMSData> roll20mList = new RadObservableCollectionEx<HMSData>();
        public RadObservableCollectionEx<HMSData> rollMaxLeft20mList = new RadObservableCollectionEx<HMSData>();
        public RadObservableCollectionEx<HMSData> rollMaxRight20mList = new RadObservableCollectionEx<HMSData>();

        public RadObservableCollectionEx<HMSData> inclinationData20mList = new RadObservableCollectionEx<HMSData>();
        public RadObservableCollectionEx<HMSData> heaveAmplitudeData20mList = new RadObservableCollectionEx<HMSData>();
        public RadObservableCollectionEx<HMSData> significantHeaveRateData20mList = new RadObservableCollectionEx<HMSData>();

        // 3 timers grafer
        public RadObservableCollectionEx<HMSData> pitch3hList = new RadObservableCollectionEx<HMSData>();
        public RadObservableCollectionEx<HMSData> pitchMaxUp3hList = new RadObservableCollectionEx<HMSData>();
        public RadObservableCollectionEx<HMSData> pitchMaxDown3hList = new RadObservableCollectionEx<HMSData>();

        public RadObservableCollectionEx<HMSData> rollData3hList = new RadObservableCollectionEx<HMSData>();
        public RadObservableCollectionEx<HMSData> rollMaxLeft3hList = new RadObservableCollectionEx<HMSData>();
        public RadObservableCollectionEx<HMSData> rollMaxRight3hList = new RadObservableCollectionEx<HMSData>();

        public RadObservableCollectionEx<HMSData> inclinationData3hList = new RadObservableCollectionEx<HMSData>();
        public RadObservableCollectionEx<HMSData> heaveAmplitudeData3hList = new RadObservableCollectionEx<HMSData>();
        public RadObservableCollectionEx<HMSData> significantHeaveRateData3hList = new RadObservableCollectionEx<HMSData>();

        // Motion Limits
        /////////////////////////////////////////////////////////////////////////////
        private HMSData motionLimitPitchRoll = new HMSData();
        private HMSData motionLimitInclination = new HMSData();
        private HMSData motionLimitHeaveAmplitude = new HMSData();
        private HMSData motionLimitSignificantHeaveRate = new HMSData();

        // Helideck Motion History
        public List<HelideckStatusType> landingTrend20mDispList = new List<HelideckStatusType>();
        public List<HelideckStatusType> statusTrend3hDispList = new List<HelideckStatusType>();


        private AdminSettingsVM adminSettingsVM;

        public void Init(AdminSettingsVM adminSettingsVM, Config config, SensorGroupStatus sensorStatus)
        {
            this.adminSettingsVM = adminSettingsVM;

            //selectedGraphTime = GraphTime.Minutes20;

            InitPitchData();
            InitRollData();
            InitInclinationData();
            InitHeaveAmplitudeData();
            InitSignificantHeaveData();

            // Init UI
            for (int i = 0; i < Constants.landingTrendHistoryDisplayListMax; i++)
            {
                landingTrend20mDispList.Add(new HelideckStatusType());
                statusTrend3hDispList.Add(new HelideckStatusType());
            }

            // Oppdatere UI
            UIUpdateTimer.Interval = TimeSpan.FromMilliseconds(config.ReadWithDefault(ConfigKey.ClientUpdateFrequencyUI, Constants.ClientUIUpdateFrequencyDefault));
            UIUpdateTimer.Tick += UIUpdate;
            UIUpdateTimer.Start();

            void UIUpdate(object sender, EventArgs e)
            {
                // Sjekke om vi har data timeout

                // Pitch
                sensorStatus.TimeoutCheck(pitchData);
                if (sensorStatus.TimeoutCheck(pitchMax20mData)) OnPropertyChanged(nameof(pitchMax20mString));
                if (sensorStatus.TimeoutCheck(pitchMaxUp20mData))
                {
                    OnPropertyChanged(nameof(pitchMaxUp20mString));
                    OnPropertyChanged(nameof(pitchMaxUp20mString2));
                }
                if (sensorStatus.TimeoutCheck(pitchMaxDown20mData))
                {
                    OnPropertyChanged(nameof(pitchMaxDown20mString));
                    OnPropertyChanged(nameof(pitchMaxDown20mString2));
                }
                sensorStatus.TimeoutCheck(pitchMax3hData);

                // Roll
                sensorStatus.TimeoutCheck(rollData);
                if (sensorStatus.TimeoutCheck(rollMax20mData)) OnPropertyChanged(nameof(rollMax20mString));
                if (sensorStatus.TimeoutCheck(rollMaxLeft20mData))
                {
                    OnPropertyChanged(nameof(rollMaxLeft20mString));
                    OnPropertyChanged(nameof(rollMaxLeft20mString2));
                }
                if (sensorStatus.TimeoutCheck(rollMaxRight20mData))
                {
                    OnPropertyChanged(nameof(rollMaxRight20mString));
                    OnPropertyChanged(nameof(rollMaxRight20mString2));
                }
                sensorStatus.TimeoutCheck(rollMax3hData);

                // Inclination
                sensorStatus.TimeoutCheck(inclinationData);
                if (sensorStatus.TimeoutCheck(inclinationMax20mData)) OnPropertyChanged(nameof(inclinationMax20mString));
                sensorStatus.TimeoutCheck(inclinationMax3hData);

                // Heave Amplitude
                sensorStatus.TimeoutCheck(heaveAmplitudeData);
                if (sensorStatus.TimeoutCheck(heaveAmplitudeMax20mData)) OnPropertyChanged(nameof(heaveAmplitudeMax20mString));
                sensorStatus.TimeoutCheck(heaveAmplitudeMax3hData);

                // Significant Heave Rate
                if (sensorStatus.TimeoutCheck(significantHeaveRateData)) OnPropertyChanged(nameof(significantHeaveRateString));
                sensorStatus.TimeoutCheck(significantHeaveRateMax20mData);
                sensorStatus.TimeoutCheck(significantHeaveRateMax3hData);

                // Limits
                sensorStatus.TimeoutCheck(motionLimitPitchRoll);
                sensorStatus.TimeoutCheck(motionLimitInclination);
                sensorStatus.TimeoutCheck(motionLimitHeaveAmplitude);
                sensorStatus.TimeoutCheck(motionLimitSignificantHeaveRate);

                if (adminSettingsVM.regulationStandard == RegulationStandard.NOROG)
                {
                    // Oppdatere data som skal ut i grafer
                    GraphBuffer.Update(pitchData, pitchBuffer20m);
                    GraphBuffer.Update(rollData, rollBuffer20m);
                    GraphBuffer.Update(inclinationData, inclinationBuffer20m);
                    GraphBuffer.Update(heaveAmplitudeData, heaveAmplitudeBuffer20m);
                    GraphBuffer.Update(significantHeaveRateData, significantHeaveRateBuffer20m);

                    GraphBuffer.Update(pitchData, pitchBuffer3h);
                    GraphBuffer.Update(rollData, rollBuffer3h);
                    GraphBuffer.Update(inclinationData, inclinationBuffer3h);
                    GraphBuffer.Update(heaveAmplitudeData, heaveAmplitudeBuffer3h);
                    GraphBuffer.Update(significantHeaveRateData, significantHeaveRateBuffer3h);
                }
                else
                {
                    // Oppdatere data som skal ut i grafer
                    GraphBuffer.UpdateWithCull(pitchMax20mData, pitch20mList, Constants.GraphCullFrequency20m);
                    GraphBuffer.UpdateWithCull(pitchMaxUp20mData, pitchMaxUp20mList, Constants.GraphCullFrequency20m);
                    GraphBuffer.UpdateWithCull(pitchMaxDown20mData, pitchMaxDown20mList, Constants.GraphCullFrequency20m);

                    GraphBuffer.UpdateWithCull(rollMax20mData, roll20mList, Constants.GraphCullFrequency20m);
                    GraphBuffer.UpdateWithCull(rollMaxLeft20mData, rollMaxLeft20mList, Constants.GraphCullFrequency20m);
                    GraphBuffer.UpdateWithCull(rollMaxRight20mData, rollMaxRight20mList, Constants.GraphCullFrequency20m);

                    GraphBuffer.UpdateWithCull(inclinationMax20mData, inclinationData20mList, Constants.GraphCullFrequency20m);
                    GraphBuffer.UpdateWithCull(significantHeaveRateData, significantHeaveRateData20mList, Constants.GraphCullFrequency20m);

                    GraphBuffer.UpdateWithCull(pitchMax3hData, pitch3hList, Constants.GraphCullFrequency3h);
                    GraphBuffer.UpdateWithCull(pitchMaxUp20mData, pitchMaxUp3hList, Constants.GraphCullFrequency3h);
                    GraphBuffer.UpdateWithCull(pitchMaxDown20mData, pitchMaxDown3hList, Constants.GraphCullFrequency3h);

                    GraphBuffer.UpdateWithCull(rollMax3hData, rollData3hList, Constants.GraphCullFrequency3h);
                    GraphBuffer.UpdateWithCull(rollMaxLeft20mData, rollMaxLeft3hList, Constants.GraphCullFrequency3h);
                    GraphBuffer.UpdateWithCull(rollMaxRight20mData, rollMaxRight3hList, Constants.GraphCullFrequency3h);

                    GraphBuffer.UpdateWithCull(inclinationMax20mData, inclinationData3hList, Constants.GraphCullFrequency3h);
                    GraphBuffer.UpdateWithCull(significantHeaveRateData, significantHeaveRateData3hList, Constants.GraphCullFrequency3h);

                    // Fjerne gamle data fra chart data
                    GraphBuffer.RemoveOldData(pitch20mList, Constants.Minutes20 + Constants.ChartTimeCorrMin);
                    GraphBuffer.RemoveOldData(pitchMaxUp20mList, Constants.Minutes20 + Constants.ChartTimeCorrMin);
                    GraphBuffer.RemoveOldData(pitchMaxDown20mList, Constants.Minutes20 + Constants.ChartTimeCorrMin);
                    
                    GraphBuffer.RemoveOldData(roll20mList, Constants.Minutes20 + Constants.ChartTimeCorrMin);
                    GraphBuffer.RemoveOldData(rollMaxLeft20mList, Constants.Minutes20 + Constants.ChartTimeCorrMin);
                    GraphBuffer.RemoveOldData(rollMaxRight20mList, Constants.Minutes20 + Constants.ChartTimeCorrMin);

                    GraphBuffer.RemoveOldData(inclinationData20mList, Constants.Minutes20 + Constants.ChartTimeCorrMin);
                    GraphBuffer.RemoveOldData(significantHeaveRateData20mList, Constants.Minutes20 + Constants.ChartTimeCorrMin);

                    GraphBuffer.RemoveOldData(pitch3hList, Constants.Hours3 + Constants.ChartTimeCorrMin);
                    GraphBuffer.RemoveOldData(pitchMaxUp3hList, Constants.Hours3 + Constants.ChartTimeCorrMin);
                    GraphBuffer.RemoveOldData(pitchMaxDown3hList, Constants.Hours3 + Constants.ChartTimeCorrMin);

                    GraphBuffer.RemoveOldData(rollData3hList, Constants.Hours3 + Constants.ChartTimeCorrMin);
                    GraphBuffer.RemoveOldData(inclinationData3hList, Constants.Hours3 + Constants.ChartTimeCorrMin);
                    GraphBuffer.RemoveOldData(significantHeaveRateData3hList, Constants.Hours3 + Constants.ChartTimeCorrMin);

                    // Oppdatere alignment datetime (nåtid) til alle chart (20m og 3h)
                    alignmentTime = DateTime.UtcNow;
                }
            }

            // Oppdatere trend data i UI: 20 minutter
            ChartDataUpdateTimer20m.Interval = TimeSpan.FromMilliseconds(config.ReadWithDefault(ConfigKey.ChartDataUpdateFrequency20m, Constants.ClientUIUpdateFrequencyDefault));
            ChartDataUpdateTimer20m.Tick += ChartDataUpdate20m;
            ChartDataUpdateTimer20m.Start();

            void ChartDataUpdate20m(object sender, EventArgs e)
            {
                if (adminSettingsVM.regulationStandard == RegulationStandard.NOROG)
                {
                    // Overføre data fra buffer til chart data: 20m
                    GraphBuffer.Transfer(pitchBuffer20m, pitch20mList);
                    GraphBuffer.Transfer(rollBuffer20m, roll20mList);
                    GraphBuffer.Transfer(inclinationBuffer20m, inclinationData20mList);
                    GraphBuffer.Transfer(heaveAmplitudeBuffer20m, heaveAmplitudeData20mList);
                    GraphBuffer.Transfer(significantHeaveRateBuffer20m, significantHeaveRateData20mList);

                    // Fjerne gamle data fra chart data
                    GraphBuffer.RemoveOldData(pitch20mList, Constants.Minutes20 + Constants.ChartTimeCorrMin);
                    GraphBuffer.RemoveOldData(roll20mList, Constants.Minutes20 + Constants.ChartTimeCorrMin);
                    GraphBuffer.RemoveOldData(inclinationData20mList, Constants.Minutes20 + Constants.ChartTimeCorrMin);
                    GraphBuffer.RemoveOldData(heaveAmplitudeData20mList, Constants.Minutes20 + Constants.ChartTimeCorrMin);
                    GraphBuffer.RemoveOldData(significantHeaveRateData20mList, Constants.Minutes20 + Constants.ChartTimeCorrMin);

                    // Oppdatere alignment datetime (nåtid) til alle chart (20m og 3h)
                    alignmentTime = DateTime.UtcNow.AddSeconds(Constants.ChartTimeCorrMax);
                }

                // Oppdatere aksene og farget område på graf
                OnPropertyChanged(nameof(pitchChartAxisMax20m));
                OnPropertyChanged(nameof(pitchChartAxisMin20m));
                OnPropertyChanged(nameof(pitchChartAnnotationGreenMax20m));
                OnPropertyChanged(nameof(pitchChartAnnotationGreenMin20m));
                OnPropertyChanged(nameof(pitchChartAnnotationRedTopMax20m));
                OnPropertyChanged(nameof(pitchChartAnnotationRedTopMin20m));
                OnPropertyChanged(nameof(pitchChartAnnotationRedBottomMax20m));
                OnPropertyChanged(nameof(pitchChartAnnotationRedBottomMin20m));

                OnPropertyChanged(nameof(rollChartAxisMax20m));
                OnPropertyChanged(nameof(rollChartAxisMin20m));
                OnPropertyChanged(nameof(rollChartAnnotationGreenMax20m));
                OnPropertyChanged(nameof(rollChartAnnotationGreenMin20m));
                OnPropertyChanged(nameof(rollChartAnnotationRedTopMax20m));
                OnPropertyChanged(nameof(rollChartAnnotationRedTopMin20m));
                OnPropertyChanged(nameof(rollChartAnnotationRedBottomMax20m));
                OnPropertyChanged(nameof(rollChartAnnotationRedBottomMin20m));

                OnPropertyChanged(nameof(inclinationChartAxisMax20m));
                OnPropertyChanged(nameof(inclinationChartAnnotationGreenMax20m));
                OnPropertyChanged(nameof(inclinationChartAnnotationRedMax20m));
                OnPropertyChanged(nameof(inclinationChartAnnotationRedMin20m));

                OnPropertyChanged(nameof(heaveAmplitudeChartAxisMax20m));
                OnPropertyChanged(nameof(heaveAmplitudeChartAnnotationGreenMax20m));
                OnPropertyChanged(nameof(heaveAmplitudeChartAnnotationRedMax20m));
                OnPropertyChanged(nameof(heaveAmplitudeChartAnnotationRedMin20m));

                OnPropertyChanged(nameof(significantHeaveRateChartAxisMax20m));
                OnPropertyChanged(nameof(significantHeaveRateChartAnnotationGreenMax20m));
                OnPropertyChanged(nameof(significantHeaveRateChartAnnotationRedMax20m));
                OnPropertyChanged(nameof(significantHeaveRateChartAnnotationRedMin20m));

                OnPropertyChanged(nameof(pitchRollLimitString));
                OnPropertyChanged(nameof(inclinationLimitString));
                OnPropertyChanged(nameof(significantHeaveRateLimitString));
            }

            // Oppdatere trend data i UI: 3 hours
            ChartDataUpdateTimer3h.Interval = TimeSpan.FromMilliseconds(config.ReadWithDefault(ConfigKey.ChartDataUpdateFrequency3h, Constants.ChartUpdateFrequencyUI3hDefault));
            ChartDataUpdateTimer3h.Tick += ChartDataUpdate3h;
            ChartDataUpdateTimer3h.Start();

            void ChartDataUpdate3h(object sender, EventArgs e)
            {
                if (adminSettingsVM.regulationStandard == RegulationStandard.NOROG)
                {
                    // Overføre data fra buffer til chart data: 20m
                    GraphBuffer.Transfer(pitchBuffer3h, pitch3hList);
                    GraphBuffer.Transfer(rollBuffer3h, rollData3hList);
                    GraphBuffer.Transfer(inclinationBuffer3h, inclinationData3hList);
                    GraphBuffer.Transfer(heaveAmplitudeBuffer3h, heaveAmplitudeData3hList);
                    GraphBuffer.Transfer(significantHeaveRateBuffer3h, significantHeaveRateData3hList);

                    // Fjerne gamle data fra chart data
                    GraphBuffer.RemoveOldData(pitch3hList, Constants.Hours3 + Constants.ChartTimeCorrMin);
                    GraphBuffer.RemoveOldData(rollData3hList, Constants.Hours3 + Constants.ChartTimeCorrMin);
                    GraphBuffer.RemoveOldData(inclinationData3hList, Constants.Hours3 + Constants.ChartTimeCorrMin);
                    GraphBuffer.RemoveOldData(heaveAmplitudeData3hList, Constants.Hours3 + Constants.ChartTimeCorrMin);
                    GraphBuffer.RemoveOldData(significantHeaveRateData3hList, Constants.Hours3 + Constants.ChartTimeCorrMin);

                    // Oppdatere alignment datetime (nåtid) til alle chart (20m og 3h)
                    alignmentTime = DateTime.UtcNow.AddSeconds(Constants.ChartTimeCorrMax);
                }

                // Oppdatere aksene og farget område på graf
                OnPropertyChanged(nameof(pitchChartAxisMax3h));
                OnPropertyChanged(nameof(pitchChartAxisMin3h));
                OnPropertyChanged(nameof(pitchChartAnnotationGreenMax3h));
                OnPropertyChanged(nameof(pitchChartAnnotationGreenMin3h));
                OnPropertyChanged(nameof(pitchChartAnnotationRedTopMax3h));
                OnPropertyChanged(nameof(pitchChartAnnotationRedTopMin3h));
                OnPropertyChanged(nameof(pitchChartAnnotationRedBottomMax3h));
                OnPropertyChanged(nameof(pitchChartAnnotationRedBottomMin3h));

                OnPropertyChanged(nameof(rollChartAxisMax3h));
                OnPropertyChanged(nameof(rollChartAxisMin3h));
                OnPropertyChanged(nameof(rollChartAnnotationGreenMax3h));
                OnPropertyChanged(nameof(rollChartAnnotationGreenMin3h));
                OnPropertyChanged(nameof(rollChartAnnotationRedTopMax3h));
                OnPropertyChanged(nameof(rollChartAnnotationRedTopMin3h));
                OnPropertyChanged(nameof(rollChartAnnotationRedBottomMax3h));
                OnPropertyChanged(nameof(rollChartAnnotationRedBottomMin3h));

                OnPropertyChanged(nameof(inclinationChartAxisMax3h));
                OnPropertyChanged(nameof(inclinationChartAnnotationGreenMax3h));
                OnPropertyChanged(nameof(inclinationChartAnnotationRedMax3h));
                OnPropertyChanged(nameof(inclinationChartAnnotationRedMin3h));

                OnPropertyChanged(nameof(heaveAmplitudeChartAxisMax3h));
                OnPropertyChanged(nameof(heaveAmplitudeChartAnnotationGreenMax3h));
                OnPropertyChanged(nameof(heaveAmplitudeChartAnnotationRedMax3h));
                OnPropertyChanged(nameof(heaveAmplitudeChartAnnotationRedMin3h));

                OnPropertyChanged(nameof(significantHeaveRateChartAxisMax3h));
                OnPropertyChanged(nameof(significantHeaveRateChartAnnotationGreenMax3h));
                OnPropertyChanged(nameof(significantHeaveRateChartAnnotationRedMax3h));
                OnPropertyChanged(nameof(significantHeaveRateChartAnnotationRedMin3h));
            }
        }

        public void UpdateData(HMSDataCollection hmsDataCollection)
        {
            // Pitch
            pitchData = hmsDataCollection.GetData(ValueType.Pitch);
            pitchMax20mData = hmsDataCollection.GetData(ValueType.PitchMax20m);
            pitchMaxUp20mData = hmsDataCollection.GetData(ValueType.PitchMaxUp20m);
            pitchMaxDown20mData = hmsDataCollection.GetData(ValueType.PitchMaxDown20m);
            pitchMax3hData = hmsDataCollection.GetData(ValueType.PitchMax3h);

            // Roll
            rollData = hmsDataCollection.GetData(ValueType.Roll);
            rollMax20mData = hmsDataCollection.GetData(ValueType.RollMax20m);
            rollMaxLeft20mData = hmsDataCollection.GetData(ValueType.RollMaxLeft20m);
            rollMaxRight20mData = hmsDataCollection.GetData(ValueType.RollMaxRight20m);
            rollMax3hData = hmsDataCollection.GetData(ValueType.RollMax3h);

            // Inclination
            inclinationData = hmsDataCollection.GetData(ValueType.Inclination);
            inclinationMax20mData = hmsDataCollection.GetData(ValueType.InclinationMax20m);
            inclinationMax3hData = hmsDataCollection.GetData(ValueType.InclinationMax3h);

            // Heave
            heaveAmplitudeData = hmsDataCollection.GetData(ValueType.HeaveAmplitude);
            heaveAmplitudeMax20mData = hmsDataCollection.GetData(ValueType.HeaveAmplitudeMax20m);
            heaveAmplitudeMax3hData = hmsDataCollection.GetData(ValueType.HeaveAmplitudeMax3h);
            significantHeaveRateData = hmsDataCollection.GetData(ValueType.SignificantHeaveRate);
            significantHeaveRateMax20mData = hmsDataCollection.GetData(ValueType.SignificantHeaveRateMax20m);
            significantHeaveRateMax3hData = hmsDataCollection.GetData(ValueType.SignificantHeaveRateMax3h);

            // Motion Limits
            motionLimitPitchRoll = hmsDataCollection.GetData(ValueType.MotionLimitPitchRoll);
            motionLimitInclination = hmsDataCollection.GetData(ValueType.MotionLimitInclination);
            motionLimitHeaveAmplitude = hmsDataCollection.GetData(ValueType.MotionLimitHeaveAmplitude);
            motionLimitSignificantHeaveRate = hmsDataCollection.GetData(ValueType.MotionLimitSignificantHeaveRate);
        }

        private double GetMotionLimit(ValueType motionDataType)
        {
            switch (motionDataType)
            {
                case ValueType.MotionLimitPitchRoll:
                    if (motionLimitPitchRoll != null)
                        return motionLimitPitchRoll.data;
                    else
                        return Constants.MotionLimitDefaultPitchRoll;

                case ValueType.MotionLimitInclination:
                    if (motionLimitInclination != null)
                        return motionLimitInclination.data;
                    else
                        return Constants.MotionLimitDefaultInclination;

                case ValueType.MotionLimitHeaveAmplitude:
                    if (motionLimitHeaveAmplitude != null)
                        return motionLimitHeaveAmplitude.data;
                    else
                        return Constants.MotionLimitDefaultHeaveAmplitude;

                case ValueType.MotionLimitSignificantHeaveRate:
                    if (motionLimitSignificantHeaveRate != null)
                        return motionLimitSignificantHeaveRate.data;
                    else
                        return Constants.MotionLimitDefaultSignificantHeaveRate;

                default:
                    return 1;
            }
        }

        public void InitPitchData()
        {
            _pitchData = new HMSData();
            _pitchMax20mData = new HMSData();
            _pitchMaxUp20mData = new HMSData();
            _pitchMaxDown20mData = new HMSData();
            _pitchMax3hData = new HMSData();

            // Init av chart data
            if (adminSettingsVM.regulationStandard == RegulationStandard.CAP)
            {
                for (double i = Constants.Minutes20 * -1000; i <= 0; i += Constants.GraphCullFrequency20m + 250)
                {
                    pitch20mList.Add(new HMSData()
                    {
                        data = 0,
                        timestamp = DateTime.UtcNow.AddMilliseconds(i)
                    });
                }

                for (double i = Constants.Hours3 * -1000; i <= 0; i += Constants.GraphCullFrequency3h + 250)
                {
                    pitch3hList.Add(new HMSData()
                    {
                        data = 0,
                        timestamp = DateTime.UtcNow.AddMilliseconds(i)
                    });
                }
            }
            else
            {
                for (int i = -Constants.Minutes20; i <= 0; i++)
                {
                    pitch20mList.Add(new HMSData()
                    {
                        data = 0,
                        timestamp = DateTime.UtcNow.AddSeconds(i)
                    });
                }

                for (int i = -Constants.Hours3; i <= 0; i++)
                {
                    pitch3hList.Add(new HMSData()
                    {
                        data = 0,
                        timestamp = DateTime.UtcNow.AddSeconds(i)
                    });
                }
            }
        }

        private HMSData _pitchData { get; set; }
        public HMSData pitchData
        {
            get
            {
                return _pitchData;
            }
            set
            {
                if (value != null)
                {
                    _pitchData.Set(value);
                }
            }
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
                    OnPropertyChanged(nameof(pitchMax20mString));

                    _pitchMax20mData.Set(value);
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
                    OnPropertyChanged(nameof(pitchMaxUp20mString2));
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
                        return string.Format("{0} °", pitch.ToString("0.0"));
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
        public string pitchMaxUp20mString2
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
                    OnPropertyChanged(nameof(pitchMaxDown20mString2));
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
                        return string.Format("{0} °", pitch.ToString("0.0"));
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
        public string pitchMaxDown20mString2
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

        private HMSData _pitchMax3hData { get; set; }
        public HMSData pitchMax3hData
        {
            get
            {
                return _pitchMax3hData;
            }
            set
            {
                if (value != null)
                {
                    _pitchMax3hData.Set(value);
                }
            }
        }

        public void InitRollData()
        {
            _rollData = new HMSData();
            _rollMax20mData = new HMSData();
            _rollMaxLeft20mData = new HMSData();
            _rollMaxRight20mData = new HMSData();
            _rollMax3hData = new HMSData();

            // Init av chart data
            if (adminSettingsVM.regulationStandard == RegulationStandard.CAP)
            {
                for (double i = Constants.Minutes20 * -1000; i <= 0; i += Constants.GraphCullFrequency20m + 250)
                {
                    roll20mList.Add(new HMSData()
                    {
                        data = 0,
                        timestamp = DateTime.UtcNow.AddMilliseconds(i)
                    });
                }

                for (double i = Constants.Hours3 * -1000; i <= 0; i += Constants.GraphCullFrequency3h + 250)
                {
                    rollData3hList.Add(new HMSData()
                    {
                        data = 0,
                        timestamp = DateTime.UtcNow.AddMilliseconds(i)
                    });
                }
            }
            else
            {
                for (int i = -Constants.Minutes20; i <= 0; i++)
                {
                    roll20mList.Add(new HMSData()
                    {
                        data = 0,
                        timestamp = DateTime.UtcNow.AddSeconds(i)
                    });
                }

                for (int i = -Constants.Hours3; i <= 0; i++)
                {
                    rollData3hList.Add(new HMSData()
                    {
                        data = 0,
                        timestamp = DateTime.UtcNow.AddSeconds(i)
                    });
                }
            }
        }

        private HMSData _rollData { get; set; }
        public HMSData rollData
        {
            get
            {
                return _rollData;
            }
            set
            {
                if (value != null)
                {
                    _rollData.Set(value);
                }
            }
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
                    OnPropertyChanged(nameof(rollMax20mString));

                    _rollMax20mData.Set(value);
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
                    OnPropertyChanged(nameof(rollMaxLeft20mString2));
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
                        return string.Format("{0} °", roll.ToString("0.0"));
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
        public string rollMaxLeft20mString2
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
                        if (rollMaxLeft20mData.data > 0)
                            dir = "R";
                        else
                            dir = "L";

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
                    OnPropertyChanged(nameof(rollMaxRight20mString2));
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
                        return string.Format("{0} °", roll.ToString("0.0"));
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
        public string rollMaxRight20mString2
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
                        if (rollMaxRight20mData.data > 0)
                            dir = "R";
                        else
                            dir = "L";

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

        private HMSData _rollMax3hData { get; set; }
        public HMSData rollMax3hData
        {
            get
            {
                return _rollMax3hData;
            }
            set
            {
                if (value != null)
                {
                    _rollMax3hData.Set(value);
                }
            }
        }

        public void InitInclinationData()
        {
            _inclinationData = new HMSData();
            _inclinationMax20mData = new HMSData();
            _inclinationMax3hData = new HMSData();

            // Init av chart data
            if (adminSettingsVM.regulationStandard == RegulationStandard.CAP)
            {
                for (double i = Constants.Minutes20 * -1000; i <= 0; i += Constants.GraphCullFrequency20m + 250)
                {
                    inclinationData20mList.Add(new HMSData()
                    {
                        data = 0,
                        timestamp = DateTime.UtcNow.AddMilliseconds(i)
                    });
                }

                for (double i = Constants.Hours3 * -1000; i <= 0; i += Constants.GraphCullFrequency3h + 250)
                {
                    inclinationData3hList.Add(new HMSData()
                    {
                        data = 0,
                        timestamp = DateTime.UtcNow.AddMilliseconds(i)
                    });
                }
            }
            else
            {
                for (int i = -Constants.Minutes20; i <= 0; i++)
                {
                    inclinationData20mList.Add(new HMSData()
                    {
                        data = 0,
                        timestamp = DateTime.UtcNow.AddSeconds(i)
                    });
                }

                for (int i = -Constants.Hours3; i <= 0; i++)
                {
                    inclinationData3hList.Add(new HMSData()
                    {
                        data = 0,
                        timestamp = DateTime.UtcNow.AddSeconds(i)
                    });
                }
            }
        }

        private HMSData _inclinationData { get; set; }
        public HMSData inclinationData
        {
            get
            {
                return _inclinationData;
            }
            set
            {
                if (value != null)
                {
                    _inclinationData.Set(value);
                }
            }
        }

        public HMSData _inclinationMax20mData { get; set; }
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
                    OnPropertyChanged(nameof(inclinationMax20mString));

                    _inclinationMax20mData.Set(value);
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
                    if (inclinationMax20mData.status == DataStatus.OK)
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

        private HMSData _inclinationMax3hData { get; set; }
        public HMSData inclinationMax3hData
        {
            get
            {
                return _inclinationMax3hData;
            }
            set
            {
                if (value != null)
                {
                    _inclinationMax3hData.Set(value);
                }
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Heave Amplitude
        /////////////////////////////////////////////////////////////////////////////
        public void InitHeaveAmplitudeData()
        {
            _heaveAmplitudeData = new HMSData();
            _heaveAmplitudeMax20mData = new HMSData();
            _heaveAmplitudeMax3hData = new HMSData();

            // Init av chart data
            if (adminSettingsVM.regulationStandard == RegulationStandard.CAP)
            {
                for (double i = Constants.Minutes20 * -1000; i <= 0; i += Constants.GraphCullFrequency20m + 250)
                {
                    heaveAmplitudeData20mList.Add(new HMSData()
                    {
                        data = 0,
                        timestamp = DateTime.UtcNow.AddMilliseconds(i)
                    });
                }

                for (double i = Constants.Hours3 * -1000; i <= 0; i += Constants.GraphCullFrequency3h + 250)
                {
                    heaveAmplitudeData3hList.Add(new HMSData()
                    {
                        data = 0,
                        timestamp = DateTime.UtcNow.AddMilliseconds(i)
                    });
                }
            }
            else
            {
                for (int i = -Constants.Minutes20; i <= 0; i++)
                {
                    heaveAmplitudeData20mList.Add(new HMSData()
                    {
                        data = 0,
                        timestamp = DateTime.UtcNow.AddSeconds(i)
                    });
                }

                for (int i = -Constants.Hours3; i <= 0; i++)
                {
                    heaveAmplitudeData3hList.Add(new HMSData()
                    {
                        data = 0,
                        timestamp = DateTime.UtcNow.AddSeconds(i)
                    });
                }
            }
        }

        private HMSData _heaveAmplitudeData { get; set; }
        public HMSData heaveAmplitudeData
        {
            get
            {
                return _heaveAmplitudeData;
            }
            set
            {
                if (value != null)
                {
                    _heaveAmplitudeData.Set(value);
                }
            }
        }

        private HMSData _heaveAmplitudeMax20mData { get; set; }
        public HMSData heaveAmplitudeMax20mData
        {
            get
            {
                return _heaveAmplitudeMax20mData;
            }
            set
            {
                if (value != null)
                {
                    OnPropertyChanged(nameof(heaveAmplitudeMax20mString));

                    _heaveAmplitudeMax20mData.Set(value);
                }
            }
        }

        public string heaveAmplitudeMax20mString
        {
            get
            {
                if (heaveAmplitudeMax20mData != null)
                {
                    // Sjekke om data er gyldig
                    if (heaveAmplitudeMax20mData.status == DataStatus.OK)
                    {
                        return string.Format("{0} m", heaveAmplitudeMax20mData.data.ToString("0.0"));
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

        private HMSData _heaveAmplitudeMax3hData { get; set; }
        public HMSData heaveAmplitudeMax3hData
        {
            get
            {
                return _heaveAmplitudeMax3hData;
            }
            set
            {
                if (value != null)
                {
                    _heaveAmplitudeMax3hData.Set(value);
                }
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Significant Heave Rate
        /////////////////////////////////////////////////////////////////////////////
        public void InitSignificantHeaveData()
        {
            _significantHeaveRateData = new HMSData();
            _significantHeaveRateMax20mData = new HMSData();
            _significantHeaveRateMax3hData = new HMSData();

            // Init av chart data
            if (adminSettingsVM.regulationStandard == RegulationStandard.CAP)
            {
                for (double i = Constants.Minutes20 * -1000; i <= 0; i += Constants.GraphCullFrequency20m + 250)
                {
                    significantHeaveRateData20mList.Add(new HMSData()
                    {
                        data = 0,
                        timestamp = DateTime.UtcNow.AddMilliseconds(i)
                    });
                }

                for (double i = Constants.Hours3 * -1000; i <= 0; i += Constants.GraphCullFrequency3h + 250)
                {
                    significantHeaveRateData3hList.Add(new HMSData()
                    {
                        data = 0,
                        timestamp = DateTime.UtcNow.AddMilliseconds(i)
                    });
                }
            }
            else
            {
                for (int i = -Constants.Minutes20; i <= 0; i++)
                {
                    significantHeaveRateData20mList.Add(new HMSData()
                    {
                        data = 0,
                        timestamp = DateTime.UtcNow.AddSeconds(i)
                    });
                }

                for (int i = -Constants.Hours3; i <= 0; i++)
                {
                    significantHeaveRateData3hList.Add(new HMSData()
                    {
                        data = 0,
                        timestamp = DateTime.UtcNow.AddSeconds(i)
                    });
                }
            }
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
                    OnPropertyChanged(nameof(significantHeaveRateString));

                    _significantHeaveRateData.Set(value);
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

        private HMSData _significantHeaveRateMax20mData { get; set; }
        public HMSData significantHeaveRateMax20mData
        {
            get
            {
                return _significantHeaveRateMax20mData;
            }
            set
            {
                if (value != null)
                    _significantHeaveRateMax20mData.Set(value);
            }
        }

        private HMSData _significantHeaveRateMax3hData { get; set; }
        public HMSData significantHeaveRateMax3hData
        {
            get
            {
                return _significantHeaveRateMax3hData;
            }
            set
            {
                if (value != null)
                    _significantHeaveRateMax3hData.Set(value);
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
        // Chart X axis tick alignment
        /////////////////////////////////////////////////////////////////////////////
        //private DateTime _tickAlignment5;
        //public DateTime tickAlignment5
        //{
        //    get
        //    {
        //        return _tickAlignment5;
        //    }
        //    set
        //    {
        //        if (_tickAlignment5 != value)
        //        {
        //            _tickAlignment5 = value;
        //            OnPropertyChanged();
        //        }
        //    }
        //}

        //private DateTime _tickAlignment10;
        //public DateTime tickAlignment10
        //{
        //    get
        //    {
        //        return _tickAlignment10;
        //    }
        //    set
        //    {
        //        if (_tickAlignment10 != value)
        //        {
        //            _tickAlignment10 = value;
        //            OnPropertyChanged();
        //        }
        //    }
        //}

        //private DateTime _tickAlignment15;
        //public DateTime tickAlignment15
        //{
        //    get
        //    {
        //        return _tickAlignment15;
        //    }
        //    set
        //    {
        //        if (_tickAlignment15 != value)
        //        {
        //            _tickAlignment15 = value;
        //            OnPropertyChanged();
        //        }
        //    }
        //}

        /////////////////////////////////////////////////////////////////////////////
        // Chart Axis Min/Max
        /////////////////////////////////////////////////////////////////////////////
        public double pitchChartAxisMax20m
        {
            get
            {
                if (pitchMax20mData.data > GetMotionLimit(ValueType.MotionLimitPitchRoll) + Constants.PitchAxisMargin)
                    return (double)((int)pitchMax20mData.data + Constants.PitchAxisMargin);
                else
                    return GetMotionLimit(ValueType.MotionLimitPitchRoll) + Constants.PitchAxisMargin;
            }
        }

        public double pitchChartAxisMax3h
        {
            get
            {
                if (pitchMax3hData.data > GetMotionLimit(ValueType.MotionLimitPitchRoll) + Constants.PitchAxisMargin)
                    return (double)((int)pitchMax3hData.data + Constants.PitchAxisMargin);
                else
                    return GetMotionLimit(ValueType.MotionLimitPitchRoll) + Constants.PitchAxisMargin;
            }
        }

        public double pitchChartAxisMin20m
        {
            get
            {
                if (-pitchMax20mData.data < -GetMotionLimit(ValueType.MotionLimitPitchRoll) - Constants.PitchAxisMargin)
                    return (double)((int)-pitchMax20mData.data - Constants.PitchAxisMargin);
                else
                    return -GetMotionLimit(ValueType.MotionLimitPitchRoll) - Constants.PitchAxisMargin;
            }
        }

        public double pitchChartAxisMin3h
        {
            get
            {
                if (-pitchMax3hData.data < -GetMotionLimit(ValueType.MotionLimitPitchRoll) - Constants.PitchAxisMargin)
                    return (double)((int)-pitchMax3hData.data - Constants.PitchAxisMargin);
                else
                    return -GetMotionLimit(ValueType.MotionLimitPitchRoll) - Constants.PitchAxisMargin;
            }
        }

        public double rollChartAxisMax20m
        {
            get
            {
                if (rollMax20mData?.data > GetMotionLimit(ValueType.MotionLimitPitchRoll) + Constants.RollAxisMargin)
                    return (double)((int)rollMax20mData.data + Constants.RollAxisMargin);
                else
                    return GetMotionLimit(ValueType.MotionLimitPitchRoll) + Constants.RollAxisMargin;
            }
        }

        public double rollChartAxisMax3h
        {
            get
            {
                if (rollMax3hData?.data > GetMotionLimit(ValueType.MotionLimitPitchRoll) + Constants.RollAxisMargin)
                    return (double)((int)rollMax3hData.data + Constants.RollAxisMargin);
                else
                    return GetMotionLimit(ValueType.MotionLimitPitchRoll) + Constants.RollAxisMargin;
            }
        }

        public double rollChartAxisMin20m
        {
            get
            {
                if (-rollMax20mData?.data < -GetMotionLimit(ValueType.MotionLimitPitchRoll) - Constants.RollAxisMargin)
                    return (double)((int)-rollMax20mData.data - Constants.RollAxisMargin);
                else
                    return -GetMotionLimit(ValueType.MotionLimitPitchRoll) - Constants.RollAxisMargin;
            }
        }

        public double rollChartAxisMin3h
        {
            get
            {
                if (-rollMax3hData?.data < -GetMotionLimit(ValueType.MotionLimitPitchRoll) - Constants.RollAxisMargin)
                    return (double)((int)-rollMax3hData.data - Constants.RollAxisMargin);
                else
                    return -GetMotionLimit(ValueType.MotionLimitPitchRoll) - Constants.RollAxisMargin;
            }
        }

        public double inclinationChartAxisMax20m
        {
            get
            {
                if (inclinationMax20mData?.data > GetMotionLimit(ValueType.MotionLimitInclination) + Constants.InclinationAxisMargin)
                    return (double)((int)inclinationMax20mData.data + Constants.InclinationAxisMargin);
                else
                    return GetMotionLimit(ValueType.MotionLimitInclination) + Constants.InclinationAxisMargin;
            }
        }

        public double inclinationChartAxisMax3h
        {
            get
            {
                if (inclinationMax3hData?.data > GetMotionLimit(ValueType.MotionLimitInclination) + Constants.InclinationAxisMargin)
                    return (double)((int)inclinationMax3hData.data + Constants.InclinationAxisMargin);
                else
                    return GetMotionLimit(ValueType.MotionLimitInclination) + Constants.InclinationAxisMargin;
            }
        }

        public double heaveAmplitudeChartAxisMax20m
        {
            get
            {
                if (heaveAmplitudeMax20mData?.data > GetMotionLimit(ValueType.MotionLimitHeaveAmplitude) + Constants.HeaveAmplitudeAxisMargin)
                    return (double)((int)heaveAmplitudeMax20mData.data + Constants.HeaveAmplitudeAxisMargin);
                else
                    return GetMotionLimit(ValueType.MotionLimitHeaveAmplitude) + Constants.HeaveAmplitudeAxisMargin;
            }
        }

        public double heaveAmplitudeChartAxisMax3h
        {
            get
            {
                if (heaveAmplitudeMax3hData?.data > GetMotionLimit(ValueType.MotionLimitHeaveAmplitude) + Constants.HeaveAmplitudeAxisMargin)
                    return (double)((int)heaveAmplitudeMax3hData.data + Constants.HeaveAmplitudeAxisMargin);
                else
                    return GetMotionLimit(ValueType.MotionLimitHeaveAmplitude) + Constants.HeaveAmplitudeAxisMargin;
            }
        }

        public double significantHeaveRateChartAxisMax20m
        {
            get
            {
                if (significantHeaveRateMax20mData?.data > GetMotionLimit(ValueType.MotionLimitSignificantHeaveRate) + Constants.SignificantHeaveRateAxisMargin)
                    return significantHeaveRateMax20mData.data + Constants.SignificantHeaveRateAxisMargin;
                else
                    return GetMotionLimit(ValueType.MotionLimitSignificantHeaveRate) + Constants.SignificantHeaveRateAxisMargin;
            }
        }

        public double significantHeaveRateChartAxisMax3h
        {
            get
            {
                if (significantHeaveRateMax3hData?.data > GetMotionLimit(ValueType.MotionLimitSignificantHeaveRate) + Constants.SignificantHeaveRateAxisMargin)
                    return significantHeaveRateMax3hData.data + Constants.SignificantHeaveRateAxisMargin;
                else
                    return GetMotionLimit(ValueType.MotionLimitSignificantHeaveRate) + Constants.SignificantHeaveRateAxisMargin;
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Chart Plot Area Annotations
        /////////////////////////////////////////////////////////////////////////////

        // Pitch
        /////////////////////////////////////////////////////////////////////////////
        public double pitchChartAnnotationGreenMax20m
        {
            get
            {
                return GetMotionLimit(ValueType.MotionLimitPitchRoll);
            }
        }

        public double pitchChartAnnotationGreenMin20m
        {
            get
            {
                return -GetMotionLimit(ValueType.MotionLimitPitchRoll);
            }
        }

        public double pitchChartAnnotationRedTopMax20m
        {
            get
            {
                return pitchChartAxisMax20m;
            }
        }

        public double pitchChartAnnotationRedTopMin20m
        {
            get
            {
                return GetMotionLimit(ValueType.MotionLimitPitchRoll);
            }
        }

        public double pitchChartAnnotationRedBottomMax20m
        {
            get
            {
                return -pitchChartAxisMax20m;
            }
        }

        public double pitchChartAnnotationRedBottomMin20m
        {
            get
            {
                return -GetMotionLimit(ValueType.MotionLimitPitchRoll);
            }
        }

        public double pitchChartAnnotationGreenMax3h
        {
            get
            {
                return GetMotionLimit(ValueType.MotionLimitPitchRoll);
            }
        }

        public double pitchChartAnnotationGreenMin3h
        {
            get
            {
                return -GetMotionLimit(ValueType.MotionLimitPitchRoll);
            }
        }

        public double pitchChartAnnotationRedTopMax3h
        {
            get
            {
                return pitchChartAxisMax3h;
            }
        }

        public double pitchChartAnnotationRedTopMin3h
        {
            get
            {
                return GetMotionLimit(ValueType.MotionLimitPitchRoll);
            }
        }

        public double pitchChartAnnotationRedBottomMax3h
        {
            get
            {
                return -pitchChartAxisMax3h;
            }
        }

        public double pitchChartAnnotationRedBottomMin3h
        {
            get
            {
                return -GetMotionLimit(ValueType.MotionLimitPitchRoll);
            }
        }

        // Roll
        /////////////////////////////////////////////////////////////////////////////
        public double rollChartAnnotationGreenMax20m
        {
            get
            {
                return GetMotionLimit(ValueType.MotionLimitPitchRoll);
            }
        }

        public double rollChartAnnotationGreenMin20m
        {
            get
            {
                return -GetMotionLimit(ValueType.MotionLimitPitchRoll);
            }
        }

        public double rollChartAnnotationRedTopMax20m
        {
            get
            {
                return rollChartAxisMax20m;
            }
        }

        public double rollChartAnnotationRedTopMin20m
        {
            get
            {
                return GetMotionLimit(ValueType.MotionLimitPitchRoll);
            }
        }

        public double rollChartAnnotationRedBottomMax20m
        {
            get
            {
                return -rollChartAxisMax20m;
            }
        }

        public double rollChartAnnotationRedBottomMin20m
        {
            get
            {
                return -GetMotionLimit(ValueType.MotionLimitPitchRoll);
            }
        }

        public double rollChartAnnotationGreenMax3h
        {
            get
            {
                return GetMotionLimit(ValueType.MotionLimitPitchRoll);
            }
        }

        public double rollChartAnnotationGreenMin3h
        {
            get
            {
                return -GetMotionLimit(ValueType.MotionLimitPitchRoll);
            }
        }

        public double rollChartAnnotationRedTopMax3h
        {
            get
            {
                return rollChartAxisMax3h;
            }
        }

        public double rollChartAnnotationRedTopMin3h
        {
            get
            {
                return GetMotionLimit(ValueType.MotionLimitPitchRoll);
            }
        }

        public double rollChartAnnotationRedBottomMax3h
        {
            get
            {
                return -rollChartAxisMax3h;
            }
        }

        public double rollChartAnnotationRedBottomMin3h
        {
            get
            {
                return -GetMotionLimit(ValueType.MotionLimitPitchRoll);
            }
        }

        // Inclination
        /////////////////////////////////////////////////////////////////////////////
        public double inclinationChartAnnotationGreenMax20m
        {
            get
            {
                return GetMotionLimit(ValueType.MotionLimitInclination);
            }
        }

        public double inclinationChartAnnotationRedMax20m
        {
            get
            {
                return inclinationChartAxisMax20m;
            }
        }

        public double inclinationChartAnnotationRedMin20m
        {
            get
            {
                return GetMotionLimit(ValueType.MotionLimitInclination);
            }
        }

        public double inclinationChartAnnotationGreenMax3h
        {
            get
            {
                return GetMotionLimit(ValueType.MotionLimitInclination);
            }
        }

        public double inclinationChartAnnotationRedMax3h
        {
            get
            {
                return inclinationChartAxisMax3h;
            }
        }

        public double inclinationChartAnnotationRedMin3h
        {
            get
            {
                return GetMotionLimit(ValueType.MotionLimitInclination);
            }
        }

        // Heave Amplitude
        /////////////////////////////////////////////////////////////////////////////
        public double heaveAmplitudeChartAnnotationGreenMax20m
        {
            get
            {
                return GetMotionLimit(ValueType.MotionLimitHeaveAmplitude);
            }
        }

        public double heaveAmplitudeChartAnnotationRedMax20m
        {
            get
            {
                return heaveAmplitudeChartAxisMax20m;
            }
        }

        public double heaveAmplitudeChartAnnotationRedMin20m
        {
            get
            {
                return GetMotionLimit(ValueType.MotionLimitHeaveAmplitude);
            }
        }

        public double heaveAmplitudeChartAnnotationGreenMax3h
        {
            get
            {
                return GetMotionLimit(ValueType.MotionLimitHeaveAmplitude);
            }
        }

        public double heaveAmplitudeChartAnnotationRedMax3h
        {
            get
            {
                return heaveAmplitudeChartAxisMax3h;
            }
        }

        public double heaveAmplitudeChartAnnotationRedMin3h
        {
            get
            {
                return GetMotionLimit(ValueType.MotionLimitHeaveAmplitude);
            }
        }

        // Significant Heave Rate
        /////////////////////////////////////////////////////////////////////////////
        public double significantHeaveRateChartAnnotationGreenMax20m
        {
            get
            {
                return GetMotionLimit(ValueType.MotionLimitSignificantHeaveRate);
            }
        }

        public double significantHeaveRateChartAnnotationRedMax20m
        {
            get
            {
                return significantHeaveRateChartAxisMax20m;
            }
        }

        public double significantHeaveRateChartAnnotationRedMin20m
        {
            get
            {
                return GetMotionLimit(ValueType.MotionLimitSignificantHeaveRate);
            }
        }

        public double significantHeaveRateChartAnnotationGreenMax3h
        {
            get
            {
                return GetMotionLimit(ValueType.MotionLimitSignificantHeaveRate);
            }
        }

        public double significantHeaveRateChartAnnotationRedMax3h
        {
            get
            {
                return significantHeaveRateChartAxisMax3h;
            }
        }

        public double significantHeaveRateChartAnnotationRedMin3h
        {
            get
            {
                return GetMotionLimit(ValueType.MotionLimitSignificantHeaveRate);
            }
        }

        // Motion Limits
        /////////////////////////////////////////////////////////////////////////////
        public string pitchRollLimitString
        {
            get
            {
                if (motionLimitPitchRoll != null)
                {
                    if (motionLimitPitchRoll.status == DataStatus.OK)
                        return string.Format("{0} °", motionLimitPitchRoll.data.ToString("0.0"));
                    else
                        return Constants.NotAvailable;
                }
                else
                {
                    return Constants.NotAvailable;
                }
            }
        }

        public string inclinationLimitString
        {
            get
            {
                if (motionLimitInclination != null)
                {
                    if (motionLimitInclination.status == DataStatus.OK)
                        return string.Format("{0} °", motionLimitInclination.data.ToString("0.0"));
                    else
                        return Constants.NotAvailable;
                }
                else
                {
                    return Constants.NotAvailable;
                }
            }
        }

        public string significantHeaveRateLimitString
        {
            get
            {
                if (motionLimitSignificantHeaveRate != null)
                {
                    if (motionLimitSignificantHeaveRate.status == DataStatus.OK)
                        return string.Format("{0} m/s", motionLimitSignificantHeaveRate.data.ToString("0.0"));
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
            PropertyChanged.Invoke(this, new PropertyChangedEventArgs(name));
        }

    }
}
