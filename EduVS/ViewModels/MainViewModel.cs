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

        public IRelayCommand ShowTestsCommand { get; }
        public IRelayCommand ShowClassesCommand { get; }
        public IRelayCommand ShowGenerateTestWindowCommand { get; }
        public IRelayCommand ShowPrepareTestCheckWindowCommand { get; }

        public MainViewModel(ILogger<MainViewModel> logger, IServiceProvider sp) : base(logger)
        {
            _sp = sp;

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
        }

        [RelayCommand]
        public void ShowGenerateTestResultsWindow()
        {
            var win = _sp.GetRequiredService<GenerateTestResultsWindowView>();
            win.Owner = Application.Current.MainWindow;
            win.ShowDialog();
        }
    }
}
