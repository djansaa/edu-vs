using Microsoft.Win32;
using System.IO;

namespace EduVS.Helpers
{
    public class PdfPicker
    {
        public static string? PickPdf()
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

        public static string? PickFolder()
        {
            var dlg = new OpenFolderDialog
            {
                Title = "Select Folder",
                InitialDirectory = AppContext.BaseDirectory
            };
            return dlg.ShowDialog() == true ? dlg.FolderName : null;
        }

        public static string? PickPdfSavePath(string suggestedName)
        {
            var dlg = new SaveFileDialog
            {
                Title = "Export PDF",
                Filter = "PDF (*.pdf)|*.pdf",
                DefaultExt = ".pdf",
                AddExtension = true,
                OverwritePrompt = true,
                FileName = suggestedName
            };
            return dlg.ShowDialog() == true ? Path.ChangeExtension(dlg.FileName, ".pdf") : null;
        }
    }
}
