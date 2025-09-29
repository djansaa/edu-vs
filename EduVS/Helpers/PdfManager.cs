using EduVS.Models;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using PDFtoImage;
using SkiaSharp;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using ZXing;
using ZXing.Common;
using ZXing.Windows.Compatibility;

namespace EduVS.Helpers
{
    internal class PdfManager
    {
        public PdfManager() {

        }

        public void GenerateTestPrintTemplate(string outputPath, string testName, string testDate, string? templateAPath, int templateACount, string? templateBPath, int templateBCount)
        {
            // validate inputs
            if (string.IsNullOrEmpty(testName))
            {
                throw new ArgumentException("Test name is empty.", nameof(testName));
            }

            if (string.IsNullOrEmpty(testName))
            {
                throw new ArgumentException("Test name is empty.", nameof(testName));
            }

            if (string.IsNullOrEmpty(testDate))
            {
                throw new ArgumentException("Test date is empty.", nameof(testDate));
            }

            if (string.IsNullOrEmpty(templateAPath) && string.IsNullOrEmpty(templateBPath))
            {
                throw new ArgumentException("At least one template path must be provided.");
            }

            if (!string.IsNullOrEmpty(templateAPath) && templateACount < 0)
            {
                throw new ArgumentException("Template A count is invalid.", nameof(templateACount));
            }

            if (!string.IsNullOrEmpty(templateBPath) && templateBCount < 0)
            {
                throw new ArgumentException("Template B count is invalid.", nameof(templateBCount));
            }

            // qr code manager
            QrCodeManager qrCodeManager = new QrCodeManager();

            // document builder
            using var dst = new PdfDocument();
            dst.Info.Title = $"{testName}";
            dst.Info.Author = "EduVS";

            // fonts
            var fontOpts = new XPdfFontOptions(PdfFontEncoding.Unicode);
            var fontBold = new XFont("DejaVu Sans", 10, XFontStyleEx.Bold, fontOpts);
            var fontBoldBigger = new XFont("DejaVu Sans", 15, XFontStyleEx.Bold, fontOpts);

            // test count for unique test ids
            int testCount = 0;

            // local function to append header
            void AppendHeader(string templatePath, int count, string groupLetter)
            {
                using var src = PdfReader.Open(templatePath, PdfDocumentOpenMode.Import);

                for (int i = 0; i < count; i++)
                {
                    testCount++;

                    for (int p = 0; p < src.PageCount; p++)
                    {
                        // add page copy from template
                        PdfPage page = dst.AddPage(src.Pages[p]);

                        // clear annotation
                        //page.Annotations.Clear();

                        XUnit w = page.Width;
                        XUnit h = page.Height;
                        double margin = 25;
                        double qrSize = 100;
                        double top = margin;

                        using var gfx = XGraphics.FromPdfPage(page, XGraphicsPdfPageOptions.Append);

                        // add header only on first page
                        if (p == 0)
                        {
                            // name
                            gfx.DrawString("jméno:", fontBold, XBrushes.Black, new XPoint(margin, top + 17));

                            // name box
                            gfx.DrawRectangle(XPens.Black, XBrushes.Transparent, new XRect(margin + 45, top, 270, 30));

                            // date
                            var dateText = $"datum: {testDate:yyyy-MM-dd}";
                            gfx.DrawString(dateText, fontBold, XBrushes.Black, new XPoint(margin + 330, top + 17));

                            // group
                            var centerX = w / 2.0;
                            gfx.DrawString(groupLetter, fontBoldBigger, XBrushes.Black, new XPoint(centerX, top + 55), XStringFormats.Center);

                            // test name
                            gfx.DrawString($"{testName} ({testCount})", fontBoldBigger, XBrushes.Black,  new XPoint(centerX, top + 75), XStringFormats.Center);
                        }

                        // ##################### QR CODE #####################

                        // qr code data
                        var qrData = $"TESTID:{testCount}|GROUPID:{groupLetter}|TESTNAME:{testName}|TESTDATE:{testDate}|PAGE:{p+1}";

                        int dpi = 300;
                        int pxSize = (int)Math.Round(Math.Min(qrSize, qrSize) * dpi / 72.0);
                        int ppm = Math.Max(4, pxSize / 33);

                        // create qr code png
                        var png = qrCodeManager.CreateQrPng(qrData, marginModules: 2);
                        using var ms = new MemoryStream(png);
                        using var img = XImage.FromStream(ms);

                        var qrRect = new XRect(w - margin - qrSize +10, top-10, qrSize, qrSize);

                        // draw qr code
                        gfx.DrawImage(img, qrRect);
                    }
                }
            }

            // create pages for template A
            if (!string.IsNullOrWhiteSpace(templateAPath) && templateACount > 0) AppendHeader(templateAPath, templateACount, "A");

            if (!string.IsNullOrWhiteSpace(templateBPath) && templateBCount > 0) AppendHeader(templateBPath, templateBCount, "B");

            dst.Save(outputPath);
        }

