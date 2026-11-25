using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

namespace MVS
{
    /// <summary>
    /// Interaction logic for DialogAbout
    /// .xaml
    /// </summary>
    public partial class DialogAbout : Window
    {
        public DialogAbout()
        {
            InitializeComponent();
            Loaded += (s, e) =>
            {
                websiteBarcodeHost.Content = CreateQrCode("https://sesenergy.com/aviation");
                emailBarcodeHost.Content = CreateQrCode("mailto:aviation.services@sesenergy.com");
            };
        }

        private static Image CreateQrCode(string value)
        {
            var bitmap = new BitmapImage();
            using (var stream = new MemoryStream(QrCodeRenderer.CreatePng(value)))
            {
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.StreamSource = stream;
                bitmap.EndInit();
            }

            bitmap.Freeze();
            return new Image
            {
                Width = 120,
                Height = 120,
                Source = bitmap,
                Stretch = Stretch.Uniform
            };
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
