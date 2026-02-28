using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EduVS.Helpers;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Windows;

namespace EduVS.ViewModels
{
    public partial class PrepareTestCheckViewModel : BaseViewModel
    {
        [ObservableProperty] private string? pdfPath;
        [ObservableProperty] private string? detectedTestSubject;
        [ObservableProperty] private string? detectedTestName;
        [ObservableProperty] private int sourcePdfPageCount;
        [ObservableProperty] private bool isPdfInfoLoaded;
        [ObservableProperty] private bool isReadingPdfInfo;

        // Output mode
        [ObservableProperty] private bool isSplitByGroup = true;
        [ObservableProperty] private bool isMergedSingle = false;

        // Sorting mode
        [ObservableProperty] private bool sortByPageNumber = true;
        [ObservableProperty] private bool sortByTestNumber = false;

        // PDFs output paths
        [ObservableProperty] private string? pdfPathA;
        [ObservableProperty] private string? pdfPathB;

        // ==== Commands ====
        public RelayCommand BrowsePdfCommand { get; }
        public RelayCommand ReadPdfInfoCommand { get; }
        public RelayCommand ExportCommand { get; }
        public RelayCommand BrowsePdfANewCommmand { get; }
        public RelayCommand BrowsePdfBNewCommmand { get; }

        public bool CanReadPdfInfo => !string.IsNullOrWhiteSpace(PdfPath) && !IsReadingPdfInfo;
        public bool CanConfigureOutput => IsPdfInfoLoaded && !IsReadingPdfInfo;

        public PrepareTestCheckViewModel(ILogger<PrepareTestCheckViewModel> logger) : base(logger)
        {
            BrowsePdfCommand = new RelayCommand(BrowsePdf);
            ReadPdfInfoCommand = new RelayCommand(ReadPdfInfo);
            BrowsePdfANewCommmand = new RelayCommand(BrowsePdfANew);
            BrowsePdfBNewCommmand = new RelayCommand(BrowsePdfBNew);
            ExportCommand = new RelayCommand(ExportTestCheck);
        }

        partial void OnPdfPathChanged(string? value)
        {
            ResetLoadedPdfInfo();
            OnPropertyChanged(nameof(CanReadPdfInfo));
        }

        partial void OnIsPdfInfoLoadedChanged(bool value)
        {
            OnPropertyChanged(nameof(CanConfigureOutput));
        }

        partial void OnIsReadingPdfInfoChanged(bool value)
        {
            OnPropertyChanged(nameof(CanReadPdfInfo));
            OnPropertyChanged(nameof(CanConfigureOutput));
        }

        private void BrowsePdf()
        {
            var path = PdfPicker.PickPdf();
            if (path is null) return;

            PdfPath = path;
        }

        private void ReadPdfInfo()
        {
            if (string.IsNullOrWhiteSpace(PdfPath))
            {
                MessageBox.Show("You must choose input PDF file.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                IsReadingPdfInfo = true;
                ResetLoadedPdfInfo();

                var pdfManager = new PdfManager();
                var info = pdfManager.ReadTestCheckPdfInfo(PdfPath);

                SourcePdfPageCount = info.PageCount;

                if (info.MetadataQr is null)
                {
                    MessageBox.Show("No readable QR code with test information was found in the selected PDF.", "Read PDF Info", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                DetectedTestSubject = info.MetadataQr.TestSubject;
                DetectedTestName = info.MetadataQr.TestName;
                IsPdfInfoLoaded = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to read PDF info from {PdfPath}", PdfPath);
                MessageBox.Show($"Failed to read PDF info.\n\n{ex.Message}", "Read PDF Info", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsReadingPdfInfo = false;
            }
        }

        private void BrowsePdfANew()
        {
            var path = PdfPicker.PickPdfSavePath(BuildDefaultOutputFileName("A_test_check.pdf"));
            if (path is null) return;

            PdfPathA = path;
        }

        private void BrowsePdfBNew()
        {
            var path = PdfPicker.PickPdfSavePath(BuildDefaultOutputFileName("B_test_check.pdf"));
            if (path is null) return;

            PdfPathB = path;
        }

        private void ExportTestCheck()
        {
            if (string.IsNullOrEmpty(PdfPath))
            {
                MessageBox.Show("You must choose input PDF file.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (!IsPdfInfoLoaded)
            {
                MessageBox.Show("Read the source PDF information before exporting.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (string.IsNullOrEmpty(PdfPathA))
            {
                MessageBox.Show("You must choose output PDF file for group A.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (IsSplitByGroup && string.IsNullOrEmpty(PdfPathB))
            {
                MessageBox.Show("You must choose output PDF file for group B.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _logger.LogInformation($"Exporting filled tests with the following settings:\n\n" +
                $"PDF: {PdfPath}\n" +
                $"Detected test subject: {DetectedTestSubject}\n" +
                $"Detected test name: {DetectedTestName}\n" +
                $"Output mode: {(IsSplitByGroup ? "Split by group" : "Single merged PDF")}\n" +
                $"Output path A: {PdfPathA}\n" +
                $"Output path B: {PdfPathB}\n" +
                $"Sorting: {(SortByPageNumber ? "By page number" : "By test number")}");

            var pdfManager = new PdfManager();
            pdfManager.GenerateTestCheck(PdfPath, IsSplitByGroup, IsMergedSingle, SortByPageNumber, SortByTestNumber, PdfPathA, PdfPathB ?? string.Empty);

            MessageBox.Show("Test check PDF generated successfully.", "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ResetLoadedPdfInfo()
        {
            DetectedTestSubject = null;
            DetectedTestName = null;
            SourcePdfPageCount = 0;
            IsPdfInfoLoaded = false;
            PdfPathA = null;
            PdfPathB = null;
        }

        private string BuildDefaultOutputFileName(string fallbackName)
        {
            if (string.IsNullOrWhiteSpace(DetectedTestSubject) || string.IsNullOrWhiteSpace(DetectedTestName))
            {
                return fallbackName;
            }

            return $"{SanitizeFileNamePart(DetectedTestSubject)}_{SanitizeFileNamePart(DetectedTestName)}_{fallbackName}";
        }

        private static string SanitizeFileNamePart(string value)
        {
            var sanitized = value;

            foreach (var invalidChar in Path.GetInvalidFileNameChars())
            {
                sanitized = sanitized.Replace(invalidChar, '_');
            }

            return sanitized.Trim();
        }
    }
}
