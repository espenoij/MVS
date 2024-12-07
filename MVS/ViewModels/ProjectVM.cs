﻿using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Threading;
using Telerik.Windows.Data;

namespace MVS
{
    public class ProjectVM : INotifyPropertyChanged
    {
        // Change notification
        public event PropertyChangedEventHandler PropertyChanged;

        private MainWindowVM mainWindowVM;
        private MVSProcessing mvsProcessing;
        private MVSDataCollection mvsInputData;
        private MVSDataCollection mvsOutputData;

        private DispatcherTimer chartUpdateTimer = new DispatcherTimer();

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
        private RadObservableCollection<HMSData> refPitchMeanBuffer = new RadObservableCollection<HMSData>();
        private RadObservableCollection<HMSData> refRollMeanBuffer = new RadObservableCollection<HMSData>();
        private RadObservableCollection<HMSData> refHeaveMeanBuffer = new RadObservableCollection<HMSData>();

        public RadObservableCollection<HMSData> refPitchMeanList = new RadObservableCollection<HMSData>();
        public RadObservableCollection<HMSData> refRollMeanList = new RadObservableCollection<HMSData>();
        public RadObservableCollection<HMSData> refHeaveMeanList = new RadObservableCollection<HMSData>();

        private RadObservableCollection<HMSData> testPitchMeanBuffer = new RadObservableCollection<HMSData>();
        private RadObservableCollection<HMSData> testRollMeanBuffer = new RadObservableCollection<HMSData>();
        private RadObservableCollection<HMSData> testHeaveMeanBuffer = new RadObservableCollection<HMSData>();

        public RadObservableCollection<HMSData> testPitchMeanList = new RadObservableCollection<HMSData>();
        public RadObservableCollection<HMSData> testRollMeanList = new RadObservableCollection<HMSData>();
        public RadObservableCollection<HMSData> testHeaveMeanList = new RadObservableCollection<HMSData>();

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
            chartUpdateTimer.Interval = TimeSpan.FromMilliseconds(Constants.ChartUpdateFrequencyUIDefault);
            chartUpdateTimer.Tick += ChartDataUpdate;

            void ChartDataUpdate(object sender, EventArgs e)
            {
                // Overføre data fra buffer til chart data
                GraphBuffer.Transfer(refPitchBuffer, refPitchList);
                GraphBuffer.Transfer(refRollBuffer, refRollList);
                GraphBuffer.Transfer(refHeaveBuffer, refHeaveList);

                GraphBuffer.Transfer(refPitchMeanBuffer, refPitchMeanList);
                GraphBuffer.Transfer(refRollMeanBuffer, refRollMeanList);
                GraphBuffer.Transfer(refHeaveMeanBuffer, refHeaveMeanList);

                GraphBuffer.Transfer(testPitchBuffer, testPitchList);
                GraphBuffer.Transfer(testRollBuffer, testRollList);
                GraphBuffer.Transfer(testHeaveBuffer, testHeaveList);

                GraphBuffer.Transfer(testPitchMeanBuffer, testPitchMeanList);
                GraphBuffer.Transfer(testRollMeanBuffer, testRollMeanList);
                GraphBuffer.Transfer(testHeaveMeanBuffer, testHeaveMeanList);

                // Fjerne gamle data fra chart data
                GraphBuffer.RemoveOldData(refPitchList, Constants.Minutes20 + Constants.ChartTimeCorrMin);
                GraphBuffer.RemoveOldData(refRollList, Constants.Minutes20 + Constants.ChartTimeCorrMin);
                GraphBuffer.RemoveOldData(refHeaveList, Constants.Minutes20 + Constants.ChartTimeCorrMin);

                GraphBuffer.RemoveOldData(refPitchMeanList, Constants.Minutes20 + Constants.ChartTimeCorrMin);
                GraphBuffer.RemoveOldData(refRollMeanList, Constants.Minutes20 + Constants.ChartTimeCorrMin);
                GraphBuffer.RemoveOldData(refHeaveMeanList, Constants.Minutes20 + Constants.ChartTimeCorrMin);

                GraphBuffer.RemoveOldData(testPitchList, Constants.Minutes20 + Constants.ChartTimeCorrMin);
                GraphBuffer.RemoveOldData(testRollList, Constants.Minutes20 + Constants.ChartTimeCorrMin);
                GraphBuffer.RemoveOldData(testHeaveList, Constants.Minutes20 + Constants.ChartTimeCorrMin);

                GraphBuffer.RemoveOldData(testPitchMeanList, Constants.Minutes20 + Constants.ChartTimeCorrMin);
                GraphBuffer.RemoveOldData(testRollMeanList, Constants.Minutes20 + Constants.ChartTimeCorrMin);
                GraphBuffer.RemoveOldData(testHeaveMeanList, Constants.Minutes20 + Constants.ChartTimeCorrMin);

                // Oppdatere alignment datetime (nåtid) til alle chart
                alignmentTime = DateTime.UtcNow;

                // Oppdatere aksene
                OnPropertyChanged(nameof(pitchChartAxisMax));
                OnPropertyChanged(nameof(pitchChartAxisMin));

                OnPropertyChanged(nameof(rollChartAxisMax));
                OnPropertyChanged(nameof(rollChartAxisMin));

                OnPropertyChanged(nameof(heaveChartAxisMax));
                OnPropertyChanged(nameof(heaveChartAxisMin));

                OnPropertyChanged(nameof(meanPitchChartAxisMax));
                OnPropertyChanged(nameof(meanPitchChartAxisMin));

                OnPropertyChanged(nameof(meanRollChartAxisMax));
                OnPropertyChanged(nameof(meanRollChartAxisMin));

                OnPropertyChanged(nameof(meanHeaveChartAxisMax));
                OnPropertyChanged(nameof(meanHeaveChartAxisMin));
            }
        }

