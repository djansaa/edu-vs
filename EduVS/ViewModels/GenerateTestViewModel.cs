using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EduVS.Helpers;
using EduVS.Models;
using EduVS.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Windows;

namespace EduVS.ViewModels
{
    public partial class GenerateTestViewModel : BaseViewModel
    {
        private const int MaxSubjectAndNameChars = 52;

        private readonly PdfManager _pdfManager;
        private readonly IServiceProvider _serviceProvider;

        [ObservableProperty] private string? testSubject;
        [ObservableProperty] private string? testName;
        [ObservableProperty] private DateTime testDate;

        [ObservableProperty] private string? templateAPath;
        [ObservableProperty] private string? templateBPath;

        [ObservableProperty] private int templateACount = 0;
        [ObservableProperty] private int templateBCount = 0;

        public RelayCommand BrowseTemplateACommand { get; }
        public RelayCommand BrowseTemplateBCommand { get; }
        public IAsyncRelayCommand ExportCommand { get; }

        public GenerateTestViewModel(ILogger<GenerateTestViewModel> logger, PdfManager pdfManager, IServiceProvider serviceProvider) : base(logger)
        {
            _pdfManager = pdfManager;
            _serviceProvider = serviceProvider;
            TestDate = DateTime.Today;

            BrowseTemplateACommand = new RelayCommand(BrowseTemplateA);
            BrowseTemplateBCommand = new RelayCommand(BrowseTemplateB);
            ExportCommand = new AsyncRelayCommand(ExportTestsAsync);
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

        private async Task ExportTestsAsync()
        {
            var testSubject = TestSubject;
            var testName = TestName;

            if (TemplateACount + TemplateBCount == 0)
            {
                MessageBox.Show("Total number of test pages is 0.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrEmpty(testSubject))
            {
                MessageBox.Show("Test subject is empty.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrEmpty(testName))
            {
                MessageBox.Show("Test name is empty.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (testSubject.Length + testName.Length > MaxSubjectAndNameChars)
            {
                var overflow = testSubject.Length + testName.Length - MaxSubjectAndNameChars;
                MessageBox.Show($"Test subject and test name can have at most {MaxSubjectAndNameChars} characters combined for the QR code.\nReduce subject/name by {overflow} characters.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrEmpty(TemplateAPath))
            {
                var result = MessageBox.Show("Template A path is empty. Continue?", "Validation Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.No) return;
            }

            if (!string.IsNullOrEmpty(TemplateAPath) && TemplateACount == 0)
            {
                var result = MessageBox.Show("Template A count is 0. Continue?", "Validation Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.No) return;
            }

            if (string.IsNullOrEmpty(TemplateBPath))
            {
                var result = MessageBox.Show("Template B path is empty. Continue?", "Validation Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.No) return;
            }

            if (!string.IsNullOrEmpty(TemplateBPath) && TemplateBCount == 0)
            {
                var result = MessageBox.Show("Template B count is 0. Continue?", "Validation Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.No) return;
            }

            var outputPath = PdfPicker.PickPdfSavePath($"{testSubject}_{testName}_{TestDate:yyyy-MM-dd}_print.pdf");
            if (outputPath is null) return;

            GenerateTestProgressWindowView? progressWindow = null;
            GenerateTestProgressViewModel? progressVm = null;
            Window? ownerWindow = null;

            try
            {
                _logger.LogInformation($"Exporting test with the following details:\n\n" +
                    $"Test Subject: {testSubject}\n" +
                    $"Test Name: {testName}\n" +
                    $"Test Date: {TestDate:yyyy-MM-dd}\n" +
                    $"Template A: {(string.IsNullOrEmpty(TemplateAPath) ? "None" : TemplateAPath)} (Count: {TemplateACount})\n" +
                    $"Template B: {(string.IsNullOrEmpty(TemplateBPath) ? "None" : TemplateBPath)} (Count: {TemplateBCount})\n\n" +
                    $"Output Path: {outputPath}");

                var totalTestsToGenerate = 0;
                if (!string.IsNullOrWhiteSpace(TemplateAPath) && TemplateACount > 0) totalTestsToGenerate += TemplateACount;
                if (!string.IsNullOrWhiteSpace(TemplateBPath) && TemplateBCount > 0) totalTestsToGenerate += TemplateBCount;

                progressWindow = _serviceProvider.GetRequiredService<GenerateTestProgressWindowView>();
                progressVm = progressWindow.ViewModel;
                progressVm.Initialize(totalTestsToGenerate);
                ownerWindow = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive && w != progressWindow);
                progressWindow.Owner = ownerWindow;
                progressWindow.Show();

                var progress = new Progress<GenerateTestProgressInfo>(progressVm.Report);
                await _pdfManager.GenerateTestPrintTemplateAsync(
                    outputPath,
                    testSubject,
                    testName,
                    TestDate,
                    TemplateAPath,
                    TemplateACount,
                    TemplateBPath,
                    TemplateBCount,
                    progress,
                    progressVm.CancellationToken);

                progressVm.Finish("Generation completed.");
                progressVm.CanClose = true;
                progressWindow.Close();
                ownerWindow?.Activate();
                ShowOwnedMessageBox(ownerWindow, "Test PDF generated successfully.", "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (OperationCanceledException)
            {
                progressVm?.Finish("Generation canceled.");
                if (progressVm is not null) progressVm.CanClose = true;
                progressWindow?.Close();
                ownerWindow?.Activate();
                ShowOwnedMessageBox(ownerWindow, "Test PDF generation was canceled.", "Export Canceled", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate test PDF.");
                progressVm?.Finish("Generation failed.");
                if (progressVm is not null) progressVm.CanClose = true;
                progressWindow?.Close();
                ownerWindow?.Activate();
                ShowOwnedMessageBox(ownerWindow, $"Failed to generate test PDF.\n\n{ex.Message}", "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (progressWindow?.IsVisible == true)
                {
                    if (progressVm is not null) progressVm.CanClose = true;
                    progressWindow.Close();
                }
            }
        }

        private static MessageBoxResult ShowOwnedMessageBox(Window? owner, string messageBoxText, string caption, MessageBoxButton button, MessageBoxImage icon)
        {
            return owner is null
                ? MessageBox.Show(messageBoxText, caption, button, icon)
                : MessageBox.Show(owner, messageBoxText, caption, button, icon);
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
