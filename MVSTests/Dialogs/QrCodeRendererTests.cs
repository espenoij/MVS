using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MVS;
using MVSTests.TestInfrastructure;

namespace MVSTests.Dialogs
{
    [TestClass]
    public class QrCodeRendererTests
    {
        [TestMethod]
        public void CreatePng_EncodesWebsiteAndEmailAsDistinctPngImages()
        {
            byte[] website = QrCodeRenderer.CreatePng("https://sesenergy.com/aviation");
            byte[] email = QrCodeRenderer.CreatePng("mailto:aviation.services@sesenergy.com");

            CollectionAssert.AreEqual(
                new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 },
                website[..8]);
            CollectionAssert.AreEqual(
                new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 },
                email[..8]);
            Assert.AreNotEqual(Convert.ToBase64String(website), Convert.ToBase64String(email));
        }

        [TestMethod]
        public void DialogAbout_Loaded_DisplaysBothSupportQrCodesAt120Pixels()
        {
            StaTestHelper.Run(() =>
            {
                if (Application.Current == null)
                {
                    var app = new App();
                    app.InitializeComponent();
                }

                var dialog = new DialogAbout();
                dialog.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));

                AssertQrImage(dialog, "websiteBarcodeHost");
                AssertQrImage(dialog, "emailBarcodeHost");
            });
        }

        private static void AssertQrImage(DialogAbout dialog, string hostName)
        {
            var host = (ContentControl)dialog.FindName(hostName);
            var image = (Image)host.Content;

            Assert.AreEqual(120, image.Width);
            Assert.AreEqual(120, image.Height);
            Assert.IsNotNull(image.Source);
            Assert.AreEqual(
                BitmapScalingMode.NearestNeighbor,
                RenderOptions.GetBitmapScalingMode(image));
        }
    }
}
