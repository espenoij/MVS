using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MVS.Models;

namespace MVSTests.Services
{
    /// <summary>
    /// Tests for <see cref="PairedSeriesStatistics"/>: Pearson correlation and
    /// the cross-correlation latency estimate used by the verification report.
    /// </summary>
    [TestClass]
    public class PairedSeriesStatisticsTests
    {
        [TestMethod]
        public void Compute_IdenticalSeries_CorrelationIsOne_LatencyZero()
        {
            double[] signal = MakeAperiodic(200, samplesPerCycle: 20);

            PairedSeriesStatistics stats = PairedSeriesStatistics.Compute(signal, signal, sampleIntervalSeconds: 0.1);

            Assert.AreEqual(200, stats.SampleCount);
            Assert.AreEqual(1.0, stats.Correlation, 1e-9);
            Assert.AreEqual(0.0, stats.EstimatedLatencySeconds, 1e-9);
        }

        [TestMethod]
        public void Compute_InvertedSeries_CorrelationIsMinusOne()
        {
            double[] reference = MakeSine(200, samplesPerCycle: 25);
            double[] test = reference.Select(v => -v).ToArray();

            PairedSeriesStatistics stats = PairedSeriesStatistics.Compute(reference, test, sampleIntervalSeconds: 0.1);

            Assert.AreEqual(-1.0, stats.Correlation, 1e-9);
        }

        [TestMethod]
        public void Compute_ShiftedTest_EstimatesPositiveLatency()
        {
            // Test signal lags the reference by a known number of samples.
            const int lagSamples = 3;
            const double interval = 0.1; // 10 Hz
            double[] reference = MakeAperiodic(400, samplesPerCycle: 40);
            double[] test = ShiftRight(reference, lagSamples);

            PairedSeriesStatistics stats = PairedSeriesStatistics.Compute(reference, test, interval);

            Assert.AreEqual(lagSamples * interval, stats.EstimatedLatencySeconds, 1e-9,
                "A test signal that lags the reference should report a positive latency.");
        }

        [TestMethod]
        public void Compute_NoSampleInterval_LatencyIsNaN()
        {
            double[] signal = MakeSine(100, samplesPerCycle: 20);

            PairedSeriesStatistics stats = PairedSeriesStatistics.Compute(signal, signal, sampleIntervalSeconds: 0);

            Assert.IsTrue(double.IsNaN(stats.EstimatedLatencySeconds));
        }

        [TestMethod]
        public void Compute_DropsNonFinitePairs()
        {
            var reference = new List<double> { 1, 2, double.NaN, 4, 5, 6, 7, 8 };
            var test = new List<double> { 1, 2, 3, 4, double.PositiveInfinity, 6, 7, 8 };

            PairedSeriesStatistics stats = PairedSeriesStatistics.Compute(reference, test, sampleIntervalSeconds: 0.1);

            // Two pairs (indices 2 and 4) are dropped -> 6 remain.
            Assert.AreEqual(6, stats.SampleCount);
        }

        [TestMethod]
        public void Compute_FlatSeries_CorrelationIsNaN()
        {
            double[] reference = MakeSine(100, samplesPerCycle: 20);
            double[] flat = Enumerable.Repeat(3.0, 100).ToArray();

            PairedSeriesStatistics stats = PairedSeriesStatistics.Compute(reference, flat, sampleIntervalSeconds: 0.1);

            Assert.IsTrue(double.IsNaN(stats.Correlation));
        }

        [TestMethod]
        public void Compute_NullInput_ReturnsEmpty()
        {
            PairedSeriesStatistics stats = PairedSeriesStatistics.Compute(null, null, 0.1);

            Assert.AreEqual(0, stats.SampleCount);
            Assert.IsTrue(double.IsNaN(stats.Correlation));
        }

        private static double[] MakeSine(int count, int samplesPerCycle)
        {
            var values = new double[count];
            for (int i = 0; i < count; i++)
                values[i] = Math.Sin(2 * Math.PI * i / samplesPerCycle);
            return values;
        }

        // A sine superimposed on a slow linear ramp so the waveform never
        // repeats. This gives the cross-correlation a single, unambiguous peak
        // at the true lag (a pure sine peaks equally at every full-period shift).
        private static double[] MakeAperiodic(int count, int samplesPerCycle)
        {
            var values = new double[count];
            for (int i = 0; i < count; i++)
                values[i] = Math.Sin(2 * Math.PI * i / samplesPerCycle) + 0.02 * i;
            return values;
        }

        // Returns a copy where element i takes the reference value from i-shift
        // (so the returned signal lags the reference by 'shift' samples).
        private static double[] ShiftRight(double[] source, int shift)
        {
            var shifted = new double[source.Length];
            for (int i = 0; i < source.Length; i++)
                shifted[i] = source[Math.Max(0, i - shift)];
            return shifted;
        }
    }
}
