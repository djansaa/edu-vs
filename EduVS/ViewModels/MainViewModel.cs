using CommunityToolkit.Mvvm.Input;
using EduVS.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Windows;

namespace EduVS.ViewModels
{
    public partial class MainViewModel : BaseViewModel
    {
        private readonly IServiceProvider _sp;

        public MainViewModel(ILogger<MainViewModel> logger, IServiceProvider sp) : base(logger)
        {
            _sp = sp;
        }

        [RelayCommand]
        public void ShowGenerateTestWindow()
        {
            var win = _sp.GetRequiredService<GenerateTestWindowView>();
            win.Owner = Application.Current.MainWindow;
            win.ShowDialog();
        }

        [RelayCommand]
        public void ShowPrepareTestCheckWindow()
        {
            var win = _sp.GetRequiredService<PrepareTestCheckWindowView>();
            win.Owner = Application.Current.MainWindow;
            win.ShowDialog();
        }

        [RelayCommand]
        public void ShowGenerateTestResultsWindow()
        {
            var win = _sp.GetRequiredService<GenerateTestResultsWindowView>();
            win.Owner = Application.Current.MainWindow;
            win.ShowDialog();
        }

        [RelayCommand]
        public void ShowCreateNewStudentsWindow()
        {
            var win = _sp.GetRequiredService<CreateNewStudentsWindowView>();
            win.Owner = Application.Current.MainWindow;
            win.ShowDialog();
        }
    }
}
