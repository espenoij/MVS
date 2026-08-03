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

            FillStats(RefStats, tbRefMean, tbRefMin, tbRefMax, tbRefStdDev, tbRefRms);
            FillStats(TestStats, tbTestMean, tbTestMin, tbTestMax, tbTestStdDev, tbTestRms);
            FillDeviation(DevStats);
            FillAssessment();
        }

        private void FillStats(AxisStatistics s, TextBlock mean, TextBlock min, TextBlock max, TextBlock std, TextBlock rms)
        {
            if (s == null || s.SampleCount == 0)
            {
                mean.Text = "—";
                min.Text = "—";
                max.Text = "—";
                std.Text = "—";
                rms.Text = "—";
                return;
            }

            mean.Text = string.Format(CultureInfo.CurrentCulture, "{0:F4}", s.Mean);
            min.Text = string.Format(CultureInfo.CurrentCulture, "{0:F3}", s.Min);
            max.Text = string.Format(CultureInfo.CurrentCulture, "{0:F3}", s.Max);
            std.Text = string.Format(CultureInfo.CurrentCulture, "{0:F4}", s.StdDev);
            rms.Text = string.Format(CultureInfo.CurrentCulture, "{0:F4}", s.Rms);
        }

        private void FillDeviation(AxisStatistics s)
        {
            if (s == null || s.SampleCount == 0)
            {
                tbDevMean.Text = "—";
                tbDevStdDev.Text = "—";
                tbDevOutliers.Text = "—";
                tbDevSamples.Text = "—";
                tbCorrection.Text = "—";
                pbDeviationMagnitude.Value = 0;
                return;
            }

            tbDevMean.Text = string.Format(CultureInfo.CurrentCulture, "{0:F4}", s.Mean);
            tbDevStdDev.Text = string.Format(CultureInfo.CurrentCulture, "{0:F4}", s.StdDev);
            tbDevOutliers.Text = string.Format(CultureInfo.CurrentCulture, "{0:F1} %", s.OutlierPercent);
            tbDevSamples.Text = string.Format(CultureInfo.CurrentCulture, "{0}", s.SampleCount);

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
                    // Success — SES Energy Green
                    badge = Color.FromRgb(0xD9, 0xFA, 0xEE);
                    badgeText = Color.FromRgb(0x17, 0xA3, 0x77);
                    panelBg = Color.FromRgb(0xEC, 0xFB, 0xF5);
                    panelBorder = Color.FromRgb(0x3D, 0xE6, 0xA9);
                    break;
                case VerificationStatus.Acceptable:
                    // Caution — accessible, professional amber
                    badge = Color.FromRgb(0xFF, 0xF4, 0xD6);
                    badgeText = Color.FromRgb(0x8A, 0x5D, 0x00);
                    panelBg = Color.FromRgb(0xFF, 0xFB, 0xEE);
                    panelBorder = Color.FromRgb(0xE6, 0xC2, 0x6A);
                    break;
                case VerificationStatus.NeedsAttention:
                    // Error — accessible, professional red
                    badge = Color.FromRgb(0xFB, 0xE1, 0xE3);
                    badgeText = Color.FromRgb(0xB3, 0x28, 0x33);
                    panelBg = Color.FromRgb(0xFD, 0xF0, 0xF1);
                    panelBorder = Color.FromRgb(0xD5, 0x30, 0x3E);
                    break;
                default:
                    // Neutral / information — derived from SES Dark Blue Grey
                    badge = Color.FromRgb(0xEA, 0xEF, 0xF2);
                    badgeText = Color.FromRgb(0x33, 0x4A, 0x5C);
                    panelBg = Color.FromRgb(0xF5, 0xF8, 0xFA);
                    panelBorder = Color.FromRgb(0xD5, 0xDD, 0xE2);
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
