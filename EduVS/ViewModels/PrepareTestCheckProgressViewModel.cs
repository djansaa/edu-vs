using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EduVS.Models;
using Microsoft.Extensions.Logging;

namespace EduVS.ViewModels
{
    public partial class PrepareTestCheckProgressViewModel : BaseViewModel
    {
        private CancellationTokenSource? _cancellationTokenSource;

        [ObservableProperty] private int processedPages;
        [ObservableProperty] private int totalPages;
        [ObservableProperty] private string statusText = string.Empty;
        [ObservableProperty] private bool canClose;

        public double ProgressValue => TotalPages == 0 ? 0 : (double)ProcessedPages / TotalPages * 100.0;
        public CancellationToken CancellationToken => _cancellationTokenSource?.Token ?? CancellationToken.None;

        public PrepareTestCheckProgressViewModel(ILogger<PrepareTestCheckProgressViewModel> logger) : base(logger)
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

        public void Initialize(int totalPages)
        {
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = new CancellationTokenSource();
            TotalPages = totalPages;
            ProcessedPages = 0;
            StatusText = totalPages > 0 ? $"Processed 0 of {totalPages} pages" : "Preparing export...";
            CanClose = false;
        }

        public void Report(PrepareTestCheckProgressInfo progress)
        {
            ProcessedPages = progress.ProcessedPages;
            TotalPages = progress.TotalPages;
            StatusText = $"Processed {ProcessedPages} of {TotalPages} pages";
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
