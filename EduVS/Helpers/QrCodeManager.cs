using System.Drawing.Imaging;
using System.IO;
using ZXing;
using ZXing.QrCode;
using ZXing.QrCode.Internal;

namespace EduVS.Helpers
{
    internal class QrCodeManager
    {
        public QrCodeManager() { }

        public byte[] CreateQrPng(string data, int sizePx = 300, int marginModules = 4)
        {
            if (string.IsNullOrWhiteSpace(data)) throw new ArgumentException("Data for QR code cannot be null or empty.", nameof(data));
            if (sizePx < 16) sizePx = 16;

            var writer = new ZXing.Windows.Compatibility.BarcodeWriter
            {
                Format = BarcodeFormat.QR_CODE,
                Options = new QrCodeEncodingOptions
                {
                    Height = sizePx,
                    Width = sizePx,
                    Margin = marginModules,
                    ErrorCorrection = ErrorCorrectionLevel.M,
                    CharacterSet = "UTF-8",
                    QrVersion = 0
                }
            };

            using var bmp = writer.Write(data);
            using var ms = new MemoryStream();
            bmp.Save(ms, ImageFormat.Png);
            return ms.ToArray();
        }
    }
}
