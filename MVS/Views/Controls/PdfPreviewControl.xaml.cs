using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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

        public PdfPreviewControl()
        {
            InitializeComponent();
            pdfViewer.CoreWebView2InitializationCompleted += PdfViewer_CoreWebView2InitializationCompleted;
        }

        public void LoadPdf(byte[] pdfBytes)
        {
            if (pdfBytes == null)
                throw new ArgumentNullException(nameof(pdfBytes));
            if (pdfBytes.Length == 0)
                throw new ArgumentException("PDF data cannot be empty.", nameof(pdfBytes));

            RetireActiveFile();

            string directory = Path.Combine(Path.GetTempPath(), "MVS", "Reports");
            Directory.CreateDirectory(directory);

            string path = Path.Combine(directory, Guid.NewGuid().ToString("N") + ".pdf");
            File.WriteAllBytes(path, pdfBytes);
            _activeFile = path;
            pdfViewer.Source = new Uri(path);
        }

        public void Clear()
        {
            RetireActiveFile();

            if (pdfViewer.CoreWebView2 != null)
                pdfViewer.CoreWebView2.Navigate("about:blank");
            else
            {
                pdfViewer.Source = null;
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
                try
                {
                    File.Delete(path);
                    deletedFiles.Add(path);
                }
                catch (IOException ex)
                {
                    Debug.WriteLine("Could not remove temporary PDF preview file: " + ex.Message);
                }
                catch (UnauthorizedAccessException ex)
                {
                    Debug.WriteLine("Could not remove temporary PDF preview file: " + ex.Message);
                }
            }

            foreach (string path in deletedFiles)
                _retiredFiles.Remove(path);
        }
    }
}
