using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using Telerik.Windows.Controls;

namespace MVS.Dialogs
{
    /// <summary>
    /// Interaction logic for DialogScreenCapture.xaml
    /// </summary>
    public partial class DialogScreenCapture : RadWindow
    {
        private Bitmap bitmap;

        public DialogScreenCapture()
        {
            InitializeComponent();
        }

        public void Init(Bitmap bitmap)
        {
            this.bitmap = bitmap;
        }

        private void btnOpenFileLocation_Click(object sender, RoutedEventArgs e)
        {
            string folder = Path.Combine(Environment.CurrentDirectory, Constants.ScreenCaptureFolder);
            Process.Start(folder);
            Close();
        }

        private void btnCopyToClipboard_Click(object sender, RoutedEventArgs e)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                bitmap.Save(memory, ImageFormat.Png);
                memory.Position = 0;
                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();

                Clipboard.SetImage(bitmapImage);
            }
            Close();
        }
    }
}