using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EduVS.Data;
using EduVS.Models;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System.Windows;

namespace EduVS.ViewModels
{
    public partial class ClassesViewModel : BaseViewModel
    {
        public ObservableCollection<Class> Classes { get; } = new();

        [ObservableProperty]
        private Class? selectedClass;
        [ObservableProperty]
        private string? tempClassName;
        [ObservableProperty]
        private int? tempClassYear;

        public IAsyncRelayCommand LoadClassesCommand { get; }
        public IRelayCommand CancelSelectedClassCommand { get; }
        public IRelayCommand UpdateSelectedClassCommand { get; }
        public IRelayCommand DeleteSelectedClassCommand { get; }
        public IRelayCommand AddNewClassCommand { get; }
        public IRelayCommand ClearNewClassCommand { get; }

        public ClassesViewModel(ILogger<ClassesViewModel> logger, AppDbContext db) : base(logger, db)
        {
            LoadClassesCommand = new AsyncRelayCommand(LoadClassesAsync);
            CancelSelectedClassCommand = new RelayCommand(CancelSelectedClass);
            UpdateSelectedClassCommand = new AsyncRelayCommand(UpdateSelectedClassAsync);
            DeleteSelectedClassCommand = new AsyncRelayCommand(DeleteSelectedClass);

            AddNewClassCommand = new AsyncRelayCommand(AddNewClassAsync);
            ClearNewClassCommand = new RelayCommand(ClearNewClass);
        }

        private void ClearNewClass()
        {
            TempClassName = null;
            TempClassYear = (int?)null;
        }

        private async Task AddNewClassAsync()
        {
            if (string.IsNullOrWhiteSpace(TempClassName) ||
                TempClassYear == null)
            {
                MessageBox.Show("Please fill in all required fields.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
        }

        private async Task DeleteSelectedClass()
        {
            throw new NotImplementedException();
        }

        partial void OnSelectedClassChanged(Class? value)
        {
            if (value is null)
            {
                TempClassName = null;
                TempClassYear = (int?)null;
            }
            else
            {
                TempClassName = value.Name;
                TempClassYear = value.Year;
            }
        }

        private async Task UpdateSelectedClassAsync()
        {
            throw new NotImplementedException();
        }

        private void CancelSelectedClass()
        {
            SelectedClass = null;
            TempClassName = null;
            TempClassYear = (int?)null;
        }

        private async Task LoadClassesAsync()
        {
            throw new NotImplementedException();
        }
    }
}
