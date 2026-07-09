using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using MVS.Services;

namespace MVS.Views.Controls
{
    /// <summary>
    /// Telerik-based editor for the operator-supplied <see cref="MruReportMetadata"/>
    /// that backs the detailed MRU verification report. The control binds directly
    /// to a metadata instance (set through <see cref="Metadata"/>) and raises
    /// <see cref="MetadataChanged"/> whenever a field loses focus so the host page
    /// can persist the owning project.
    ///
    /// The Acceptance Criteria section shows a read-only table of built-in quality
    /// thresholds vs the actual values from the current verification run. Call
    /// <see cref="UpdateVerificationData"/> whenever the verification stats change.
    /// </summary>
    public partial class ReportMetadataEditor : UserControl
    {
        public ReportMetadataEditor()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Raised after any field commits an edit (on lost focus). The host page
        /// should persist the current project in response.
        /// </summary>
        public event EventHandler MetadataChanged;

        /// <summary>
        /// The metadata object being edited. Setting it (re)binds every field.
        /// Passing null clears the editor.
        /// </summary>
        public MruReportMetadata Metadata
        {
            get => DataContext as MruReportMetadata;
            set => DataContext = value;
        }

        private void Field_LostFocus(object sender, RoutedEventArgs e)
        {
            if (Metadata != null)
                MetadataChanged?.Invoke(this, EventArgs.Empty);
        }

        // ── Formatted read-only strings for the Acceptance Criteria display ──────

        public static readonly DependencyProperty ActualSampleCountTextProperty =
            DependencyProperty.Register(nameof(ActualSampleCountText), typeof(string),
                typeof(ReportMetadataEditor), new PropertyMetadata("—"));

        public string ActualSampleCountText
        {
            get => (string)GetValue(ActualSampleCountTextProperty);
            private set => SetValue(ActualSampleCountTextProperty, value);
        }

        public static readonly DependencyProperty ActualDurationTextProperty =
            DependencyProperty.Register(nameof(ActualDurationText), typeof(string),
                typeof(ReportMetadataEditor), new PropertyMetadata("—"));

        public string ActualDurationText
        {
            get => (string)GetValue(ActualDurationTextProperty);
            private set => SetValue(ActualDurationTextProperty, value);
        }

        public static readonly DependencyProperty ActualWorstOutlierTextProperty =
            DependencyProperty.Register(nameof(ActualWorstOutlierText), typeof(string),
                typeof(ReportMetadataEditor), new PropertyMetadata("—"));

        public string ActualWorstOutlierText
        {
            get => (string)GetValue(ActualWorstOutlierTextProperty);
            private set => SetValue(ActualWorstOutlierTextProperty, value);
        }

        public static readonly DependencyProperty SampleStatusTextProperty =
            DependencyProperty.Register(nameof(SampleStatusText), typeof(string),
                typeof(ReportMetadataEditor), new PropertyMetadata("—"));

        public string SampleStatusText
        {
            get => (string)GetValue(SampleStatusTextProperty);
            private set => SetValue(SampleStatusTextProperty, value);
        }

        public static readonly DependencyProperty DurationStatusTextProperty =
            DependencyProperty.Register(nameof(DurationStatusText), typeof(string),
                typeof(ReportMetadataEditor), new PropertyMetadata("—"));

        public string DurationStatusText
        {
            get => (string)GetValue(DurationStatusTextProperty);
            private set => SetValue(DurationStatusTextProperty, value);
        }

        public static readonly DependencyProperty OutlierStatusTextProperty =
            DependencyProperty.Register(nameof(OutlierStatusText), typeof(string),
                typeof(ReportMetadataEditor), new PropertyMetadata("—"));

        public string OutlierStatusText
        {
            get => (string)GetValue(OutlierStatusTextProperty);
            private set => SetValue(OutlierStatusTextProperty, value);
        }

        /// <summary>
        /// Pushes the latest verification measurements into the read-only
        /// Acceptance Criteria display. Call this from the host page whenever
        /// the selected project or its statistics change.
        /// </summary>
        /// <param name="sampleCount">Number of averaged deviation samples.</param>
        /// <param name="durationMinutes">Total capture duration in minutes.</param>
        /// <param name="worstOutlierPercent">
        /// Highest outlier percentage across all reference and test axes, or
        /// <see cref="double.NaN"/> when no data is available.
        /// </param>
        public void UpdateVerificationData(int sampleCount, double durationMinutes, double worstOutlierPercent)
        {
            var ci = CultureInfo.CurrentCulture;

            ActualSampleCountText = sampleCount > 0
                ? sampleCount.ToString("N0", ci)
                : "—";

            ActualDurationText = durationMinutes > 0
                ? string.Format(ci, "{0:F1} min", durationMinutes)
                : "—";

            ActualWorstOutlierText = !double.IsNaN(worstOutlierPercent)
                ? string.Format(ci, "{0:F1} %", worstOutlierPercent)
                : "—";

            // Per-criterion status, using the same thresholds as the PDF report's
            // "10. Compliance Assessment" table so the on-screen card matches it.
            SampleStatusText =
                sampleCount >= VerificationAssessment.MinSamplesGood       ? "Good" :
                sampleCount >= VerificationAssessment.MinSamplesAcceptable ? "Acceptable" :
                sampleCount > 0                                           ? "Insufficient" : "No data";

            DurationStatusText =
                durationMinutes >= VerificationAssessment.MinDurationRecommendedMinutes ? "Good" :
                durationMinutes >= VerificationAssessment.MinDurationAcceptableMinutes  ? "Acceptable" :
                durationMinutes > 0                                                     ? "Insufficient" : "No data";

            OutlierStatusText =
                double.IsNaN(worstOutlierPercent)                                        ? "No data" :
                worstOutlierPercent <= VerificationAssessment.OutlierAcceptablePercent   ? "Good" :
                worstOutlierPercent <= VerificationAssessment.OutlierAttentionPercent    ? "Acceptable" :
                                                                                          "Too noisy";

            // Populate the Assessment narrative with an auto-generated, plain-language
            // summary of the captured data, unless the operator has already written
            // their own. This makes the assessment visible on the Step 5 card without
            // having to generate the PDF first.
            if (Metadata != null && string.IsNullOrWhiteSpace(Metadata.AcceptanceCriteriaDiscussion))
            {
                Metadata.AcceptanceCriteriaDiscussion =
                    VerificationAssessment.GenerateAssessmentText(sampleCount, durationMinutes, worstOutlierPercent);

                // MruReportMetadata is a plain POCO, so refresh the bound text box
                // target explicitly to show the generated assessment.
                txtAssessment?.GetBindingExpression(TextBox.TextProperty)?.UpdateTarget();

                // Persist the generated assessment with the project. This fires only
                // once because the field is no longer empty on later refreshes.
                MetadataChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}
