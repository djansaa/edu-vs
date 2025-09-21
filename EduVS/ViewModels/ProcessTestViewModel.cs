using EduVS.Data;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EduVS.ViewModels
{
    internal class ProcessTestViewModel : BaseViewModel
    {
        public ProcessTestViewModel(ILogger logger, AppDbContext db) : base(logger, db)
        {
        }
    }
}