        public void UpdateData(MVSDataCollection mvsDataCollection)
        {
            // Oppdatere data som skal ut i grafer
            GraphBuffer.Update(mvsDataCollection.GetData(ValueType.Ref_Pitch), refPitchBuffer);
            GraphBuffer.Update(mvsDataCollection.GetData(ValueType.Ref_Roll), refRollBuffer);
            GraphBuffer.Update(mvsDataCollection.GetData(ValueType.Ref_Heave), refHeaveBuffer);

            GraphBuffer.Update(mvsDataCollection.GetData(ValueType.Ref_PitchMean), refPitchMeanBuffer);
            GraphBuffer.Update(mvsDataCollection.GetData(ValueType.Ref_RollMean), refRollMeanBuffer);
            GraphBuffer.Update(mvsDataCollection.GetData(ValueType.Ref_HeaveMean), refHeaveMeanBuffer);

            GraphBuffer.Update(mvsDataCollection.GetData(ValueType.Test_Pitch), testPitchBuffer);
            GraphBuffer.Update(mvsDataCollection.GetData(ValueType.Test_Roll), testRollBuffer);
            GraphBuffer.Update(mvsDataCollection.GetData(ValueType.Test_Heave), testHeaveBuffer);

            GraphBuffer.Update(mvsDataCollection.GetData(ValueType.Test_PitchMean), testPitchMeanBuffer);
            GraphBuffer.Update(mvsDataCollection.GetData(ValueType.Test_RollMean), testRollMeanBuffer);
            GraphBuffer.Update(mvsDataCollection.GetData(ValueType.Test_HeaveMean), testHeaveMeanBuffer);

            // Oppdatere max verdier
            refPitchMaxData = mvsDataCollection.GetData(ValueType.Ref_PitchMax);
            refPitchMaxUpData = mvsDataCollection.GetData(ValueType.Ref_PitchMaxUp);
            refPitchMaxDownData = mvsDataCollection.GetData(ValueType.Ref_PitchMaxDown);
            refRollMaxData = mvsDataCollection.GetData(ValueType.Ref_RollMax);
            refRollMaxLeftData = mvsDataCollection.GetData(ValueType.Ref_RollMaxLeft);
            refRollMaxRightData = mvsDataCollection.GetData(ValueType.Ref_RollMaxRight);
            refHeaveMaxData = mvsDataCollection.GetData(ValueType.Ref_HeaveMax);

            testPitchMaxData = mvsDataCollection.GetData(ValueType.Test_PitchMax);
            testPitchMaxUpData = mvsDataCollection.GetData(ValueType.Test_PitchMaxUp);
            testPitchMaxDownData = mvsDataCollection.GetData(ValueType.Test_PitchMaxDown);
            testRollMaxData = mvsDataCollection.GetData(ValueType.Test_RollMax);
            testRollMaxLeftData = mvsDataCollection.GetData(ValueType.Test_RollMaxLeft);
            testRollMaxRightData = mvsDataCollection.GetData(ValueType.Test_RollMaxRight);
            testRollMeanData = mvsDataCollection.GetData(ValueType.Test_RollMean);
            testHeaveMaxData = mvsDataCollection.GetData(ValueType.Test_HeaveMax);
            testHeaveMeanData = mvsDataCollection.GetData(ValueType.Test_HeaveMean);

            // Oppdatere mean verdier
            refPitchMeanData = mvsDataCollection.GetData(ValueType.Ref_PitchMean);
            refPitchMeanMaxData = mvsDataCollection.GetData(ValueType.Ref_PitchMeanMax);
            
            refRollMeanData = mvsDataCollection.GetData(ValueType.Ref_RollMean);

            refHeaveMeanData = mvsDataCollection.GetData(ValueType.Ref_HeaveMean);

            testPitchMeanData = mvsDataCollection.GetData(ValueType.Test_PitchMean);
            testPitchMeanMaxData = mvsDataCollection.GetData(ValueType.Test_PitchMeanMax);

            testRollMeanData = mvsDataCollection.GetData(ValueType.Test_RollMean);
            testHeaveMeanData = mvsDataCollection.GetData(ValueType.Test_HeaveMean);
        }

        public void StartRecording()
        {
            ClearPitchData();
            ClearRollData();
            ClearHeaveData();

            chartUpdateTimer.Start();
        }

        public void StopRecording()
        {
            chartUpdateTimer.Stop();
        }

