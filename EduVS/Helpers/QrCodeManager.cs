using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using ZXing;
using ZXing.Common;
using ZXing.QrCode;
using ZXing.QrCode.Internal;

namespace EduVS.Helpers
{
    internal class QrCodeManager
    {
        public QrCodeManager() { }

        public byte[] CreateQrPng(string data, int sizePx = 300, int marginModules = 5)
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
                    QrVersion = null
                }
            };

            using var bmp = writer.Write(data);
            using var ms = new MemoryStream();
            bmp.Save(ms, ImageFormat.Png);
            return ms.ToArray();
        }

        public string? DecodeQr(byte[] bitmapToDecode)
        {
            if (bitmapToDecode == null || bitmapToDecode.Length == 0) throw new ArgumentException("Bitmap to decode cannot be null or empty.", nameof(bitmapToDecode));
            var reader = new ZXing.Windows.Compatibility.BarcodeReader
            {
                AutoRotate = true,
                Options = new DecodingOptions
                {
                    TryHarder = true,
                    PossibleFormats = new List<BarcodeFormat> { BarcodeFormat.QR_CODE },
                    CharacterSet = "UTF-8"
                }
            };
            using var ms = new MemoryStream(bitmapToDecode);
            using var bmp = (Bitmap)System.Drawing.Image.FromStream(ms);
            var result = reader.Decode(bmp);
            return result?.Text;
        }
    }
}
