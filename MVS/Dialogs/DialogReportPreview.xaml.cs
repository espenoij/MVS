using System.Windows;

namespace MVS
{
    /// <summary>
    /// Interaction logic for DialogReportPreview.xaml.
    /// A popup window that displays the generated verification report PDF in a
    /// WebView2's built-in PDF viewer. The report bytes are loaded via
    /// <see cref="LoadReport"/> and the temporary preview file is removed on close.
    /// </summary>
    public partial class DialogReportPreview : Window
    {
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
            pdfReportPreview.LoadPdf(pdfBytes);
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void DialogReportPreview_Closed(object sender, System.EventArgs e)
        {
            pdfReportPreview.Clear();
        }
    }
}
