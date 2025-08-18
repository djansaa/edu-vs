using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EduVS.Helpers
{
    public class PdfPicker
    {
        public string? PickPdf()
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "PDF files (*.pdf)|*.pdf",
                DefaultExt = ".pdf",
                Multiselect = false
            };
            bool? ok = dlg.ShowDialog();
            return ok == true ? dlg.FileName : null;
        }
    }
}
