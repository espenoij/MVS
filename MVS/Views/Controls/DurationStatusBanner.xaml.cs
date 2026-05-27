using System;
using System.Windows;
using System.Windows.Controls;

namespace MVS.Views.Controls
{
    /// <summary>
    /// Neutral status banner showing how close the current recording is to the
    /// recommended 40-minute verification window. No pass/fail coloring; the
    /// only accent is reaching the recommended threshold.
    /// </summary>
    public partial class DurationStatusBanner : UserControl
    {
        // Target verification duration in minutes.
        private const double RecommendedMinutes = 40.0;
        private const double AcceptableMinutes = 20.0;

        public DurationStatusBanner()
        {
            InitializeComponent();
            Update(null);
        }

        public static readonly DependencyProperty ProjectProperty =
            DependencyProperty.Register(
                nameof(Project),
                typeof(Project),
                typeof(DurationStatusBanner),
                new PropertyMetadata(null, OnProjectChanged));

        public Project Project
        {
            get { return (Project)GetValue(ProjectProperty); }
            set { SetValue(ProjectProperty, value); }
        }

        private static void OnProjectChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var banner = (DurationStatusBanner)d;
            banner.Update(e.NewValue as Project);
        }

        public void Update(Project project)
        {
            if (project == null || !project.DataSetHasData())
            {
                tbDurationValue.Text = "--:--:--";
                pbDuration.Value = 0;
                tbStatusMessage.Text = "No data recorded yet. Start a recording or import a data set.";
                return;
            }

            TimeSpan duration = project.EndTime - project.StartTime;
            tbDurationValue.Text = string.Format("{0:00}:{1:00}:{2:00}",
                (int)duration.TotalHours, duration.Minutes, duration.Seconds);

            double percent = Math.Min(100.0, (duration.TotalMinutes / RecommendedMinutes) * 100.0);
            pbDuration.Value = percent;

            if (duration.TotalMinutes >= RecommendedMinutes)
            {
                tbStatusMessage.Text = "Recommended duration reached. Analysis results will be most reliable.";
            }
            else if (duration.TotalMinutes >= AcceptableMinutes)
            {
                int remaining = (int)Math.Ceiling(RecommendedMinutes - duration.TotalMinutes);
                tbStatusMessage.Text = string.Format("Acceptable for analysis. About {0} more minute(s) reach the recommended 40-minute window.", remaining);
            }
            else
            {
                int remaining = (int)Math.Ceiling(AcceptableMinutes - duration.TotalMinutes);
                tbStatusMessage.Text = string.Format("Short recording. About {0} more minute(s) needed before analysis is meaningful.", remaining);
            }
        }
    }
}
