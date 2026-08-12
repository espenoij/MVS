using Microsoft.VisualStudio.TestTools.UnitTesting;
using MVS;
using System.Collections.Generic;
using Telerik.Windows.Data;

namespace MVSTests.Services
{
    [TestClass]
    public class ReadinessReportTests
    {
        // ── ReadinessReport flag derivation ─────────────────────────────────────

        [TestMethod]
        public void AllGood_WhenEveryItemPasses()
        {
            var items = new List<ReadinessItem>
            {
                new ReadinessItem("A", true, ReadinessSeverity.Blocking),
                new ReadinessItem("B", true, ReadinessSeverity.Advisory),
            };
            var report = new ReadinessReport(items);

            Assert.IsTrue(report.AllGood);
            Assert.IsFalse(report.HasAnyFailure);
            Assert.IsFalse(report.HasBlockingFailure);
        }

        [TestMethod]
        public void HasBlockingFailure_WhenBlockingItemFails()
        {
            var items = new List<ReadinessItem>
            {
                new ReadinessItem("A", false, ReadinessSeverity.Blocking),
                new ReadinessItem("B", true,  ReadinessSeverity.Advisory),
            };
            var report = new ReadinessReport(items);

            Assert.IsFalse(report.AllGood);
            Assert.IsTrue(report.HasAnyFailure);
            Assert.IsTrue(report.HasBlockingFailure);
        }

        [TestMethod]
        public void HasAnyFailure_ButNotBlocking_WhenOnlyAdvisoryFails()
        {
            var items = new List<ReadinessItem>
            {
                new ReadinessItem("A", true,  ReadinessSeverity.Blocking),
                new ReadinessItem("B", false, ReadinessSeverity.Advisory),
            };
            var report = new ReadinessReport(items);

            Assert.IsFalse(report.AllGood);
            Assert.IsTrue(report.HasAnyFailure);
            Assert.IsFalse(report.HasBlockingFailure);
        }

        [TestMethod]
        public void Items_AreExposedUnchanged()
        {
            var items = new List<ReadinessItem>
            {
                new ReadinessItem("Check 1", true,  ReadinessSeverity.Blocking),
                new ReadinessItem("Check 2", false, ReadinessSeverity.Advisory),
            };
            var report = new ReadinessReport(items);

            Assert.AreEqual(2, report.Items.Count);
            Assert.AreEqual("Check 1", report.Items[0].Label);
            Assert.AreEqual("Check 2", report.Items[1].Label);
        }
    }

    [TestClass]
    public class CaptureReadinessCheckerTests
    {
        // ── helpers ──────────────────────────────────────────────────────────────

        private static RadObservableCollection<SensorData> MakeSensorList(
            bool hasRef, bool hasTest)
        {
            var list = new RadObservableCollection<SensorData>();

            if (hasRef)
            {
                var s = new SensorData();
                s.type    = SensorType.SerialPort;
                s.mruType = MRUType.ReferenceMRU;
                list.Add(s);
            }

            if (hasTest)
            {
                var s = new SensorData();
                s.type    = SensorType.SerialPort;
                s.mruType = MRUType.TestMRU;
                list.Add(s);
            }

            return list;
        }

        private static DatabaseHandler ConnectedDb()  => new DatabaseHandler(null, isDatabaseConnectionOK: true);
        private static DatabaseHandler DisconnectedDb() => new DatabaseHandler(null, isDatabaseConnectionOK: false);

        private static LivoxLidarCorrection ActiveCorrection()
        {
            var c = new LivoxLidarCorrection();
            c.Apply(pitchOffset: 0, rollOffset: 0, headingOffset: 0, fitRmse: 0, pointCount: 100);
            return c;
        }

        private static LivoxLidarCorrection InactiveCorrection() => new LivoxLidarCorrection();

        // ── Evaluate always returns exactly 4 items ──────────────────────────────

        [TestMethod]
        public void Evaluate_ReturnsExactlyFourItems()
        {
            var report = CaptureReadinessChecker.Evaluate(
                MakeSensorList(true, true),
                ConnectedDb(),
                ActiveCorrection());

            Assert.AreEqual(4, report.Items.Count);
        }

        // ── All-good path ────────────────────────────────────────────────────────

        [TestMethod]
        public void Evaluate_AllGood_WhenEverythingConfigured()
        {
            var report = CaptureReadinessChecker.Evaluate(
                MakeSensorList(true, true),
                ConnectedDb(),
                ActiveCorrection());

            Assert.IsTrue(report.AllGood);
        }

        // ── Blocking failures ────────────────────────────────────────────────────

        [TestMethod]
        public void Evaluate_BlockingFailure_WhenReferenceMruMissing()
        {
            var report = CaptureReadinessChecker.Evaluate(
                MakeSensorList(hasRef: false, hasTest: true),
                ConnectedDb(),
                ActiveCorrection());

            Assert.IsTrue(report.HasBlockingFailure);
            Assert.IsFalse(report.Items[0].Pass);   // index 0 = Reference MRU
            Assert.AreEqual(ReadinessSeverity.Blocking, report.Items[0].Severity);
        }

