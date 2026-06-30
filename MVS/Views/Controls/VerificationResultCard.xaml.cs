using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using Telerik.Windows.Controls;
using MVS.Models;
using MVS.Services;

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
            btnInfo.Click += BtnInfo_Click;
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

        // Which axis this card represents; selects the acceptance thresholds and unit.
        public static readonly DependencyProperty AxisKindProperty =
            DependencyProperty.Register(nameof(AxisKind), typeof(VerificationAxisKind), typeof(VerificationResultCard),
                new PropertyMetadata(VerificationAxisKind.Pitch, OnSimpleChanged));

        public string AxisTitle { get { return (string)GetValue(AxisTitleProperty); } set { SetValue(AxisTitleProperty, value); } }
        public string Unit { get { return (string)GetValue(UnitProperty); } set { SetValue(UnitProperty, value); } }
        public AxisStatistics RefStats { get { return (AxisStatistics)GetValue(RefStatsProperty); } set { SetValue(RefStatsProperty, value); } }
        public AxisStatistics TestStats { get { return (AxisStatistics)GetValue(TestStatsProperty); } set { SetValue(TestStatsProperty, value); } }
        public AxisStatistics DevStats { get { return (AxisStatistics)GetValue(DevStatsProperty); } set { SetValue(DevStatsProperty, value); } }
        public double AppliedCorrection { get { return (double)GetValue(AppliedCorrectionProperty); } set { SetValue(AppliedCorrectionProperty, value); } }
        public bool HasCorrectionApplied { get { return (bool)GetValue(HasCorrectionAppliedProperty); } set { SetValue(HasCorrectionAppliedProperty, value); } }
        public double MagnitudeScale { get { return (double)GetValue(MagnitudeScaleProperty); } set { SetValue(MagnitudeScaleProperty, value); } }
        public VerificationAxisKind AxisKind { get { return (VerificationAxisKind)GetValue(AxisKindProperty); } set { SetValue(AxisKindProperty, value); } }

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
            FillAssessment();
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
                tbCorrection.Text = "—";
                pbDeviationMagnitude.Value = 0;
                return;
            }

            tbDevMean.Text = string.Format(CultureInfo.CurrentCulture, "{0:F4}", s.Mean);
            tbDevStdDev.Text = string.Format(CultureInfo.CurrentCulture, "σ {0:F4}", s.StdDev);
            tbDevOutliers.Text = string.Format(CultureInfo.CurrentCulture, "outliers {0:F1} %", s.OutlierPercent);
            tbDevSamples.Text = string.Format(CultureInfo.CurrentCulture, "samples {0}", s.SampleCount);

            // Correction is the negative of deviation, rounded to 1 decimal place
            double correction = Math.Round(-s.Mean, 1, MidpointRounding.AwayFromZero);
            tbCorrection.Text = string.Format(CultureInfo.CurrentCulture, "{0:+0.0;-0.0;0.0}", correction);

            double scale = MagnitudeScale <= 0 ? 1.0 : MagnitudeScale;
            double pct = Math.Min(100.0, (Math.Abs(s.Mean) / scale) * 100.0);
            pbDeviationMagnitude.Value = pct;
        }

        private void FillAssessment()
        {
            VerificationStatus status = VerificationAssessment.Classify(AxisKind, RefStats, TestStats, DevStats);

            tbStatusLabel.Text = VerificationAssessment.StatusLabel(status);
            tbAssessment.Text = VerificationAssessment.Summary(AxisKind, RefStats, TestStats, DevStats);

            ApplyStatusColors(status);
        }

        private void ApplyStatusColors(VerificationStatus status)
        {
            Color badge, badgeText, panelBg, panelBorder;

            switch (status)
            {
                case VerificationStatus.Good:
                    badge = Color.FromRgb(0xDA, 0xFB, 0xE1);
                    badgeText = Color.FromRgb(0x1A, 0x7F, 0x37);
                    panelBg = Color.FromRgb(0xF0, 0xFB, 0xF3);
                    panelBorder = Color.FromRgb(0xAC, 0xEE, 0xBB);
                    break;
                case VerificationStatus.Acceptable:
                    badge = Color.FromRgb(0xFF, 0xF8, 0xC5);
                    badgeText = Color.FromRgb(0x9A, 0x67, 0x00);
                    panelBg = Color.FromRgb(0xFF, 0xFC, 0xE8);
                    panelBorder = Color.FromRgb(0xF0, 0xDA, 0x7A);
                    break;
                case VerificationStatus.NeedsAttention:
                    badge = Color.FromRgb(0xFF, 0xEB, 0xE9);
                    badgeText = Color.FromRgb(0xCF, 0x22, 0x2E);
                    panelBg = Color.FromRgb(0xFF, 0xF4, 0xF3);
                    panelBorder = Color.FromRgb(0xFF, 0xC1, 0xBC);
                    break;
                default:
                    badge = Color.FromRgb(0xEF, 0xF2, 0xF5);
                    badgeText = Color.FromRgb(0x57, 0x60, 0x6A);
                    panelBg = Color.FromRgb(0xEF, 0xF2, 0xF5);
                    panelBorder = Color.FromRgb(0xD0, 0xD7, 0xDE);
                    break;
            }

            statusBadge.Background = new SolidColorBrush(badge);
            tbStatusLabel.Foreground = new SolidColorBrush(badgeText);
            assessmentPanel.Background = new SolidColorBrush(panelBg);
            assessmentPanel.BorderBrush = new SolidColorBrush(panelBorder);
        }

        private void BtnInfo_Click(object sender, RoutedEventArgs e)
        {
            string glossaryText = VerificationAssessment.Glossary(AxisKind);

            var window = new RadWindow
            {
                Width = 600,
                Height = 500,
                ResizeMode = ResizeMode.NoResize,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = Window.GetWindow(this),
                Header = string.Format(CultureInfo.CurrentCulture, "{0} — what the numbers mean", AxisTitle)
            };

            var scrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Padding = new Thickness(16)
            };

            var textBlock = new TextBlock
            {
                Text = glossaryText,
                TextWrapping = TextWrapping.Wrap,
                FontSize = 13,
                LineHeight = 20
            };

            scrollViewer.Content = textBlock;
            window.Content = scrollViewer;
            window.ShowDialog();
        }
    }
}
