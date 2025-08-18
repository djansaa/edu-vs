using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EduVS.Data;
using Microsoft.Extensions.Logging;

namespace EduVS.ViewModels
{
    public partial class MainViewModel : BaseViewModel
    {
        [ObservableProperty]
        private ObservableObject? currentViewModel;

        public IRelayCommand ShowTestsCommand { get; }
        public IRelayCommand ShowStudentsCommand { get; }

        public MainViewModel(ILogger<MainViewModel> logger, AppDbContext db, TestsViewModel testsVm, StudentsViewModel studentsVm) : base(logger, db)
        {
            ShowTestsCommand = new RelayCommand(() => CurrentViewModel = testsVm);
            ShowStudentsCommand = new RelayCommand(() => CurrentViewModel = studentsVm);

            CurrentViewModel = testsVm;
        }
    }
}
