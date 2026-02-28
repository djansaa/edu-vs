using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EduVS.Helpers;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Windows;


namespace EduVS.ViewModels
{
    public partial class GenerateTestViewModel : BaseViewModel
    {
        private const int MaxSubjectAndNameChars = 52;

        [ObservableProperty] private string? testSubject;
        [ObservableProperty] private string? testName;
        [ObservableProperty] private DateTime testDate;

        [ObservableProperty] private string? templateAPath;
        [ObservableProperty] private string? templateBPath;

        [ObservableProperty] private int templateACount = 0;
        [ObservableProperty] private int templateBCount = 0;

        public RelayCommand BrowseTemplateACommand { get; }
        public RelayCommand BrowseTemplateBCommand { get; }
        public RelayCommand ExportCommand { get; }

        public GenerateTestViewModel(ILogger<GenerateTestViewModel> logger) : base(logger)
        {
            TestDate = DateTime.Today;

            BrowseTemplateACommand = new RelayCommand(BrowseTemplateA);
            BrowseTemplateBCommand = new RelayCommand(BrowseTemplateB);
            ExportCommand = new RelayCommand(ExportTests);
        }

        partial void OnTestSubjectChanged(string? value)
        {
            EnforceQrPayloadBudget(isSubjectChanged: true);
        }

        partial void OnTestNameChanged(string? value)
        {
            EnforceQrPayloadBudget(isSubjectChanged: false);
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
            // check page count
            if (TemplateACount + TemplateBCount == 0)
            {
                MessageBox.Show("Total number of test pages is 0.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            // testSubject
            if (string.IsNullOrEmpty(TestSubject))
            {
                MessageBox.Show("Test subject is empty.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            // testName
            if (string.IsNullOrEmpty(TestName))
            {
                MessageBox.Show("Test name is empty.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if ((TestSubject?.Length ?? 0) + (TestName?.Length ?? 0) > MaxSubjectAndNameChars)
            {
                var currentLength = (TestSubject?.Length ?? 0) + (TestName?.Length ?? 0);
                var overflow = currentLength - MaxSubjectAndNameChars;
                MessageBox.Show($"Test subject and test name can have at most {MaxSubjectAndNameChars} characters combined for the QR code.\nReduce subject/name by {overflow} characters.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
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
            var outputPath = PdfPicker.PickPdfSavePath($"{TestSubject}_{TestName}_{TestDate:yyyy-MM-dd}_print.pdf");
            if (outputPath == null) return;

            // summary name of templates, number of A and B tests, date
            _logger.LogInformation($"Exporting test with the following details:\n\n" +
                $"Test Subject: {TestSubject}\n" +
                $"Test Name: {TestName}\n" +
                $"Test Date: {TestDate:yyyy-MM-dd}\n" +
                $"Template A: {(string.IsNullOrEmpty(TemplateAPath) ? "None" : TemplateAPath)} (Count: {TemplateACount})\n" +
                $"Template B: {(string.IsNullOrEmpty(TemplateBPath) ? "None" : TemplateBPath)} (Count: {TemplateBCount})\n\n" +
                $"Output Path: {outputPath}");

            // create PDF
            var pdfManager = new PdfManager();
            pdfManager.GenerateTestPrintTemplate(outputPath, TestSubject, TestName, TestDate.ToString("yyyy-MM-dd"), TemplateAPath, TemplateACount, TemplateBPath, TemplateBCount);

            MessageBox.Show("Test PDF generated successfully.", "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void EnforceQrPayloadBudget(bool isSubjectChanged)
        {
            var subject = TestSubject ?? string.Empty;
            var name = TestName ?? string.Empty;
            var totalLength = subject.Length + name.Length;

            if (totalLength <= MaxSubjectAndNameChars) return;

            var overflow = totalLength - MaxSubjectAndNameChars;
            if (isSubjectChanged)
            {
                TestSubject = subject[..Math.Max(0, subject.Length - overflow)];
            }
            else
            {
                TestName = name[..Math.Max(0, name.Length - overflow)];
            }
        }
    }
}