        public void AnalyseProjectData()
        {
            // Slette gamle data i output felt
            HMSData noData = new HMSData()
            {
                data = double.NaN,
                status = DataStatus.NONE
            };

            refPitchMaxUpData = noData;
            refPitchMaxDownData = noData;
            refPitchMeanData = noData;
            testPitchMaxUpData = noData;
            testPitchMaxDownData = noData;
            testPitchMeanData = noData;

            refRollMaxLeftData = noData;
            refRollMaxRightData = noData;
            refRollMeanData = noData;
            testRollMaxLeftData = noData;
            testRollMaxRightData = noData;
            testRollMeanData = noData;

            refHeaveMaxData = noData;
            refHeaveMeanData = noData;
            testHeaveMaxData = noData;
            testHeaveMeanData = noData;

            // Slette gamle data i buffer lister
            refPitchBuffer.Clear();
            refPitchMeanBuffer.Clear();
            testPitchBuffer.Clear();
            testPitchMeanBuffer.Clear();

            refRollBuffer.Clear();
            refRollMeanBuffer.Clear();
            testRollBuffer.Clear();
            testRollMeanBuffer.Clear();

            refHeaveBuffer.Clear();
            refHeaveMeanBuffer.Clear();
            testHeaveBuffer.Clear();
            testHeaveMeanBuffer.Clear();

            foreach (SessionData sessionData in sessionDataList.ToList())
            {
                // Overføre database data til MVS input
                mvsInputData.TransferData(sessionData);

                // Prosessere data i input
                mvsProcessing.Update(mvsInputData, mainWindowVM);

                // Overføre data til grafer - Pitch
                if (mvsOutputData.GetData(ValueType.Ref_Pitch).status == DataStatus.OK)
                {
                    refPitchBuffer.Add(new HMSData()
                    {
                        data = mvsOutputData.GetData(ValueType.Ref_Pitch).data,
                        timestamp = sessionData.timestamp,
                        status = DataStatus.OK
                    });
                }

                if (mvsOutputData.GetData(ValueType.Ref_PitchMean).status == DataStatus.OK)
                {
                    refPitchMeanBuffer.Add(new HMSData()
                    {
                        data = mvsOutputData.GetData(ValueType.Ref_PitchMean).data,
                        timestamp = sessionData.timestamp,
                        status = DataStatus.OK
                    });
                }

                if (mvsOutputData.GetData(ValueType.Test_Pitch).status == DataStatus.OK)
                {
                    testPitchBuffer.Add(new HMSData()
                    {
                        data = mvsOutputData.GetData(ValueType.Test_Pitch).data,
                        timestamp = sessionData.timestamp,
                        status = DataStatus.OK
                    });
                }

                if (mvsOutputData.GetData(ValueType.Test_PitchMean).status == DataStatus.OK)
                {
                    testPitchMeanBuffer.Add(new HMSData()
                    {
                        data = mvsOutputData.GetData(ValueType.Test_PitchMean).data,
                        timestamp = sessionData.timestamp,
                        status = DataStatus.OK
                    });
                }

                // Overføre data til grafer - Roll
                if (mvsOutputData.GetData(ValueType.Ref_Roll).status == DataStatus.OK)
                {
                    refRollBuffer.Add(new HMSData()
                    {
                        data = mvsOutputData.GetData(ValueType.Ref_Roll).data,
                        timestamp = sessionData.timestamp,
                        status = DataStatus.OK
                    });
                }

                if (mvsOutputData.GetData(ValueType.Ref_RollMean).status == DataStatus.OK)
                {
                    refRollMeanBuffer.Add(new HMSData()
                    {
                        data = mvsOutputData.GetData(ValueType.Ref_RollMean).data,
                        timestamp = sessionData.timestamp,
                        status = DataStatus.OK
                    });
                }

                if (mvsOutputData.GetData(ValueType.Test_Roll).status == DataStatus.OK)
                {
                    testRollBuffer.Add(new HMSData()
                    {
                        data = mvsOutputData.GetData(ValueType.Test_Roll).data,
                        timestamp = sessionData.timestamp,
                        status = DataStatus.OK
                    });
                }

                if (mvsOutputData.GetData(ValueType.Test_RollMean).status == DataStatus.OK)
                {
                    testRollMeanBuffer.Add(new HMSData()
                    {
                        data = mvsOutputData.GetData(ValueType.Test_RollMean).data,
                        timestamp = sessionData.timestamp,
                        status = DataStatus.OK
                    });
                }

                // Overføre data til grafer - Heave
                if (mvsOutputData.GetData(ValueType.Ref_Heave).status == DataStatus.OK)
                {
                    refHeaveBuffer.Add(new HMSData()
                    {
                        data = mvsOutputData.GetData(ValueType.Ref_Heave).data,
                        timestamp = sessionData.timestamp,
                        status = DataStatus.OK
                    });
                }

                if (mvsOutputData.GetData(ValueType.Ref_HeaveMean).status == DataStatus.OK)
                {
                    refHeaveMeanBuffer.Add(new HMSData()
                    {
                        data = mvsOutputData.GetData(ValueType.Ref_HeaveMean).data,
                        timestamp = sessionData.timestamp,
                        status = DataStatus.OK
                    });
                }

                if (mvsOutputData.GetData(ValueType.Test_Heave).status == DataStatus.OK)
                {
                    testHeaveBuffer.Add(new HMSData()
                    {
                        data = mvsOutputData.GetData(ValueType.Test_Heave).data,
                        timestamp = sessionData.timestamp,
                        status = DataStatus.OK
                    });
                }

                if (mvsOutputData.GetData(ValueType.Test_HeaveMean).status == DataStatus.OK)
                {
                    testHeaveMeanBuffer.Add(new HMSData()
                    {
                        data = mvsOutputData.GetData(ValueType.Test_HeaveMean).data,
                        timestamp = sessionData.timestamp,
                        status = DataStatus.OK
                    });
                }
            }

            if (mvsOutputData.Count() > 0)
            {
                // Hente max verdier
                refPitchMaxData = mvsOutputData.GetData(ValueType.Ref_PitchMax);
                refPitchMaxUpData = mvsOutputData.GetData(ValueType.Ref_PitchMaxUp);
                refPitchMaxDownData = mvsOutputData.GetData(ValueType.Ref_PitchMaxDown);

                refRollMaxData = mvsOutputData.GetData(ValueType.Ref_RollMax);
                refRollMaxLeftData = mvsOutputData.GetData(ValueType.Ref_RollMaxLeft);
                refRollMaxRightData = mvsOutputData.GetData(ValueType.Ref_RollMaxRight);

                refHeaveMaxData = mvsOutputData.GetData(ValueType.Ref_HeaveMax);

                testPitchMaxData = mvsOutputData.GetData(ValueType.Test_PitchMax);
                testPitchMaxUpData = mvsOutputData.GetData(ValueType.Test_PitchMaxUp);
                testPitchMaxDownData = mvsOutputData.GetData(ValueType.Test_PitchMaxDown);

                testRollMaxData = mvsOutputData.GetData(ValueType.Test_RollMax);
                testRollMaxLeftData = mvsOutputData.GetData(ValueType.Test_RollMaxLeft);
                testRollMaxRightData = mvsOutputData.GetData(ValueType.Test_RollMaxRight);

                testHeaveMaxData = mvsOutputData.GetData(ValueType.Test_HeaveMax);

                // Hente mean verdier
                refPitchMeanData = mvsOutputData.GetData(ValueType.Ref_PitchMean);
                refPitchMeanMaxData = mvsOutputData.GetData(ValueType.Ref_PitchMeanMax);
                refRollMeanData = mvsOutputData.GetData(ValueType.Ref_RollMean);
                refHeaveMeanData = mvsOutputData.GetData(ValueType.Ref_HeaveMean);

                testPitchMeanData = mvsOutputData.GetData(ValueType.Test_PitchMean);
                testPitchMeanMaxData = mvsOutputData.GetData(ValueType.Test_PitchMeanMax);
                testRollMeanData = mvsOutputData.GetData(ValueType.Test_RollMean);
                testHeaveMeanData = mvsOutputData.GetData(ValueType.Test_HeaveMean);
            }

            // Oppdatere alignment datetime til alle chart
            if (sessionDataList.Count > 0)
                alignmentTime = sessionDataList.Last().timestamp;
            else
                alignmentTime = DateTime.UtcNow;

            // Oppdatere aksene
            OnPropertyChanged(nameof(pitchChartAxisMax));
            OnPropertyChanged(nameof(pitchChartAxisMin));

            OnPropertyChanged(nameof(rollChartAxisMax));
            OnPropertyChanged(nameof(rollChartAxisMin));

            OnPropertyChanged(nameof(heaveChartAxisMax));
            OnPropertyChanged(nameof(heaveChartAxisMin));
        }

