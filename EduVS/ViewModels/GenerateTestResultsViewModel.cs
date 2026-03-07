using ClosedXML.Excel;
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

        [ObservableProperty] private string? detectedTestSubject;
        [ObservableProperty] private string? detectedTestName;

        [ObservableProperty] private bool isStarted;

        private readonly PdfManager _pdf;
        private readonly IServiceProvider _serviceProvider;
        private string? _combinedPdfPath;
        private Dictionary<int, List<int>> _pagesByTestId = new();

        public GenerateTestResultsViewModel(ILogger<GenerateTestResultsViewModel> logger, IServiceProvider serviceProvider, PdfManager pdfManager) : base(logger)
        {
            _serviceProvider = serviceProvider;
            _pdf = pdfManager;
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

            try
            {
                var temp = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"combined_{Guid.NewGuid():N}.pdf");
                var combinedPdfPath = _pdf.MergePdfs(SelectedPdfPaths, temp);
                var info = _pdf.ReadTestCheckPdfInfo(combinedPdfPath);

                var qrTopRight = new System.Drawing.RectangleF(0.55f, 0f, 0.45f, 0.45f);
                var nameBoxTL = new System.Drawing.RectangleF(0.175f, 0.0095f, 0.49f, 0.06f);

                var (testsFound, pagesMap) = _pdf.Scan(combinedPdfPath, qrTopRight, nameBoxTL);

                Students = new ObservableCollection<StudentData>(parsedStudents);
                Tests = new ObservableCollection<TestData>(testsFound);
                SelectedTest = Tests.FirstOrDefault();
                DetectedTestSubject = info.MetadataQr?.TestSubject;
                DetectedTestName = info.MetadataQr?.TestName;
                _pagesByTestId = new Dictionary<int, List<int>>(pagesMap);
                _combinedPdfPath = combinedPdfPath;
                IsStarted = true;
            }
            catch (OperationCanceledException)
            {
                Abort();
                MessageBox.Show("Loading was canceled during manual QR resolution.", "Loading Canceled", MessageBoxButton.OK, MessageBoxImage.Information);
            }
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
                    if (TryBuildStudent(row.Values, out var student))
                    {
                        students.Add(student);
                    }
                }

                return students;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load students file.\nError: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return new List<StudentData>();
            }
        }

        private static bool TryBuildStudent(string[] values, out StudentData student)
        {
            student = new StudentData();

            var nonEmptyValues = values
                .Select(v => NormalizeCsvCell(v ?? string.Empty))
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .ToArray();

            if (nonEmptyValues.Length == 0 || IsHeaderRow(nonEmptyValues))
            {
                return false;
            }

            if (TryParseStructuredTwoColumnRow(nonEmptyValues, out student))
            {
                return true;
            }

            if (TryParseSeparatedRow(nonEmptyValues, out student))
            {
                return true;
            }

            student = BuildStudentFromCombinedValue(nonEmptyValues[0]);
            return !string.IsNullOrWhiteSpace(student.Name) || !string.IsNullOrWhiteSpace(student.Surname);
        }

        private static bool IsHeaderRow(string[] values)
        {
            if (values.Length < 2)
            {
                return false;
            }

            var first = NormalizeHeaderValue(values[0]);
            var second = NormalizeHeaderValue(values[1]);

            return (first, second) is ("surname", "name")
                or ("prijmeni", "jmeno")
                or ("last name", "first name");
        }

        private static bool TryParseStructuredTwoColumnRow(string[] values, out StudentData student)
        {
            student = new StudentData();
            if (values.Length < 2)
            {
                return false;
            }

            var first = values[0];
            var second = values[1];
            if (LooksLikeOrdinal(first))
            {
                return false;
            }

            student = new StudentData
            {
                Surname = first,
                Name = second,
                TestId = null
            };

            return !string.IsNullOrWhiteSpace(student.Surname) || !string.IsNullOrWhiteSpace(student.Name);
        }

        private static bool TryParseSeparatedRow(string[] values, out StudentData student)
        {
            student = new StudentData();

            var candidateValues = values;
            if (LooksLikeOrdinal(values[0]))
            {
                candidateValues = values.Skip(1).ToArray();
            }

            if (candidateValues.Length < 2)
            {
                return false;
            }

            student = new StudentData
            {
                Surname = candidateValues[0],
                Name = candidateValues[1],
                TestId = null
            };

            return !string.IsNullOrWhiteSpace(student.Surname) || !string.IsNullOrWhiteSpace(student.Name);
        }

        private static StudentData BuildStudentFromCombinedValue(string value)
        {
            var parts = value
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (parts.Length == 0)
            {
                return new StudentData();
            }

            if (parts.Length == 1)
            {
                return new StudentData { Surname = parts[0], Name = string.Empty, TestId = null };
            }

            return new StudentData
            {
                Surname = parts[0],
                Name = string.Join(" ", parts.Skip(1)),
                TestId = null
            };
        }

        private static bool LooksLikeOrdinal(string value) => int.TryParse(value, out _);

        private static string NormalizeHeaderValue(string value)
        {
            return value.Trim().Trim('"').ToLowerInvariant();
        }

        private static List<(int RowNumber, string[] Values)> LoadRowsFromCsv(string path)
        {
            var rows = new List<(int, string[])>();
            using var reader = new StreamReader(path, Encoding.GetEncoding(1250), detectEncodingFromByteOrderMarks: true);
            var rowNumber = 0;

            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine() ?? string.Empty;
                rowNumber++;
                var split = line.Split(new[] { ';', ',', '\t', '|' }, StringSplitOptions.None)
                    .Select(NormalizeCsvCell)
                    .ToArray();
                rows.Add((rowNumber, split));
            }

            return rows;
        }

        private static string NormalizeCsvCell(string value)
        {
            var normalized = value.Trim();
            if (normalized.Length >= 2 && normalized.StartsWith('"') && normalized.EndsWith('"'))
            {
                normalized = normalized[1..^1].Replace("\"\"", "\"");
            }

            return normalized.Trim();
        }

        private static List<(int RowNumber, string[] Values)> LoadRowsFromExcel(string path)
        {
            var rows = new List<(int, string[])>();
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
            DetectedTestSubject = null;
            DetectedTestName = null;
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

            var assignedExports = Students.Where(s => s.TestId.HasValue)
                                          .Select(s => (s.DisplayName, s.TestId!.Value))
                                          .ToList();
            var assignedTestIds = assignedExports.Select(x => x.Item2).ToHashSet();
            var unassignedExports = _pagesByTestId.Keys
                                                  .Where(testId => !assignedTestIds.Contains(testId))
                                                  .OrderBy(testId => testId)
                                                  .Select(testId => ("#", testId));
            var toExport = assignedExports.Concat(unassignedExports).ToList();

            if (toExport.Count == 0)
            {
                MessageBox.Show("No tests are available to export.", "Export", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var outlierExports = GetOutlierExportPageCounts(toExport);
            if (outlierExports.Count > 0)
            {
                MessageBox.Show(
                    "Some exported PDFs have an outlier page count.\n\n" +
                    string.Join("\n", outlierExports.Select(item => $"\"{item.FileName}\" -> {item.PageCount} pages")),
                    "Outlier PDF Warning",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }

            var outputFolder = PdfPicker.PickFolder();
            if (outputFolder is null) return;

            _pdf.ExportPerStudent(_combinedPdfPath, _pagesByTestId, toExport, outputFolder, DetectedTestName);
        }

        private List<(string FileName, int PageCount)> GetOutlierExportPageCounts(IEnumerable<(string name, int testId)> exports)
        {
            var exportedTests = exports
                .Select(item => new
                {
                    item.name,
                    item.testId,
                    PageCount = _pagesByTestId.TryGetValue(item.testId, out var pages) ? pages.Count : 0
                })
                .Where(item => item.PageCount > 0)
                .ToList();

            if (exportedTests.Count == 0)
            {
                return new List<(string FileName, int PageCount)>();
            }

            var expectedPageCount = exportedTests
                .GroupBy(item => item.PageCount)
                .OrderByDescending(group => group.Count())
                .ThenBy(group => group.Key)
                .Select(group => group.Key)
                .First();

            return exportedTests
                .Where(item => item.PageCount != expectedPageCount)
                .Select(item => (FileName: BuildExportFileName(item.name, item.testId), item.PageCount))
                .Distinct()
                .OrderBy(item => item.FileName)
                .ToList();
        }

        private string BuildExportFileName(string name, int testId)
        {
            var exportLabel = string.IsNullOrWhiteSpace(DetectedTestName) ? "Test" : SanitizeFileNamePart(DetectedTestName);
            return $"{SanitizeFileNamePart(name)}_{exportLabel}_{testId}.pdf";
        }

        private static string SanitizeFileNamePart(string value)
        {
            var sanitized = value;

            foreach (var invalidChar in Path.GetInvalidFileNameChars())
            {
                sanitized = sanitized.Replace(invalidChar, '_');
            }

            return sanitized.Trim();
        }
    }
}
