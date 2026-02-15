using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EduVS.Helpers;
using EduVS.Models;
using EduVS.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Windows;
using ClosedXML.Excel;

namespace EduVS.ViewModels
{
    public partial class GenerateTestResultsViewModel : BaseViewModel
    {
        [ObservableProperty] private ObservableCollection<string> selectedPdfPaths = new();
        [ObservableProperty] private string studentsFilePath = string.Empty;

        [ObservableProperty] private ObservableCollection<StudentData> students = new();
        [ObservableProperty] private StudentData? selectedStudent;

        [ObservableProperty] private ObservableCollection<TestData> tests = new();
        [ObservableProperty] private TestData? selectedTest;

        [ObservableProperty] private bool isStarted;

        private readonly PdfManager _pdf = new();
        private readonly IServiceProvider _serviceProvider;
        private string? _combinedPdfPath;
        private Dictionary<int, List<int>> _pagesByTestId = new();

        public GenerateTestResultsViewModel(ILogger<GenerateTestResultsViewModel> logger, IServiceProvider serviceProvider) : base(logger)
        {
            _serviceProvider = serviceProvider;
        }

        partial void OnStudentsFilePathChanged(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                Students = new ObservableCollection<StudentData>();
            }
        }

        [RelayCommand]
        private void BrowsePdfs()
        {
            var paths = PdfPicker.PickPdfs();
            if (paths is null || !paths.Any()) return;

            SelectedPdfPaths = new ObservableCollection<string>(paths);
        }

        [RelayCommand]
        private void BrowseStudentsFile()
        {
            var path = PdfPicker.PickStudentFile();
            if (path is null) return;
            StudentsFilePath = path;
        }

        [RelayCommand]
        private void OpenCreateNewStudentsWindow()
        {
            var window = _serviceProvider.GetRequiredService<CreateNewStudentsWindowView>();
            window.Owner = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive);
            window.ShowDialog();
        }

        [RelayCommand]
        private void Start()
        {
            if (SelectedPdfPaths is null || !SelectedPdfPaths.Any())
            {
                MessageBox.Show("You must choose input PDFs files.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (string.IsNullOrEmpty(StudentsFilePath))
            {
                MessageBox.Show("You must choose students file (CSV/Excel).", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var parsedStudents = ParseStudentsFromFile();

            if (parsedStudents.Count == 0)
            {
                MessageBox.Show("No students could be parsed from the selected file.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Students = new ObservableCollection<StudentData>(parsedStudents);

            // 1) Merge selected PDFs into one temporary file.
            var temp = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"combined_{Guid.NewGuid():N}.pdf");
            _combinedPdfPath = _pdf.MergePdfs(SelectedPdfPaths, temp);

            // 2) Scan merged PDF and discover tests/pages.
            var qrTopRight = new System.Drawing.RectangleF(0.55f, 0f, 0.45f, 0.45f);
            var nameBoxTL = new System.Drawing.RectangleF(0.10f, 0.0095f, 0.49f, 0.06f);

            var (testsFound, pagesMap) = _pdf.Scan(_combinedPdfPath, qrTopRight, nameBoxTL);
            Tests = new ObservableCollection<TestData>(testsFound);
            SelectedTest = Tests.FirstOrDefault();
            _pagesByTestId = new Dictionary<int, List<int>>(pagesMap);

            IsStarted = true;
        }

        private List<StudentData> ParseStudentsFromFile()
        {
            if (string.IsNullOrWhiteSpace(StudentsFilePath) || !File.Exists(StudentsFilePath))
            {
                return new List<StudentData>();
            }

            try
            {
                var extension = Path.GetExtension(StudentsFilePath).ToLowerInvariant();
                var rows = extension switch
                {
                    ".csv" or ".txt" => LoadRowsFromCsv(StudentsFilePath),
                    ".xlsx" or ".xls" => LoadRowsFromExcel(StudentsFilePath),
                    _ => throw new InvalidOperationException("Unsupported file format. Choose CSV or Excel file.")
                };

                var students = new List<StudentData>();
                foreach (var row in rows)
                {
                    var name = BuildNameFromRow(row.Values);
                    if (string.IsNullOrWhiteSpace(name))
                    {
                        continue;
                    }

                    students.Add(new StudentData { Name = name, TestId = null });
                }

                return students;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load students file.\nError: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return new List<StudentData>();
            }
        }

        private static string BuildNameFromRow(string[] values)
        {
            var nonEmptyValues = values
                .Select(v => v?.Trim() ?? string.Empty)
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Take(2)
                .ToArray();

            if (nonEmptyValues.Length == 0)
            {
                return string.Empty;
            }

            return string.Join(" ", nonEmptyValues);
        }

        private static List<(int RowNumber, string[] Values)> LoadRowsFromCsv(string path)
        {
            var rows = new List<(int, string[])>();
            // Existing datasets are commonly exported as windows-1250.
            var enc = Encoding.GetEncoding(1250);
            var allLines = File.ReadAllLines(path, enc);

            for (var i = 0; i < allLines.Length; i++)
            {
                var line = allLines[i];
                var split = line.Split(new[] { ';', ',', '	', '|' }, StringSplitOptions.None)
                    .Select(x => x.Trim())
                    .ToArray();
                rows.Add((i + 1, split));
            }

            return rows;
        }

        private static List<(int RowNumber, string[] Values)> LoadRowsFromExcel(string path)
        {
            var rows = new List<(int, string[])>();
            // Use first worksheet as source for import preview.
            using var workbook = new XLWorkbook(path);
            var worksheet = workbook.Worksheets.First();
            var usedRange = worksheet.RangeUsed();
            if (usedRange is null) return rows;

            foreach (var row in usedRange.Rows())
            {
                var lastUsed = row.LastCellUsed()?.Address.ColumnNumber ?? 0;
                if (lastUsed == 0)
                {
                    rows.Add((row.RowNumber(), Array.Empty<string>()));
                    continue;
                }

                var values = Enumerable.Range(1, lastUsed)
                    .Select(col => row.Cell(col).GetFormattedString().Trim())
                    .ToArray();
                rows.Add((row.RowNumber(), values));
            }

            return rows;
        }

        [RelayCommand]
        private void Abort()
        {
            SelectedTest = null;
            Tests.Clear();
            Students.Clear();
            _pagesByTestId.Clear();
            _combinedPdfPath = null;
            IsStarted = false;
        }

        [RelayCommand]
        private void Assign()
        {
            if (SelectedStudent is null || SelectedTest is null)
            {
                MessageBox.Show("Pick a student and a test.", "Assign", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            SelectedStudent.TestId = SelectedTest.TestId;
            SelectedTest.Assigned = true;
        }

        [RelayCommand]
        private void Export()
        {
            if (string.IsNullOrEmpty(_combinedPdfPath) || _pagesByTestId.Count == 0)
            {
                MessageBox.Show("Nothing to export. Run START first.", "Export", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var toExport = Students.Where(s => s.TestId.HasValue)
                                   .Select(s => (s.Name, s.TestId!.Value))
                                   .ToList();
            if (toExport.Count == 0)
            {
                MessageBox.Show("No students have a TestId assigned.", "Export", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var outputFolder = PdfPicker.PickFolder();
            if (outputFolder is null) return;

            _pdf.ExportPerStudent(_combinedPdfPath, _pagesByTestId, toExport, outputFolder);
        }
    }

}