        public double AddToMeanSum(HMSData hmsData)
        {
            if (hmsData.status == DataStatus.OK && !double.IsNaN(hmsData.data))
                return hmsData.data;
            else
                return 0;
        }

        public HMSData UpdateMeanSum(double meanSum, double count)
        {
            HMSData meanData = new HMSData();

            meanData.data = meanSum / count;
            meanData.status = DataStatus.OK;

            return meanData;
        }

        public bool MaxValueCheckOK(HMSData hmsData)
        {
            if (hmsData.status == DataStatus.OK && !double.IsNaN(hmsData.data))
                return true;
            else
                return false;
        }

        public void TransferToDisplay()
        {
            // Slette gamle data i graf data lister
            refPitchList.Clear();
            refPitchMeanList.Clear();
            testPitchList.Clear();
            testPitchMeanList.Clear();

            refRollList.Clear();
            refRollMeanList.Clear();
            testRollList.Clear();
            testRollMeanList.Clear();

            refHeaveList.Clear();
            refHeaveMeanList.Clear();
            testHeaveList.Clear();
            testHeaveMeanList.Clear();

            // Overføre data fra buffer til chart data
            GraphBuffer.Transfer(refPitchBuffer, refPitchList);
            GraphBuffer.Transfer(refRollBuffer, refRollList);
            GraphBuffer.Transfer(refHeaveBuffer, refHeaveList);

            GraphBuffer.Transfer(refPitchMeanBuffer, refPitchMeanList);
            GraphBuffer.Transfer(refRollMeanBuffer, refRollMeanList);
            GraphBuffer.Transfer(refHeaveMeanBuffer, refHeaveMeanList);

            GraphBuffer.Transfer(testPitchBuffer, testPitchList);
            GraphBuffer.Transfer(testRollBuffer, testRollList);
            GraphBuffer.Transfer(testHeaveBuffer, testHeaveList);

            GraphBuffer.Transfer(testPitchMeanBuffer, testPitchMeanList);
            GraphBuffer.Transfer(testRollMeanBuffer, testRollMeanList);
            GraphBuffer.Transfer(testHeaveMeanBuffer, testHeaveMeanList);
        }

        /////////////////////////////////////////////////////////////////////////////
        // Pitch
        /////////////////////////////////////////////////////////////////////////////
        private void InitPitchData()
        {
            _pitchData = new HMSData();

            _refPitchMaxData = new HMSData();
            _refPitchMaxUpData = new HMSData();
            _refPitchMaxDownData = new HMSData();
            _refPitchMeanData = new HMSData();
            _refPitchMeanMaxData = new HMSData();

            _testPitchMaxData = new HMSData();
            _testPitchMaxUpData = new HMSData();
            _testPitchMaxDownData = new HMSData();
            _testPitchMeanData = new HMSData();
            _testPitchMeanMaxData = new HMSData();

        }

