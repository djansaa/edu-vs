using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EduVS.Data;
using EduVS.Helpers;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EduVS.ViewModels
{
    internal partial class PrepareTestCheckViewModel : BaseViewModel
    {
        [ObservableProperty] private string? pdfPath;

        // Output mode
        [ObservableProperty] private bool isSplitByGroup = true;
        [ObservableProperty] private bool isMergedSingle = false;

        // Sorting mode
        [ObservableProperty] private bool sortByPageNumber = true;
        [ObservableProperty] private bool sortByTestNumber = false;

        // ==== Commands ====
        public RelayCommand BrowsePdfCommand { get; }
        public RelayCommand ExportCommand { get; }

        public PrepareTestCheckViewModel(ILogger logger, AppDbContext db) : base(logger, db)
        {
            BrowsePdfCommand = new RelayCommand(BrowsePdf);
            ExportCommand = new RelayCommand(ExportTestCheck);
        }

        private void BrowsePdf()
        {
            var path = PdfPicker.PickPdf();
            if (path is null) return;

            PdfPath = path;
        }

        private void ExportTestCheck()
        {
            throw new NotImplementedException();
        }
    }
}
