using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Threading;
using Telerik.Windows.Data;

namespace MVS
{
    public class RecordingsVM : INotifyPropertyChanged
    {
        // Change notification
        public event PropertyChangedEventHandler PropertyChanged;

        private MainWindowVM mainWindowVM;
        private MVSProcessing mvsProcessing;
        private MVSDataCollection mvsInputData;
        private MVSDataCollection mvsOutputData;

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
        private RadObservableCollection<HMSData> refPitchMean20mBuffer = new RadObservableCollection<HMSData>();
        private RadObservableCollection<HMSData> refRollMean20mBuffer = new RadObservableCollection<HMSData>();
        private RadObservableCollection<HMSData> refHeaveAmplitudeMean20mBuffer = new RadObservableCollection<HMSData>();

        public RadObservableCollection<HMSData> refPitchMean20mList = new RadObservableCollection<HMSData>();
        public RadObservableCollection<HMSData> refRollMean20mList = new RadObservableCollection<HMSData>();
        public RadObservableCollection<HMSData> refHeaveAmplitudeMean20mList = new RadObservableCollection<HMSData>();

        private RadObservableCollection<HMSData> testPitchMean20mBuffer = new RadObservableCollection<HMSData>();
        private RadObservableCollection<HMSData> testRollMean20mBuffer = new RadObservableCollection<HMSData>();
        private RadObservableCollection<HMSData> testHeaveAmplitudeMean20mBuffer = new RadObservableCollection<HMSData>();

        public RadObservableCollection<HMSData> testPitchMean20mList = new RadObservableCollection<HMSData>();
        public RadObservableCollection<HMSData> testRollMean20mList = new RadObservableCollection<HMSData>();
        public RadObservableCollection<HMSData> testHeaveAmplitudeMean20mList = new RadObservableCollection<HMSData>();

        // Alle session data
        public RadObservableCollection<SessionData> sessionDataList = new RadObservableCollection<SessionData>();

