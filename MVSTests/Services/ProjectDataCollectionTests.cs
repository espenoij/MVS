using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MVS;
using MVS.Models;
using MVSTests.TestInfrastructure;
using Telerik.Windows.Data;

namespace MVSTests.Services
{
    [TestClass]
    public class ProjectDataCollectionTests
    {
        [TestMethod]
        public void ProjectSamples_MatchTelerikLoadAndClearNotifications()
        {
            StaTestHelper.Run(() =>
            {
                var legacy = new RadObservableCollection<ProjectData>();
                var current = new ProjectVM().projectsDataList;
                var legacyEvents = CaptureEvents(legacy);
                var currentEvents = CaptureEvents(current);
                var first = new ProjectData { id = 1 };
                var second = new ProjectData { id = 2 };

                legacy.Clear();
                current.Clear();
                legacy.Add(first);
                current.Add(first);
                legacy.Add(second);
                current.Add(second);
                CollectionAssert.AreEqual(legacy.ToArray(), current.ToArray());
                legacy.Clear();
                current.Clear();
                legacy.Add(second);
                current.Add(second);

                CollectionAssert.AreEqual(legacy.ToArray(), current.ToArray());
                CollectionAssert.AreEqual(legacyEvents, currentEvents);
            });
        }

        [TestMethod]
        public void LoadSessionData_DisconnectedDatabaseClearsSamplesAndResetsError()
        {
            StaTestHelper.Run(() =>
            {
                var handler = new DatabaseHandler(null, isDatabaseConnectionOK: false);
                var errors = new ErrorHandler(handler);
                var database = new MVSDatabase(handler, errors);
                var samples = new ProjectVM().projectsDataList;
                samples.Add(new ProjectData { id = 1 });
                errors.SetDatabaseError(ErrorHandler.DatabaseErrorType.LoadSessionData);

                database.LoadSessionData(null, samples);

                Assert.AreEqual(0, samples.Count);
                Assert.IsFalse(errors.IsDatabaseError());
            });
        }

        [TestMethod]
        public void ProjectSamples_ProduceAndClearAnalysisStatistics()
        {
            StaTestHelper.Run(() =>
            {
                var project = new ProjectVM();
                Assert.IsFalse(project.IsAnalysisAvailable);
                project.projectsDataList.Add(new ProjectData
                {
                    timestamp = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    refPitch = 1,
                    testPitch = 2
                });
                project.projectsDataList.Add(new ProjectData
                {
                    timestamp = new DateTime(2026, 1, 1, 0, 0, 1, DateTimeKind.Utc),
                    refPitch = 3,
                    testPitch = 4
                });

                project.ComputeExtendedStatistics();

                Assert.IsTrue(project.IsAnalysisAvailable);
                Assert.AreEqual(2, project.RefPitchStats.SampleCount);
                Assert.AreEqual(2d, project.RefPitchStats.Mean);
                Assert.AreEqual(3d, project.TestPitchStats.Mean);
                Assert.AreEqual(1d, project.RecommendedCorrectionPitch);
                Assert.AreEqual(2, project.PitchPairStats.SampleCount);
                Assert.AreEqual(1d, project.PitchPairStats.Correlation, 1e-10);

                project.projectsDataList.Clear();
                project.ComputeExtendedStatistics();
                Assert.IsFalse(project.IsAnalysisAvailable);
                Assert.AreEqual(0, project.RefPitchStats.SampleCount);
                Assert.AreEqual(0d, project.RecommendedCorrectionPitch);
            });
        }

        [TestMethod]
        public void ProjectSamples_ProduceAnalysisChartsAndProgress()
        {
            StaTestHelper.Run(() =>
            {
                var input = new MVSDataCollection();
                foreach (var id in new[]
                {
                    global::ValueType.Ref_Pitch, global::ValueType.Ref_Roll, global::ValueType.Ref_Heave,
                    global::ValueType.Test_Pitch, global::ValueType.Test_Roll, global::ValueType.Test_Heave
                })
                    input.GetDataList().Add(new HMSData { id = (int)id, dataId = (int)id });

                var output = new MVSDataCollection();
                var main = new MainWindowVM
                {
                    SelectedProject = new Project { InputMRUs = InputMRUType.ReferenceMRU_TestMRU },
                    OperationsMode = OperationsMode.ViewData
                };
                var project = new ProjectVM();
                project.Init(main, new MVSProcessing(output, new AdminSettingsVM(), null), input, output);
                var timestamp = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                project.projectsDataList.Add(new ProjectData { timestamp = timestamp, refPitch = 1, testPitch = 2 });
                project.projectsDataList.Add(new ProjectData { timestamp = timestamp.AddSeconds(1), refPitch = 3, testPitch = 4 });
                var progress = new List<int>();

                project.AnalyseProjectData(progress.Add);
                project.TransferToDisplay();

                CollectionAssert.AreEqual(new[] { 0, 50 }, progress);
                CollectionAssert.AreEqual(new[] { 1d, 3d }, project.refPitchList.Select(x => x.data).ToArray());
                CollectionAssert.AreEqual(new[] { 2d, 4d }, project.testPitchList.Select(x => x.data).ToArray());
                Assert.AreEqual(timestamp.AddSeconds(1), project.alignmentTime);
                Assert.AreEqual(1d, project.RecommendedCorrectionPitch);
            });
        }

        private static List<string> CaptureEvents(INotifyCollectionChanged collection)
        {
            var events = new List<string>();
            ((INotifyPropertyChanged)collection).PropertyChanged += (_, e) => events.Add("P:" + e.PropertyName);
            collection.CollectionChanged += (_, e) =>
                events.Add(string.Format("C:{0}:{1}:{2}", e.Action, e.NewStartingIndex, e.OldStartingIndex));
            return events;
        }
    }
}
