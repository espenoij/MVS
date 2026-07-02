using System;
using System.Globalization;
using MVS.Models;

namespace MVS.Services
{
    /// <summary>
    /// The axis a verification result card represents. Used to pick the correct
    /// measurement unit and the minimum excitation expected on that axis.
    /// </summary>
    public enum VerificationAxisKind
    {
        Pitch,
        Roll,
        Heave
    }

    /// <summary>
    /// Qualitative verdict on whether the captured input data is good enough for
    /// the calculated deviation on this axis to be trusted.
    /// </summary>
    public enum VerificationStatus
    {
        Unknown,
        Good,
        Acceptable,
        NeedsAttention
    }

    /// <summary>
    /// Outcome of comparing a measured axis deviation against an operator-entered
    /// acceptance threshold (Section 11 of the verification report).
    /// </summary>
    public enum ComplianceResult
    {
        NotAssessed,
        Pass,
        Conditional,
        Fail
    }

    /// <summary>
    /// Judges whether the per-axis deviation produced on the Review step can be
    /// trusted, based on the quality of the input data used to calculate it.
    ///
    /// The deviation itself is the result the application is built to find, so it
    /// is never graded against a target value. Instead this looks at the inputs
    /// that feed the calculation:
    ///   * how many samples were averaged,
    ///   * and how noisy that input data was (outliers).
    ///
    /// While some vessel motion is preferable for validation, it is not required;
    /// the deviation can be measured even when stationary.
    ///
    /// The thresholds below are operator guidance only and have no effect on the
    /// correction that gets applied to the project.
    /// </summary>
    public static class VerificationAssessment
    {
        // ── How many averaged samples we want before the mean deviation is
        //    considered statistically meaningful.
        public const int MinSamplesAcceptable = 300;   // below this: not enough data
        public const int MinSamplesGood = 1500;        // at/above this: well averaged

        // ── Outlier share in the input data. Above the "attention" level the
        //    captured signal is too noisy to trust the averaged result.
        public const double OutlierAcceptablePercent = 5.0;
        public const double OutlierAttentionPercent = 15.0;

        public static string Unit(VerificationAxisKind axis)
        {
            return axis == VerificationAxisKind.Heave ? "m" : "\u00B0";
        }

        // The worst (largest) outlier share across the reference and test inputs.
        private static double WorstOutlier(AxisStatistics reference, AxisStatistics test)
        {
            double worst = double.NaN;
            if (reference != null && reference.SampleCount > 0 && !double.IsNaN(reference.OutlierPercent))
                worst = reference.OutlierPercent;
            if (test != null && test.SampleCount > 0 && !double.IsNaN(test.OutlierPercent))
                worst = double.IsNaN(worst) ? test.OutlierPercent : Math.Max(worst, test.OutlierPercent);
            return worst;
        }

        /// <summary>
        /// Classify whether the input data backing the deviation is good enough to
        /// trust the calculated result. The deviation value itself is not graded.
        /// </summary>
        public static VerificationStatus Classify(VerificationAxisKind axis, AxisStatistics reference, AxisStatistics test, AxisStatistics dev)
        {
            int samples = dev?.SampleCount ?? 0;
            if (samples == 0)
                return VerificationStatus.Unknown;

            double outliers = WorstOutlier(reference, test);

            // Each input-quality dimension votes for a tier; the worst one wins.
            VerificationStatus sampleTier =
                samples >= MinSamplesGood ? VerificationStatus.Good :
                samples >= MinSamplesAcceptable ? VerificationStatus.Acceptable :
                VerificationStatus.NeedsAttention;

            VerificationStatus outlierTier =
                double.IsNaN(outliers) ? VerificationStatus.Acceptable :
                outliers <= OutlierAcceptablePercent ? VerificationStatus.Good :
                outliers <= OutlierAttentionPercent ? VerificationStatus.Acceptable :
                VerificationStatus.NeedsAttention;

            return Worst(sampleTier, outlierTier);
        }

        private static VerificationStatus Worst(VerificationStatus a, VerificationStatus b)
        {
            // Higher enum value = worse here (Unknown=0, Good=1, Acceptable=2, NeedsAttention=3).
            return (int)a >= (int)b ? a : b;
        }

        /// <summary>
        /// Short label for the status badge. Describes the trustworthiness of the
        /// captured data, not the size of the deviation.
        /// </summary>
        public static string StatusLabel(VerificationStatus status)
        {
            switch (status)
            {
                case VerificationStatus.Good: return "Data OK";
                case VerificationStatus.Acceptable: return "Data usable";
                case VerificationStatus.NeedsAttention: return "Data weak";
                default: return "No data";
            }
        }

        // A deviation within the threshold passes; within 1.5x the threshold is a
        // conditional (marginal) pass; anything larger fails.
        private const double ConditionalToleranceFactor = 1.5;