        public void Init(MainWindowVM mainWindowVM, MVSProcessing mvsProcessing, MVSDataCollection mvsInputData, MVSDataCollection mvsOutputData)
        {
            this.mainWindowVM = mainWindowVM;
            this.mvsProcessing = mvsProcessing;
            this.mvsInputData = mvsInputData;
            this.mvsOutputData = mvsOutputData;

            InitPitchData();
            InitRollData();
            InitHeaveData();

            // Oppdatere trend data i UI: 20 minutter
            ChartUpdateTimer.Interval = TimeSpan.FromMilliseconds(Constants.ChartUpdateFrequencyUI20mDefault);
            ChartUpdateTimer.Tick += ChartDataUpdate20m;

            void ChartDataUpdate20m(object sender, EventArgs e)
            {
                // Overføre data fra buffer til chart data
                GraphBuffer.Transfer(refPitchBuffer, refPitchList);
                GraphBuffer.Transfer(refRollBuffer, refRollList);
                GraphBuffer.Transfer(refHeaveBuffer, refHeaveList);

                GraphBuffer.Transfer(refPitchMean20mBuffer, refPitchMean20mList);
                GraphBuffer.Transfer(refRollMean20mBuffer, refRollMean20mList);
                GraphBuffer.Transfer(refHeaveAmplitudeMean20mBuffer, refHeaveAmplitudeMean20mList);

                GraphBuffer.Transfer(testPitchBuffer, testPitchList);
                GraphBuffer.Transfer(testRollBuffer, testRollList);
                GraphBuffer.Transfer(testHeaveBuffer, testHeaveList);

                GraphBuffer.Transfer(testPitchMean20mBuffer, testPitchMean20mList);
                GraphBuffer.Transfer(testRollMean20mBuffer, testRollMean20mList);
                GraphBuffer.Transfer(testHeaveAmplitudeMean20mBuffer, testHeaveAmplitudeMean20mList);

                // Fjerne gamle data fra chart data
                GraphBuffer.RemoveOldData(refPitchList, Constants.Minutes20 + Constants.ChartTimeCorrMin);
                GraphBuffer.RemoveOldData(refRollList, Constants.Minutes20 + Constants.ChartTimeCorrMin);
                GraphBuffer.RemoveOldData(refHeaveList, Constants.Minutes20 + Constants.ChartTimeCorrMin);

                GraphBuffer.RemoveOldData(refPitchMean20mList, Constants.Minutes20 + Constants.ChartTimeCorrMin);
                GraphBuffer.RemoveOldData(refRollMean20mList, Constants.Minutes20 + Constants.ChartTimeCorrMin);
                GraphBuffer.RemoveOldData(refHeaveAmplitudeMean20mList, Constants.Minutes20 + Constants.ChartTimeCorrMin);

                GraphBuffer.RemoveOldData(testPitchList, Constants.Minutes20 + Constants.ChartTimeCorrMin);
                GraphBuffer.RemoveOldData(testRollList, Constants.Minutes20 + Constants.ChartTimeCorrMin);
                GraphBuffer.RemoveOldData(testHeaveList, Constants.Minutes20 + Constants.ChartTimeCorrMin);

                GraphBuffer.RemoveOldData(testPitchMean20mList, Constants.Minutes20 + Constants.ChartTimeCorrMin);
                GraphBuffer.RemoveOldData(testRollMean20mList, Constants.Minutes20 + Constants.ChartTimeCorrMin);
                GraphBuffer.RemoveOldData(testHeaveAmplitudeMean20mList, Constants.Minutes20 + Constants.ChartTimeCorrMin);

                // Oppdatere alignment datetime (nåtid) til alle chart
                alignmentTime = DateTime.UtcNow;

                // Oppdatere aksene
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

            GraphBuffer.Update(mvsDataCollection.GetData(ValueType.Ref_PitchMean20m), refPitchMean20mBuffer);
            GraphBuffer.Update(mvsDataCollection.GetData(ValueType.Ref_RollMean20m), refRollMean20mBuffer);
            GraphBuffer.Update(mvsDataCollection.GetData(ValueType.Ref_HeaveAmplitudeMean20m), refHeaveAmplitudeMean20mBuffer);

            GraphBuffer.Update(mvsDataCollection.GetData(ValueType.Test_Pitch), testPitchBuffer);
            GraphBuffer.Update(mvsDataCollection.GetData(ValueType.Test_Roll), testRollBuffer);
            GraphBuffer.Update(mvsDataCollection.GetData(ValueType.Test_Heave), testHeaveBuffer);

            GraphBuffer.Update(mvsDataCollection.GetData(ValueType.Test_PitchMean20m), testPitchMean20mBuffer);
            GraphBuffer.Update(mvsDataCollection.GetData(ValueType.Test_RollMean20m), testRollMean20mBuffer);
            GraphBuffer.Update(mvsDataCollection.GetData(ValueType.Test_HeaveAmplitudeMean20m), testHeaveAmplitudeMean20mBuffer);

            // Oppdatere max verdier
            refPitchMax20mData = mvsDataCollection.GetData(ValueType.Ref_PitchMax20m);
            refPitchMaxUp20mData = mvsDataCollection.GetData(ValueType.Ref_PitchMaxUp20m);
            refPitchMaxDown20mData = mvsDataCollection.GetData(ValueType.Ref_PitchMaxDown20m);
            refRollMax20mData = mvsDataCollection.GetData(ValueType.Ref_RollMax20m);
            refRollMaxLeft20mData = mvsDataCollection.GetData(ValueType.Ref_RollMaxLeft20m);
            refRollMaxRight20mData = mvsDataCollection.GetData(ValueType.Ref_RollMaxRight20m);
            refHeaveMax20mData = mvsDataCollection.GetData(ValueType.Ref_HeaveMax20m);
            refHeaveAmplitudeMax20mData = mvsDataCollection.GetData(ValueType.Ref_HeaveAmplitudeMax20m);
            refHeaveAmplitudeMean20mData = mvsDataCollection.GetData(ValueType.Ref_HeaveAmplitudeMean20m);

            testPitchMax20mData = mvsDataCollection.GetData(ValueType.Test_PitchMax20m);
            testPitchMaxUp20mData = mvsDataCollection.GetData(ValueType.Test_PitchMaxUp20m);
            testPitchMaxDown20mData = mvsDataCollection.GetData(ValueType.Test_PitchMaxDown20m);
            testRollMax20mData = mvsDataCollection.GetData(ValueType.Test_RollMax20m);
            testRollMaxLeft20mData = mvsDataCollection.GetData(ValueType.Test_RollMaxLeft20m);
            testRollMaxRight20mData = mvsDataCollection.GetData(ValueType.Test_RollMaxRight20m);
            testRollMean20mData = mvsDataCollection.GetData(ValueType.Test_RollMean20m);
            testHeaveMax20mData = mvsDataCollection.GetData(ValueType.Test_HeaveMax20m);
            testHeaveAmplitudeMax20mData = mvsDataCollection.GetData(ValueType.Test_HeaveAmplitudeMax20m);
            testHeaveAmplitudeMean20mData = mvsDataCollection.GetData(ValueType.Test_HeaveAmplitudeMean20m);

            // Oppdatere mean verdier
            refPitchMean20mData = mvsDataCollection.GetData(ValueType.Ref_PitchMean20m);
            refRollMean20mData = mvsDataCollection.GetData(ValueType.Ref_RollMean20m);
            refHeaveMean20mData = mvsDataCollection.GetData(ValueType.Ref_HeaveAmplitudeMean20m);

            testPitchMean20mData = mvsDataCollection.GetData(ValueType.Test_PitchMean20m);
            testRollMean20mData = mvsDataCollection.GetData(ValueType.Test_RollMean20m);
            testHeaveMean20mData = mvsDataCollection.GetData(ValueType.Test_HeaveAmplitudeMean20m);
        }

        public void StartRecording()
        {
            ClearPitchData();
            ClearRollData();
            ClearHeaveData();

            ChartUpdateTimer.Start();
        }

        public void StopRecording()
        {
            ChartUpdateTimer.Stop();
        }

        public void ProcessSessionData()
        {
            double refMeanPitchSum = 0;
            double refMeanRollSum = 0;
            double refMeanHeaveSum = 0;
            double testMeanPitchSum = 0;
            double testMeanRollSum = 0;
            double testMeanHeaveSum = 0;

            // Slette gamle data i buffer lister
            refPitchBuffer.Clear();
            refPitchMean20mBuffer.Clear();
            testPitchBuffer.Clear();
            testPitchMean20mBuffer.Clear();

            refRollBuffer.Clear();
            refRollMean20mBuffer.Clear();
            testRollBuffer.Clear();
            testRollMean20mBuffer.Clear();

            refHeaveBuffer.Clear();
            refHeaveAmplitudeMean20mBuffer.Clear();
            testHeaveBuffer.Clear();
            testHeaveAmplitudeMean20mBuffer.Clear();

            foreach (SessionData sessionData in sessionDataList.ToList())
            {
                // Overføre database data til MVS input
                mvsInputData.TransferData(sessionData);

                // Prosessere data i input
                mvsProcessing.Update(mvsInputData, mainWindowVM);

                // Overføre data til grafer - Pitch
                refPitchBuffer.Add(new HMSData()
                {
                    data = mvsOutputData.GetData(ValueType.Ref_Pitch).data,
                    timestamp = sessionData.timestamp,
                    status = DataStatus.OK
                });

                refPitchMean20mBuffer.Add(new HMSData()
                {
                    data = mvsOutputData.GetData(ValueType.Ref_PitchMean20m).data,
                    timestamp = sessionData.timestamp,
                    status = DataStatus.OK
                });

                testPitchBuffer.Add(new HMSData()
                {
                    data = mvsOutputData.GetData(ValueType.Test_Pitch).data,
                    timestamp = sessionData.timestamp,
                    status = DataStatus.OK
                });

                testPitchMean20mBuffer.Add(new HMSData()
                {
                    data = mvsOutputData.GetData(ValueType.Test_PitchMean20m).data,
                    timestamp = sessionData.timestamp,
                    status = DataStatus.OK
                });

                // Overføre data til grafer - Roll
                refRollBuffer.Add(new HMSData()
                {
                    data = mvsOutputData.GetData(ValueType.Ref_Roll).data,
                    timestamp = sessionData.timestamp,
                    status = DataStatus.OK
                });

                refRollMean20mBuffer.Add(new HMSData()
                {
                    data = mvsOutputData.GetData(ValueType.Ref_RollMean20m).data,
                    timestamp = sessionData.timestamp,
                    status = DataStatus.OK
                });

                testRollBuffer.Add(new HMSData()
                {
                    data = mvsOutputData.GetData(ValueType.Test_Roll).data,
                    timestamp = sessionData.timestamp,
                    status = DataStatus.OK
                });

                testRollMean20mBuffer.Add(new HMSData()
                {
                    data = mvsOutputData.GetData(ValueType.Test_RollMean20m).data,
                    timestamp = sessionData.timestamp,
                    status = DataStatus.OK
                });

                // Overføre data til grafer - Heave
                refHeaveBuffer.Add(new HMSData()
                {
                    data = mvsOutputData.GetData(ValueType.Ref_Heave).data,
                    timestamp = sessionData.timestamp,
                    status = DataStatus.OK
                });

                refHeaveAmplitudeMean20mBuffer.Add(new HMSData()
                {
                    data = mvsOutputData.GetData(ValueType.Ref_HeaveAmplitudeMean20m).data,
                    timestamp = sessionData.timestamp,
                    status = DataStatus.OK
                });

                testHeaveBuffer.Add(new HMSData()
                {
                    data = mvsOutputData.GetData(ValueType.Test_Heave).data,
                    timestamp = sessionData.timestamp,
                    status = DataStatus.OK
                });

                testHeaveAmplitudeMean20mBuffer.Add(new HMSData()
                {
                    data = mvsOutputData.GetData(ValueType.Test_HeaveAmplitudeMean20m).data,
                    timestamp = sessionData.timestamp,
                    status = DataStatus.OK
                });

                // Beregner et totalt snitt av alle enkeltverdiene for pitch, roll og heave
                refMeanPitchSum += mvsOutputData.GetData(ValueType.Ref_Pitch).data;
                refMeanRollSum += mvsOutputData.GetData(ValueType.Ref_Roll).data;
                refMeanHeaveSum += mvsOutputData.GetData(ValueType.Ref_Heave).data;

                testMeanPitchSum += mvsOutputData.GetData(ValueType.Test_Pitch).data;
                testMeanRollSum += mvsOutputData.GetData(ValueType.Test_Roll).data;
                testMeanHeaveSum += mvsOutputData.GetData(ValueType.Test_Heave).data;
            }

            // Oppdatere max verdier
            refPitchMax20mData = mvsOutputData.GetData(ValueType.Ref_PitchMax20m);
            refPitchMaxUp20mData = mvsOutputData.GetData(ValueType.Ref_PitchMaxUp20m);
            refPitchMaxDown20mData = mvsOutputData.GetData(ValueType.Ref_PitchMaxDown20m);
            refRollMax20mData = mvsOutputData.GetData(ValueType.Ref_RollMax20m);
            refRollMaxLeft20mData = mvsOutputData.GetData(ValueType.Ref_RollMaxLeft20m);
            refRollMaxRight20mData = mvsOutputData.GetData(ValueType.Ref_RollMaxRight20m);
            refHeaveMax20mData = mvsOutputData.GetData(ValueType.Ref_HeaveMax20m);
            refHeaveAmplitudeMax20mData = mvsOutputData.GetData(ValueType.Ref_HeaveAmplitudeMax20m);
            refHeaveAmplitudeMean20mData = mvsOutputData.GetData(ValueType.Ref_HeaveAmplitudeMean20m);

            testPitchMax20mData = mvsOutputData.GetData(ValueType.Test_PitchMax20m);
            testPitchMaxUp20mData = mvsOutputData.GetData(ValueType.Test_PitchMaxUp20m);
            testPitchMaxDown20mData = mvsOutputData.GetData(ValueType.Test_PitchMaxDown20m);
            testRollMax20mData = mvsOutputData.GetData(ValueType.Test_RollMax20m);
            testRollMaxLeft20mData = mvsOutputData.GetData(ValueType.Test_RollMaxLeft20m);
            testRollMaxRight20mData = mvsOutputData.GetData(ValueType.Test_RollMaxRight20m);
            testRollMean20mData = mvsOutputData.GetData(ValueType.Test_RollMean20m);
            testHeaveMax20mData = mvsOutputData.GetData(ValueType.Test_HeaveMax20m);
            testHeaveAmplitudeMax20mData = mvsOutputData.GetData(ValueType.Test_HeaveAmplitudeMax20m);
            testHeaveAmplitudeMean20mData = mvsOutputData.GetData(ValueType.Test_HeaveAmplitudeMean20m);

            // Oppdatere mean verdier
            HMSData meanData = new HMSData();

            meanData.data = refMeanPitchSum / sessionDataList.Count;
            meanData.status = DataStatus.OK;
            refPitchMean20mData = meanData;

            meanData.data = refMeanRollSum / sessionDataList.Count;
            meanData.status = DataStatus.OK;
            refRollMean20mData = meanData;

            meanData.data = refMeanHeaveSum / sessionDataList.Count;
            meanData.status = DataStatus.OK;
            refHeaveMean20mData = meanData;

            meanData.data = testMeanPitchSum / sessionDataList.Count;
            meanData.status = DataStatus.OK;
            testPitchMean20mData = meanData;

            meanData.data = testMeanRollSum / sessionDataList.Count;
            meanData.status = DataStatus.OK;
            testRollMean20mData = meanData;

            meanData.data = testMeanHeaveSum / sessionDataList.Count;
            meanData.status = DataStatus.OK;
            testHeaveMean20mData = meanData;

            // Oppdatere alignment datetime til alle chart
            alignmentTime = sessionDataList.Last().timestamp;

            // Oppdatere aksene
            OnPropertyChanged(nameof(pitchChartAxisMax20m));
            OnPropertyChanged(nameof(pitchChartAxisMin20m));

            OnPropertyChanged(nameof(rollChartAxisMax20m));
            OnPropertyChanged(nameof(rollChartAxisMin20m));

            OnPropertyChanged(nameof(heaveChartAxisMax20m));
            OnPropertyChanged(nameof(heaveChartAxisMin20m));
            OnPropertyChanged(nameof(heaveAmplitudeChartAxisMax20m));
        }

        public void TransferToDisplay()
        {
            // Slette gamle data i graf data lister
            refPitchList.Clear();
            refPitchMean20mList.Clear();
            testPitchList.Clear();
            testPitchMean20mList.Clear();

            refRollList.Clear();
            refRollMean20mList.Clear();
            testRollList.Clear();
            testRollMean20mList.Clear();

            refHeaveList.Clear();
            refHeaveAmplitudeMean20mList.Clear();
            testHeaveList.Clear();
            testHeaveAmplitudeMean20mList.Clear();

            // Overføre data fra buffer til chart data
            GraphBuffer.Transfer(refPitchBuffer, refPitchList);
            GraphBuffer.Transfer(refRollBuffer, refRollList);
            GraphBuffer.Transfer(refHeaveBuffer, refHeaveList);

            GraphBuffer.Transfer(refPitchMean20mBuffer, refPitchMean20mList);
            GraphBuffer.Transfer(refRollMean20mBuffer, refRollMean20mList);
            GraphBuffer.Transfer(refHeaveAmplitudeMean20mBuffer, refHeaveAmplitudeMean20mList);

            GraphBuffer.Transfer(testPitchBuffer, testPitchList);
            GraphBuffer.Transfer(testRollBuffer, testRollList);
            GraphBuffer.Transfer(testHeaveBuffer, testHeaveList);

            GraphBuffer.Transfer(testPitchMean20mBuffer, testPitchMean20mList);
            GraphBuffer.Transfer(testRollMean20mBuffer, testRollMean20mList);
            GraphBuffer.Transfer(testHeaveAmplitudeMean20mBuffer, testHeaveAmplitudeMean20mList);
        }

        /////////////////////////////////////////////////////////////////////////////
        // Pitch
        /////////////////////////////////////////////////////////////////////////////
        private void InitPitchData()
        {
            _pitchData = new HMSData();

            _refPitchMax20mData = new HMSData();
            _refPitchMaxUp20mData = new HMSData();
            _refPitchMaxDown20mData = new HMSData();
            _refPitchMean20mData = new HMSData();

            _testPitchMax20mData = new HMSData();
            _testPitchMaxUp20mData = new HMSData();
            _testPitchMaxDown20mData = new HMSData();
            _testPitchMean20mData = new HMSData();

        }

        private void ClearPitchData()
        {
            HMSData noData = new HMSData()
            {
                data = double.NaN,
                status = DataStatus.NONE
            };

            refPitchMaxUp20mData = noData;
            refPitchMaxDown20mData = noData;
            refPitchMean20mData = noData;

            testPitchMaxUp20mData = noData;
            testPitchMaxDown20mData = noData;
            testPitchMean20mData = noData;

            refPitchBuffer.Clear();
            refPitchMean20mBuffer.Clear();
            refPitchList.Clear();
            refPitchMean20mList.Clear();

            testPitchBuffer.Clear();
            testPitchMean20mBuffer.Clear();
            testPitchList.Clear();
            testPitchMean20mList.Clear();

            for (int i = -Constants.Minutes20; i <= 0; i++)
            {
                refPitchMean20mList.Add(new HMSData()
                {
                    data = 0,
                    timestamp = DateTime.UtcNow.AddSeconds(i)
                });

                testPitchMean20mList.Add(new HMSData()
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
                    if (_refPitchMaxUp20mData.status == DataStatus.OK && !double.IsNaN(_refPitchMaxUp20mData.data))
                    {
                        double pitch = Math.Abs(_refPitchMaxUp20mData.data);

                        string dir;
                        if (_refPitchMaxUp20mData.data > 0)
                            dir = "U";
                        else
                            dir = "D";

                        return string.Format("{0} ° {1}", Math.Round(pitch, 3, MidpointRounding.AwayFromZero).ToString(Constants.numberFormatRecData), dir);
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
                    if (_testPitchMaxUp20mData.status == DataStatus.OK && !double.IsNaN(_testPitchMaxUp20mData.data))
                    {
                        double pitch = Math.Abs(_testPitchMaxUp20mData.data);

                        string dir;
                        if (_testPitchMaxUp20mData.data > 0)
                            dir = "U";
                        else
                            dir = "D";

                        return string.Format("{0} ° {1}", Math.Round(pitch, 3, MidpointRounding.AwayFromZero).ToString(Constants.numberFormatRecData), dir);
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
                    if (_refPitchMaxDown20mData.status == DataStatus.OK && !double.IsNaN(_refPitchMaxDown20mData.data))
                    {
                        double pitch = Math.Abs(_refPitchMaxDown20mData.data);

                        string dir;
                        if (_refPitchMaxDown20mData.data > 0)
                            dir = "U";
                        else
                            dir = "D";

                        return string.Format("{0} ° {1}", Math.Round(pitch, 3, MidpointRounding.AwayFromZero).ToString(Constants.numberFormatRecData), dir);
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
                    if (_testPitchMaxDown20mData.status == DataStatus.OK && !double.IsNaN(_testPitchMaxDown20mData.data))
                    {
                        double pitch = Math.Abs(_testPitchMaxDown20mData.data);

                        string dir;
                        if (_testPitchMaxDown20mData.data > 0)
                            dir = "U";
                        else
                            dir = "D";

                        return string.Format("{0} ° {1}", Math.Round(pitch, 3, MidpointRounding.AwayFromZero).ToString(Constants.numberFormatRecData), dir);
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


        private HMSData _refPitchMean20mData { get; set; }
        public HMSData refPitchMean20mData
        {
            get
            {
                return _refPitchMean20mData;
            }
            set
            {
                if (value != null)
                {
                    _refPitchMean20mData.Set(value);

                    OnPropertyChanged(nameof(refPitchMean20mDataString));
                    OnPropertyChanged(nameof(pitchMeanDeviationString));
                }
            }
        }
        public string refPitchMean20mDataString
        {
            get
            {
                if (_refPitchMean20mData != null)
                {
                    // Sjekke om data er gyldig
                    if (_refPitchMean20mData.status == DataStatus.OK && !double.IsNaN(_refPitchMean20mData.data))
                    {
                        return string.Format("{0} °", Math.Round(_refPitchMean20mData.data, 3, MidpointRounding.AwayFromZero).ToString(Constants.numberFormatRecData));
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

        private HMSData _testPitchMean20mData { get; set; }
        public HMSData testPitchMean20mData
        {
            get
            {
                return _testPitchMean20mData;
            }
            set
            {
                if (value != null)
                {
                    _testPitchMean20mData.Set(value);

                    OnPropertyChanged(nameof(testPitchMean20mDataString));
                    OnPropertyChanged(nameof(pitchMeanDeviationString)); 
                }
            }
        }
        public string testPitchMean20mDataString
        {
            get
            {
                if (_testPitchMean20mData != null)
                {
                    // Sjekke om data er gyldig
                    if (_testPitchMean20mData.status == DataStatus.OK && !double.IsNaN(_testPitchMean20mData.data))
                    {
                        return string.Format("{0} °", Math.Round(_testPitchMean20mData.data, 3, MidpointRounding.AwayFromZero).ToString(Constants.numberFormatRecData));
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

        public string pitchMeanDeviationString
        {
            get
            {
                if (_testPitchMean20mData.status == DataStatus.OK && !double.IsNaN(_testPitchMean20mData.data) &&
                    _refPitchMean20mData.status == DataStatus.OK && !double.IsNaN(_refPitchMean20mData.data))
                {
                    return Math.Round(_testPitchMean20mData.data - _refPitchMean20mData.data, 3, MidpointRounding.AwayFromZero).ToString(Constants.numberFormatRecDataSigned);
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
        public void InitRollData()
        {
            _refRollData = new HMSData();

            _refRollMax20mData = new HMSData();
            _refRollMaxLeft20mData = new HMSData();
            _refRollMaxRight20mData = new HMSData();
            _refRollMean20mData = new HMSData();

            _testRollMax20mData = new HMSData();
            _testRollMaxLeft20mData = new HMSData();
            _testRollMaxRight20mData = new HMSData();
            _testRollMean20mData = new HMSData();

            // Init av chart data
            refRollMean20mList.Clear();
            testRollMean20mList.Clear();

            for (int i = -Constants.Minutes20; i <= 0; i++)
            {
                refRollMean20mList.Add(new HMSData()
                {
                    data = 0,
                    timestamp = DateTime.UtcNow.AddSeconds(i)
                });

               testRollMean20mList.Add(new HMSData()
                {
                    data = 0,
                    timestamp = DateTime.UtcNow.AddSeconds(i)
                });
            }
        }

        private void ClearRollData()
        {
            HMSData noData = new HMSData()
            {
                data = double.NaN,
                status = DataStatus.NONE
            };

            refRollMaxLeft20mData = noData;
            refRollMaxRight20mData = noData;
            refRollMean20mData = noData;

            testRollMaxLeft20mData = noData;
            testRollMaxRight20mData = noData;
            testRollMean20mData = noData;

            refRollBuffer.Clear();
            refRollMean20mBuffer.Clear();
            refRollList.Clear();
            refRollMean20mList.Clear();

            testRollBuffer.Clear();
            testRollMean20mBuffer.Clear();
            testRollList.Clear();
            testRollMean20mList.Clear();

            for (int i = -Constants.Minutes20; i <= 0; i++)
            {
                refRollMean20mList.Add(new HMSData()
                {
                    data = 0,
                    timestamp = DateTime.UtcNow.AddSeconds(i)
                });

                testRollMean20mList.Add(new HMSData()
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
                    if (_refRollMaxLeft20mData.status == DataStatus.OK && !double.IsNaN(_refRollMaxLeft20mData.data))
                    {
                        double roll = Math.Abs(_refRollMaxLeft20mData.data);

                        string dir;
                        if (_refRollMaxLeft20mData.data >= 0)
                            dir = "L";
                        else
                            dir = "R";

                        return string.Format("{0} ° {1}", Math.Round(roll, 3, MidpointRounding.AwayFromZero).ToString(Constants.numberFormatRecData), dir);
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
                    if (_testRollMaxLeft20mData.status == DataStatus.OK && !double.IsNaN(_testRollMaxLeft20mData.data))
                    {
                        double roll = Math.Abs(_testRollMaxLeft20mData.data);

                        string dir;
                        if (_testRollMaxLeft20mData.data >= 0)
                            dir = "L";
                        else
                            dir = "R";

                        return string.Format("{0} ° {1}", Math.Round(roll, 3, MidpointRounding.AwayFromZero).ToString(Constants.numberFormatRecData), dir);
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
                    if (_refRollMaxRight20mData.status == DataStatus.OK && !double.IsNaN(_refRollMaxRight20mData.data))
                    {
                        double roll = Math.Abs(_refRollMaxRight20mData.data);

                        string dir;
                        if (_refRollMaxRight20mData.data >= 0)
                            dir = "L";
                        else
                            dir = "R";

                        return string.Format("{0} ° {1}", Math.Round(roll, 3, MidpointRounding.AwayFromZero).ToString(Constants.numberFormatRecData), dir);
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
                    if (_testRollMaxRight20mData.status == DataStatus.OK && !double.IsNaN(_testRollMaxRight20mData.data))
                    {
                        double roll = Math.Abs(_testRollMaxRight20mData.data);

                        string dir;
                        if (_testRollMaxRight20mData.data >= 0)
                            dir = "L";
                        else
                            dir = "R";

                        return string.Format("{0} ° {1}", Math.Round(roll, 3, MidpointRounding.AwayFromZero).ToString(Constants.numberFormatRecData), dir);
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

        private HMSData _refRollMean20mData { get; set; }
        public HMSData refRollMean20mData
        {
            get
            {
                return _refRollMean20mData;
            }
            set
            {
                if (value != null)
                {
                    _refRollMean20mData.Set(value);

                    OnPropertyChanged(nameof(refRollMean20mDataString));
                    OnPropertyChanged(nameof(rollMeanDeviationString));
                }
            }
        }
        public string refRollMean20mDataString
        {
            get
            {
                if (_refRollMean20mData != null)
                {
                    // Sjekke om data er gyldig
                    if (_refRollMean20mData.status == DataStatus.OK && !double.IsNaN(_refRollMean20mData.data))
                    {
                        return string.Format("{0} °", Math.Round(_refRollMean20mData.data, 3, MidpointRounding.AwayFromZero).ToString(Constants.numberFormatRecData));
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

        private HMSData _testRollMean20mData { get; set; }
        public HMSData testRollMean20mData
        {
            get
            {
                return _testRollMean20mData;
            }
            set
            {
                if (value != null)
                {
                    _testRollMean20mData.Set(value);

                    OnPropertyChanged(nameof(testRollMean20mDataString));
                    OnPropertyChanged(nameof(rollMeanDeviationString));                    
                }
            }
        }
        public string testRollMean20mDataString
        {
            get
            {
                if (_testRollMean20mData != null)
                {
                    // Sjekke om data er gyldig
                    if (_testRollMean20mData.status == DataStatus.OK && !double.IsNaN(_testRollMean20mData.data))
                    {
                        return string.Format("{0} °", Math.Round(_testRollMean20mData.data, 3, MidpointRounding.AwayFromZero).ToString(Constants.numberFormatRecData));
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

        public string rollMeanDeviationString
        {
            get
            {
                if (_testRollMean20mData.status == DataStatus.OK && !double.IsNaN(_testRollMean20mData.data) &&
                    _refRollMean20mData.status == DataStatus.OK && !double.IsNaN(_refRollMean20mData.data))
                {
                    return Math.Round(_testRollMean20mData.data - _refRollMean20mData.data, 3, MidpointRounding.AwayFromZero).ToString(Constants.numberFormatRecDataSigned);
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
            _refHeaveAmplitudeMean20mData = new HMSData();
            _refHeaveMean20mData = new HMSData();

            _testHeaveMax20mData = new HMSData();
            _testHeaveAmplitudeData = new HMSData();
            _testHeaveAmplitudeMax20mData = new HMSData();
            _testHeaveAmplitudeMean20mData = new HMSData();
            _testHeaveMean20mData = new HMSData();

            // Init av chart data
            refHeaveAmplitudeMean20mList.Clear();
            testHeaveAmplitudeMean20mList.Clear();

            for (int i = -Constants.Minutes20; i <= 0; i++)
            {
                refHeaveAmplitudeMean20mList.Add(new HMSData()
                {
                    data = 0,
                    timestamp = DateTime.UtcNow.AddSeconds(i)
                });

                testHeaveAmplitudeMean20mList.Add(new HMSData()
                {
                    data = 0,
                    timestamp = DateTime.UtcNow.AddSeconds(i)
                });
            }
        }

        private void ClearHeaveData()
        {
            HMSData noData = new HMSData()
            {
                data = double.NaN,
                status = DataStatus.NONE
            };

            refHeaveMax20mData = noData;
            refHeaveAmplitudeMax20mData = noData;
            refHeaveMean20mData = noData;

            testHeaveMax20mData = noData;
            testHeaveAmplitudeMax20mData = noData;
            testHeaveMean20mData = noData;

            refHeaveBuffer.Clear();
            refHeaveAmplitudeMean20mBuffer.Clear();
            refHeaveList.Clear();
            refHeaveAmplitudeMean20mList.Clear();

            testHeaveBuffer.Clear();
            testHeaveAmplitudeMean20mBuffer.Clear();
            testHeaveList.Clear();
            testHeaveAmplitudeMean20mList.Clear();

            for (int i = -Constants.Minutes20; i <= 0; i++)
            {
                refHeaveAmplitudeMean20mList.Add(new HMSData()
                {
                    data = 0,
                    timestamp = DateTime.UtcNow.AddSeconds(i)
                });

                testHeaveAmplitudeMean20mList.Add(new HMSData()
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
                        return string.Format("{0} m", Math.Round(_refHeaveMax20mData.data, 3, MidpointRounding.AwayFromZero).ToString(Constants.numberFormatRecData));
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
                        return string.Format("{0} m", Math.Round(_testHeaveMax20mData.data, 3, MidpointRounding.AwayFromZero).ToString(Constants.numberFormatRecData));
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
                    if (_refHeaveAmplitudeMax20mData.status == DataStatus.OK && !double.IsNaN(_refHeaveAmplitudeMax20mData.data))
                    {
                        return string.Format("{0} m", Math.Round(_refHeaveAmplitudeMax20mData.data, 3, MidpointRounding.AwayFromZero).ToString(Constants.numberFormatRecData));
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
                    if (_testHeaveAmplitudeMax20mData.status == DataStatus.OK && !double.IsNaN(_testHeaveAmplitudeMax20mData.data))
                    {
                        return string.Format("{0} m", Math.Round(_testHeaveAmplitudeMax20mData.data, 3, MidpointRounding.AwayFromZero).ToString(Constants.numberFormatRecData));
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

        private HMSData _refHeaveAmplitudeMean20mData { get; set; }
        public HMSData refHeaveAmplitudeMean20mData
        {
            get
            {
                return _refHeaveAmplitudeMean20mData;
            }
            set
            {
                if (value != null)
                {
                    _refHeaveAmplitudeMean20mData.Set(value);

                    OnPropertyChanged(nameof(refHeaveAmplitudeMean20mString));
                    OnPropertyChanged(nameof(heaveAmplitudeMeanDeviationString));
                }
            }
        }
        public string refHeaveAmplitudeMean20mString
        {
            get
            {
                if (_refHeaveAmplitudeMean20mData != null)
                {
                    // Sjekke om data er gyldig
                    if (_refHeaveAmplitudeMean20mData.status == DataStatus.OK && !double.IsNaN(_refHeaveAmplitudeMean20mData.data))
                    {
                        return string.Format("{0} m", Math.Round(_refHeaveAmplitudeMean20mData.data, 3, MidpointRounding.AwayFromZero).ToString(Constants.numberFormatRecData));
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

        private HMSData _testHeaveAmplitudeMean20mData { get; set; }
        public HMSData testHeaveAmplitudeMean20mData
        {
            get
            {
                return _testHeaveAmplitudeMean20mData;
            }
            set
            {
                if (value != null)
                {
                    _testHeaveAmplitudeMean20mData.Set(value);

                    OnPropertyChanged(nameof(testHeaveAmplitudeMean20mString));
                    OnPropertyChanged(nameof(heaveAmplitudeMeanDeviationString));
                }
            }
        }
        public string testHeaveAmplitudeMean20mString
        {
            get
            {
                if (_testHeaveAmplitudeMean20mData != null)
                {
                    // Sjekke om data er gyldig
                    if (_testHeaveAmplitudeMean20mData.status == DataStatus.OK && !double.IsNaN(_testHeaveAmplitudeMean20mData.data))
                    {
                        return string.Format("{0} m", Math.Round(_testHeaveAmplitudeMean20mData.data, 3, MidpointRounding.AwayFromZero).ToString(Constants.numberFormatRecData));
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

        private HMSData _refHeaveMean20mData { get; set; }
        public HMSData refHeaveMean20mData
        {
            get
            {
                return _refHeaveMean20mData;
            }
            set
            {
                if (value != null)
                {
                    _refHeaveMean20mData.Set(value);

                    OnPropertyChanged(nameof(refHeaveMean20mDataString));
                }
            }
        }
        public string refHeaveMean20mDataString
        {
            get
            {
                if (_refHeaveMean20mData != null)
                {
                    // Sjekke om data er gyldig
                    if (_refHeaveMean20mData.status == DataStatus.OK)
                    {
                        return string.Format("{0} m", Math.Round(_refHeaveMean20mData.data, 3, MidpointRounding.AwayFromZero).ToString(Constants.numberFormatRecData));
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

        private HMSData _testHeaveMean20mData { get; set; }
        public HMSData testHeaveMean20mData
        {
            get
            {
                return _testHeaveMean20mData;
            }
            set
            {
                if (value != null)
                {
                    _testHeaveMean20mData.Set(value);

                    OnPropertyChanged(nameof(testHeaveMean20mDataString));
                }
            }
        }
        public string testHeaveMean20mDataString
        {
            get
            {
                if (_testHeaveMean20mData != null)
                {
                    // Sjekke om data er gyldig
                    if (_testHeaveMean20mData.status == DataStatus.OK)
                    {
                        return string.Format("{0} m", Math.Round(_testHeaveMean20mData.data, 3, MidpointRounding.AwayFromZero).ToString(Constants.numberFormatRecData));
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

        public string heaveAmplitudeMeanDeviationString
        {
            get
            {
                if (_testHeaveAmplitudeMean20mData.status == DataStatus.OK && !double.IsNaN(_testHeaveAmplitudeMean20mData.data) &&
                    _refHeaveAmplitudeMean20mData.status == DataStatus.OK && !double.IsNaN(_refHeaveAmplitudeMean20mData.data))
                {
                    return Math.Round(_testHeaveAmplitudeMean20mData.data - _refHeaveAmplitudeMean20mData.data, 3, MidpointRounding.AwayFromZero).ToString(Constants.numberFormatRecDataSigned);
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
                if (mainWindowVM.OperationsMode == OperationsMode.Analysis && _alignmentTime != DateTime.MinValue)
                    return _alignmentTime.AddSeconds(-Constants.Minutes20);
                else
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
                return ChartAxisMax(_refPitchMax20mData, _testPitchMax20mData, Constants.PitchAxisMargin, 2);
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
                return ChartAxisMax(_refRollMax20mData, _testRollMax20mData, Constants.RollAxisMargin, 2);
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
                return ChartAxisMax(_refHeaveMax20mData, _testHeaveMax20mData, Constants.HeaveAmplitudeAxisMargin, 2);
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
                return ChartAxisMax(_refHeaveAmplitudeMax20mData, _testHeaveAmplitudeMax20mData, Constants.HeaveAmplitudeAxisMargin, 4);
            }
        }

        // Variabel oppdatert
        // Dersom navn ikke er satt brukes kallende medlem sitt navn
        protected virtual void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private double ChartAxisMax(HMSData refData, HMSData testData, double margin, double defaultValue)
        {
            if (!double.IsNaN(refData.data) && !double.IsNaN(testData.data))
            {
                if (refData.data > testData.data)
                    return (double)((Math.Round(refData.data * (1 / margin), MidpointRounding.AwayFromZero) / (1 / margin)) + margin);
                else
                    return (double)((Math.Round(testData.data * (1 / margin), MidpointRounding.AwayFromZero) / (1 / margin)) + margin);
            }
            else
            {
                return defaultValue;
            }
        }
    }
}