        [TestMethod]
        public void Evaluate_BlockingFailure_WhenVesselMruMissing()
        {
            var report = CaptureReadinessChecker.Evaluate(
                MakeSensorList(hasRef: true, hasTest: false),
                ConnectedDb(),
                ActiveCorrection());

            Assert.IsTrue(report.HasBlockingFailure);
            Assert.IsFalse(report.Items[1].Pass);   // index 1 = Vessel MRU
            Assert.AreEqual(ReadinessSeverity.Blocking, report.Items[1].Severity);
        }

        [TestMethod]
        public void Evaluate_BlockingFailure_WhenBothMrusMissing()
        {
            var report = CaptureReadinessChecker.Evaluate(
                MakeSensorList(hasRef: false, hasTest: false),
                ConnectedDb(),
                ActiveCorrection());

            Assert.IsTrue(report.HasBlockingFailure);
            Assert.IsFalse(report.Items[0].Pass);
            Assert.IsFalse(report.Items[1].Pass);
        }

        // ── Advisory failures ────────────────────────────────────────────────────

        [TestMethod]
        public void Evaluate_AdvisoryFailure_WhenDatabaseDisconnected()
        {
            var report = CaptureReadinessChecker.Evaluate(
                MakeSensorList(true, true),
                DisconnectedDb(),
                ActiveCorrection());

            Assert.IsFalse(report.AllGood);
            Assert.IsFalse(report.HasBlockingFailure);
            Assert.IsFalse(report.Items[2].Pass);   // index 2 = database
            Assert.AreEqual(ReadinessSeverity.Advisory, report.Items[2].Severity);
        }

        [TestMethod]
        public void Evaluate_AdvisoryFailure_WhenLidarCorrectionInactive()
        {
            var report = CaptureReadinessChecker.Evaluate(
                MakeSensorList(true, true),
                ConnectedDb(),
                InactiveCorrection());

            Assert.IsFalse(report.AllGood);
            Assert.IsFalse(report.HasBlockingFailure);
            Assert.IsFalse(report.Items[3].Pass);   // index 3 = LiDAR correction
            Assert.AreEqual(ReadinessSeverity.Advisory, report.Items[3].Severity);
        }

        // ── Null-safety ──────────────────────────────────────────────────────────

        [TestMethod]
        public void Evaluate_NullSensorList_BlockingFailuresForBothMrus()
        {
            var report = CaptureReadinessChecker.Evaluate(
                null,
                ConnectedDb(),
                ActiveCorrection());

            Assert.IsTrue(report.HasBlockingFailure);
            Assert.IsFalse(report.Items[0].Pass);
            Assert.IsFalse(report.Items[1].Pass);
        }

        [TestMethod]
        public void Evaluate_NullDatabase_AdvisoryFailureForDatabase()
        {
            var report = CaptureReadinessChecker.Evaluate(
                MakeSensorList(true, true),
                null,
                ActiveCorrection());

            Assert.IsFalse(report.Items[2].Pass);
            Assert.AreEqual(ReadinessSeverity.Advisory, report.Items[2].Severity);
        }

        [TestMethod]
        public void Evaluate_NullLivoxCorrection_AdvisoryFailureForLidar()
        {
            var report = CaptureReadinessChecker.Evaluate(
                MakeSensorList(true, true),
                ConnectedDb(),
                null);

            Assert.IsFalse(report.Items[3].Pass);
            Assert.AreEqual(ReadinessSeverity.Advisory, report.Items[3].Severity);
        }

        [TestMethod]
        public void Evaluate_AllNullArguments_DoesNotThrow()
        {
            ReadinessReport report = null;
            try
            {
                report = CaptureReadinessChecker.Evaluate(null, null, null);
            }
            catch
            {
                Assert.Fail("Evaluate threw an exception when given all-null arguments.");
            }

            Assert.IsNotNull(report);
            Assert.IsTrue(report.HasBlockingFailure);
        }

        // ── Mixed blocking + advisory failures ───────────────────────────────────

        [TestMethod]
        public void Evaluate_MixedFailures_HasBothFlagsSets()
        {
            var report = CaptureReadinessChecker.Evaluate(
                MakeSensorList(hasRef: false, hasTest: true),
                DisconnectedDb(),
                InactiveCorrection());

            Assert.IsTrue(report.HasBlockingFailure);
            Assert.IsTrue(report.HasAnyFailure);
            Assert.IsFalse(report.AllGood);
        }

        // ── Sensor with SensorType.None is not counted as configured ─────────────

        [TestMethod]
        public void Evaluate_SensorTypeNone_IsNotCountedAsConfigured()
        {
            // Add a ReferenceMRU entry whose type is SensorType.None — should fail
            var list = new RadObservableCollection<SensorData>();
            var s = new SensorData();
            // leave s.type as default SensorType.None
            s.mruType = MRUType.ReferenceMRU;
            list.Add(s);

            // Add a proper Vessel MRU
            var t = new SensorData();
            t.type    = SensorType.SerialPort;
            t.mruType = MRUType.TestMRU;
            list.Add(t);

            var report = CaptureReadinessChecker.Evaluate(list, ConnectedDb(), ActiveCorrection());

            Assert.IsFalse(report.Items[0].Pass, "SensorType.None should not count as a configured Reference MRU.");
            Assert.IsTrue(report.Items[1].Pass);
        }
    }
}
