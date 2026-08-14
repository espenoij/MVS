using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Navigation;
using Telerik.Windows.Controls;

namespace MVS
{
    /// <summary>
    /// Interaction logic for DialogAbout
    /// .xaml
    /// </summary>
    public partial class DialogAbout : RadWindow
    {
        private const string TelerikXmlns = "xmlns:telerik=\"http://schemas.telerik.com/2008/xaml/presentation\"";

        // Load DataVisualization assembly before any XAML parsing so that
        // XamlReader can resolve RadBarcode / QRCode via XmlnsDefinitionAttribute.
        private static readonly Assembly? _dataVisAsm = LoadDataVisualizationAssembly();

        public DialogAbout()
        {
            InitializeComponent();
            Loaded += (s, e) =>
            {
                websiteBarcodeHost.Content = CreateQrBarcode("https://sesenergy.com/aviation");
                emailBarcodeHost.Content = CreateQrBarcode("mailto:aviation.services@sesenergy.com");
            };
        }

        private static Assembly? LoadDataVisualizationAssembly()
        {
            try
            {
                string path = Path.Combine(
                    AppContext.BaseDirectory,
                    "Telerik.Windows.Controls.DataVisualization.dll");
                return File.Exists(path)
                    ? Assembly.LoadFrom(path)
                    : Assembly.Load("Telerik.Windows.Controls.DataVisualization");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to load Telerik DataVisualization assembly: {ex.Message}");
                return null;
            }
        }

        private static UIElement? CreateQrBarcode(string value)
        {
            if (_dataVisAsm == null)
                return null;

            try
            {
                string xaml =
                    $"<telerik:RadBarcode {TelerikXmlns} Width=\"120\" Height=\"120\" Value=\"{value}\">" +
                    "<telerik:RadBarcode.Symbology><telerik:QRCode /></telerik:RadBarcode.Symbology>" +
                    "</telerik:RadBarcode>";
                return (UIElement)XamlReader.Parse(xaml);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to create QR barcode: {ex.Message}");
                return null;
            }
        }

        public void Init(AboutVM aboutVM)
        {
            DataContext = aboutVM;
        }

        private void btnActivate_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Close();
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = e.Uri.AbsoluteUri,
                UseShellExecute = true
            });
            e.Handled = true;
        }
    }
}
