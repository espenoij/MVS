using System.IO;
using System.Windows;
using Telerik.Windows.Controls;
using Telerik.Windows.Documents.Fixed;

namespace MVS
{
    /// <summary>
    /// Interaction logic for DialogReportPreview.xaml.
    /// A popup window that displays the generated verification report PDF in a
    /// <see cref="RadPdfViewer"/>. The report bytes are loaded via
    /// <see cref="LoadReport"/>; the backing stream is disposed when the window closes.
    /// </summary>
    public partial class DialogReportPreview : RadWindow
    {
        // Backing stream for the viewer; kept alive for the window's lifetime.
        private MemoryStream _previewStream;

        public DialogReportPreview()
        {
            InitializeComponent();
            Closed += DialogReportPreview_Closed;
        }

        /// <summary>
        /// Loads the given PDF bytes into the viewer.
        /// </summary>
        public void LoadReport(byte[] pdfBytes)
        {
            _previewStream?.Dispose();
            _previewStream = new MemoryStream(pdfBytes, writable: false);
            pdfReportPreview.DocumentSource = new PdfDocumentSource(_previewStream);
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void DialogReportPreview_Closed(object sender, WindowClosedEventArgs e)
        {
            pdfReportPreview.DocumentSource = null;
            _previewStream?.Dispose();
            _previewStream = null;
        }
    }
}
