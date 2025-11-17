using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;

namespace EduVS.ViewModels
{
    public abstract class BaseViewModel : ObservableObject
    {
        protected readonly ILogger _logger;

        protected BaseViewModel(ILogger logger)
        {
            _logger = logger;
        }
    }
}
