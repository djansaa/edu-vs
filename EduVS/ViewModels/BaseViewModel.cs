using CommunityToolkit.Mvvm.ComponentModel;
using EduVS.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EduVS.ViewModels
{
    public abstract class BaseViewModel : ObservableObject
    {
        protected readonly ILogger _logger;
        protected readonly AppDbContext _db;

        protected BaseViewModel(ILogger logger, AppDbContext db)
        {
            _logger = logger;
            _db = db;
        }
    }
}
