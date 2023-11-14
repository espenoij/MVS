using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Threading;
using Telerik.Windows.Data;

namespace MVS
{
    public class HelideckMotionVM : INotifyPropertyChanged
    {
        // Change notification
        public event PropertyChangedEventHandler PropertyChanged;

        private DispatcherTimer ChartUpdateTimer = new DispatcherTimer();

        // Live Data
        private RadObservableCollection<HMSData> refPitchBuffer = new RadObservableCollection<HMSData>();
        private RadObservableCollection<HMSData> refRollBuffer = new RadObservableCollection<HMSData>();
        private RadObservableCollection<HMSData> refHeaveBuffer = new RadObservableCollection<HMSData>();

        public RadObservableCollection<HMSData> refPitchList = new RadObservableCollection<HMSData>();
        public RadObservableCollection<HMSData> refRollList = new RadObservableCollection<HMSData>();
        public RadObservableCollection<HMSData> refHeaveList = new RadObservableCollection<HMSData>();

        private RadObservableCollection<HMSData> testPitchBuffer = new RadObservableCollection<HMSData>();
        private RadObservableCollection<HMSData> testRollBuffer = new RadObservableCollection<HMSData>();
        private RadObservableCollection<HMSData> testHeaveBuffer = new RadObservableCollection<HMSData>();

        public RadObservableCollection<HMSData> testPitchList = new RadObservableCollection<HMSData>();
        public RadObservableCollection<HMSData> testRollList = new RadObservableCollection<HMSData>();
        public RadObservableCollection<HMSData> testHeaveList = new RadObservableCollection<HMSData>();

        // 20 minutters data
        public RadObservableCollection<HMSData> refPitch20mList = new RadObservableCollection<HMSData>();
        public RadObservableCollection<HMSData> refRoll20mList = new RadObservableCollection<HMSData>();
        public RadObservableCollection<HMSData> refHeave20mList = new RadObservableCollection<HMSData>();

        private RadObservableCollection<HMSData> refPitch20mBuffer = new RadObservableCollection<HMSData>();
        private RadObservableCollection<HMSData> refRoll20mBuffer = new RadObservableCollection<HMSData>();
        private RadObservableCollection<HMSData> refHeave20mBuffer = new RadObservableCollection<HMSData>();

        public RadObservableCollection<HMSData> testPitch20mList = new RadObservableCollection<HMSData>();
        public RadObservableCollection<HMSData> testRoll20mList = new RadObservableCollection<HMSData>();
        public RadObservableCollection<HMSData> testHeave20mList = new RadObservableCollection<HMSData>();

        private RadObservableCollection<HMSData> testPitch20mBuffer = new RadObservableCollection<HMSData>();
        private RadObservableCollection<HMSData> testRoll20mBuffer = new RadObservableCollection<HMSData>();
        private RadObservableCollection<HMSData> testHeave20mBuffer = new RadObservableCollection<HMSData>();

