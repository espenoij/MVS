using Org.BouncyCastle.Ocsp;
using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Threading;
using Telerik.Windows.Data;
using static MVS.DialogImport;

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

        // Input
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

        // Mean
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

        // Deviation
        public RadObservableCollection<HMSData> devPitchMeanBuffer = new RadObservableCollection<HMSData>();
        public RadObservableCollection<HMSData> devRollMeanBuffer = new RadObservableCollection<HMSData>();
        public RadObservableCollection<HMSData> devHeaveMeanBuffer = new RadObservableCollection<HMSData>();
        
        public RadObservableCollection<HMSData> devPitchMeanList = new RadObservableCollection<HMSData>();
        public RadObservableCollection<HMSData> devRollMeanList = new RadObservableCollection<HMSData>();
        public RadObservableCollection<HMSData> devHeaveMeanList = new RadObservableCollection<HMSData>();

        // Alle prosjekt data
        public RadObservableCollection<ProjectData> projectsDataList = new RadObservableCollection<ProjectData>();

        public void Init(MainWindowVM mainWindowVM, MVSProcessing mvsProcessing, MVSDataCollection mvsInputData, MVSDataCollection mvsOutputData)
        {
            this.mainWindowVM = mainWindowVM;
            this.mvsProcessing = mvsProcessing;
            this.mvsInputData = mvsInputData;
            this.mvsOutputData = mvsOutputData;

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

                GraphBuffer.Transfer(devPitchMeanBuffer, devPitchMeanList);
                GraphBuffer.Transfer(devRollMeanBuffer, devRollMeanList);
                GraphBuffer.Transfer(devHeaveMeanBuffer, devHeaveMeanList);

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

                GraphBuffer.RemoveOldData(devPitchMeanList, Constants.Minutes20 + Constants.ChartTimeCorrMin);
                GraphBuffer.RemoveOldData(devRollMeanList, Constants.Minutes20 + Constants.ChartTimeCorrMin);
                GraphBuffer.RemoveOldData(devHeaveMeanList, Constants.Minutes20 + Constants.ChartTimeCorrMin);

                // Oppdatere alignment datetime (nåtid) til alle chart
                alignmentTime = DateTime.UtcNow;

                // Oppdatere aksene
                UpdateAxies();
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

            GraphBuffer.Update(mvsDataCollection.GetData(ValueType.Dev_PitchMean), devPitchMeanBuffer);
            GraphBuffer.Update(mvsDataCollection.GetData(ValueType.Dev_RollMean), devRollMeanBuffer);
            GraphBuffer.Update(mvsDataCollection.GetData(ValueType.Dev_HeaveMean), devHeaveMeanBuffer);

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
            refRollMeanMaxData = mvsDataCollection.GetData(ValueType.Ref_RollMeanMax);

            refHeaveMeanData = mvsDataCollection.GetData(ValueType.Ref_HeaveMean);
            refHeaveMeanMaxData = mvsDataCollection.GetData(ValueType.Ref_HeaveMeanMax);

            testPitchMeanData = mvsDataCollection.GetData(ValueType.Test_PitchMean);
            testPitchMeanMaxData = mvsDataCollection.GetData(ValueType.Test_PitchMeanMax);

            testRollMeanData = mvsDataCollection.GetData(ValueType.Test_RollMean);
            testRollMeanMaxData = mvsDataCollection.GetData(ValueType.Test_RollMeanMax);

            testHeaveMeanData = mvsDataCollection.GetData(ValueType.Test_HeaveMean);
            testHeaveMeanMaxData = mvsDataCollection.GetData(ValueType.Test_HeaveMeanMax);

            // Oppdatere deviation verdier
            devPitchData = mvsDataCollection.GetData(ValueType.Dev_Pitch);
            devPitchMeanData = mvsDataCollection.GetData(ValueType.Dev_PitchMean);
            devPitchMaxData = mvsDataCollection.GetData(ValueType.Dev_PitchMax);

            devRollData = mvsDataCollection.GetData(ValueType.Dev_Roll);
            devRollMeanData = mvsDataCollection.GetData(ValueType.Dev_RollMean);
            devRollMaxData = mvsDataCollection.GetData(ValueType.Dev_RollMax);
            
            devHeaveData = mvsDataCollection.GetData(ValueType.Dev_Heave);
            devHeaveMeanData = mvsDataCollection.GetData(ValueType.Dev_HeaveMean);
            devHeaveMaxData = mvsDataCollection.GetData(ValueType.Dev_HeaveMax);
        }

        public void UpdateAxies()
        {
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

            OnPropertyChanged(nameof(devPitchChartAxisMax));
            OnPropertyChanged(nameof(devPitchChartAxisMin));

            OnPropertyChanged(nameof(devRollChartAxisMax));
            OnPropertyChanged(nameof(devRollChartAxisMin));

            OnPropertyChanged(nameof(devHeaveChartAxisMax));
            OnPropertyChanged(nameof(devHeaveChartAxisMin));
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

        public void AnalyseProjectData(ReportProgressDelegate reportProgress)
        {
            double progressCount = 0;

            foreach (ProjectData sessionData in projectsDataList.ToList())
            {
                // Overføre database data til MVS input
                mvsInputData.TransferData(sessionData);

                // Prosessere data i input
                mvsProcessing.Update(mvsInputData, mainWindowVM, ProcessingType.DATA_ANALYSIS);

                // Overføre data til grafer - Pitch
                if (mvsOutputData.GetData(ValueType.Ref_Pitch)?.status == DataStatus.OK)
                {
                    refPitchBuffer.Add(new HMSData()
                    {
                        data = mvsOutputData.GetData(ValueType.Ref_Pitch).data,
                        timestamp = sessionData.timestamp,
                        status = DataStatus.OK
                    });
                }

                if (mvsOutputData.GetData(ValueType.Ref_PitchMean)?.status == DataStatus.OK)
                {
                    refPitchMeanBuffer.Add(new HMSData()
                    {
                        data = mvsOutputData.GetData(ValueType.Ref_PitchMean).data,
                        timestamp = sessionData.timestamp,
                        status = DataStatus.OK
                    });
                }

                if (mvsOutputData.GetData(ValueType.Test_Pitch)?.status == DataStatus.OK)
                {
                    testPitchBuffer.Add(new HMSData()
                    {
                        data = mvsOutputData.GetData(ValueType.Test_Pitch).data,
                        timestamp = sessionData.timestamp,
                        status = DataStatus.OK
                    });
                }

                if (mvsOutputData.GetData(ValueType.Test_PitchMean)?.status == DataStatus.OK)
                {
                    testPitchMeanBuffer.Add(new HMSData()
                    {
                        data = mvsOutputData.GetData(ValueType.Test_PitchMean).data,
                        timestamp = sessionData.timestamp,
                        status = DataStatus.OK
                    });
                }

                if (mvsOutputData.GetData(ValueType.Dev_PitchMean)?.status == DataStatus.OK)
                {
                    devPitchMeanBuffer.Add(new HMSData()
                    {
                        data = mvsOutputData.GetData(ValueType.Dev_PitchMean).data,
                        timestamp = sessionData.timestamp,
                        status = DataStatus.OK
                    });
                }

                // Overføre data til grafer - Roll
                if (mvsOutputData.GetData(ValueType.Ref_Roll)?.status == DataStatus.OK)
                {
                    refRollBuffer.Add(new HMSData()
                    {
                        data = mvsOutputData.GetData(ValueType.Ref_Roll).data,
                        timestamp = sessionData.timestamp,
                        status = DataStatus.OK
                    });
                }

                if (mvsOutputData.GetData(ValueType.Ref_RollMean)?.status == DataStatus.OK)
                {
                    refRollMeanBuffer.Add(new HMSData()
                    {
                        data = mvsOutputData.GetData(ValueType.Ref_RollMean).data,
                        timestamp = sessionData.timestamp,
                        status = DataStatus.OK
                    });
                }

                if (mvsOutputData.GetData(ValueType.Test_Roll)?.status == DataStatus.OK)
                {
                    testRollBuffer.Add(new HMSData()
                    {
                        data = mvsOutputData.GetData(ValueType.Test_Roll).data,
                        timestamp = sessionData.timestamp,
                        status = DataStatus.OK
                    });
                }

                if (mvsOutputData.GetData(ValueType.Test_RollMean)?.status == DataStatus.OK)
                {
                    testRollMeanBuffer.Add(new HMSData()
                    {
                        data = mvsOutputData.GetData(ValueType.Test_RollMean).data,
                        timestamp = sessionData.timestamp,
                        status = DataStatus.OK
                    });
                }

                if (mvsOutputData.GetData(ValueType.Dev_RollMean)?.status == DataStatus.OK)
                {
                    devRollMeanBuffer.Add(new HMSData()
                    {
                        data = mvsOutputData.GetData(ValueType.Dev_RollMean).data,
                        timestamp = sessionData.timestamp,
                        status = DataStatus.OK
                    });
                }

                // Overføre data til grafer - Heave
                if (mvsOutputData.GetData(ValueType.Ref_Heave)?.status == DataStatus.OK)
                {
                    refHeaveBuffer.Add(new HMSData()
                    {
                        data = mvsOutputData.GetData(ValueType.Ref_Heave).data,
                        timestamp = sessionData.timestamp,
                        status = DataStatus.OK
                    });
                }

                if (mvsOutputData.GetData(ValueType.Ref_HeaveMean)?.status == DataStatus.OK)
                {
                    refHeaveMeanBuffer.Add(new HMSData()
                    {
                        data = mvsOutputData.GetData(ValueType.Ref_HeaveMean).data,
                        timestamp = sessionData.timestamp,
                        status = DataStatus.OK
                    });
                }

                if (mvsOutputData.GetData(ValueType.Test_Heave)?.status == DataStatus.OK)
                {
                    testHeaveBuffer.Add(new HMSData()
                    {
                        data = mvsOutputData.GetData(ValueType.Test_Heave).data,
                        timestamp = sessionData.timestamp,
                        status = DataStatus.OK
                    });
                }

                if (mvsOutputData.GetData(ValueType.Test_HeaveMean)?.status == DataStatus.OK)
                {
                    testHeaveMeanBuffer.Add(new HMSData()
                    {
                        data = mvsOutputData.GetData(ValueType.Test_HeaveMean).data,
                        timestamp = sessionData.timestamp,
                        status = DataStatus.OK
                    });
                }

                if (mvsOutputData.GetData(ValueType.Dev_HeaveMean)?.status == DataStatus.OK)
                {
                    devHeaveMeanBuffer.Add(new HMSData()
                    {
                        data = mvsOutputData.GetData(ValueType.Dev_HeaveMean).data,
                        timestamp = sessionData.timestamp,
                        status = DataStatus.OK
                    });
                }

                // Progress oppdatering
                reportProgress((int)((progressCount++ / projectsDataList.Count) * 100));
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

                devPitchMaxData = mvsOutputData.GetData(ValueType.Dev_PitchMax);
                devRollMaxData = mvsOutputData.GetData(ValueType.Dev_RollMax);
                devHeaveMaxData = mvsOutputData.GetData(ValueType.Dev_HeaveMax);

                // Hente mean verdier
                refPitchMeanData = mvsOutputData.GetData(ValueType.Ref_PitchMean);
                refPitchMeanMaxData = mvsOutputData.GetData(ValueType.Ref_PitchMeanMax);
                refRollMeanData = mvsOutputData.GetData(ValueType.Ref_RollMean);
                refRollMeanMaxData = mvsOutputData.GetData(ValueType.Ref_RollMeanMax);
                refHeaveMeanData = mvsOutputData.GetData(ValueType.Ref_HeaveMean);
                refHeaveMeanMaxData = mvsOutputData.GetData(ValueType.Ref_HeaveMeanMax);

                testPitchMeanData = mvsOutputData.GetData(ValueType.Test_PitchMean);
                testPitchMeanMaxData = mvsOutputData.GetData(ValueType.Test_PitchMeanMax);
                testRollMeanData = mvsOutputData.GetData(ValueType.Test_RollMean);
                testRollMeanMaxData = mvsOutputData.GetData(ValueType.Test_RollMeanMax);
                testHeaveMeanData = mvsOutputData.GetData(ValueType.Test_HeaveMean);
                testHeaveMeanMaxData = mvsOutputData.GetData(ValueType.Test_HeaveMeanMax);

                devPitchMeanData = mvsOutputData.GetData(ValueType.Dev_PitchMean);
                devRollMeanData = mvsOutputData.GetData(ValueType.Dev_RollMean);
                devHeaveMeanData = mvsOutputData.GetData(ValueType.Dev_HeaveMean);
            }

            // Oppdatere alignment datetime til alle chart
            if (projectsDataList.Count > 0)
                alignmentTime = projectsDataList.Last().timestamp;
            else
                alignmentTime = DateTime.UtcNow;
        }

        public void ClearDisplayData()
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
            refRollMeanMaxData = noData;
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

            devPitchMeanBuffer.Clear();
            devRollMeanBuffer.Clear();
            devHeaveMeanBuffer.Clear();

            // Slette gamle data i graf lister
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

            devPitchMeanList.Clear();
            devRollMeanList.Clear();
            devHeaveMeanList.Clear();
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
            devPitchMeanList.Clear();

            refRollList.Clear();
            refRollMeanList.Clear();
            testRollList.Clear();
            testRollMeanList.Clear();
            devRollMeanList.Clear();

            refHeaveList.Clear();
            refHeaveMeanList.Clear();
            testHeaveList.Clear();
            testHeaveMeanList.Clear();
            devHeaveMeanList.Clear();

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

            GraphBuffer.Transfer(devPitchMeanBuffer, devPitchMeanList);
            GraphBuffer.Transfer(devRollMeanBuffer, devRollMeanList);
            GraphBuffer.Transfer(devHeaveMeanBuffer, devHeaveMeanList);

            // Oppdatere aksene
            UpdateAxies();
        }

        /////////////////////////////////////////////////////////////////////////////
        // Pitch
        /////////////////////////////////////////////////////////////////////////////
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

            devPitchMaxData = noData;

            refPitchBuffer.Clear();
            refPitchMeanBuffer.Clear();
            refPitchList.Clear();
            refPitchMeanList.Clear();

            testPitchBuffer.Clear();
            testPitchMeanBuffer.Clear();
            testPitchList.Clear();
            testPitchMeanList.Clear();

            devPitchMeanBuffer.Clear();
            devPitchMeanList.Clear();
        }

        private HMSData _pitchData { get; set; } = new HMSData();
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

        private HMSData _refPitchMaxData { get; set; } = new HMSData();
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

        private HMSData _testPitchMaxData { get; set; } = new HMSData();
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

        private HMSData _refPitchMaxUpData { get; set; } = new HMSData();
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

        private HMSData _testPitchMaxUpData { get; set; } = new HMSData();
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

        private HMSData _refPitchMaxDownData { get; set; } = new HMSData();
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

        private HMSData _testPitchMaxDownData { get; set; } = new HMSData();
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


        private HMSData _refPitchMeanData { get; set; } = new HMSData();
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
                    OnPropertyChanged(nameof(devPitchMeanString));
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
        private HMSData _refPitchMeanMaxData { get; set; } = new HMSData();
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

        private HMSData _testPitchMeanData { get; set; } = new HMSData();
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
                    OnPropertyChanged(nameof(devPitchMeanString)); 
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
        private HMSData _testPitchMeanMaxData { get; set; } = new HMSData();
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

        private HMSData _devPitchData { get; set; } = new HMSData();
        public HMSData devPitchData
        {
            get
            {
                return _devPitchData;
            }
            set
            {
                if (value != null)
                {
                    _devPitchData.Set(value);
                }
            }
        }

        private HMSData _devPitchMaxData { get; set; } = new HMSData();
        public HMSData devPitchMaxData
        {
            get
            {
                return _devPitchMaxData;
            }
            set
            {
                if (value != null)
                {
                    _devPitchMaxData.Set(value);

                    OnPropertyChanged(nameof(devPitchChartAxisMax));
                    OnPropertyChanged(nameof(devPitchChartAxisMin));
                    OnPropertyChanged(nameof(devPitchMaxString));
                }
            }
        }

        public string devPitchMaxString
        {
            get
            {
                if (_devPitchMaxData.status == DataStatus.OK && !double.IsNaN(_devPitchMaxData.data))
                {
                    return Math.Round(_devPitchMaxData.data, 3, MidpointRounding.AwayFromZero).ToString(Constants.numberFormatRecDataSigned);
                }
                else
                {
                    return Constants.NotAvailable;
                }
            }
        }

        private HMSData _devPitchMeanData { get; set; } = new HMSData();
        public HMSData devPitchMeanData
        {
            get
            {
                return _devPitchMeanData;
            }
            set
            {
                if (value != null)
                {
                    _devPitchMeanData.Set(value);

                    OnPropertyChanged(nameof(devPitchMeanString));
                }
            }
        }

        public string devPitchMeanString
        {
            get
            {
                if (_devPitchMeanData.status == DataStatus.OK && !double.IsNaN(_devPitchMeanData.data))
                {
                    return Math.Round(_devPitchMeanData.data, 3, MidpointRounding.AwayFromZero).ToString(Constants.numberFormatRecDataSigned);
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
            testRollMeanMaxData = noData;

            refRollBuffer.Clear();
            refRollMeanBuffer.Clear();
            refRollList.Clear();
            refRollMeanList.Clear();

            testRollBuffer.Clear();
            testRollMeanBuffer.Clear();
            testRollList.Clear();
            testRollMeanList.Clear();

            devRollMeanBuffer.Clear();
            devRollMeanList.Clear();
        }

        private HMSData _refRollData { get; set; } = new HMSData();
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

        private HMSData _refRollMaxData { get; set; } = new HMSData();
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

        private HMSData _testRollMaxData { get; set; } = new HMSData();
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

        private HMSData _refRollMaxLeftData { get; set; } = new HMSData();
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

        private HMSData _testRollMaxLeftData { get; set; } = new HMSData();
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

        private HMSData _refRollMaxRightData { get; set; } = new HMSData();
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

        private HMSData _testRollMaxRightData { get; set; } = new HMSData();
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

        private HMSData _refRollMeanData { get; set; } = new HMSData();
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
                    OnPropertyChanged(nameof(devRollMeanString));
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

        private HMSData _refRollMeanMaxData { get; set; } = new HMSData();
        public HMSData refRollMeanMaxData
        {
            get
            {
                return _refRollMeanMaxData;
            }
            set
            {
                if (value != null)
                {
                    _refRollMeanMaxData.Set(value);

                    OnPropertyChanged(nameof(meanRollChartAxisMax));
                    OnPropertyChanged(nameof(meanRollChartAxisMin));
                }
            }
        }

        private HMSData _testRollMeanData { get; set; } = new HMSData();
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
                    OnPropertyChanged(nameof(devRollMeanString));                    
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

        private HMSData _testRollMeanMaxData { get; set; } = new HMSData();
        public HMSData testRollMeanMaxData
        {
            get
            {
                return _testRollMeanMaxData;
            }
            set
            {
                if (value != null)
                {
                    _testRollMeanMaxData.Set(value);

                    OnPropertyChanged(nameof(meanRollChartAxisMax));
                    OnPropertyChanged(nameof(meanRollChartAxisMin));
                }
            }
        }

        private HMSData _devRollData { get; set; } = new HMSData();
        public HMSData devRollData
        {
            get
            {
                return _devRollData;
            }
            set
            {
                if (value != null)
                {
                    _devRollData.Set(value);
                }
            }
        }

        private HMSData _devRollMaxData { get; set; } = new HMSData();
        public HMSData devRollMaxData
        {
            get
            {
                return _devRollMaxData;
            }
            set
            {
                if (value != null)
                {
                    _devRollMaxData.Set(value);

                    OnPropertyChanged(nameof(devRollChartAxisMax));
                    OnPropertyChanged(nameof(devRollChartAxisMin));
                    OnPropertyChanged(nameof(devRollMaxString));
                }
            }
        }

        public string devRollMaxString
        {
            get
            {
                if (_devRollMaxData.status == DataStatus.OK && !double.IsNaN(_devRollMaxData.data))
                {
                    return Math.Round(_devRollMaxData.data, 3, MidpointRounding.AwayFromZero).ToString(Constants.numberFormatRecDataSigned);
                }
                else
                {
                    return Constants.NotAvailable;
                }
            }
        }

        private HMSData _devRollMeanData { get; set; } = new HMSData();
        public HMSData devRollMeanData
        {
            get
            {
                return _devRollMeanData;
            }
            set
            {
                if (value != null)
                {
                    _devRollMeanData.Set(value);

                    OnPropertyChanged(nameof(devRollMeanString));
                }
            }
        }

        public string devRollMeanString
        {
            get
            {
                if (_devRollMeanData.status == DataStatus.OK && !double.IsNaN(_devRollMeanData.data))
                {
                    return Math.Round(_devRollMeanData.data, 3, MidpointRounding.AwayFromZero).ToString(Constants.numberFormatRecDataSigned);
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
        private void ClearHeaveData()
        {
            HMSData noData = new HMSData()
            {
                data = double.NaN,
                status = DataStatus.NONE
            };

            refHeaveMaxData = noData;
            refHeaveMeanData = noData;
            refHeaveMeanMaxData = noData;

            testHeaveMaxData = noData;
            testHeaveMeanData = noData;
            testHeaveMeanMaxData = noData;

            refHeaveBuffer.Clear();
            refHeaveMeanBuffer.Clear();
            refHeaveList.Clear();
            refHeaveMeanList.Clear();

            testHeaveBuffer.Clear();
            testHeaveMeanBuffer.Clear();
            testHeaveList.Clear();
            testHeaveMeanList.Clear();

            devHeaveMeanBuffer.Clear();
            devHeaveMeanList.Clear();
        }

        private HMSData _refHeaveData { get; set; } = new HMSData();
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

        private HMSData _testHeaveData { get; set; } = new HMSData();
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

        private HMSData _refHeaveMaxData { get; set; } = new HMSData();
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

        private HMSData _testHeaveMaxData { get; set; } = new HMSData();
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

        private HMSData _refHeaveMeanData { get; set; } = new HMSData();
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
                    OnPropertyChanged(nameof(devHeaveMeanString));
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

        private HMSData _refHeaveMeanMaxData { get; set; } = new HMSData();
        public HMSData refHeaveMeanMaxData
        {
            get
            {
                return _refHeaveMeanMaxData;
            }
            set
            {
                if (value != null)
                {
                    _refHeaveMeanMaxData.Set(value);

                    OnPropertyChanged(nameof(meanHeaveChartAxisMax));
                    OnPropertyChanged(nameof(meanHeaveChartAxisMin));
                }
            }
        }

        private HMSData _testHeaveMeanData { get; set; } = new HMSData();
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

        private HMSData _testHeaveMeanMaxData { get; set; } = new HMSData();
        public HMSData testHeaveMeanMaxData
        {
            get
            {
                return _testHeaveMeanMaxData;
            }
            set
            {
                if (value != null)
                {
                    _testHeaveMeanMaxData.Set(value);

                    OnPropertyChanged(nameof(meanHeaveChartAxisMax));
                    OnPropertyChanged(nameof(meanHeaveChartAxisMin));
                }
            }
        }

        private HMSData _devHeaveData { get; set; } = new HMSData();
        public HMSData devHeaveData
        {
            get
            {
                return _devHeaveData;
            }
            set
            {
                if (value != null)
                {
                    _devHeaveData.Set(value);
                }
            }
        }

        private HMSData _devHeaveMaxData { get; set; } = new HMSData();
        public HMSData devHeaveMaxData
        {
            get
            {
                return _devHeaveMaxData;
            }
            set
            {
                if (value != null)
                {
                    _devHeaveMaxData.Set(value);

                    OnPropertyChanged(nameof(devHeaveChartAxisMax));
                    OnPropertyChanged(nameof(devHeaveChartAxisMin));
                    OnPropertyChanged(nameof(devHeaveMaxString));
                }
            }
        }

        public string devHeaveMaxString
        {
            get
            {
                if (_devHeaveMaxData.status == DataStatus.OK && !double.IsNaN(_devHeaveMaxData.data))
                {
                    return Math.Round(_devHeaveMaxData.data, 3, MidpointRounding.AwayFromZero).ToString(Constants.numberFormatRecDataSigned);
                }
                else
                {
                    return Constants.NotAvailable;
                }
            }
        }

        private HMSData _devHeaveMeanData { get; set; } = new HMSData();
        public HMSData devHeaveMeanData
        {
            get
            {
                return _devHeaveMeanData;
            }
            set
            {
                if (value != null)
                {
                    _devHeaveMeanData.Set(value);

                    OnPropertyChanged(nameof(devHeaveMeanString));
                }
            }
        }

        public string devHeaveMeanString
        {
            get
            {
                if (_devHeaveMeanData.status == DataStatus.OK && !double.IsNaN(_devHeaveMeanData.data))
                {
                    return Math.Round(_devHeaveMeanData.data, 3, MidpointRounding.AwayFromZero).ToString(Constants.numberFormatRecDataSigned);
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
        
        // Pitch
        /////////////////////////////////////////////////
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

        public double devPitchChartAxisMax
        {
            get
            {
                return ChartAxisMax(_devPitchMaxData.data, Constants.InputAxisMargin, Constants.InputAxisDefault);
            }
        }

        public double devPitchChartAxisMin
        {
            get
            {
                return -devPitchChartAxisMax;
            }
        }

        // Roll
        /////////////////////////////////////////////////
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
                return ChartAxisMax(_refRollMeanMaxData, _testRollMeanMaxData, Constants.MeanAxisMargin, Constants.MeanAxisDefault, 0);
            }
        }

        public double meanRollChartAxisMin
        {
            get
            {
                return -meanRollChartAxisMax;
            }
        }

        public double devRollChartAxisMax
        {
            get
            {
                return ChartAxisMax(_devRollMaxData.data, Constants.InputAxisMargin, Constants.InputAxisDefault);
            }
        }

        public double devRollChartAxisMin
        {
            get
            {
                return -devRollChartAxisMax;
            }
        }

        // Heave
        /////////////////////////////////////////////////
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
                return ChartAxisMax(_refHeaveMeanMaxData, _testHeaveMeanMaxData, Constants.MeanAxisDefault, Constants.InputAxisDefault, 0);
            }
        }

        public double meanHeaveChartAxisMin
        {
            get
            {
                return -meanHeaveChartAxisMax;
            }
        }

        public double devHeaveChartAxisMax
        {
            get
            {
                return ChartAxisMax(_devHeaveMaxData.data, Constants.InputAxisMargin, Constants.InputAxisDefault);
            }
        }

        public double devHeaveChartAxisMin
        {
            get
            {
                return -devHeaveChartAxisMax;
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
                    return (double)(Math.Round(((Math.Round(Math.Abs(refData.data) * (1 / margin), MidpointRounding.AwayFromZero) / (1 / margin)) + margin), decimals, MidpointRounding.AwayFromZero));
                else
                    return (double)(Math.Round(((Math.Round(Math.Abs(testData.data) * (1 / margin), MidpointRounding.AwayFromZero) / (1 / margin)) + margin), decimals, MidpointRounding.AwayFromZero));
            }
            else
            if (!double.IsNaN(refData.data))
            {
                return (double)(Math.Round(((Math.Round(Math.Abs(refData.data) * (1 / margin), MidpointRounding.AwayFromZero) / (1 / margin)) + margin), decimals, MidpointRounding.AwayFromZero));
            }
            else
            if (!double.IsNaN(testData.data))
            {
                return (double)(Math.Round(((Math.Round(Math.Abs(testData.data) * (1 / margin), MidpointRounding.AwayFromZero) / (1 / margin)) + margin), decimals, MidpointRounding.AwayFromZero));
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
                return (double)((Math.Round(Math.Abs(inputData) * (1 / margin), MidpointRounding.AwayFromZero) / (1 / margin)) + margin);
            }
            else
            {
                return defaultValue;
            }
        }
    }
}