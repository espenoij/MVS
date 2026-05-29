using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MVS.Views.Controls
{
    /// <summary>
    /// Live status banner for the Capture step. Shows recording duration progress toward
    /// the recommended 40-minute verification window, with colour-coded quality tiers
    /// and actionable guidance messages.
    /// </summary>
    public partial class DurationStatusBanner : UserControl
    {
        // Quality thresholds (minutes).
        private const double RecommendedMinutes = 40.0;
        private const double AcceptableMinutes  = 20.0;

        // Colours for quality tiers.
        private static readonly Color ColourTooShort   = Color.FromRgb(0xFF, 0xF3, 0xCD); // amber bg
        private static readonly Color ColourAcceptable = Color.FromRgb(0xD1, 0xEC, 0xF1); // blue/teal bg
        private static readonly Color ColourGood       = Color.FromRgb(0xD1, 0xE7, 0xDD); // green bg
        private static readonly Color ColourIdle       = Color.FromRgb(0xEF, 0xF2, 0xF5); // grey bg

        private static readonly Color ColourTooShortFg   = Color.FromRgb(0x66, 0x4D, 0x03);
        private static readonly Color ColourAcceptableFg = Color.FromRgb(0x0C, 0x54, 0x6E);
        private static readonly Color ColourGoodFg       = Color.FromRgb(0x0A, 0x36, 0x22);
        private static readonly Color ColourIdleFg       = Color.FromRgb(0x57, 0x60, 0x6A);

        // Progress-bar colours.
        private static readonly Color PbTooShort   = Color.FromRgb(0xFF, 0xC1, 0x07);
        private static readonly Color PbAcceptable = Color.FromRgb(0x17, 0xA2, 0xB8);
        private static readonly Color PbGood       = Color.FromRgb(0x28, 0xA7, 0x45);

        public DurationStatusBanner()
        {
            InitializeComponent();
            Update(null);
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            PositionMilestoneLabels();
        }

        /// <summary>Position the ▲ milestone labels as a fraction of the bar width.</summary>
        private void PositionMilestoneLabels()
        {
            double barWidth = pbDuration.ActualWidth;
            if (barWidth <= 0) return;

            double pct20 = AcceptableMinutes  / RecommendedMinutes; // 0.5
            double pct40 = 1.0;

            ttMilestone20.X = barWidth * pct20 - tbMilestone20.ActualWidth / 2;
            ttMilestone40.X = barWidth * pct40 - tbMilestone40.ActualWidth;
        }

        // -----------------------------------------------------------------------
        // Dependency Property: Project (for finished/imported datasets)
        // -----------------------------------------------------------------------

        public static readonly DependencyProperty ProjectProperty =
            DependencyProperty.Register(
                nameof(Project),
                typeof(Project),
                typeof(DurationStatusBanner),
                new PropertyMetadata(null, OnProjectChanged));

        public Project Project
        {
            get => (Project)GetValue(ProjectProperty);
            set => SetValue(ProjectProperty, value);
        }

        private static void OnProjectChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((DurationStatusBanner)d).Update(e.NewValue as Project);
        }

        // -----------------------------------------------------------------------
        // Update — called for a static project (no active recording)
        // -----------------------------------------------------------------------

        public void Update(Project project)
        {
            if (project == null || !project.DataSetHasData())
            {
                ResetToIdle();
                return;
            }

            TimeSpan duration = project.EndTime - project.StartTime;
            ApplyDuration(duration, isLive: false);
        }

        // -----------------------------------------------------------------------
        // UpdateLive — called every second while recording is active
        // -----------------------------------------------------------------------

        /// <summary>
        /// Call this every tick while a recording is active.
        /// <paramref name="recordingStart"/> is the UTC time recording began.
        /// </summary>
        public void UpdateLive(DateTime recordingStart)
        {
            TimeSpan elapsed = DateTime.UtcNow - recordingStart;
            if (elapsed < TimeSpan.Zero) elapsed = TimeSpan.Zero;
            ApplyDuration(elapsed, isLive: true);
        }

        // -----------------------------------------------------------------------
        // Core display logic
        // -----------------------------------------------------------------------

        private void ApplyDuration(TimeSpan duration, bool isLive)
        {
            double totalMin = duration.TotalMinutes;
            double percent  = Math.Min(100.0, (totalMin / RecommendedMinutes) * 100.0);

            tbDurationValue.Text = string.Format("{0:00}:{1:00}:{2:00}",
                (int)duration.TotalHours, duration.Minutes, duration.Seconds);
            pbDuration.Value = percent;

            PositionMilestoneLabels();

            string quality, message;
            Color bgColour, fgColour, pbColour;

            if (totalMin >= RecommendedMinutes)
            {
                quality  = isLive ? "✔  Excellent — recommended duration reached!" : "✔  Excellent recording";
                message  = "40 minutes reached. Analysis results will be most reliable. You may stop the recording whenever you are ready.";
                bgColour = ColourGood;
                fgColour = ColourGoodFg;
                pbColour = PbGood;
            }
            else if (totalMin >= AcceptableMinutes)
            {
                int remaining = (int)Math.Ceiling(RecommendedMinutes - totalMin);
                quality  = "◑  Acceptable — keep going for best results";
                message  = isLive
                    ? string.Format("Good progress! About {0} more minute(s) to reach the recommended 40-minute window. Keep the vessel in representative sea-state.", remaining)
                    : string.Format("Acceptable for analysis. About {0} more minute(s) to reach the recommended 40-minute window.", remaining);
                bgColour = ColourAcceptable;
                fgColour = ColourAcceptableFg;
                pbColour = PbAcceptable;
            }
            else
            {
                int remaining = (int)Math.Ceiling(AcceptableMinutes - totalMin);
                quality  = isLive ? "● Recording…" : "⚠  Short recording";
                message  = isLive
                    ? string.Format("Recording in progress. About {0} more minute(s) before analysis becomes meaningful (20-minute minimum). Ensure vessel motion is representative.", remaining)
                    : string.Format("Short recording. At least 20 minutes of data are needed before analysis is meaningful ({0} more minute(s) required).", remaining);
                bgColour = isLive ? ColourIdle : ColourTooShort;
                fgColour = isLive ? ColourIdleFg : ColourTooShortFg;
                pbColour = PbTooShort;
            }

            tbQualityLabel.Text = quality;
            tbStatusMessage.Text = message;

            qualityBadge.Background  = new SolidColorBrush(bgColour);
            tbQualityLabel.Foreground = new SolidColorBrush(fgColour);
            tbStatusMessage.Foreground = new SolidColorBrush(fgColour);

            pbDuration.Foreground = new SolidColorBrush(pbColour);

            outerBorder.BorderBrush = new SolidColorBrush(
                Color.FromArgb(0xFF,
                    (byte)(pbColour.R * 0.7),
                    (byte)(pbColour.G * 0.7),
                    (byte)(pbColour.B * 0.7)));
        }

        private void ResetToIdle()
        {
            tbDurationValue.Text   = "--:--:--";
            pbDuration.Value       = 0;
            tbQualityLabel.Text    = "No recording active";
            tbStatusMessage.Text   = "Start a recording or import a data set to begin.";

            qualityBadge.Background    = new SolidColorBrush(ColourIdle);
            tbQualityLabel.Foreground  = new SolidColorBrush(ColourIdleFg);
            tbStatusMessage.Foreground = new SolidColorBrush(ColourIdleFg);

            pbDuration.Foreground = new SolidColorBrush(Color.FromRgb(0xCC, 0xCC, 0xCC));
            outerBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(0xD0, 0xD7, 0xDE));
        }
    }
}
