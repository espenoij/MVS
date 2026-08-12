using System.Linq;
using System.Windows;
using Telerik.Windows.Controls;

namespace MVS
{
    /// <summary>
    /// Pre-recording readiness check dialog.
    /// </summary>
    public partial class DialogCaptureReadiness : RadWindow
    {
        public bool UserConfirmed { get; private set; }

        public DialogCaptureReadiness()
        {
            InitializeComponent();
        }

        /// <summary>Populates the checklist and enables/disables the Start button.</summary>
        public void Init(ReadinessReport report)
        {
            // Bind items to the ItemsControl
            icItems.ItemsSource = report.Items.Select(i => new ReadinessItemVM(i));

            // Blocking failures disable the Start button
            btnStart.IsEnabled = !report.HasBlockingFailure;
        }

        private void btnStart_Click(object sender, RoutedEventArgs e)
        {
            UserConfirmed = true;
            Close();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            UserConfirmed = false;
            Close();
        }
    }

    /// <summary>
    /// Thin presentation wrapper so the DataTemplate can bind <see cref="IsAdvisoryFailure"/>.
    /// </summary>
    public sealed class ReadinessItemVM
    {
        public string Label             { get; }
        public bool   Pass              { get; }
        public bool   IsAdvisoryFailure { get; }

        public ReadinessItemVM(ReadinessItem item)
        {
            Label             = item.Label;
            Pass              = item.Pass;
            IsAdvisoryFailure = !item.Pass && item.Severity == ReadinessSeverity.Advisory;
        }
    }
}
