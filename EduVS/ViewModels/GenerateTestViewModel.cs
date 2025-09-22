using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EduVS.Data;
using EduVS.Helpers;
using Microsoft.Extensions.Logging;
using System.Windows;


namespace EduVS.ViewModels
{
    internal partial class GenerateTestViewModel : BaseViewModel
    {
        [ObservableProperty] private string? testName;
        [ObservableProperty] private DateTime testDate;

        [ObservableProperty] private string? templateAPath;
        [ObservableProperty] private string? templateBPath;

        [ObservableProperty] private int templateACount = 0;
        [ObservableProperty] private int templateBCount = 0;

        public RelayCommand BrowseTemplateACommand { get; }
        public RelayCommand BrowseTemplateBCommand { get; }
        public RelayCommand ExportCommand { get; }

        public GenerateTestViewModel(ILogger logger, AppDbContext db) : base(logger, db)
        {
            TestDate = DateTime.Today;

            BrowseTemplateACommand = new RelayCommand(BrowseTemplateA);
            BrowseTemplateBCommand = new RelayCommand(BrowseTemplateB);
            ExportCommand = new RelayCommand(ExportTests);
        }

        private void BrowseTemplateA()
        {
            var path = PdfPicker.PickPdf();
            if (path is null) return;

            TemplateAPath = path;
        }

        private void BrowseTemplateB()
        {
            var path = PdfPicker.PickPdf();
            if (path is null) return;

            TemplateBPath = path;
        }

        private void ExportTests()
        {
            // testName
            if (string.IsNullOrEmpty(TestName))
            {
                MessageBox.Show("Test name is empty.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            // templateAPath
            if (string.IsNullOrEmpty(TemplateAPath))
            {
                var result = MessageBox.Show("Template A path is empty. Continue?", "Validation Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.No) return;
            }
            // templateACount
            if (!string.IsNullOrEmpty(TemplateAPath) && TemplateACount == 0)
            {
                var result = MessageBox.Show("Template A count is 0. Continue?", "Validation Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.No) return;
            }
            // templateBPath
            if (string.IsNullOrEmpty(TemplateBPath))
            {
                var result = MessageBox.Show("Template B path is empty. Continue?", "Validation Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.No) return;
            }
            // templateBCount
            if (!string.IsNullOrEmpty(TemplateBPath) && TemplateBCount == 0)
            {
                var result = MessageBox.Show("Template B count is 0. Continue?", "Validation Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.No) return;
            }

            // select export path for As and Bs tests
            var outputPath = PdfPicker.PickPdfSavePath($"{TestName}_{TestDate:yyyy-MM-dd}_print.pdf");
            if (outputPath == null) return;

            // summary name of templates, number of A and B tests, date
            var summary = MessageBox.Show(
                $"Exporting test with the following details:\n\n" +
                $"Test Name: {TestName}\n" +
                $"Test Date: {TestDate:yyyy-MM-dd}\n" +
                $"Template A: {(string.IsNullOrEmpty(TemplateAPath) ? "None" : TemplateAPath)} (Count: {TemplateACount})\n" +
                $"Template B: {(string.IsNullOrEmpty(TemplateBPath) ? "None" : TemplateBPath)} (Count: {TemplateBCount})\n\n" +
                $"Output Path: {outputPath}\n\n" +
                $"Proceed with export?",
                "Confirm Export",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (summary == MessageBoxResult.No) return;

            // create PDF
            var pdfManager = new PdfManager();
            pdfManager.GenerateTestPrintTemplate(outputPath, TestName, TestDate.ToString("yyyy-MM-dd"), TemplateAPath, TemplateACount, TemplateBPath, TemplateBCount);
        }
    }
}
