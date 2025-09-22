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
            int testCount = 1;

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
                            gfx.DrawString("jméno:", fontBold, XBrushes.Black, new XPoint(margin, top + 12));

                            // date
                            var dateText = $"datum: {testDate:yyyy-MM-dd}";
                            gfx.DrawString(dateText, fontBold, XBrushes.Black, new XRect(margin, top, w - margin - qrSize - margin - 12, 24), XStringFormats.TopRight);

                            // group
                            var centerX = w / 2.0;
                            gfx.DrawString(groupLetter, fontBoldBigger, XBrushes.Black, new XPoint(centerX, top + 70), XStringFormats.Center);

                            // test name
                            gfx.DrawString(testName, fontBoldBigger, XBrushes.Black,  new XPoint(centerX, top + 90), XStringFormats.Center);
                        }

                        // ##################### QR CODE #####################

                        // qr code data
                        var qrData = $"{testCount}|{groupLetter}|{testName}|{testDate}|{p}";

                        int dpi = 300;
                        int pxSize = (int)Math.Round(Math.Min(qrSize, qrSize) * dpi / 72.0);
                        int ppm = Math.Max(4, pxSize / 33);

                        // create qr code png
                        var png = qrCodeManager.CreateQrPng(qrData);
                        using var ms = new MemoryStream(png);
                        using var img = XImage.FromStream(ms);

                        var qrRect = new XRect(w - margin - qrSize, top, qrSize, qrSize);

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

        //public void GenerateTestPrintTemplate(string outputPath, string testName, string testDate, string? templateAPath, int templateACount, string? templateBPath, int templateBCount)
        //{
        //    // validate inputs
        //    if (string.IsNullOrEmpty(testName)) {
        //        throw new ArgumentException("Test name is empty.", nameof(testName));
        //    }

        //    if (string.IsNullOrEmpty(testName))
        //    {
        //        throw new ArgumentException("Test name is empty.", nameof(testName));
        //    }

        //    if (string.IsNullOrEmpty(testDate))
        //    {
        //        throw new ArgumentException("Test date is empty.", nameof(testDate));
        //    }

        //    if (string.IsNullOrEmpty(templateAPath) && string.IsNullOrEmpty(templateBPath))
        //    {
        //        throw new ArgumentException("At least one template path must be provided.");
        //    }

        //    if (!string.IsNullOrEmpty(templateAPath) && templateACount < 0)
        //    {
        //        throw new ArgumentException("Template A count is invalid.", nameof(templateACount));
        //    }

        //    if (!string.IsNullOrEmpty(templateBPath) && templateBCount < 0)
        //    {
        //        throw new ArgumentException("Template B count is invalid.", nameof(templateBCount));
        //    }

        //    // document builder
        //    var builder = new PdfDocumentBuilder();

        //    // read fonts from embedded resources
        //    //var fontReg = builder.AddTrueTypeFont(EmbeddedResourceLoader.LoadEmbeddedFont("resources.DejaVuSans.ttf"));
        //    //var fontBold = builder.AddTrueTypeFont(EmbeddedResourceLoader.LoadEmbeddedFont("resources.DejaVuSans-Bold.ttf"));
        //    var fontReg = builder.AddStandard14Font(Standard14Font.Helvetica);
        //    var fontBold = builder.AddStandard14Font(Standard14Font.HelveticaBold);

        //    // test count for unique test ids
        //    int testCount = 1;

        //    // create pages for template A
        //    if (!string.IsNullOrEmpty(templateAPath) && templateACount > 0) {

        //        using (var src = PdfDocument.Open(templateAPath))
        //        {
        //            var page = src.GetPage(1);
        //            var pageWidth = page.Width;
        //            var pageHeight = page.Height;

        //            for (int i = 0; i < templateACount; i++)
        //            {
        //                testCount++;

        //                // copy all pages from source to new document
        //                for (int p = 1; p <= src.NumberOfPages; p++)
        //                {
        //                    // get source page
        //                    var newPage = builder.AddPage(src, p);

        //                    double w = pageWidth;
        //                    double h = pageHeight;
        //                    double margin = 36;
        //                    double qrSize = 100;
        //                    double top = h - margin;

        //                    // first page header
        //                    if (p == 1)
        //                    {


        //                        // name
        //                        newPage.AddText($"jméno:", 10, new PdfPoint(25, 17), fontBold);

        //                        // date
        //                        newPage.AddText($"datum: {testDate}", 10, new PdfPoint(25, 17), fontBold);

        //                        // group name
        //                        newPage.AddText("A", 16, new PdfPoint(50, 50), fontBold);

        //                        // test name
        //                        newPage.AddText(testName, 14, new PdfPoint(50, 50), fontBold);
        //                    }

        //                    // rectangle for qr code
        //                    newPage.DrawRectangle(new PdfPoint(w - margin - qrSize, top - qrSize), qrSize, qrSize);
        //                }
        //            }
        //        }
        //    }

        //    byte[] documentBytes = builder.Build();
        //    File.WriteAllBytes(outputPath, documentBytes);
        //}
    }
}