        public void Init()
        {
            InitPitchData();
            InitRollData();
            InitHeaveData();

            // Oppdatere trend data i UI: 20 minutter
            ChartUpdateTimer.Interval = TimeSpan.FromMilliseconds(Constants.ChartUpdateFrequencyUI20mDefault);
            ChartUpdateTimer.Tick += ChartDataUpdate20m;
            ChartUpdateTimer.Start();

            void ChartDataUpdate20m(object sender, EventArgs e)
            {
                // Overføre data fra buffer til chart data: 20m
                GraphBuffer.Transfer(refPitchBuffer, refPitchList);
                GraphBuffer.Transfer(refRollBuffer, refRollList);
                GraphBuffer.Transfer(refHeaveBuffer, refHeaveList);

                GraphBuffer.Transfer(refPitch20mBuffer, refPitch20mList);
                GraphBuffer.Transfer(refRoll20mBuffer, refRoll20mList);
                GraphBuffer.Transfer(refHeave20mBuffer, refHeave20mList);

                GraphBuffer.Transfer(testPitchBuffer, testPitchList);
                GraphBuffer.Transfer(testRollBuffer, testRollList);
                GraphBuffer.Transfer(testHeaveBuffer, testHeaveList);

                GraphBuffer.Transfer(testPitch20mBuffer, testPitch20mList);
                GraphBuffer.Transfer(testRoll20mBuffer, testRoll20mList);
                GraphBuffer.Transfer(testHeave20mBuffer, testHeave20mList);

                // Fjerne gamle data fra chart data
                GraphBuffer.RemoveOldData(refPitchList, Constants.Minutes20 + Constants.ChartTimeCorrMin);
                GraphBuffer.RemoveOldData(refRollList, Constants.Minutes20 + Constants.ChartTimeCorrMin);
                GraphBuffer.RemoveOldData(refHeaveList, Constants.Minutes20 + Constants.ChartTimeCorrMin);

                GraphBuffer.RemoveOldData(refPitch20mList, Constants.Minutes20 + Constants.ChartTimeCorrMin);
                GraphBuffer.RemoveOldData(refRoll20mList, Constants.Minutes20 + Constants.ChartTimeCorrMin);
                GraphBuffer.RemoveOldData(refHeave20mList, Constants.Minutes20 + Constants.ChartTimeCorrMin);

                GraphBuffer.RemoveOldData(testPitchList, Constants.Minutes20 + Constants.ChartTimeCorrMin);
                GraphBuffer.RemoveOldData(testRollList, Constants.Minutes20 + Constants.ChartTimeCorrMin);
                GraphBuffer.RemoveOldData(testHeaveList, Constants.Minutes20 + Constants.ChartTimeCorrMin);

                GraphBuffer.RemoveOldData(testPitch20mList, Constants.Minutes20 + Constants.ChartTimeCorrMin);
                GraphBuffer.RemoveOldData(testRoll20mList, Constants.Minutes20 + Constants.ChartTimeCorrMin);
                GraphBuffer.RemoveOldData(testHeave20mList, Constants.Minutes20 + Constants.ChartTimeCorrMin);

                // Oppdatere alignment datetime (nåtid) til alle chart
                alignmentTime = DateTime.UtcNow;

                // Oppdatere aksene og farget område på graf
                OnPropertyChanged(nameof(pitchChartAxisMax20m));
                OnPropertyChanged(nameof(pitchChartAxisMin20m));

                OnPropertyChanged(nameof(rollChartAxisMax20m));
                OnPropertyChanged(nameof(rollChartAxisMin20m));

                OnPropertyChanged(nameof(heaveChartAxisMax20m));
                OnPropertyChanged(nameof(heaveChartAxisMin20m));
                OnPropertyChanged(nameof(heaveAmplitudeChartAxisMax20m));
            }
        }

        public void UpdateData(MVSDataCollection mvsDataCollection)
        {
            // Oppdatere data som skal ut i grafer
            GraphBuffer.Update(mvsDataCollection.GetData(ValueType.Ref_Pitch), refPitchBuffer);
            GraphBuffer.Update(mvsDataCollection.GetData(ValueType.Ref_Roll), refRollBuffer);
            GraphBuffer.Update(mvsDataCollection.GetData(ValueType.Ref_Heave), refHeaveBuffer);

            GraphBuffer.Update(mvsDataCollection.GetData(ValueType.Ref_PitchMean20m), refPitch20mBuffer);
            GraphBuffer.Update(mvsDataCollection.GetData(ValueType.Ref_RollMean20m), refRoll20mBuffer);
            GraphBuffer.Update(mvsDataCollection.GetData(ValueType.Ref_HeaveMean20m), refHeave20mBuffer);

            GraphBuffer.Update(mvsDataCollection.GetData(ValueType.Test_Pitch), testPitchBuffer);
            GraphBuffer.Update(mvsDataCollection.GetData(ValueType.Test_Roll), testRollBuffer);
            GraphBuffer.Update(mvsDataCollection.GetData(ValueType.Test_Heave), testHeaveBuffer);

            GraphBuffer.Update(mvsDataCollection.GetData(ValueType.Test_PitchMean20m), testPitch20mBuffer);
            GraphBuffer.Update(mvsDataCollection.GetData(ValueType.Test_RollMean20m), testRoll20mBuffer);
            GraphBuffer.Update(mvsDataCollection.GetData(ValueType.Test_HeaveMean20m), testHeave20mBuffer);

            // Oppdatere max verdier
            refPitchMax20mData = mvsDataCollection.GetData(ValueType.Ref_PitchMax20m);
            refRollMax20mData = mvsDataCollection.GetData(ValueType.Ref_RollMax20m);
            refHeaveMax20mData = mvsDataCollection.GetData(ValueType.Ref_HeaveMax20m);
            refHeaveAmplitudeMax20mData = mvsDataCollection.GetData(ValueType.Ref_HeaveAmplitudeMax20m);

            testPitchMax20mData = mvsDataCollection.GetData(ValueType.Test_PitchMax20m);
            testRollMax20mData = mvsDataCollection.GetData(ValueType.Test_RollMax20m);
            testHeaveMax20mData = mvsDataCollection.GetData(ValueType.Test_HeaveMax20m);
            testHeaveAmplitudeMax20mData = mvsDataCollection.GetData(ValueType.Test_HeaveAmplitudeMax20m);
        }

