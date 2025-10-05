using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EduVS.Data;
using EduVS.Helpers;
using Microsoft.Extensions.Logging;
using System.Windows;

namespace EduVS.ViewModels
{
    public partial class PrepareTestCheckViewModel : BaseViewModel
    {
        [ObservableProperty] private string? pdfPath;

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
        public RelayCommand ExportCommand { get; }
        public RelayCommand BrowsePdfANewCommmand { get; }
        public RelayCommand BrowsePdfBNewCommmand { get; }

        public PrepareTestCheckViewModel(ILogger<PrepareTestCheckViewModel> logger, AppDbContext db) : base(logger, db)
        {
            BrowsePdfCommand = new RelayCommand(BrowsePdf);
            BrowsePdfANewCommmand = new RelayCommand(BrowsePdfANew);
            BrowsePdfBNewCommmand = new RelayCommand(BrowsePdfBNew);
            ExportCommand = new RelayCommand(ExportTestCheck);

        }

        private void BrowsePdf()
        {
            var path = PdfPicker.PickPdf();
            if (path is null) return;

            PdfPath = path;
        }

        private void BrowsePdfANew()
        {
            var path = PdfPicker.PickPdfSavePath("A_test_check.pdf");
            if (path is null) return;

            PdfPathA = path;
        }

        private void BrowsePdfBNew()
        {
            var path = PdfPicker.PickPdfSavePath("B_test_check.pdf");
            if (path is null) return;

            PdfPathB = path;
        }

        private void ExportTestCheck()
        {
            // pdfPath - must be defined
            if (string.IsNullOrEmpty(PdfPath))
            {
                MessageBox.Show("You must choose input PDF file.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
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

            // summary name of templates, number of A and B tests, date
            var summary = MessageBox.Show(
                $"Exporting filled tests with the following settings:\n\n" +
                $"PDF: {PdfPath}\n" +
                $"Output mode: {(IsSplitByGroup ? "Split by group" : "Single merged PDF")}\n" +
                $"Output path A: {PdfPathA}\n" +
                $"Output path B: {PdfPathB}\n" +
                $"Sorting: {(SortByPageNumber ? "By page number" : "By test number")}\n\n" +
                $"Proceed with export?",
                "Confirm Export",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (summary == MessageBoxResult.No) return;

            // pdf manager
            var pdfManager = new PdfManager();

            pdfManager.GenerateTestCheck(PdfPath, IsSplitByGroup, IsMergedSingle, SortByPageNumber, SortByTestNumber, PdfPathA, PdfPathB);

            MessageBox.Show("Test check PDF generated successfully.", "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
