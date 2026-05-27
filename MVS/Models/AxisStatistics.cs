using System;
using System.Collections.Generic;
using System.Linq;

namespace MVS.Models
{
    /// <summary>
    /// Extended per-axis statistics derived from raw motion samples
    /// (reference, test, or deviation series). These are used by the
    /// verification wizard to give a richer picture than mean alone.
    /// </summary>
    public class AxisStatistics
    {
        public int SampleCount { get; set; }
        public double Mean { get; set; }
        public double StdDev { get; set; }
        public double Min { get; set; }
        public double Max { get; set; }
        public double Rms { get; set; }
        // Percent of samples flagged as outliers by Tukey's 1.5*IQR rule.
        public double OutlierPercent { get; set; }

        public static readonly AxisStatistics Empty = new AxisStatistics
        {
            SampleCount = 0,
            Mean = double.NaN,
            StdDev = double.NaN,
            Min = double.NaN,
            Max = double.NaN,
            Rms = double.NaN,
            OutlierPercent = double.NaN
        };

        public static AxisStatistics Compute(IEnumerable<double> samples)
        {
            if (samples == null)
                return Empty;

            // Filter out NaN/Infinity once so all downstream math stays sane.
            var values = samples.Where(v => !double.IsNaN(v) && !double.IsInfinity(v)).ToArray();
            if (values.Length == 0)
                return Empty;

            double sum = 0;
            double sumSq = 0;
            double min = double.PositiveInfinity;
            double max = double.NegativeInfinity;

            for (int i = 0; i < values.Length; i++)
            {
                double v = values[i];
                sum += v;
                sumSq += v * v;
                if (v < min) min = v;
                if (v > max) max = v;
            }

            double mean = sum / values.Length;
            double rms = Math.Sqrt(sumSq / values.Length);
            double variance = (sumSq / values.Length) - (mean * mean);
            if (variance < 0) variance = 0; // numerical safety
            double stdDev = Math.Sqrt(variance);

            // Tukey 1.5*IQR outlier percentage.
            double outlierPercent = 0;
            if (values.Length >= 4)
            {
                var sorted = (double[])values.Clone();
                Array.Sort(sorted);
                double q1 = Percentile(sorted, 25);
                double q3 = Percentile(sorted, 75);
                double iqr = q3 - q1;
                double lower = q1 - 1.5 * iqr;
                double upper = q3 + 1.5 * iqr;
                int outliers = 0;
                for (int i = 0; i < values.Length; i++)
                {
                    if (values[i] < lower || values[i] > upper)
                        outliers++;
                }
                outlierPercent = (outliers * 100.0) / values.Length;
            }

            return new AxisStatistics
            {
                SampleCount = values.Length,
                Mean = mean,
                StdDev = stdDev,
                Min = min,
                Max = max,
                Rms = rms,
                OutlierPercent = outlierPercent
            };
        }

        // Linear interpolation percentile on a pre-sorted array.
        private static double Percentile(double[] sorted, double percentile)
        {
            if (sorted.Length == 1) return sorted[0];
            double rank = (percentile / 100.0) * (sorted.Length - 1);
            int lower = (int)Math.Floor(rank);
            int upper = (int)Math.Ceiling(rank);
            if (lower == upper) return sorted[lower];
            double weight = rank - lower;
            return sorted[lower] * (1 - weight) + sorted[upper] * weight;
        }
    }
}
