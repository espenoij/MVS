using System.Collections.Generic;
using System.Linq;
using Telerik.Windows.Data;

namespace MVS
{
    public enum ReadinessSeverity
    {
        /// <summary>Recording cannot start until this item passes.</summary>
        Blocking,
        /// <summary>Advisory only — the user may override and start anyway.</summary>
        Advisory
    }

    public sealed class ReadinessItem
    {
        public string             Label    { get; }
        public bool               Pass     { get; }
        public ReadinessSeverity  Severity { get; }

        public ReadinessItem(string label, bool pass, ReadinessSeverity severity)
        {
            Label    = label;
            Pass     = pass;
            Severity = severity;
        }
    }

    public sealed class ReadinessReport
    {
        public IReadOnlyList<ReadinessItem> Items              { get; }
        public bool                         HasBlockingFailure { get; }
        public bool                         HasAnyFailure      { get; }
        /// <summary>True when every item passes — no dialog needs to be shown.</summary>
        public bool                         AllGood            { get; }

        public ReadinessReport(IReadOnlyList<ReadinessItem> items)
        {
            Items              = items;
            HasBlockingFailure = items.Any(i => !i.Pass && i.Severity == ReadinessSeverity.Blocking);
            HasAnyFailure      = items.Any(i => !i.Pass);
            AllGood            = !HasAnyFailure;
        }
    }

    /// <summary>
    /// Evaluates pre-capture readiness conditions and returns a <see cref="ReadinessReport"/>.
    /// No WPF dependencies — purely testable logic.
    /// </summary>
    public static class CaptureReadinessChecker
    {
        /// <summary>
        /// Runs all readiness checks and returns the combined report.
        /// </summary>
        /// <param name="sensorDataList">
        ///   The live sensor list from <c>SensorDataRetrieval.GetSensorDataList()</c>.
        ///   Used to verify that both Reference MRU and Vessel MRU sensors are configured.
        /// </param>
        /// <param name="database">
        ///   The <see cref="DatabaseHandler"/> instance. Used to read
        ///   <see cref="DatabaseHandler.IsDatabaseConnectionOK"/>.
        /// </param>
        /// <param name="livoxCorrection">
        ///   The shared <see cref="LivoxLidarCorrection"/> object. Used to check whether a
        ///   helideck correction has been applied.
        /// </param>
        public static ReadinessReport Evaluate(
            RadObservableCollection<SensorData> sensorDataList,
            DatabaseHandler                     database,
            LivoxLidarCorrection                livoxCorrection)
        {
            var items = new List<ReadinessItem>();

            // ── 1. Reference MRU sensor configured ──────────────────────────────────
            bool refConfigured = sensorDataList != null &&
                                 sensorDataList.Any(s => s.mruType  == MRUType.ReferenceMRU &&
                                                         s.type    != SensorType.None);
            items.Add(new ReadinessItem(
                "Reference MRU sensor configured",
                refConfigured,
                ReadinessSeverity.Blocking));

            // ── 2. Vessel MRU sensor configured ─────────────────────────────────────
            bool testConfigured = sensorDataList != null &&
                                  sensorDataList.Any(s => s.mruType == MRUType.TestMRU &&
                                                          s.type   != SensorType.None);
            items.Add(new ReadinessItem(
                "Vessel MRU sensor configured",
                testConfigured,
                ReadinessSeverity.Blocking));

            // ── 3. Database connection ───────────────────────────────────────────────
            bool dbOk = database?.IsDatabaseConnectionOK == true;
            items.Add(new ReadinessItem(
                "Database connection reachable",
                dbOk,
                ReadinessSeverity.Advisory));

            // ── 4. LiDAR helideck correction applied ─────────────────────────────────
            bool lidarApplied = livoxCorrection?.IsActive == true;
            items.Add(new ReadinessItem(
                "LiDAR helideck correction applied",
                lidarApplied,
                ReadinessSeverity.Advisory));

            return new ReadinessReport(items);
        }
    }
}
