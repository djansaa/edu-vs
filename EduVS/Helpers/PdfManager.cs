using EduVS.Models;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using PDFtoImage;
using SkiaSharp;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Media.Imaging;
using ZXing;
using ZXing.Common;
using ZXing.Windows.Compatibility;

namespace EduVS.Helpers
{
    internal class PdfManager
    {
        public PdfManager()
        {

        }

        public void GenerateTestPrintTemplate(string outputPath, string testSubject, string testName, string testDate, string? templateAPath, int templateACount, string? templateBPath, int templateBCount)
        {
            // validate inputs
            if (string.IsNullOrEmpty(testSubject))
            {
                throw new ArgumentException("Test subject is empty.", nameof(outputPath));
            }
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
            dst.Info.Title = $"{testSubject}_{testName}";
            dst.Info.Author = "EduVS";

            // fonts
            var fontOpts = new XPdfFontOptions(PdfFontEncoding.Unicode);
            var fontBold = new XFont("DejaVu Sans", 10, XFontStyleEx.Bold, fontOpts);
            var fontBoldBigger = new XFont("DejaVu Sans", 15, XFontStyleEx.Bold, fontOpts);

            // test count for unique test ids
            int testCount = 0;

            // local function to append header
            void AppendHeader(string templatePath, int count, string testSubject, string groupLetter)
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

                            // test subject + group
                            var centerX = w / 2.0;
                            gfx.DrawString($"{testSubject} [{groupLetter}]", fontBoldBigger, XBrushes.Black, new XPoint(centerX, top + 55), XStringFormats.Center);

                            // test name
                            gfx.DrawString($"{testName}", fontBoldBigger, XBrushes.Black, new XPoint(centerX, top + 75), XStringFormats.Center);
                        }

                        // ##################### QR CODE #####################

                        // qr code data
                        var qrData = $"TESTID:{testCount}|GROUPID:{groupLetter}|TESTSUBJECT:{testSubject}|TESTNAME:{testName}|TESTDATE:{testDate}|PAGE:{p + 1}";

                        int dpi = 300;
                        int pxSize = (int)Math.Round(Math.Min(qrSize, qrSize) * dpi / 72.0);
                        int ppm = Math.Max(4, pxSize / 33);

                        // create qr code png
                        var png = qrCodeManager.CreateQrPng(qrData, marginModules: 2);
                        using var ms = new MemoryStream(png);
                        using var img = XImage.FromStream(ms);

                        var qrRect = new XRect(w - margin - qrSize + 10, top - 10, qrSize, qrSize);