        private void ClearPitchData()
        {
            HMSData noData = new HMSData()
            {
                data = double.NaN,
                status = DataStatus.NONE
            };

            refPitchMaxUpData = noData;
            refPitchMaxDownData = noData;
            refPitchMeanData = noData;
            refPitchMeanMaxData = noData;

            testPitchMaxUpData = noData;
            testPitchMaxDownData = noData;
            testPitchMeanData = noData;
            testPitchMeanMaxData = noData;

            refPitchBuffer.Clear();
            refPitchMeanBuffer.Clear();
            refPitchList.Clear();
            refPitchMeanList.Clear();

            testPitchBuffer.Clear();
            testPitchMeanBuffer.Clear();
            testPitchList.Clear();
            testPitchMeanList.Clear();

            for (int i = -Constants.Minutes20; i <= 0; i++)
            {
                refPitchMeanList.Add(new HMSData()
                {
                    data = 0,
                    timestamp = DateTime.UtcNow.AddSeconds(i)
                });

                testPitchMeanList.Add(new HMSData()
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

        private HMSData _refPitchMaxData { get; set; }
        public HMSData refPitchMaxData
        {
            get
            {
                return _refPitchMaxData;
            }
            set
            {
                if (value != null)
                {
                    _refPitchMaxData.Set(value);
                }
            }
        }

        private HMSData _testPitchMaxData { get; set; }
        public HMSData testPitchMaxData
        {
            get
            {
                return _testPitchMaxData;
            }
            set
            {
                if (value != null)
                {
                    _testPitchMaxData.Set(value);
                }
            }
        }

        private HMSData _refPitchMaxUpData { get; set; }
        public HMSData refPitchMaxUpData
        {
            get
            {
                return _refPitchMaxUpData;
            }
            set
            {
                if (value != null)
                {
                    _refPitchMaxUpData.Set(value);

                    OnPropertyChanged(nameof(refPitchMaxUpString));
                }
            }
        }
        public string refPitchMaxUpString
        {
            get
            {
                if (_refPitchMaxUpData != null)
                {
                    // Sjekke om data er gyldig
                    if (_refPitchMaxUpData.status == DataStatus.OK && !double.IsNaN(_refPitchMaxUpData.data))
                    {
                        double pitch = Math.Abs(_refPitchMaxUpData.data);

                        string dir;
                        if (_refPitchMaxUpData.data > 0)
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

        private HMSData _testPitchMaxUpData { get; set; }
        public HMSData testPitchMaxUpData
        {
            get
            {
                return _testPitchMaxUpData;
            }
            set
            {
                if (value != null)
                {
                    _testPitchMaxUpData.Set(value);

                    OnPropertyChanged(nameof(testPitchMaxUpString));
                }
            }
        }
        public string testPitchMaxUpString
        {
            get
            {
                if (_testPitchMaxUpData != null)
                {
                    // Sjekke om data er gyldig
                    if (_testPitchMaxUpData.status == DataStatus.OK && !double.IsNaN(_testPitchMaxUpData.data))
                    {
                        double pitch = Math.Abs(_testPitchMaxUpData.data);

                        string dir;
                        if (_testPitchMaxUpData.data > 0)
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

        private HMSData _refPitchMaxDownData { get; set; }
        public HMSData refPitchMaxDownData
        {
            get
            {
                return _refPitchMaxDownData;
            }
            set
            {
                if (value != null)
                {
                    _refPitchMaxDownData.Set(value);

                    OnPropertyChanged(nameof(refPitchMaxDownString));
                }
            }
        }
        public string refPitchMaxDownString
        {
            get
            {
                if (_refPitchMaxDownData != null)
                {
                    // Sjekke om data er gyldig
                    if (_refPitchMaxDownData.status == DataStatus.OK && !double.IsNaN(_refPitchMaxDownData.data))
                    {
                        double pitch = Math.Abs(_refPitchMaxDownData.data);

                        string dir;
                        if (_refPitchMaxDownData.data > 0)
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

        private HMSData _testPitchMaxDownData { get; set; }
        public HMSData testPitchMaxDownData
        {
            get
            {
                return _testPitchMaxDownData;
            }
            set
            {
                if (value != null)
                {
                    _testPitchMaxDownData.Set(value);

                    OnPropertyChanged(nameof(testPitchMaxDownString));
                }
            }
        }
        public string testPitchMaxDownString
        {
            get
            {
                if (_testPitchMaxDownData != null)
                {
                    // Sjekke om data er gyldig
                    if (_testPitchMaxDownData.status == DataStatus.OK && !double.IsNaN(_testPitchMaxDownData.data))
                    {
                        double pitch = Math.Abs(_testPitchMaxDownData.data);

                        string dir;
                        if (_testPitchMaxDownData.data > 0)
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


        private HMSData _refPitchMeanData { get; set; }
        public HMSData refPitchMeanData
        {
            get
            {
                return _refPitchMeanData;
            }
            set
            {
                if (value != null)
                {
                    _refPitchMeanData.Set(value);

                    OnPropertyChanged(nameof(refPitchMeanDataString));
                    OnPropertyChanged(nameof(deviationPitchMeanString));
                }
            }
        }
        public string refPitchMeanDataString
        {
            get
            {
                if (_refPitchMeanData != null)
                {
                    // Sjekke om data er gyldig
                    if (_refPitchMeanData.status == DataStatus.OK && !double.IsNaN(_refPitchMeanData.data))
                    {
                        return string.Format("{0} °", Math.Round(_refPitchMeanData.data, 3, MidpointRounding.AwayFromZero).ToString(Constants.numberFormatRecData));
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
        private HMSData _refPitchMeanMaxData { get; set; }
        public HMSData refPitchMeanMaxData
        {
            get
            {
                return _refPitchMeanMaxData;
            }
            set
            {
                if (value != null)
                {
                    _refPitchMeanMaxData.Set(value);

                    OnPropertyChanged(nameof(meanPitchChartAxisMax));
                    OnPropertyChanged(nameof(meanPitchChartAxisMin));
                }
            }
        }

        private HMSData _testPitchMeanData { get; set; }
        public HMSData testPitchMeanData
        {
            get
            {
                return _testPitchMeanData;
            }
            set
            {
                if (value != null)
                {
                    _testPitchMeanData.Set(value);

                    OnPropertyChanged(nameof(testPitchMeanDataString));
                    OnPropertyChanged(nameof(deviationPitchMeanString)); 
                }
            }
        }
        public string testPitchMeanDataString
        {
            get
            {
                if (_testPitchMeanData != null)
                {
                    // Sjekke om data er gyldig
                    if (_testPitchMeanData.status == DataStatus.OK && !double.IsNaN(_testPitchMeanData.data))
                    {
                        return string.Format("{0} °", Math.Round(_testPitchMeanData.data, 3, MidpointRounding.AwayFromZero).ToString(Constants.numberFormatRecData));
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
        private HMSData _testPitchMeanMaxData { get; set; }
        public HMSData testPitchMeanMaxData
        {
            get
            {
                return _testPitchMeanMaxData;
            }
            set
            {
                if (value != null)
                {
                    _testPitchMeanMaxData.Set(value);

                    OnPropertyChanged(nameof(meanPitchChartAxisMax));
                    OnPropertyChanged(nameof(meanPitchChartAxisMin));
                }
            }
        }

        public double deviationPitchMeanMax
        {
            get
            {
                if (_testPitchMeanData.status == DataStatus.OK && !double.IsNaN(_testPitchMeanData.data) &&
                    _refPitchMeanData.status == DataStatus.OK && !double.IsNaN(_refPitchMeanData.data))
                {
                    return Math.Round(_testPitchMeanData.data - _refPitchMeanData.data, 3, MidpointRounding.AwayFromZero);
                }
                else
                {
                    return double.NaN;
                }
            }
        }

        public string deviationPitchMeanString
        {
            get
            {
                if (_testPitchMeanData.status == DataStatus.OK && !double.IsNaN(_testPitchMeanData.data) &&
                    _refPitchMeanData.status == DataStatus.OK && !double.IsNaN(_refPitchMeanData.data))
                {
                    return Math.Round(_testPitchMeanData.data - _refPitchMeanData.data, 3, MidpointRounding.AwayFromZero).ToString(Constants.numberFormatRecDataSigned);
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

            _refRollMaxData = new HMSData();
            _refRollMaxLeftData = new HMSData();
            _refRollMaxRightData = new HMSData();
            _refRollMeanData = new HMSData();

            _testRollMaxData = new HMSData();
            _testRollMaxLeftData = new HMSData();
            _testRollMaxRightData = new HMSData();
            _testRollMeanData = new HMSData();

            // Init av chart data
            refRollMeanList.Clear();
            testRollMeanList.Clear();

            for (int i = -Constants.Minutes20; i <= 0; i++)
            {
                refRollMeanList.Add(new HMSData()
                {
                    data = 0,
                    timestamp = DateTime.UtcNow.AddSeconds(i)
                });

               testRollMeanList.Add(new HMSData()
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

            refRollMaxLeftData = noData;
            refRollMaxRightData = noData;
            refRollMeanData = noData;

            testRollMaxLeftData = noData;
            testRollMaxRightData = noData;
            testRollMeanData = noData;

            refRollBuffer.Clear();
            refRollMeanBuffer.Clear();
            refRollList.Clear();
            refRollMeanList.Clear();

            testRollBuffer.Clear();
            testRollMeanBuffer.Clear();
            testRollList.Clear();
            testRollMeanList.Clear();

            for (int i = -Constants.Minutes20; i <= 0; i++)
            {
                refRollMeanList.Add(new HMSData()
                {
                    data = 0,
                    timestamp = DateTime.UtcNow.AddSeconds(i)
                });

                testRollMeanList.Add(new HMSData()
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

        private HMSData _refRollMaxData { get; set; }
        public HMSData refRollMaxData
        {
            get
            {
                return _refRollMaxData;
            }
            set
            {
                if (value != null)
                {
                    _refRollMaxData.Set(value);
                }
            }
        }

        private HMSData _testRollMaxData { get; set; }
        public HMSData testRollMaxData
        {
            get
            {
                return _testRollMaxData;
            }
            set
            {
                if (value != null)
                {
                    _testRollMaxData.Set(value);
                }
            }
        }

        private HMSData _refRollMaxLeftData { get; set; }
        public HMSData refRollMaxLeftData
        {
            get
            {
                return _refRollMaxLeftData;
            }
            set
            {
                if (value != null)
                {
                    _refRollMaxLeftData.Set(value);

                    OnPropertyChanged(nameof(refRollMaxLeftString));
                }
            }
        }
        public string refRollMaxLeftString
        {
            get
            {
                if (_refRollMaxLeftData != null)
                {
                    // Sjekke om data er gyldig
                    if (_refRollMaxLeftData.status == DataStatus.OK && !double.IsNaN(_refRollMaxLeftData.data))
                    {
                        double roll = Math.Abs(_refRollMaxLeftData.data);

                        string dir;
                        if (_refRollMaxLeftData.data >= 0)
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

        private HMSData _testRollMaxLeftData { get; set; }
        public HMSData testRollMaxLeftData
        {
            get
            {
                return _testRollMaxLeftData;
            }
            set
            {
                if (value != null)
                {
                    _testRollMaxLeftData.Set(value);

                    OnPropertyChanged(nameof(testRollMaxLeftString));
                }
            }
        }
        public string testRollMaxLeftString
        {
            get
            {
                if (_testRollMaxLeftData != null)
                {
                    // Sjekke om data er gyldig
                    if (_testRollMaxLeftData.status == DataStatus.OK && !double.IsNaN(_testRollMaxLeftData.data))
                    {
                        double roll = Math.Abs(_testRollMaxLeftData.data);

                        string dir;
                        if (_testRollMaxLeftData.data >= 0)
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

        private HMSData _refRollMaxRightData { get; set; }
        public HMSData refRollMaxRightData
        {
            get
            {
                return _refRollMaxRightData;
            }
            set
            {
                if (value != null)
                {
                    _refRollMaxRightData.Set(value);

                    OnPropertyChanged(nameof(refRollMaxRightString));
                }
            }
        }
        public string refRollMaxRightString
        {
            get
            {
                if (_refRollMaxRightData != null)
                {
                    // Sjekke om data er gyldig
                    if (_refRollMaxRightData.status == DataStatus.OK && !double.IsNaN(_refRollMaxRightData.data))
                    {
                        double roll = Math.Abs(_refRollMaxRightData.data);

                        string dir;
                        if (_refRollMaxRightData.data >= 0)
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

        private HMSData _testRollMaxRightData { get; set; }
        public HMSData testRollMaxRightData
        {
            get
            {
                return _testRollMaxRightData;
            }
            set
            {
                if (value != null)
                {
                    _testRollMaxRightData.Set(value);

                    OnPropertyChanged(nameof(testRollMaxRightString));
                }
            }
        }
        public string testRollMaxRightString
        {
            get
            {
                if (_testRollMaxRightData != null)
                {
                    // Sjekke om data er gyldig
                    if (_testRollMaxRightData.status == DataStatus.OK && !double.IsNaN(_testRollMaxRightData.data))
                    {
                        double roll = Math.Abs(_testRollMaxRightData.data);

                        string dir;
                        if (_testRollMaxRightData.data >= 0)
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

        private HMSData _refRollMeanData { get; set; }
        public HMSData refRollMeanData
        {
            get
            {
                return _refRollMeanData;
            }
            set
            {
                if (value != null)
                {
                    _refRollMeanData.Set(value);

                    OnPropertyChanged(nameof(refRollMeanDataString));
                    OnPropertyChanged(nameof(rollMeanDeviationString));
                }
            }
        }
        public string refRollMeanDataString
        {
            get
            {
                if (_refRollMeanData != null)
                {
                    // Sjekke om data er gyldig
                    if (_refRollMeanData.status == DataStatus.OK && !double.IsNaN(_refRollMeanData.data))
                    {
                        return string.Format("{0} °", Math.Round(_refRollMeanData.data, 3, MidpointRounding.AwayFromZero).ToString(Constants.numberFormatRecData));
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

        private HMSData _testRollMeanData { get; set; }
        public HMSData testRollMeanData
        {
            get
            {
                return _testRollMeanData;
            }
            set
            {
                if (value != null)
                {
                    _testRollMeanData.Set(value);

                    OnPropertyChanged(nameof(testRollMeanDataString));
                    OnPropertyChanged(nameof(rollMeanDeviationString));                    
                }
            }
        }
        public string testRollMeanDataString
        {
            get
            {
                if (_testRollMeanData != null)
                {
                    // Sjekke om data er gyldig
                    if (_testRollMeanData.status == DataStatus.OK && !double.IsNaN(_testRollMeanData.data))
                    {
                        return string.Format("{0} °", Math.Round(_testRollMeanData.data, 3, MidpointRounding.AwayFromZero).ToString(Constants.numberFormatRecData));
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

        public double deviationMeanRollMax
        {
            get
            {
                if (_testRollMeanData.status == DataStatus.OK && !double.IsNaN(_testRollMeanData.data) &&
                    _refRollMeanData.status == DataStatus.OK && !double.IsNaN(_refRollMeanData.data))
                {
                    return Math.Round(_testRollMeanData.data - _refRollMeanData.data, 3, MidpointRounding.AwayFromZero);
                }
                else
                {
                    return double.NaN;
                }
            }
        }

        public string rollMeanDeviationString
        {
            get
            {
                if (_testRollMeanData.status == DataStatus.OK && !double.IsNaN(_testRollMeanData.data) &&
                    _refRollMeanData.status == DataStatus.OK && !double.IsNaN(_refRollMeanData.data))
                {
                    return Math.Round(_testRollMeanData.data - _refRollMeanData.data, 3, MidpointRounding.AwayFromZero).ToString(Constants.numberFormatRecDataSigned);
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
            _refHeaveMaxData = new HMSData();
            _refHeaveMeanData = new HMSData();

            _testHeaveData = new HMSData();
            _testHeaveMaxData = new HMSData();
            _testHeaveMeanData = new HMSData();

            // Init av chart data
            refHeaveMeanList.Clear();
            testHeaveMeanList.Clear();

            for (int i = -Constants.Minutes20; i <= 0; i++)
            {
                refHeaveMeanList.Add(new HMSData()
                {
                    data = 0,
                    timestamp = DateTime.UtcNow.AddSeconds(i)
                });

                testHeaveMeanList.Add(new HMSData()
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

            refHeaveMaxData = noData;
            refHeaveMeanData = noData;

            testHeaveMaxData = noData;
            testHeaveMeanData = noData;

            refHeaveBuffer.Clear();
            refHeaveMeanBuffer.Clear();
            refHeaveList.Clear();
            refHeaveMeanList.Clear();

            testHeaveBuffer.Clear();
            testHeaveMeanBuffer.Clear();
            testHeaveList.Clear();
            testHeaveMeanList.Clear();

            for (int i = -Constants.Minutes20; i <= 0; i++)
            {
                refHeaveMeanList.Add(new HMSData()
                {
                    data = 0,
                    timestamp = DateTime.UtcNow.AddSeconds(i)
                });

                testHeaveMeanList.Add(new HMSData()
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

        private HMSData _testHeaveData { get; set; }
        public HMSData testHeaveData
        {
            get
            {
                return _testHeaveData;
            }
            set
            {
                if (value != null)
                {
                    _testHeaveData.Set(value);
                }
            }
        }

        private HMSData _refHeaveMaxData { get; set; }
        public HMSData refHeaveMaxData
        {
            get
            {
                return _refHeaveMaxData;
            }
            set
            {
                if (value != null)
                {
                    _refHeaveMaxData.Set(value);

                    OnPropertyChanged(nameof(refHeaveMaxString));
                }
            }
        }
        public string refHeaveMaxString
        {
            get
            {
                if (_refHeaveMaxData != null)
                {
                    // Sjekke om data er gyldig
                    if (_refHeaveMaxData.status == DataStatus.OK && !double.IsNaN(_refHeaveMaxData.data))
                    {
                        return string.Format("{0} m", Math.Round(_refHeaveMaxData.data, 3, MidpointRounding.AwayFromZero).ToString(Constants.numberFormatRecData));
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

        private HMSData _testHeaveMaxData { get; set; }
        public HMSData testHeaveMaxData
        {
            get
            {
                return _testHeaveMaxData;
            }
            set
            {
                if (value != null)
                {
                    _testHeaveMaxData.Set(value);

                    OnPropertyChanged(nameof(testHeaveMaxString));
                }
            }
        }
        public string testHeaveMaxString
        {
            get
            {
                if (_testHeaveMaxData != null)
                {
                    // Sjekke om data er gyldig
                    if (_testHeaveMaxData.status == DataStatus.OK && !double.IsNaN(_testHeaveMaxData.data))
                    {
                        return string.Format("{0} m", Math.Round(_testHeaveMaxData.data, 3, MidpointRounding.AwayFromZero).ToString(Constants.numberFormatRecData));
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

        private HMSData _refHeaveMeanData { get; set; }
        public HMSData refHeaveMeanData
        {
            get
            {
                return _refHeaveMeanData;
            }
            set
            {
                if (value != null)
                {
                    _refHeaveMeanData.Set(value);

                    OnPropertyChanged(nameof(refHeaveMeanString));
                    OnPropertyChanged(nameof(deviationMeanHeaveMax));
                    OnPropertyChanged(nameof(deviationMeanHeaveString));
                }
            }
        }
        public string refHeaveMeanString
        {
            get
            {
                if (_refHeaveMeanData != null)
                {
                    // Sjekke om data er gyldig
                    if (_refHeaveMeanData.status == DataStatus.OK && !double.IsNaN(_refHeaveMeanData.data))
                    {
                        return string.Format("{0} m", Math.Round(_refHeaveMeanData.data, 3, MidpointRounding.AwayFromZero).ToString(Constants.numberFormatRecData));
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

        private HMSData _testHeaveMeanData { get; set; }
        public HMSData testHeaveMeanData
        {
            get
            {
                return _testHeaveMeanData;
            }
            set
            {
                if (value != null)
                {
                    _testHeaveMeanData.Set(value);

                    OnPropertyChanged(nameof(testHeaveMeanString));
                    OnPropertyChanged(nameof(deviationMeanHeaveMax));
                    OnPropertyChanged(nameof(deviationMeanHeaveString));
                }
            }
        }
        public string testHeaveMeanString
        {
            get
            {
                if (_testHeaveMeanData != null)
                {
                    // Sjekke om data er gyldig
                    if (_testHeaveMeanData.status == DataStatus.OK && !double.IsNaN(_testHeaveMeanData.data))
                    {
                        return string.Format("{0} m", Math.Round(_testHeaveMeanData.data, 3, MidpointRounding.AwayFromZero).ToString(Constants.numberFormatRecData));
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

        public double deviationMeanHeaveMax
        {
            get
            {
                if (_testHeaveMeanData.status == DataStatus.OK && !double.IsNaN(_testHeaveMeanData.data) &&
                    _refHeaveMeanData.status == DataStatus.OK && !double.IsNaN(_refHeaveMeanData.data))
                {
                    return Math.Round(_testHeaveMeanData.data - _refHeaveMeanData.data, 3, MidpointRounding.AwayFromZero);
                }
                else
                {
                    return double.NaN;
                }
            }
        }

        public string deviationMeanHeaveString
        {
            get
            {
                if (_testHeaveMeanData.status == DataStatus.OK && !double.IsNaN(_testHeaveMeanData.data) &&
                    _refHeaveMeanData.status == DataStatus.OK && !double.IsNaN(_refHeaveMeanData.data))
                {
                    return Math.Round(_testHeaveMeanData.data - _refHeaveMeanData.data, 3, MidpointRounding.AwayFromZero).ToString(Constants.numberFormatRecDataSigned);
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
                    OnPropertyChanged(nameof(alignmentTimeMin));
                    OnPropertyChanged(nameof(alignmentTimeMax));
                }
            }
        }

        public DateTime alignmentTimeMin
        {
            get
            {
                if (mainWindowVM.OperationsMode == OperationsMode.ViewData && _alignmentTime != DateTime.MinValue)
                    return _alignmentTime.AddSeconds(-Constants.Minutes20);
                else
                    return DateTime.UtcNow.AddSeconds(-Constants.Minutes20);
            }
        }

        public DateTime alignmentTimeMax
        {
            get
            {
                return _alignmentTime;
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Chart Axis Min/Max
        /////////////////////////////////////////////////////////////////////////////
        public double pitchChartAxisMax
        {
            get
            {
                return ChartAxisMax(_refPitchMaxData, _testPitchMaxData, Constants.InputAxisMargin, Constants.InputAxisDefault, 0);
            }
        }

        public double pitchChartAxisMin
        {
            get
            {
                return -pitchChartAxisMax;
            }
        }

        public double meanPitchChartAxisMax
        {
            get
            {
                return ChartAxisMax(_refPitchMeanMaxData, _testPitchMeanMaxData, Constants.MeanAxisMargin, Constants.MeanAxisDefault, 1);
            }
        }

        public double meanPitchChartAxisMin
        {
            get
            {
                return -meanPitchChartAxisMax;
            }
        }

        public double rollChartAxisMax
        {
            get
            {
                return ChartAxisMax(_refRollMaxData, _testRollMaxData, Constants.InputAxisMargin, Constants.InputAxisDefault, 0);
            }
        }

        public double rollChartAxisMin
        {
            get
            {
                return -rollChartAxisMax;
            }
        }

        public double meanRollChartAxisMax
        {
            get
            {
                return ChartAxisMax(deviationMeanRollMax, Constants.MeanAxisMargin, Constants.MeanAxisDefault);
            }
        }

        public double meanRollChartAxisMin
        {
            get
            {
                return -meanRollChartAxisMax;
            }
        }

        public double heaveChartAxisMax
        {
            get
            {
                return ChartAxisMax(_refHeaveMaxData, _testHeaveMaxData, Constants.InputAxisMargin, Constants.InputAxisDefault, 0);
            }
        }

        public double heaveChartAxisMin
        {
            get
            {
                return -heaveChartAxisMax;
            }
        }

        public double meanHeaveChartAxisMax
        {
            get
            {
                return ChartAxisMax(deviationMeanHeaveMax, Constants.MeanAxisMargin, Constants.MeanAxisDefault);
            }
        }

        public double meanHeaveChartAxisMin
        {
            get
            {
                return -meanHeaveChartAxisMax;
            }
        }

        // Variabel oppdatert
        // Dersom navn ikke er satt brukes kallende medlem sitt navn
        protected virtual void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private double ChartAxisMax(HMSData refData, HMSData testData, double margin, double defaultValue, int decimals)
        {
            if (!double.IsNaN(refData.data) && !double.IsNaN(testData.data))
            {
                if (Math.Abs(refData.data) > Math.Abs(testData.data))
                    return (double)(Math.Round(((Math.Round(refData.data * (1 / margin), MidpointRounding.AwayFromZero) / (1 / margin)) + margin), decimals, MidpointRounding.AwayFromZero));
                else
                    return (double)(Math.Round(((Math.Round(testData.data * (1 / margin), MidpointRounding.AwayFromZero) / (1 / margin)) + margin), decimals, MidpointRounding.AwayFromZero));
            }
            else
            if (!double.IsNaN(refData.data))
            {
                return (double)(Math.Round(((Math.Round(refData.data * (1 / margin), MidpointRounding.AwayFromZero) / (1 / margin)) + margin), decimals, MidpointRounding.AwayFromZero));
            }
            else
            if (!double.IsNaN(testData.data))
            {
                return (double)(Math.Round(((Math.Round(testData.data * (1 / margin), MidpointRounding.AwayFromZero) / (1 / margin)) + margin), decimals, MidpointRounding.AwayFromZero));
            }
            else
            {
                return defaultValue;
            }
        }

        private double ChartAxisMax(double inputData, double margin, double defaultValue)
        {
            if (!double.IsNaN(inputData))
            {
                return (double)((Math.Round(inputData * (1 / margin), MidpointRounding.AwayFromZero) / (1 / margin)) + margin);
            }
            else
            {
                return defaultValue;
            }
        }
    }
}
