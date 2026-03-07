using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EduVS.Models;
using Microsoft.Extensions.Logging;
using System.Windows;

namespace EduVS.ViewModels
{
    public partial class GenerateTestProgressViewModel : BaseViewModel
    {
        private CancellationTokenSource? _cancellationTokenSource;

        [ObservableProperty] private int completedTests;
        [ObservableProperty] private int totalTests;
        [ObservableProperty] private string statusText = string.Empty;
        [ObservableProperty] private bool canClose;

        public double ProgressValue => TotalTests == 0 ? 0 : (double)CompletedTests / TotalTests * 100.0;
        public CancellationToken CancellationToken => _cancellationTokenSource?.Token ?? CancellationToken.None;

        public GenerateTestProgressViewModel(ILogger<GenerateTestProgressViewModel> logger) : base(logger)
        {
        }

        partial void OnCompletedTestsChanged(int value)
        {
            OnPropertyChanged(nameof(ProgressValue));
        }

        partial void OnTotalTestsChanged(int value)
        {
            OnPropertyChanged(nameof(ProgressValue));
        }

        public void Initialize(int totalTests)
        {
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = new CancellationTokenSource();
            TotalTests = totalTests;
            CompletedTests = 0;
            StatusText = totalTests > 0 ? $"Generated 0 of {totalTests} tests" : "Preparing export...";
            CanClose = false;
        }

        public void Report(GenerateTestProgressInfo progress)
        {
            CompletedTests = progress.CompletedTests;
            TotalTests = progress.TotalTests;
            StatusText = $"Generated {CompletedTests} of {TotalTests} tests";
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
