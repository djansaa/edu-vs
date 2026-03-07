using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EduVS.Models;
using Microsoft.Extensions.Logging;

namespace EduVS.ViewModels
{
    public partial class GenerateTestResultsStartProgressViewModel : BaseViewModel
    {
        private CancellationTokenSource? _cancellationTokenSource;

        [ObservableProperty] private int processedPages;
        [ObservableProperty] private int totalPages;
        [ObservableProperty] private string statusText = string.Empty;
        [ObservableProperty] private bool canClose;

        public double ProgressValue => TotalPages == 0 ? 0 : (double)ProcessedPages / TotalPages * 100.0;
        public CancellationToken CancellationToken => _cancellationTokenSource?.Token ?? CancellationToken.None;

        public GenerateTestResultsStartProgressViewModel(ILogger<GenerateTestResultsStartProgressViewModel> logger) : base(logger)
        {
        }

        partial void OnProcessedPagesChanged(int value)
        {
            OnPropertyChanged(nameof(ProgressValue));
        }

        partial void OnTotalPagesChanged(int value)
        {
            OnPropertyChanged(nameof(ProgressValue));
        }

        public void Initialize()
        {
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = new CancellationTokenSource();
            ProcessedPages = 0;
            TotalPages = 0;
            StatusText = "Merging PDFs...";
            CanClose = false;
        }

        public void Report(GenerateTestResultsStartProgressInfo progress)
        {
            ProcessedPages = progress.ProcessedPages;
            TotalPages = progress.TotalPages;
            StatusText = progress.StatusText;
        }

        public void Finish(string statusText)
        {
            StatusText = statusText;
            CanClose = true;
        }

        [RelayCommand]
        private void Cancel()
        {
            StatusText = "Canceling...";
            _cancellationTokenSource?.Cancel();
        }
    }
}
