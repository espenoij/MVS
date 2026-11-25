using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security;
using System.Windows.Controls;
using Microsoft.Web.WebView2.Core;

namespace MVS.Views.Controls
{
    /// <summary>
    /// Displays PDF files with the WebView2 built-in PDF viewer.
    /// Temporary files are retained until the browser has navigated away from them.
    /// </summary>
    public partial class PdfPreviewControl : UserControl
    {
        private readonly HashSet<string> _retiredFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private string _activeFile;
        private bool _disposed;

        internal Action<string, byte[]> WritePdfBytes { get; set; } = File.WriteAllBytes;

        public PdfPreviewControl()
        {
            InitializeComponent();
            pdfViewer.CoreWebView2InitializationCompleted += PdfViewer_CoreWebView2InitializationCompleted;
        }

        public void LoadPdf(byte[] pdfBytes)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(PdfPreviewControl));
            if (pdfBytes == null)
                throw new ArgumentNullException(nameof(pdfBytes));
            if (pdfBytes.Length == 0)
                throw new ArgumentException("PDF data cannot be empty.", nameof(pdfBytes));

            RetireActiveFile();

            string directory = Path.Combine(Path.GetTempPath(), "MVS", "Reports");
            Directory.CreateDirectory(directory);

            string path = Path.Combine(directory, Guid.NewGuid().ToString("N") + ".pdf");
            try
            {
                WritePdfBytes(path, pdfBytes);
            }
            catch
            {
                _retiredFiles.Add(path);
                if (TryDeleteFile(path))
                    _retiredFiles.Remove(path);
                throw;
            }
            _activeFile = path;
            pdfViewer.Source = new Uri(path);
        }

        public void Clear()
        {
            if (_disposed)
                return;
            RetireActiveFile();

            if (pdfViewer.CoreWebView2 != null)
                pdfViewer.CoreWebView2.Navigate("about:blank");
            else if (pdfViewer.Source != null)
                pdfViewer.Source = new Uri("about:blank");
            else
                DeleteRetiredFiles();
        }

        /// <summary>
        /// Releases the browser and deletes files owned by this preview when its owner closes.
        /// Unlike Clear, this control cannot be reused afterward.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            pdfViewer.CoreWebView2InitializationCompleted -= PdfViewer_CoreWebView2InitializationCompleted;
            if (pdfViewer.CoreWebView2 != null)
                pdfViewer.CoreWebView2.NavigationCompleted -= PdfViewer_NavigationCompleted;
            RetireActiveFile();
            try
            {
                pdfViewer.Dispose();
            }
            finally
            {
                DeleteRetiredFiles();
            }
        }

        private void PdfViewer_CoreWebView2InitializationCompleted(
            object sender,
            CoreWebView2InitializationCompletedEventArgs e)
        {
            if (!e.IsSuccess)
            {
                Debug.WriteLine("WebView2 PDF preview initialization failed: " + e.InitializationException);
                RetireActiveFile();
                DeleteRetiredFiles();
                System.Windows.MessageBox.Show(
                    "The PDF preview could not be initialized. Ensure the Microsoft Edge WebView2 Runtime is installed.",
                    "PDF preview unavailable",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
                return;
            }

            pdfViewer.CoreWebView2.NavigationCompleted += PdfViewer_NavigationCompleted;
        }

        private void PdfViewer_NavigationCompleted(
            object sender,
            CoreWebView2NavigationCompletedEventArgs e)
        {
            DeleteRetiredFiles();
        }

        private void RetireActiveFile()
        {
            if (string.IsNullOrEmpty(_activeFile))
                return;

            _retiredFiles.Add(_activeFile);
            _activeFile = null;
        }

        private void DeleteRetiredFiles()
        {
            var deletedFiles = new List<string>();
            foreach (string path in _retiredFiles)
            {
                if (TryDeleteFile(path))
                    deletedFiles.Add(path);
            }

            foreach (string path in deletedFiles)
                _retiredFiles.Remove(path);
        }

        private static bool TryDeleteFile(string path)
        {
            try
            {
                File.Delete(path);
                return true;
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is SecurityException)
            {
                Debug.WriteLine("Could not remove temporary PDF preview file: " + ex.Message);
                return false;
            }
        }
    }
}