        public void GenerateTestCheck(string inputPath, bool isSplitByGroup, bool isMergedSingle, bool sortByPageNumber, bool sortByTestNumber, string outputPathA, string outputPathB)
        {
            // validate inputs
            if (string.IsNullOrEmpty(inputPath) || !File.Exists(inputPath))
            {
                throw new ArgumentException("Input PDF path is invalid.", nameof(inputPath));
            }

            if (!isSplitByGroup && !isMergedSingle)
            {
                throw new ArgumentException("At least one output mode must be selected.");
            }

            if (!sortByPageNumber && !sortByTestNumber)
            {
                throw new ArgumentException("At least one sorting mode must be selected.");
            }

            if (string.IsNullOrEmpty(outputPathA) && string.IsNullOrEmpty(outputPathB))
            {
                throw new ArgumentException("At least one output path must be provided.");
            }

            // output pages collection
            var outputPagesData = new List<(int pageId, int rotation, QrCodeData qr)>();

            // qr code manager
            QrCodeManager qrCodeManager = new QrCodeManager();

            // source pdf
            using var src = PdfReader.Open(inputPath, PdfDocumentOpenMode.Import);

            var reader = new ZXing.Windows.Compatibility.BarcodeReader
            {
                AutoRotate = false,
                Options = new DecodingOptions
                {
                    TryHarder = true,
                    PossibleFormats = new[] { BarcodeFormat.QR_CODE },
                    CharacterSet = "UTF-8"
                }
            };

            // for each page, render to bitmap, try decode QR in top-right corner
            for (int i = 0; i < src.PageCount; i++)
            {
                string? qr = null;
                int rotate = 0;

                // render page -> bitmap -> try decode QR
                using (var sk = PdfPageToSKBitmap(inputPath, i))
                using (var full = SKBitmapToBitmap(sk))
                {
                    // try decode QR in top-right corner
                    qr = TryDecodeTopRight(reader, full);

                    // if not found, rotate 180 and try again
                    if (qr == null)
                    {
                        Debug.WriteLine($"Page: {i + 1} - QR code not found -> rotating 180 degrees.");
                        using var full180 = RotateBitmap180(full);
                        qr = TryDecodeTopRight(reader, full180);
                    }
                }

                if (qr == null)
                {
                    // QR code not found even after rotation
                    Debug.WriteLine($"Page: {i + 1} - QR code not found.");
                    continue;
                }

                // qr code data
                QrCodeData qcd = QrCodeData.Parse(qr);

                // make tuple made of QrCodeData and pdf page
                Debug.WriteLine($"Page: {i + 1} - QR code found: TestId={qcd.TestId}, GroupId={qcd.GroupId}, TestName={qcd.TestName}, TestDate={qcd.TestDate:yyyy-MM-dd}, Page={qcd.Page}");

                outputPagesData.Add((i, rotate, qcd));
            }

            // destination docs
            using var merged = new PdfDocument();
            using var outA = new PdfDocument();
            using var outB = new PdfDocument();

            // SPLIT PAGES BY

            if (isMergedSingle)
            {
                merged.AddPage(newPage);
                pagesMerged.Add((merged.Pages[merged.PageCount - 1], i));
            }

            if (isSplitByGroup)
            {
                var group = ParseGroupFromQr(qr); // 'A', 'B' nebo null
                if (group == 'B')
                {
                    outB.AddPage(newPage);
                    pagesB.Add((outB.Pages[outB.PageCount - 1], i));
                }
                else
                {
                    outA.AddPage(newPage);
                    pagesA.Add((outA.Pages[outA.PageCount - 1], i));
                }
            }

            // SORTING
        }

        private string? TryDecodeTopRight(BarcodeReader reader, Bitmap full, float relW = 0.45f, float relH = 0.45f)
        {
            using var roi = CropTopRight(full, relW, relH);
            var r = reader.Decode(roi);
            return r?.Text;
        }

        private SKBitmap PdfPageToSKBitmap(string pdfPath, int pageIndex)
        {
            return Conversion.ToImage(pdfPath, pageIndex);
        }

        private Bitmap SKBitmapToBitmap(SKBitmap skb)
        {
            using var img = SKImage.FromPixels(skb.PeekPixels());
            using var data = img.Encode(SKEncodedImageFormat.Png, 100);
            using var ms = new MemoryStream(data.ToArray());
            return (Bitmap)System.Drawing.Image.FromStream(ms);
        }

        private Bitmap CropTopRight(Bitmap bmp, float relW, float relH)
        {
            int w = Math.Max(1, (int)(bmp.Width * relW));
            int h = Math.Max(1, (int)(bmp.Height * relH));
            var rect = new Rectangle(bmp.Width - w, 0, w, h);
            return bmp.Clone(rect, bmp.PixelFormat);
        }

        private Bitmap RotateBitmap180(Bitmap src)
        {
            var dest = new Bitmap(src.Height, src.Width, src.PixelFormat);
            using var g = Graphics.FromImage(dest);
            g.Clear(Color.White);
            g.TranslateTransform(dest.Width / 2f, dest.Height / 2f);
            g.RotateTransform(180f);
            g.TranslateTransform(-src.Width / 2f, -src.Height / 2f);
            g.DrawImageUnscaled(src, 0, 0);
            return dest;
        }

        private string? DecodeQrTopRight(string pdfPath, int pageIndex)
        {
            using var sk = PdfPageToSKBitmap(pdfPath, pageIndex);
            using var full = SKBitmapToBitmap(sk);
            var roi = CropTopRight(full, 0.45f, 0.45f);
            try
            {
                var reader = new ZXing.Windows.Compatibility.BarcodeReader
                {
                    AutoRotate = false,
                    Options = new DecodingOptions { TryHarder = true, PossibleFormats = new[] { BarcodeFormat.QR_CODE }}
                };
                var r = reader.Decode(roi);
                return r?.Text;
            }
            finally { roi.Dispose(); }
        }

        private int NormalizeRotate(int deg) => ((deg % 360) + 360) % 360 switch { 0 => 0, 90 => 90, 180 => 180, 270 => 270, _ => 0 };
    }
}
