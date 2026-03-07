using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EduVS.Models;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media.Imaging;

namespace EduVS.ViewModels
{
    public partial class ManualQrResolutionViewModel : BaseViewModel
    {
        [ObservableProperty] private BitmapSource originalPreview = null!;
        [ObservableProperty] private BitmapSource rotatedPreview = null!;
        [ObservableProperty] private bool isOriginalSelected = true;
        [ObservableProperty] private bool isRotatedSelected;
        [ObservableProperty] private int? testId = 0;
        [ObservableProperty] private char selectedGroupId = 'A';
        [ObservableProperty] private int? pageNumber = 1;
        [ObservableProperty] private string dialogTitle = "Resolve unreadable QR";
        [ObservableProperty] private string pageLabel = string.Empty;
        [ObservableProperty] private string? testSubject;
        [ObservableProperty] private string? testName;

        public ObservableCollection<char> AvailableGroups { get; } = new(['A', 'B']);

        public ManualQrResolutionResult? Result { get; private set; }

        public ManualQrResolutionViewModel(ILogger<ManualQrResolutionViewModel> logger) : base(logger)
        {
        }

        public void Initialize(ManualQrResolutionRequest request)
        {
            OriginalPreview = request.OriginalPreview;
            RotatedPreview = request.RotatedPreview;
            TestId = request.SuggestedTestId ?? 0;
            SelectedGroupId = request.SuggestedGroupId is 'A' or 'B' ? request.SuggestedGroupId.Value : 'A';
            PageNumber = request.SuggestedPageNumber ?? 1;
            TestSubject = request.TestSubject;
            TestName = request.TestName;
            PageLabel = $"PDF page {request.SourcePageNumber}";
            IsOriginalSelected = true;
            IsRotatedSelected = false;
            Result = null;
        }

        [RelayCommand]
        private void SelectOriginal()
        {
            IsOriginalSelected = true;
            IsRotatedSelected = false;
        }

        [RelayCommand]
        private void SelectRotated()
        {
            IsOriginalSelected = false;
            IsRotatedSelected = true;
        }

        [RelayCommand]
        private void Confirm(Window? window)
        {
            if (TestId is null || TestId < 0)
            {
                MessageBox.Show("TestId must be 0 or greater.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (PageNumber is null || PageNumber <= 0)
            {
                MessageBox.Show("Page must be a positive number.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (SelectedGroupId is not ('A' or 'B'))
            {
                MessageBox.Show("Group must be A or B.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!IsOriginalSelected && !IsRotatedSelected)
            {
                MessageBox.Show("Select one preview image.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Result = new ManualQrResolutionResult
            {
                Rotation = IsRotatedSelected ? 180 : 0,
                TestId = TestId.Value,
                GroupId = SelectedGroupId,
                Page = PageNumber.Value
            };

            if (window is not null)
            {
                window.DialogResult = true;
                window.Close();
            }
        }

        [RelayCommand]
        private void Cancel(Window? window)
        {
            Result = null;

            if (window is not null)
            {
                window.DialogResult = false;
                window.Close();
            }
        }

        [RelayCommand]
        private void IncreaseTestId()
        {
            TestId = Math.Max(0, TestId ?? 0) + 1;
        }

        [RelayCommand]
        private void DecreaseTestId()
        {
            TestId = Math.Max(0, (TestId ?? 0) - 1);
        }

        [RelayCommand]
        private void IncreasePageNumber()
        {
            PageNumber = Math.Max(1, PageNumber ?? 1) + 1;
        }

        [RelayCommand]
        private void DecreasePageNumber()
        {
            PageNumber = Math.Max(1, (PageNumber ?? 1) - 1);
        }
    }
}
