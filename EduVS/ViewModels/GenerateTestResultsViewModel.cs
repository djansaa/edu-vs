using CommunityToolkit.Mvvm.Input;
using EduVS.Data;
using Microsoft.Extensions.Logging;
using PdfSharp.Snippets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EduVS.ViewModels
{
    internal class GenerateTestResultsViewModel : BaseViewModel
    {
        // ==== Commands ====
        public RelayCommand BrowsePdfResults { get; }
        public RelayCommand BrowseCsvStudents { get; }

        public GenerateTestResultsViewModel(ILogger<GenerateTestResultsViewModel> logger, AppDbContext db) : base(logger, db)
        {
            
        }
    }
}
