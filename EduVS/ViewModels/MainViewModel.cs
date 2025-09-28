using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EduVS.Data;
using EduVS.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Windows;

namespace EduVS.ViewModels
{
    public partial class MainViewModel : BaseViewModel
    {
        private readonly IServiceProvider _sp;

        [ObservableProperty]
        private ObservableObject? currentViewModel;

        public IRelayCommand ShowTestsCommand { get; }
        public IRelayCommand ShowClassesCommand { get; }
        public IRelayCommand ShowGenerateTestWindowCommand { get; }
        public IRelayCommand ShowPrepareTestCheckWindowCommand { get; }

        public MainViewModel(ILogger<MainViewModel> logger, AppDbContext db, IServiceProvider sp) : base(logger, db)
        {
            _sp = sp;

            // show tests
            ShowTestsCommand = new RelayCommand(() => CurrentViewModel = _sp.GetRequiredService<TestsViewModel>());

            // show classes
            ShowClassesCommand = new RelayCommand(() => CurrentViewModel = _sp.GetRequiredService<ClassesViewModel>());

            // show generate test window
            ShowGenerateTestWindowCommand = new RelayCommand(() =>
            {
                var win = _sp.GetRequiredService<GenerateTestWindowView>();
                win.Owner = Application.Current.MainWindow;
                win.ShowDialog();
            });

            // show prepare test check window
            ShowPrepareTestCheckWindowCommand = new RelayCommand(() =>
            {
                var win = _sp.GetRequiredService<PrepareTestCheckWindowView>();
                win.Owner = Application.Current.MainWindow;
                win.ShowDialog();
            });


            // default view
            //CurrentViewModel = testsVm;
        }
    }
}
