using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using MVS.Models;

namespace MVS.Views.Controls
{
    /// <summary>
    /// Card showing reference, test, and deviation statistics for a single axis
    /// (pitch, roll or heave).
    /// </summary>
    public partial class VerificationResultCard : UserControl
    {
        public VerificationResultCard()
        {
            InitializeComponent();
            Refresh();
        }

        public static readonly DependencyProperty AxisTitleProperty =
            DependencyProperty.Register(nameof(AxisTitle), typeof(string), typeof(VerificationResultCard),
                new PropertyMetadata("Axis", OnSimpleChanged));

        public static readonly DependencyProperty UnitProperty =
            DependencyProperty.Register(nameof(Unit), typeof(string), typeof(VerificationResultCard),
                new PropertyMetadata("", OnSimpleChanged));

        public static readonly DependencyProperty RefStatsProperty =
            DependencyProperty.Register(nameof(RefStats), typeof(AxisStatistics), typeof(VerificationResultCard),
                new PropertyMetadata(null, OnSimpleChanged));

        public static readonly DependencyProperty TestStatsProperty =
            DependencyProperty.Register(nameof(TestStats), typeof(AxisStatistics), typeof(VerificationResultCard),
                new PropertyMetadata(null, OnSimpleChanged));

        public static readonly DependencyProperty DevStatsProperty =
            DependencyProperty.Register(nameof(DevStats), typeof(AxisStatistics), typeof(VerificationResultCard),
                new PropertyMetadata(null, OnSimpleChanged));

        public static readonly DependencyProperty AppliedCorrectionProperty =
            DependencyProperty.Register(nameof(AppliedCorrection), typeof(double), typeof(VerificationResultCard),
                new PropertyMetadata(0d));

        public static readonly DependencyProperty HasCorrectionAppliedProperty =
            DependencyProperty.Register(nameof(HasCorrectionApplied), typeof(bool), typeof(VerificationResultCard),
                new PropertyMetadata(false));

        // Reference scale for the magnitude bar (e.g., 1.0 degree or meter).
        public static readonly DependencyProperty MagnitudeScaleProperty =
            DependencyProperty.Register(nameof(MagnitudeScale), typeof(double), typeof(VerificationResultCard),
                new PropertyMetadata(1.0, OnSimpleChanged));

        public string AxisTitle { get { return (string)GetValue(AxisTitleProperty); } set { SetValue(AxisTitleProperty, value); } }
        public string Unit { get { return (string)GetValue(UnitProperty); } set { SetValue(UnitProperty, value); } }
        public AxisStatistics RefStats { get { return (AxisStatistics)GetValue(RefStatsProperty); } set { SetValue(RefStatsProperty, value); } }
        public AxisStatistics TestStats { get { return (AxisStatistics)GetValue(TestStatsProperty); } set { SetValue(TestStatsProperty, value); } }
        public AxisStatistics DevStats { get { return (AxisStatistics)GetValue(DevStatsProperty); } set { SetValue(DevStatsProperty, value); } }
        public double AppliedCorrection { get { return (double)GetValue(AppliedCorrectionProperty); } set { SetValue(AppliedCorrectionProperty, value); } }
        public bool HasCorrectionApplied { get { return (bool)GetValue(HasCorrectionAppliedProperty); } set { SetValue(HasCorrectionAppliedProperty, value); } }
        public double MagnitudeScale { get { return (double)GetValue(MagnitudeScaleProperty); } set { SetValue(MagnitudeScaleProperty, value); } }

        private static void OnSimpleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((VerificationResultCard)d).Refresh();
        }

        private void Refresh()
        {
            tbAxisTitle.Text = AxisTitle ?? string.Empty;
            tbAxisUnit.Text = string.IsNullOrEmpty(Unit) ? string.Empty : "(" + Unit + ")";

            FillStats(RefStats, tbRefMean, tbRefStdDev, tbRefMinMax, tbRefRms, includeOutliers: false);
            FillStats(TestStats, tbTestMean, tbTestStdDev, tbTestMinMax, tbTestRms, includeOutliers: false);
            FillDeviation(DevStats);
        }

        private void FillStats(AxisStatistics s, TextBlock mean, TextBlock std, TextBlock minMax, TextBlock rms, bool includeOutliers)
        {
            if (s == null || s.SampleCount == 0)
            {
                mean.Text = "—";
                std.Text = "σ —";
                minMax.Text = "min/max —";
                rms.Text = "RMS —";
                return;
            }

            mean.Text = string.Format(CultureInfo.CurrentCulture, "{0:F4}", s.Mean);
            std.Text = string.Format(CultureInfo.CurrentCulture, "σ {0:F4}", s.StdDev);
            minMax.Text = string.Format(CultureInfo.CurrentCulture, "min/max {0:F3} / {1:F3}", s.Min, s.Max);
            rms.Text = string.Format(CultureInfo.CurrentCulture, "RMS {0:F4}", s.Rms);
        }

        private void FillDeviation(AxisStatistics s)
        {
            if (s == null || s.SampleCount == 0)
            {
                tbDevMean.Text = "—";
                tbDevStdDev.Text = "σ —";
                tbDevOutliers.Text = "outliers —";
                tbDevSamples.Text = "samples —";
                pbDeviationMagnitude.Value = 0;
                return;
            }

            tbDevMean.Text = string.Format(CultureInfo.CurrentCulture, "{0:F4}", s.Mean);
            tbDevStdDev.Text = string.Format(CultureInfo.CurrentCulture, "σ {0:F4}", s.StdDev);
            tbDevOutliers.Text = string.Format(CultureInfo.CurrentCulture, "outliers {0:F1} %", s.OutlierPercent);
            tbDevSamples.Text = string.Format(CultureInfo.CurrentCulture, "samples {0}", s.SampleCount);

            double scale = MagnitudeScale <= 0 ? 1.0 : MagnitudeScale;
            double pct = Math.Min(100.0, (Math.Abs(s.Mean) / scale) * 100.0);
            pbDeviationMagnitude.Value = pct;
        }
    }
}
