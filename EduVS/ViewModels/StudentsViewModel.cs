using EduVS.Data;
using Microsoft.Extensions.Logging;

namespace EduVS.ViewModels
{
    public class StudentsViewModel : BaseViewModel
    {
        public StudentsViewModel(ILogger<StudentsViewModel> logger, AppDbContext db) : base(logger, db) { }
    }
}