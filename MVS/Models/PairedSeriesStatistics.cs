using System;
using System.Collections.Generic;
using System.Linq;

namespace MVS.Models
{
    /// <summary>
    /// Pairwise statistics between the reference series and the test (vessel)
    /// series for a single axis: the Pearson correlation coefficient and an
    /// estimated latency of the test signal relative to the reference.
    ///
    /// Latency is estimated by cross-correlation: the sample lag that maximises
    /// the correlation between the reference and the (shifted) test series is
    /// converted to seconds using the capture's sample interval. A positive
    /// latency means the test/vessel unit lags the reference.
    /// </summary>
    public class PairedSeriesStatistics
    {
        // Only search for latencies within this window (seconds). MRU output
        // latency is typically well under a second; the margin covers logging
        // and transport delays without letting the search run away on long,
        // high-rate captures.
        private const double MaxLatencySeconds = 3.0;

        // Hard cap on the number of lags evaluated, to bound the O(lag * n) cost
        // on very high sample-rate captures.
        private const int MaxLagSamplesCap = 250;

        public int SampleCount { get; set; }

        /// <summary>Pearson correlation coefficient at zero lag (-1..+1).</summary>
        public double Correlation { get; set; }

        /// <summary>
        /// Estimated latency in seconds of the test series relative to the
        /// reference (positive = test lags reference). NaN when it cannot be
        /// determined (no sample interval, too few samples, or flat signal).
        /// </summary>
        public double EstimatedLatencySeconds { get; set; }

        public static readonly PairedSeriesStatistics Empty = new PairedSeriesStatistics
        {
            SampleCount = 0,
            Correlation = double.NaN,
            EstimatedLatencySeconds = double.NaN
        };

        /// <summary>
        /// Computes correlation and estimated latency between two equal-length,
        /// time-aligned series. Pairs containing a non-finite value in either
        /// series are dropped before any statistics are computed.
        /// </summary>
        /// <param name="reference">Reference MRU samples.</param>
        /// <param name="test">Test/vessel MRU samples.</param>
        /// <param name="sampleIntervalSeconds">
        /// Mean time between samples; pass 0 (or less) if unknown, in which case
        /// latency is reported as NaN.
        /// </param>
        public static PairedSeriesStatistics Compute(IEnumerable<double> reference, IEnumerable<double> test, double sampleIntervalSeconds)
        {
            if (reference == null || test == null)
                return Empty;

            double[] r = reference.ToArray();
            double[] t = test.ToArray();
            int n = Math.Min(r.Length, t.Length);

            // Keep only pairs that are finite in both series.
            var refClean = new List<double>(n);
            var testClean = new List<double>(n);
            for (int i = 0; i < n; i++)
            {
                double a = r[i];
                double b = t[i];
                if (double.IsNaN(a) || double.IsInfinity(a) || double.IsNaN(b) || double.IsInfinity(b))
                    continue;
                refClean.Add(a);
                testClean.Add(b);
            }

            int m = refClean.Count;
            if (m < 2)
                return Empty;

            double correlation = LaggedCorrelation(refClean, testClean, 0);

            double latency = double.NaN;
            if (sampleIntervalSeconds > 0 && m >= 8)
            {
                int maxLag = (int)Math.Round(MaxLatencySeconds / sampleIntervalSeconds);
                maxLag = Math.Min(maxLag, m / 4);
                maxLag = Math.Min(maxLag, MaxLagSamplesCap);

                if (maxLag >= 1)
                {
                    int bestLag = 0;
                    double bestCorr = double.NegativeInfinity;
                    for (int lag = -maxLag; lag <= maxLag; lag++)
                    {
                        double c = LaggedCorrelation(refClean, testClean, lag);
                        if (!double.IsNaN(c) && c > bestCorr)
                        {
                            bestCorr = c;
                            bestLag = lag;
                        }
                    }

                    if (!double.IsNegativeInfinity(bestCorr))
                        latency = bestLag * sampleIntervalSeconds;
                }
            }

            return new PairedSeriesStatistics
            {
                SampleCount = m,
                Correlation = correlation,
                EstimatedLatencySeconds = latency
            };
        }

        /// <summary>
        /// Pearson correlation between reference[i] and test[i + lag] over the
        /// overlapping region. Returns NaN when the overlap is too small or a
        /// series is flat (zero variance).
        /// </summary>
        private static double LaggedCorrelation(IList<double> reference, IList<double> test, int lag)
        {
            int m = reference.Count;
            int start = Math.Max(0, -lag);
            int end = Math.Min(m - 1, m - 1 - lag);
            int count = end - start + 1;
            if (count < 2)
                return double.NaN;

            double sumRef = 0, sumTest = 0, sumRefRef = 0, sumTestTest = 0, sumRefTest = 0;
            for (int i = start; i <= end; i++)
            {
                double x = reference[i];
                double y = test[i + lag];
                sumRef += x;
                sumTest += y;
                sumRefRef += x * x;
                sumTestTest += y * y;
                sumRefTest += x * y;
            }

            double denom = Math.Sqrt((count * sumRefRef - sumRef * sumRef) * (count * sumTestTest - sumTest * sumTest));
            if (denom <= 0)
                return double.NaN;

            return (count * sumRefTest - sumRef * sumTest) / denom;
        }
    }
}
