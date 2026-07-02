using System;
using System.Windows;
using System.Windows.Controls;

namespace MVS.Views.Controls
{
    /// <summary>
    /// Telerik-based editor for the operator-supplied <see cref="MruReportMetadata"/>
    /// that backs the detailed MRU verification report. The control binds directly
    /// to a metadata instance (set through <see cref="Metadata"/>) and raises
    /// <see cref="MetadataChanged"/> whenever a field loses focus so the host page
    /// can persist the owning project.
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
    }
}
