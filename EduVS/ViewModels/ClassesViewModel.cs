using EduVS.Data;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EduVS.ViewModels
{
    public class ClassesViewModel : BaseViewModel
    {
        public ClassesViewModel(ILogger<TestsViewModel> logger, AppDbContext db) : base(logger, db)
        {

        }
    }
}
