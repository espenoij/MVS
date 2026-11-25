using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MVS;
using MVS.Views.Controls;
using MVSTests.TestInfrastructure;

namespace MVSTests.Helpers
{
    [TestClass]
    public class PdfPreviewControlTests
    {
        [TestMethod]
        public void PreviewTeardownAndWriteFailurePreserveReportActions()
        {
            StaTestHelper.Run(() =>
            {
                if (Application.Current == null)
                {
                    var app = new App();
                    app.InitializeComponent();
                }

                var createdFiles = new List<string>();
                var preview = new PdfPreviewControl();
                try
                {
                    preview.WritePdfBytes = (path, bytes) =>
                    {
                        createdFiles.Add(path);
                        File.WriteAllBytes(path, bytes);
                    };
                    preview.LoadPdf(new byte[] { 1 });
                    using (var locked = new FileStream(createdFiles[0], FileMode.Open, FileAccess.Read, FileShare.None))
                    {
                        preview.Clear();
                        Assert.IsTrue(File.Exists(createdFiles[0]));
                    }

                    preview.LoadPdf(new byte[] { 2 });
                    preview.Dispose();
                    preview.Dispose();
                    Assert.IsFalse(File.Exists(createdFiles[0]), "Retired files must be retried on teardown.");
                    Assert.IsFalse(File.Exists(createdFiles[1]), "The active file must be removed on teardown.");
                    Assert.ThrowsExactly<ObjectDisposedException>(() => preview.LoadPdf(new byte[] { 3 }));

                    var projects = new Projects();
                    var inline = (PdfPreviewControl)projects.FindName("inlinePdfViewer");
                    string partialFile = null;
                    inline.WritePdfBytes = (path, bytes) =>
                    {
                        partialFile = path;
                        File.WriteAllBytes(path, bytes);
                        throw new IOException("Preview storage unavailable");
                    };
                    byte[] report = { 4, 5 };
                    var bytesField = typeof(Projects).GetField("_reportPdfBytes", BindingFlags.Instance | BindingFlags.NonPublic);
                    bytesField.SetValue(projects, report);
                    try
                    {
                        typeof(Projects).GetMethod("MarkReportAvailable", BindingFlags.Instance | BindingFlags.NonPublic)
                            .Invoke(projects, null);
                        Assert.AreSame(report, bytesField.GetValue(projects));
                        Assert.IsTrue(((Button)projects.FindName("btnSaveReportToDisc")).IsEnabled);
                        Assert.IsTrue(((Button)projects.FindName("btnOpenPdfReport")).IsEnabled);
                        Assert.AreEqual(Visibility.Visible, ((FrameworkElement)projects.FindName("reportViewerPlaceholder")).Visibility);
                        StringAssert.Contains(((TextBlock)projects.FindName("reportViewerPlaceholderTitle")).Text, "preview unavailable");
                        StringAssert.Contains(((TextBlock)projects.FindName("reportViewerPlaceholderDetail")).Text, "Preview storage unavailable");
                        Assert.IsNotNull(partialFile);
                        Assert.IsFalse(File.Exists(partialFile), "A partially written preview must be removed.");
                    }
                    finally
                    {
                        projects.DisposeReportPreview();
                        if (partialFile != null && File.Exists(partialFile))
                            File.Delete(partialFile);
                    }
                }
                finally
                {
                    preview.Dispose();
                    foreach (string path in createdFiles)
                        if (File.Exists(path))
                            File.Delete(path);
                }
            });
        }
    }
}
