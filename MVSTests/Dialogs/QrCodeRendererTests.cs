using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MVS;

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
    }
}
