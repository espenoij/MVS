using Microsoft.Win32;
using System;
using System.IO;
using System.Windows;
using Telerik.Windows.Documents.Fixed.FormatProviders.Pdf;
using Telerik.Windows.Documents.Fixed.FormatProviders.Pdf.Export;
using Telerik.Windows.Documents.Fixed.Model;
using Telerik.Windows.Documents.Fixed.Model.Editing;

namespace HMS_Client
{
    class PDFHandler
    {
        public void SaveToFileWithDialog(System.Windows.Controls.Border element, string fileName)
        {
            PrepareForExport(element);

            var dialog = new SaveFileDialog
            {
                DefaultExt = "pdf",
                Filter = "PDF files|*.pdf|All Files (*.*)|*.*",
                FileName = string.Format("{0}_{1}.pdf", fileName, DateTime.UtcNow.ToString("yyyy-MM-dd_hh-mm-ss")),
                InitialDirectory = Path.Combine(Environment.CurrentDirectory, Constants.HelideckReportFolder)
            };
            if (dialog.ShowDialog() == true)
            {
                RadFixedDocument document = CreateDocument(element);
                PdfFormatProvider provider = new PdfFormatProvider();
                provider.ExportSettings.ImageQuality = ImageQuality.High;

                using (var output = dialog.OpenFile())
                {
                    provider.Export(document, output);
                }
            }
        }

        public void SaveToFile(System.Windows.Controls.Border element, string reportFile)
        {
            PrepareForExport(element);

            RadFixedDocument document = CreateDocument(element);
            PdfFormatProvider pdfFormatProvider = new PdfFormatProvider();
            pdfFormatProvider.ExportSettings.ImageQuality = ImageQuality.High;

            string reportFolder = Path.Combine(Environment.CurrentDirectory, Constants.HelideckReportFolder);

            using (FileStream output = File.Create(string.Format(@"{0}\{1}", reportFolder, reportFile)))
            {
                pdfFormatProvider.Export(document, output);
            }
        }

        private RadFixedDocument CreateDocument(System.Windows.Controls.Border element)
        {
            RadFixedDocument document = new RadFixedDocument();

            RadFixedPage page = CreatePage(element);
            document.Pages.Add(page);

            return document;
        }

        private RadFixedPage CreatePage(System.Windows.Controls.Border element)
        {
            RadFixedPage page = new RadFixedPage();
            page.Size = new Size(element.Width, element.Height);

            FixedContentEditor editor = new FixedContentEditor(page, Telerik.Windows.Documents.Fixed.Model.Data.MatrixPosition.Default);

            ExportHelper.ExportToPdf(element, editor);

            return page;
        }

        private static void PrepareForExport(FrameworkElement element)
        {
            if (element.ActualWidth == 0 && element.ActualHeight == 0)
            {
                double width = element.Width > 0 ? element.Width : 500;
                double height = element.Height > 0 ? element.Height : 300;
                element.Measure(Size.Empty);
                element.Measure(new Size(width, height));
                element.Arrange(new Rect(0, 0, width, height));
                element.UpdateLayout();
            }
        }
    }
}
