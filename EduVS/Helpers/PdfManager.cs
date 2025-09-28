using PdfSharp.Drawing;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using System.IO;

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
                        var qrData = $"{testCount}|{groupLetter}|{testName}|{testDate}|{p+1}";

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
    }
}
