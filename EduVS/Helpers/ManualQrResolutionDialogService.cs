using EduVS.Models;
using EduVS.ViewModels;
using EduVS.Views;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;

namespace EduVS.Helpers
{
    internal class ManualQrResolutionDialogService : IManualQrResolutionDialogService
    {
        private readonly IServiceProvider _serviceProvider;

        public ManualQrResolutionDialogService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public ManualQrResolutionResult? Show(ManualQrResolutionRequest request)
        {
            var window = _serviceProvider.GetRequiredService<ManualQrResolutionWindowView>();
            var viewModel = window.ViewModel;

            viewModel.Initialize(request);

            window.Owner = Application.Current.Windows
                .OfType<Window>()
                .FirstOrDefault(w => w.IsActive && w != window);

            var dialogResult = window.ShowDialog();
            return dialogResult == true ? viewModel.Result : null;
        }
    }
}