        /// <summary>
        /// Grades a measured mean deviation against an acceptance threshold
        /// (same unit as the deviation). Returns <see cref="ComplianceResult.NotAssessed"/>
        /// when no positive threshold was supplied or the deviation is unavailable.
        /// </summary>
        public static ComplianceResult EvaluateCompliance(double meanDeviation, double? acceptanceThreshold)
        {
            if (!acceptanceThreshold.HasValue ||
                double.IsNaN(acceptanceThreshold.Value) ||
                acceptanceThreshold.Value <= 0 ||
                double.IsNaN(meanDeviation))
            {
                return ComplianceResult.NotAssessed;
            }

            double magnitude = Math.Abs(meanDeviation);
            double threshold = acceptanceThreshold.Value;

            if (magnitude <= threshold)
                return ComplianceResult.Pass;
            if (magnitude <= threshold * ConditionalToleranceFactor)
                return ComplianceResult.Conditional;
            return ComplianceResult.Fail;
        }

        /// <summary>Short report label for a compliance result.</summary>
        public static string ComplianceLabel(ComplianceResult result)
        {
            switch (result)
            {
                case ComplianceResult.Pass: return "Pass";
                case ComplianceResult.Conditional: return "Conditional Pass";
                case ComplianceResult.Fail: return "Fail";
                default: return "Not assessed";
            }
        }

        /// <summary>
        /// One-line, plain-language verdict on whether the captured input data is
        /// solid enough to trust this axis's deviation, naming the limiting factor.
        /// </summary>
        public static string Summary(VerificationAxisKind axis, AxisStatistics reference, AxisStatistics test, AxisStatistics dev)
        {
            string unit = Unit(axis);
            CultureInfo ci = CultureInfo.CurrentCulture;

            int samples = dev?.SampleCount ?? 0;
            if (samples == 0)
                return "No captured data yet, so this axis cannot be validated.";

            double outliers = WorstOutlier(reference, test);

            // Lead with the limiting input-quality factor.
            if (samples < MinSamplesAcceptable)
            {
                return string.Format(ci,
                    "Only {0} samples were averaged ({1}+ recommended). The calculated deviation may not be statistically reliable — capture for longer.",
                    samples, MinSamplesAcceptable);
            }

            if (!double.IsNaN(outliers) && outliers > OutlierAttentionPercent)
            {
                return string.Format(ci,
                    "The input data is noisy ({0:F1}% outliers, max {1:F0}% recommended). Spikes in the captured signal can bias the average — check the sensors and recapture.",
                    outliers, OutlierAttentionPercent);
            }

            // Inputs are at least usable. Report the result as trustworthy and quote
            // the deviation as the produced value (not as a pass/fail).
            string outlierNote = double.IsNaN(outliers)
                ? string.Empty
                : string.Format(ci, ", {0:F1}% outliers", outliers);

            return string.Format(ci,
                "Inputs look solid: {0} samples{1}. The calculated deviation of {2:F3}{3} can be trusted.",
                samples, outlierNote, dev.Mean, unit);
        }

        /// <summary>
        /// Full glossary of every metric on the card plus the input-quality bands
        /// that decide whether the deviation can be trusted. Intended for an info
        /// dialog.
        /// </summary>
        public static string Glossary(VerificationAxisKind axis)
        {
            string unit = Unit(axis);
            CultureInfo ci = CultureInfo.CurrentCulture;

            return
                "WHAT THE NUMBERS MEAN\n\n" +
                "Reference MRU\n" +
                "    The trusted baseline motion (mean over the capture).\n\n" +
                "Vessel MRU\n" +
                "    The unit being verified against the reference.\n\n" +
                "Deviation\n" +
                "    Vessel minus Reference. This is the value the app calculates;\n" +
                "    it is the result, not a pass/fail score.\n\n" +
                "σ (sigma)\n" +
                "    Standard deviation: how much the signal varies.\n\n" +
                "min/max\n" +
                "    The most extreme values seen during the capture.\n\n" +
                "RMS\n" +
                "    Root-mean-square magnitude of the signal.\n\n" +
                "outliers\n" +
                "    Percent of samples flagged by Tukey's 1.5×IQR rule.\n\n" +
                "samples\n" +
                "    Number of data points averaged.\n\n\n" +
                "CAN THE DEVIATION BE TRUSTED?\n\n" +
                "The trustworthiness depends on INPUT data quality, not the deviation value:\n\n" +
                string.Format(ci,
                    "Samples averaged\n" +
                    "    ≥ {0}  samples → usable\n" +
                    "    ≥ {1} samples → good\n\n",
                    MinSamplesAcceptable, MinSamplesGood) +
                string.Format(ci,
                    "Input noise\n" +
                    "    ≤ {0:F0}% outliers → good\n" +
                    "    ≤ {1:F0}% outliers → usable\n" +
                    "    > {1:F0}% outliers → too noisy\n\n",
                    OutlierAcceptablePercent, OutlierAttentionPercent) +
                "The deviation can be measured even when the vessel is stationary, though\n" +
                "some motion is preferable for validation. If input data is too noisy or too\n" +
                "few samples were captured, record for longer before relying on the calculated deviation.";
        }
    }
}