        public void InitPitchData()
        {
            _pitchData = new HMSData();

            _refPitchMax20mData = new HMSData();
            _refPitchMaxUp20mData = new HMSData();
            _refPitchMaxDown20mData = new HMSData();
            
            _testPitchMax20mData = new HMSData();
            _testPitchMaxUp20mData = new HMSData();
            _testPitchMaxDown20mData = new HMSData();

            // Init av chart data
            for (int i = -Constants.Minutes20; i <= 0; i++)
            {
                refPitch20mList.Add(new HMSData()
                {
                    data = 0,
                    timestamp = DateTime.UtcNow.AddSeconds(i)
                });
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

        private HMSData _refPitchMax20mData { get; set; }
        public HMSData refPitchMax20mData
        {
            get
            {
                return _refPitchMax20mData;
            }
            set
            {
                if (value != null)
                {
                    _refPitchMax20mData.Set(value);

                    OnPropertyChanged(nameof(refPitchMax20mString));
                }
            }
        }
        public string refPitchMax20mString
        {
            get
            {
                if (_refPitchMax20mData != null)
                {
                    // Sjekke om data er gyldig
                    if (_refPitchMax20mData.status == DataStatus.OK)
                    {
                        return string.Format("{0} °", _refPitchMax20mData.data.ToString("0.0"));
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

        private HMSData _testPitchMax20mData { get; set; }
        public HMSData testPitchMax20mData
        {
            get
            {
                return _testPitchMax20mData;
            }
            set
            {
                if (value != null)
                {
                    _testPitchMax20mData.Set(value);

                    OnPropertyChanged(nameof(testPitchMax20mString));
                }
            }
        }
        public string testPitchMax20mString
        {
            get
            {
                if (_testPitchMax20mData != null)
                {
                    // Sjekke om data er gyldig
                    if (_testPitchMax20mData.status == DataStatus.OK)
                    {
                        return string.Format("{0} °", _testPitchMax20mData.data.ToString("0.0"));
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

        private HMSData _refPitchMaxUp20mData { get; set; }
        public HMSData refPitchMaxUp20mData
        {
            get
            {
                return _refPitchMaxUp20mData;
            }
            set
            {
                if (value != null)
                {
                    _refPitchMaxUp20mData.Set(value);

                    OnPropertyChanged(nameof(refPitchMaxUp20mString));
                }
            }
        }
        public string refPitchMaxUp20mString
        {
            get
            {
                if (_refPitchMaxUp20mData != null)
                {
                    // Sjekke om data er gyldig
                    if (_refPitchMaxUp20mData.status == DataStatus.OK)
                    {
                        double pitch = Math.Abs(_refPitchMaxUp20mData.data);
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

        private HMSData _testPitchMaxUp20mData { get; set; }
        public HMSData testPitchMaxUp20mData
        {
            get
            {
                return _testPitchMaxUp20mData;
            }
            set
            {
                if (value != null)
                {
                    _testPitchMaxUp20mData.Set(value);

                    OnPropertyChanged(nameof(testPitchMaxUp20mString));
                }
            }
        }
        public string testPitchMaxUp20mString
        {
            get
            {
                if (_testPitchMaxUp20mData != null)
                {
                    // Sjekke om data er gyldig
                    if (_testPitchMaxUp20mData.status == DataStatus.OK)
                    {
                        double pitch = Math.Abs(_testPitchMaxUp20mData.data);
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

        private HMSData _refPitchMaxDown20mData { get; set; }
        public HMSData refPitchMaxDown20mData
        {
            get
            {
                return _refPitchMaxDown20mData;
            }
            set
            {
                if (value != null)
                {
                    _refPitchMaxDown20mData.Set(value);

                    OnPropertyChanged(nameof(refPitchMaxDown20mString));
                }
            }
        }
        public string refPitchMaxDown20mString
        {
            get
            {
                if (_refPitchMaxDown20mData != null)
                {
                    // Sjekke om data er gyldig
                    if (_refPitchMaxDown20mData.status == DataStatus.OK)
                    {
                        double pitch = Math.Abs(_refPitchMaxDown20mData.data);
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

        private HMSData _testPitchMaxDown20mData { get; set; }
        public HMSData testPitchMaxDown20mData
        {
            get
            {
                return _testPitchMaxDown20mData;
            }
            set
            {
                if (value != null)
                {
                    _testPitchMaxDown20mData.Set(value);

                    OnPropertyChanged(nameof(testPitchMaxDown20mString));
                }
            }
        }
        public string testPitchMaxDown20mString
        {
            get
            {
                if (_testPitchMaxDown20mData != null)
                {
                    // Sjekke om data er gyldig
                    if (_testPitchMaxDown20mData.status == DataStatus.OK)
                    {
                        double pitch = Math.Abs(_testPitchMaxDown20mData.data);
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

        public void InitRollData()
        {
            _refRollData = new HMSData();

            _refRollMax20mData = new HMSData();
            _refRollMaxLeft20mData = new HMSData();
            _refRollMaxRight20mData = new HMSData();

            _testRollMax20mData = new HMSData();
            _testRollMaxLeft20mData = new HMSData();
            _testRollMaxRight20mData = new HMSData();

            // Init av chart data
            for (int i = -Constants.Minutes20; i <= 0; i++)
            {
                refRoll20mList.Add(new HMSData()
                {
                    data = 0,
                    timestamp = DateTime.UtcNow.AddSeconds(i)
                });
            }
        }

        private HMSData _refRollData { get; set; }
        public HMSData refRollData
        {
            get
            {
                return _refRollData;
            }
            set
            {
                if (value != null)
                {
                    _refRollData.Set(value);
                }
            }
        }

        private HMSData _refRollMax20mData { get; set; }
        public HMSData refRollMax20mData
        {
            get
            {
                return _refRollMax20mData;
            }
            set
            {
                if (value != null)
                {
                    _refRollMax20mData.Set(value);

                    OnPropertyChanged(nameof(refRollMax20mString));
                }
            }
        }
        public string refRollMax20mString
        {
            get
            {
                if (_refRollMax20mData != null)
                {
                    // Sjekke om data er gyldig
                    if (_refRollMax20mData.status == DataStatus.OK)
                    {
                        return string.Format("{0} °", _refRollMax20mData.data.ToString("0.0"));
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

        private HMSData _testRollMax20mData { get; set; }
        public HMSData testRollMax20mData
        {
            get
            {
                return _testRollMax20mData;
            }
            set
            {
                if (value != null)
                {
                    _testRollMax20mData.Set(value);

                    OnPropertyChanged(nameof(testRollMax20mString));
                }
            }
        }
        public string testRollMax20mString
        {
            get
            {
                if (_testRollMax20mData != null)
                {
                    // Sjekke om data er gyldig
                    if (_testRollMax20mData.status == DataStatus.OK)
                    {
                        return string.Format("{0} °", _testRollMax20mData.data.ToString("0.0"));
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

        private HMSData _refRollMaxLeft20mData { get; set; }
        public HMSData refRollMaxLeft20mData
        {
            get
            {
                return _refRollMaxLeft20mData;
            }
            set
            {
                if (value != null)
                {
                    _refRollMaxLeft20mData.Set(value);

                    OnPropertyChanged(nameof(refRollMaxLeft20mString));
                }
            }
        }
        public string refRollMaxLeft20mString
        {
            get
            {
                if (_refRollMaxLeft20mData != null)
                {
                    // Sjekke om data er gyldig
                    if (_refRollMaxLeft20mData.status == DataStatus.OK)
                    {
                        double roll = Math.Abs(_refRollMaxLeft20mData.data);
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

        private HMSData _testRollMaxLeft20mData { get; set; }
        public HMSData testRollMaxLeft20mData
        {
            get
            {
                return _testRollMaxLeft20mData;
            }
            set
            {
                if (value != null)
                {
                    _testRollMaxLeft20mData.Set(value);

                    OnPropertyChanged(nameof(testRollMaxLeft20mString));
                }
            }
        }
        public string testRollMaxLeft20mString
        {
            get
            {
                if (_testRollMaxLeft20mData != null)
                {
                    // Sjekke om data er gyldig
                    if (_testRollMaxLeft20mData.status == DataStatus.OK)
                    {
                        double roll = Math.Abs(_testRollMaxLeft20mData.data);
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

        private HMSData _refRollMaxRight20mData { get; set; }
        public HMSData refRollMaxRight20mData
        {
            get
            {
                return _refRollMaxRight20mData;
            }
            set
            {
                if (value != null)
                {
                    _refRollMaxRight20mData.Set(value);

                    OnPropertyChanged(nameof(refRollMaxRight20mString));
                }
            }
        }
        public string refRollMaxRight20mString
        {
            get
            {
                if (_refRollMaxRight20mData != null)
                {
                    // Sjekke om data er gyldig
                    if (_refRollMaxRight20mData.status == DataStatus.OK)
                    {
                        double roll = Math.Abs(_refRollMaxRight20mData.data);
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

        private HMSData _testRollMaxRight20mData { get; set; }
        public HMSData testRollMaxRight20mData
        {
            get
            {
                return _testRollMaxRight20mData;
            }
            set
            {
                if (value != null)
                {
                    _testRollMaxRight20mData.Set(value);

                    OnPropertyChanged(nameof(testRollMaxRight20mString));
                }
            }
        }
        public string testRollMaxRight20mString
        {
            get
            {
                if (_testRollMaxRight20mData != null)
                {
                    // Sjekke om data er gyldig
                    if (_testRollMaxRight20mData.status == DataStatus.OK)
                    {
                        double roll = Math.Abs(_testRollMaxRight20mData.data);
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

        /////////////////////////////////////////////////////////////////////////////
        // Heave
        /////////////////////////////////////////////////////////////////////////////
        public void InitHeaveData()
        {
            _refHeaveData = new HMSData();

            _refHeaveMax20mData = new HMSData();
            _refHeaveAmplitudeData = new HMSData();
            _refHeaveAmplitudeMax20mData = new HMSData();

            _testHeaveMax20mData = new HMSData();
            _testHeaveAmplitudeData = new HMSData();
            _testHeaveAmplitudeMax20mData = new HMSData();

            // Init av chart data
            for (int i = -Constants.Minutes20; i <= 0; i++)
            {
                refHeave20mList.Add(new HMSData()
                {
                    data = 0,
                    timestamp = DateTime.UtcNow.AddSeconds(i)
                });
            }
        }

        private HMSData _refHeaveData { get; set; }
        public HMSData refHeaveData
        {
            get
            {
                return _refHeaveData;
            }
            set
            {
                if (value != null)
                {
                    _refHeaveData.Set(value);
                }
            }
        }

        private HMSData _refHeaveMax20mData { get; set; }
        public HMSData refHeaveMax20mData
        {
            get
            {
                return _refHeaveMax20mData;
            }
            set
            {
                if (value != null)
                {
                    _refHeaveMax20mData.Set(value);

                    OnPropertyChanged(nameof(refHeaveMax20mString));
                }
            }
        }
        public string refHeaveMax20mString
        {
            get
            {
                if (_refHeaveMax20mData != null)
                {
                    // Sjekke om data er gyldig
                    if (_refHeaveMax20mData.status == DataStatus.OK)
                    {
                        return string.Format("{0} m", _refHeaveMax20mData.data.ToString("0.0"));
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

        private HMSData _testHeaveMax20mData { get; set; }
        public HMSData testHeaveMax20mData
        {
            get
            {
                return _testHeaveMax20mData;
            }
            set
            {
                if (value != null)
                {
                    _testHeaveMax20mData.Set(value);

                    OnPropertyChanged(nameof(testHeaveMax20mString));
                }
            }
        }
        public string testHeaveMax20mString
        {
            get
            {
                if (_testHeaveMax20mData != null)
                {
                    // Sjekke om data er gyldig
                    if (_testHeaveMax20mData.status == DataStatus.OK)
                    {
                        return string.Format("{0} m", _testHeaveMax20mData.data.ToString("0.0"));
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

        private HMSData _refHeaveAmplitudeData { get; set; }
        public HMSData refHeaveAmplitudeData
        {
            get
            {
                return _refHeaveAmplitudeData;
            }
            set
            {
                if (value != null)
                {
                    _refHeaveAmplitudeData.Set(value);
                }
            }
        }

        private HMSData _testHeaveAmplitudeData { get; set; }
        public HMSData testHeaveAmplitudeData
        {
            get
            {
                return _testHeaveAmplitudeData;
            }
            set
            {
                if (value != null)
                {
                    _testHeaveAmplitudeData.Set(value);
                }
            }
        }

        private HMSData _refHeaveAmplitudeMax20mData { get; set; }
        public HMSData refHeaveAmplitudeMax20mData
        {
            get
            {
                return _refHeaveAmplitudeMax20mData;
            }
            set
            {
                if (value != null)
                {
                    _refHeaveAmplitudeMax20mData.Set(value);

                    OnPropertyChanged(nameof(refHeaveAmplitudeMax20mString));
                }
            }
        }
        public string refHeaveAmplitudeMax20mString
        {
            get
            {
                if (_refHeaveAmplitudeMax20mData != null)
                {
                    // Sjekke om data er gyldig
                    if (_refHeaveAmplitudeMax20mData.status == DataStatus.OK)
                    {
                        return string.Format("{0} m", _refHeaveAmplitudeMax20mData.data.ToString("0.0"));
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

        private HMSData _testHeaveAmplitudeMax20mData { get; set; }
        public HMSData testHeaveAmplitudeMax20mData
        {
            get
            {
                return _testHeaveAmplitudeMax20mData;
            }
            set
            {
                if (value != null)
                {
                    _testHeaveAmplitudeMax20mData.Set(value);

                    OnPropertyChanged(nameof(testHeaveAmplitudeMax20mString));
                }
            }
        }
        public string testHeaveAmplitudeMax20mString
        {
            get
            {
                if (_testHeaveAmplitudeMax20mData != null)
                {
                    // Sjekke om data er gyldig
                    if (_testHeaveAmplitudeMax20mData.status == DataStatus.OK)
                    {
                        return string.Format("{0} m", _testHeaveAmplitudeMax20mData.data.ToString("0.0"));
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
                    OnPropertyChanged(nameof(alignmentTimeMax20m));
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

        public DateTime alignmentTimeMax20m
        {
            get
            {
                return _alignmentTime;
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Chart Axis Min/Max
        /////////////////////////////////////////////////////////////////////////////
        public double pitchChartAxisMax20m
        {
            get
            {
                return ChartAxisMax(_refPitchMax20mData, _testPitchMax20mData, Constants.PitchAxisMargin);
            }
        }

        public double pitchChartAxisMin20m
        {
            get
            {
                return -pitchChartAxisMax20m;
            }
        }

        public double rollChartAxisMax20m
        {
            get
            {
                return ChartAxisMax(_refRollMax20mData, _testRollMax20mData, Constants.RollAxisMargin);
            }
        }

        public double rollChartAxisMin20m
        {
            get
            {
                return -rollChartAxisMax20m;
            }
        }

        public double heaveChartAxisMax20m
        {
            get
            {
                return ChartAxisMax(_refHeaveMax20mData, _testHeaveMax20mData, Constants.HeaveAmplitudeAxisMargin);
            }
        }

        public double heaveChartAxisMin20m
        {
            get
            {
                return -heaveChartAxisMax20m;
            }
        }

        public double heaveAmplitudeChartAxisMax20m
        {
            get
            {
                return ChartAxisMax(_refHeaveAmplitudeMax20mData, _testHeaveAmplitudeMax20mData, Constants.HeaveAmplitudeAxisMargin);
            }
        }

        // Variabel oppdatert
        // Dersom navn ikke er satt brukes kallende medlem sitt navn
        protected virtual void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private double ChartAxisMax(HMSData refData, HMSData testData, double margin)
        {
            if (refData.data > testData.data)
                return (double)((int)refData.data + margin);
            else
                return (double)((int)testData.data + margin);
        }
    }
}
