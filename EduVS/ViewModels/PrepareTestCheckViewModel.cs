using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EduVS.Helpers;
using EduVS.Models;
using EduVS.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Windows;

namespace EduVS.ViewModels
{
    public partial class PrepareTestCheckViewModel : BaseViewModel
    {
        private readonly PdfManager _pdfManager;
        private readonly IServiceProvider _serviceProvider;

        [ObservableProperty] private string? pdfPath;
        [ObservableProperty] private string? detectedTestSubject;
        [ObservableProperty] private string? detectedTestName;
        [ObservableProperty] private int sourcePdfPageCount;
        [ObservableProperty] private bool isPdfInfoLoaded;
        [ObservableProperty] private bool isReadingPdfInfo;

        [ObservableProperty] private bool isSplitByGroup = true;
        [ObservableProperty] private bool isMergedSingle = false;

        [ObservableProperty] private bool sortByPageNumber = true;
        [ObservableProperty] private bool sortByTestNumber = false;

        [ObservableProperty] private string? pdfPathA;
        [ObservableProperty] private string? pdfPathB;

        public RelayCommand BrowsePdfCommand { get; }
        public IAsyncRelayCommand ExportCommand { get; }
        public RelayCommand BrowsePdfANewCommmand { get; }
        public RelayCommand BrowsePdfBNewCommmand { get; }

        public bool CanConfigureOutput => IsPdfInfoLoaded && !IsReadingPdfInfo;

        public PrepareTestCheckViewModel(ILogger<PrepareTestCheckViewModel> logger, PdfManager pdfManager, IServiceProvider serviceProvider) : base(logger)
        {
            _pdfManager = pdfManager;
            _serviceProvider = serviceProvider;
            BrowsePdfCommand = new RelayCommand(BrowsePdf);
            BrowsePdfANewCommmand = new RelayCommand(BrowsePdfANew);
            BrowsePdfBNewCommmand = new RelayCommand(BrowsePdfBNew);
            ExportCommand = new AsyncRelayCommand(ExportTestCheckAsync);
        }

        partial void OnPdfPathChanged(string? value)
        {
            ResetLoadedPdfInfo();
        }

        partial void OnIsPdfInfoLoadedChanged(bool value)
        {
            OnPropertyChanged(nameof(CanConfigureOutput));
        }

        partial void OnIsReadingPdfInfoChanged(bool value)
        {
            OnPropertyChanged(nameof(CanConfigureOutput));
        }

        private void BrowsePdf()
        {
            var path = PdfPicker.PickPdf();
            if (path is null) return;

            PdfPath = path;
            ReadPdfInfo();
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

                var info = _pdfManager.ReadTestCheckPdfInfo(PdfPath);

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

        private async Task ExportTestCheckAsync()
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

            PrepareTestCheckProgressWindowView? progressWindow = null;
            PrepareTestCheckProgressViewModel? progressVm = null;
            Window? ownerWindow = null;

            try
            {
                _logger.LogInformation($"Exporting filled tests with the following settings:\n\n" +
                    $"PDF: {PdfPath}\n" +
                    $"Detected test subject: {DetectedTestSubject}\n" +
                    $"Detected test name: {DetectedTestName}\n" +
                    $"Output mode: {(IsSplitByGroup ? "Split by group" : "Single merged PDF")}\n" +
                    $"Output path A: {PdfPathA}\n" +
                    $"Output path B: {PdfPathB}\n" +
                    $"Sorting: {(SortByPageNumber ? "By page number" : "By test number")}");

                progressWindow = _serviceProvider.GetRequiredService<PrepareTestCheckProgressWindowView>();
                progressVm = progressWindow.ViewModel;
                progressVm.Initialize(SourcePdfPageCount);
                ownerWindow = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive && w != progressWindow);
                progressWindow.Owner = ownerWindow;
                progressWindow.Show();

                var progress = new Progress<PrepareTestCheckProgressInfo>(progressVm.Report);
                await _pdfManager.GenerateTestCheckAsync(
                    PdfPath,
                    IsSplitByGroup,
                    IsMergedSingle,
                    SortByPageNumber,
                    SortByTestNumber,
                    PdfPathA,
                    PdfPathB ?? string.Empty,
                    progress,
                    progressVm.CancellationToken);

                progressVm.Finish("Export completed.");
                progressVm.CanClose = true;
                progressWindow.Close();
                ownerWindow?.Activate();
                ShowOwnedMessageBox(ownerWindow, "Test check PDF generated successfully.", "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (OperationCanceledException)
            {
                progressVm?.Finish("Export canceled.");
                if (progressVm is not null) progressVm.CanClose = true;
                progressWindow?.Close();
                ownerWindow?.Activate();
                ShowOwnedMessageBox(ownerWindow, "Export was canceled during test check preparation.", "Export Canceled", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                progressVm?.Finish("Export failed.");
                if (progressVm is not null) progressVm.CanClose = true;
                progressWindow?.Close();
                ownerWindow?.Activate();
                _logger.LogError(ex, "Failed to export test check.");
                ShowOwnedMessageBox(ownerWindow, $"Failed to export test check.\n\n{ex.Message}", "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
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

        private static MessageBoxResult ShowOwnedMessageBox(Window? owner, string messageBoxText, string caption, MessageBoxButton button, MessageBoxImage icon)
        {
            return owner is null
                ? MessageBox.Show(messageBoxText, caption, button, icon)
                : MessageBox.Show(owner, messageBoxText, caption, button, icon);
        }
    }
}
