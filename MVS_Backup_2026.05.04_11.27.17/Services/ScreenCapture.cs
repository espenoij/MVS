using MVS.Dialogs;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace MVS.Services
{
    class ScreenCapture
    {
        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        public static extern bool DeleteObject(IntPtr onj);

        public void Capture(string screenCaptureFolder, string screenCaptureFile)
        {
            Bitmap bitmap;

            // DPI oppløsning på skjermen
            float dpiX, dpiY;
            using (Graphics graphics = Graphics.FromHwnd(IntPtr.Zero))
            {
                dpiX = graphics.DpiX;
                dpiY = graphics.DpiY;
            }

            // Høyden på taskbar
            int WindowsTaskBarHeight = Screen.PrimaryScreen.Bounds.Height - Screen.PrimaryScreen.WorkingArea.Height;

            // Størrelsen på skjermen
            int screenPixelSizeX = (int)(SystemParameters.PrimaryScreenWidth / 96 * dpiX);
            int screenPixelSizeY = (int)(SystemParameters.PrimaryScreenHeight / 96 * dpiY) - WindowsTaskBarHeight; // Trekker fra taskbar slik at denne ikke kommer med på screen capture

            // Opprette bitmap
            bitmap = new Bitmap(screenPixelSizeX, screenPixelSizeY, PixelFormat.Format32bppArgb);

            using (Graphics g = Graphics.FromImage(bitmap))
            {
                // Kopiere skjermbildet
                g.CopyFromScreen(0, 0, 0, 0, bitmap.Size);
            }

            IntPtr handle = IntPtr.Zero;

            try
            {
                // Laget et bilde som kan lagres til fil fra bitmap
                handle = bitmap.GetHbitmap();
                Imaging.CreateBitmapSourceFromHBitmap(handle, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());

                // Lagre til fil
                if (!Directory.Exists(screenCaptureFolder))
                {
                    Directory.CreateDirectory(screenCaptureFolder);
                }
                bitmap.Save(string.Format(@"{0}\{1}", screenCaptureFolder, screenCaptureFile), ImageFormat.Jpeg);

                // OK melding
                DialogScreenCapture screenCaptureOK = new DialogScreenCapture();
                screenCaptureOK.Init(bitmap);
                screenCaptureOK.Owner = App.Current.MainWindow;
                screenCaptureOK.ShowDialog();
            }
            catch (Exception ex)
            {
                DialogHandler.Warning("Screen capture error", ex.Message);
            }
            finally
            {
                DeleteObject(handle);
            }
        }
    }
}
