using System;
using QRCoder;

namespace MVS
{
    internal static class QrCodeRenderer
    {
        private const int PixelsPerModule = 4;

        internal static byte[] CreatePng(string value)
        {
            using var generator = new QRCodeGenerator();
            using var data = generator.CreateQrCode(value, QRCodeGenerator.ECCLevel.M);
            return new PngByteQRCode(data).GetGraphic(PixelsPerModule);
        }
    }
}
