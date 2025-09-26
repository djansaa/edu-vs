using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EduVS.Data;
using Microsoft.Extensions.Logging;
using System.Windows.Controls;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace EduVS.ViewModels
{
    public partial class MainViewModel : BaseViewModel
    {
        [ObservableProperty]
        private ObservableObject? currentViewModel;

        public IRelayCommand ShowTestsCommand { get; }
        public IRelayCommand ShowClassesCommand { get; }
        public IRelayCommand ShowGenerateTestWindowCommand { get; }
        public IRelayCommand ShowPrepareTestCheckWindowCommand { get; }

        public MainViewModel(ILogger<MainViewModel> logger, AppDbContext db, TestsViewModel testsVm, ClassesViewModel classesVm) : base(logger, db)
        {
            // show tests
            ShowTestsCommand = new RelayCommand(() => CurrentViewModel = testsVm);

            // show classes
            ShowClassesCommand = new RelayCommand(() => CurrentViewModel = classesVm);

            // show generate test window
            ShowGenerateTestWindowCommand = new RelayCommand(() =>
            {
                var window = new Views.GenerateTestWindowView
                {
                    DataContext = new GenerateTestViewModel(_logger, _db)
                };
                window.ShowDialog();
            });

            // show prepare test check window
            ShowPrepareTestCheckWindowCommand = new RelayCommand(() =>
            {
                var window = new Views.PrepareTestCheckWindowView
                {
                    DataContext = new PrepareTestCheckViewModel(_logger, _db)
                };
                window.ShowDialog();
            });


            // default view
            //CurrentViewModel = testsVm;
        }
    }
}