                        // draw qr code
                        gfx.DrawImage(img, qrRect);
                    }
                }
            }

            // create pages for template A
            if (!string.IsNullOrWhiteSpace(templateAPath) && templateACount > 0) AppendHeader(templateAPath, templateACount, testSubject, "A");

            if (!string.IsNullOrWhiteSpace(templateBPath) && templateBCount > 0) AppendHeader(templateBPath, templateBCount, testSubject, "B");

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
                        rotate = 180;
                        // save img full180
                        //full180.Save(@$"C:\Users\David\Downloads\page_{i}_rotated.png", ImageFormat.Png);

                        qr = TryDecodeTopRight(reader, full180);
                    }
                }

                if (qr == null)
                {
                    // QR code not found even after rotation
                    Debug.WriteLine($"Page: {i} - QR code not found.");
                    continue;
                }

                // qr code data
                QrCodeData qcd = QrCodeData.Parse(qr);

                // make tuple made of QrCodeData and pdf page
                Debug.WriteLine($"Page: {i} - QR code found: TestId={qcd.TestId}, GroupId={qcd.GroupId}, TestName={qcd.TestName}, TestDate={qcd.TestDate:yyyy-MM-dd}, Page={qcd.Page}");

                outputPagesData.Add((i, rotate, qcd));
            }

            // ================== CREATE OUTPUT PDFS ==================
            // SORTING
            // sort by page number
            if (sortByPageNumber)
            {
                outputPagesData = outputPagesData.OrderBy(t => t.qr.Page).ThenBy(t => t.qr.TestId).ToList();
            }
            // sort by test number
            else if (sortByTestNumber)
            {
                outputPagesData = outputPagesData.OrderBy(t => t.qr.TestId).ThenBy(t => t.qr.Page).ToList();
            }

            // SPLIT PAGES BY
            // groups A and B
            if (isSplitByGroup)
            {
                var pagesA = outputPagesData.Where(t => t.qr.GroupId == 'A').ToList();
                var pagesB = outputPagesData.Where(t => t.qr.GroupId == 'B').ToList();

                // copy pages to output pdfs
                CopyTestCheckPages(src, outputPathA, pagesA);
                CopyTestCheckPages(src, outputPathB, pagesB);
            }
            // merged
            else if (isMergedSingle)
            {
                var pagesMarged = outputPagesData.ToList();
                CopyTestCheckPages(src, outputPathA, pagesMarged);
            }
        }

        private void CopyTestCheckPages(PdfDocument src, string outputPath, IEnumerable<(int pageId, int rotation, QrCodeData qr)> tcp)
        {
            if (string.IsNullOrEmpty(outputPath)) throw new ArgumentException("Output path is null or empty.", nameof(outputPath));
            if (tcp == null) return;

            using var dst = new PdfDocument();

            // for each page, apply rotation and add to destination
            foreach (var (pageId, rotation, qr) in tcp)
            {
                var newPage = src.Pages[pageId];
                if (rotation != 0) newPage.Rotate = NormalizeRotate(newPage.Rotate + rotation);
                dst.AddPage(newPage);
            }

            // export pdf
            dst.Save(outputPath);
        }

        private string? TryDecodeTopRight(BarcodeReader reader, Bitmap full, float relW = 0.45f, float relH = 0.45f)
        {
            using var roi = CropTopRight(full, relW, relH);
            var r = reader.Decode(roi);
            return r?.Text;
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
            var dest = new Bitmap(src.Width, src.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            dest.SetResolution(src.HorizontalResolution, src.VerticalResolution);

            using var g = Graphics.FromImage(dest);
            g.TranslateTransform(src.Width / 2f, src.Height / 2f);
            g.RotateTransform(180f);
            g.TranslateTransform(-src.Width / 2f, -src.Height / 2f);
            g.DrawImage(src, 0, 0, src.Width, src.Height);
            return dest;
        }

        private int NormalizeRotate(int deg) => ((deg % 360) + 360) % 360 switch { 0 => 0, 90 => 90, 180 => 180, 270 => 270, _ => 0 };

        // ================================================ AI =============================================

        public string MergePdfs(IEnumerable<string> inputs, string outputPath)
        {
            using var dst = new PdfDocument();
            foreach (var p in inputs.Where(File.Exists))
            {
                using var src = PdfReader.Open(p, PdfDocumentOpenMode.Import);
                for (int i = 0; i < src.PageCount; i++)
                    dst.AddPage(src.Pages[i]);
            }
            dst.Save(outputPath);
            return outputPath;
        }

        public (List<TestData> tests, Dictionary<int, List<int>> pagesByTestId)
            Scan(string combinedPdfPath, RectangleF qrTopRightRel, RectangleF nameBoxTopLeftRel)
        {
            var tests = new List<TestData>();
            var pagesByTest = new Dictionary<int, List<int>>();

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

            using var src = PdfReader.Open(combinedPdfPath, PdfDocumentOpenMode.Import);
            var haveNameBox = new HashSet<int>();

            for (int i = 0; i < src.PageCount; i++)
            {
                using var skb = PdfPageToSKBitmap(combinedPdfPath, i);

                string? qr = TryDecodeQr(reader, skb, qrTopRightRel);
                if (qr is null)
                {
                    using var rot = RotateSK180(skb);
                    qr = TryDecodeQr(reader, rot, qrTopRightRel);
                }
                if (qr is null) continue;

                if (!QrCodeData.TryParse(qr, out var q) || q is null) continue;

                if (!pagesByTest.TryGetValue(q.TestId, out var list))
                    pagesByTest[q.TestId] = list = new List<int>();
                list.Add(i);

                if (q.Page == 1 && !haveNameBox.Contains(q.TestId))
                {
                    var nb = CropRelToBitmapSource(skb, nameBoxTopLeftRel);
                    tests.Add(new TestData { TestId = q.TestId, NameBoxBitmap = nb });
                    haveNameBox.Add(q.TestId);
                }
            }

            // sort pages
            foreach (var k in pagesByTest.Keys.ToList())
                pagesByTest[k] = pagesByTest[k].Distinct().OrderBy(x => x).ToList();

            // sort tests by id
            tests = tests.OrderBy(t => t.TestId).ToList();

            return (tests, pagesByTest);
        }


        public void ExportPerStudent(string combinedPdfPath,
                                     IReadOnlyDictionary<int, List<int>> pagesByTestId,
                                     IEnumerable<(string name, int testId)> assignments,
                                     string outputFolder)
        {
            Directory.CreateDirectory(outputFolder);
            using var src = PdfReader.Open(combinedPdfPath, PdfDocumentOpenMode.Import);

            foreach (var (name, testId) in assignments)
            {
                if (!pagesByTestId.TryGetValue(testId, out var pages) || pages.Count == 0) continue;

                using var dst = new PdfDocument();
                foreach (var p in pages) dst.AddPage(src.Pages[p]);

                var file = Path.Combine(outputFolder, $"{Safe(name)}_Test_{testId}.pdf");
                dst.Save(file);
            }
        }


        static string Safe(string s)
        {
            foreach (var ch in Path.GetInvalidFileNameChars()) s = s.Replace(ch, '_');
            return s.Trim();
        }

        static SKBitmap PdfPageToSKBitmap(string pdfPath, int pageIndex)
        {
            using var fs = File.OpenRead(pdfPath);
            return Conversion.ToImage(fs, pageIndex);
        }

        static string? TryDecodeQr(ZXing.Windows.Compatibility.BarcodeReader reader, SKBitmap src, RectangleF rel)
        {
            using var crop = CropRelShared(src, rel);
            using var img = SKImage.FromPixels(crop.PeekPixels());
            using var data = img.Encode(SKEncodedImageFormat.Png, 100);
            using var ms = new MemoryStream(data.ToArray());
            using var bmp = (System.Drawing.Bitmap)System.Drawing.Image.FromStream(ms);
            return reader.Decode(bmp)?.Text;
        }

        static SKBitmap CropRelShared(SKBitmap src, RectangleF rel)
        {
            int x = Math.Clamp((int)(src.Width * rel.X), 0, Math.Max(0, src.Width - 1));
            int y = Math.Clamp((int)(src.Height * rel.Y), 0, Math.Max(0, src.Height - 1));
            int w = Math.Clamp((int)(src.Width * rel.Width), 1, src.Width - x);
            int h = Math.Clamp((int)(src.Height * rel.Height), 1, src.Height - y);

            var subset = new SKBitmap();
            if (!src.ExtractSubset(subset, new SKRectI(x, y, x + w, y + h)))
                throw new InvalidOperationException("ExtractSubset failed.");
            return subset;
        }

        static SKBitmap RotateSK180(SKBitmap src)
        {
            var dst = new SKBitmap(src.Width, src.Height, src.ColorType, src.AlphaType);
            using var canvas = new SKCanvas(dst);
            canvas.Translate(src.Width, src.Height);
            canvas.RotateDegrees(180);
            canvas.DrawBitmap(src, 0, 0);
            return dst;
        }

        static BitmapSource CropRelToBitmapSource(SKBitmap src, RectangleF rel)
        {
            using var crop = CropRelShared(src, rel);
            using var img = SKImage.FromPixels(crop.PeekPixels());
            using var data = img.Encode(SKEncodedImageFormat.Png, 100);
            var bi = new BitmapImage();
            bi.BeginInit();
            bi.CacheOption = BitmapCacheOption.OnLoad;
            bi.StreamSource = new MemoryStream(data.ToArray());
            bi.EndInit();
            bi.Freeze();
            return bi;
        }
    }
}
