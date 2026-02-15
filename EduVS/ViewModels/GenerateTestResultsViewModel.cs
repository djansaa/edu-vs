using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EduVS.Helpers;
using EduVS.Models;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System.Data;
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

        [ObservableProperty] private ObservableCollection<string> separatorOptions = new() { ";", ",", "\t", "|" };
        [ObservableProperty] private string selectedSeparator = ";";
        [ObservableProperty] private int startRow = 1;

        [ObservableProperty] private ObservableCollection<ColumnOption> availableColumns = new();
        [ObservableProperty] private ColumnOption? selectedNameColumn;
        [ObservableProperty] private ColumnOption? selectedSurnameColumn;

        [ObservableProperty] private DataView? previewRows;
        [ObservableProperty] private ObservableCollection<StudentData> parsedStudentsPreview = new();

        private readonly PdfManager _pdf = new();
        private string? _combinedPdfPath;
        private Dictionary<int, List<int>> _pagesByTestId = new();
        private List<string[]> _filteredRows = new();

        public GenerateTestResultsViewModel(ILogger<GenerateTestResultsViewModel> logger) : base(logger) { }

        // Keep previews in sync with import settings changes.
        partial void OnStudentsFilePathChanged(string value) => RefreshStudentsPreview();
        partial void OnSelectedSeparatorChanged(string value) => RefreshStudentsPreview();
        partial void OnStartRowChanged(int value) => RefreshStudentsPreview();
        partial void OnSelectedNameColumnChanged(ColumnOption? value) => BuildParsedStudentsPreview();
        partial void OnSelectedSurnameColumnChanged(ColumnOption? value) => BuildParsedStudentsPreview();

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

            // Use parsed preview as the source of truth for imported students.
            var parsedStudents = ParsedStudentsPreview
                .Where(s => !string.IsNullOrWhiteSpace(s.Name))
                .Select(s => new StudentData { Name = s.Name, TestId = null })
                .ToList();

            if (parsedStudents.Count == 0)
            {
                MessageBox.Show("No students could be parsed from the selected file and settings.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
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

        private void RefreshStudentsPreview()
        {
            PreviewRows = null;
            ParsedStudentsPreview = new ObservableCollection<StudentData>();
            AvailableColumns = new ObservableCollection<ColumnOption>();
            _filteredRows.Clear();

            if (string.IsNullOrWhiteSpace(StudentsFilePath) || !File.Exists(StudentsFilePath))
            {
                return;
            }

            try
            {
                // Load rows from CSV/TXT or Excel depending on extension.
                var extension = Path.GetExtension(StudentsFilePath).ToLowerInvariant();
                var allRows = extension switch
                {
                    ".csv" or ".txt" => LoadRowsFromCsv(StudentsFilePath, SelectedSeparator),
                    ".xlsx" or ".xls" => LoadRowsFromExcel(StudentsFilePath),
                    _ => throw new InvalidOperationException("Unsupported file format. Choose CSV or Excel file.")
                };

                // Apply row filter and ignore fully empty rows.
                _filteredRows = allRows
                    .Where(x => x.RowNumber >= StartRow)
                    .Select(x => x.Values)
                    .Where(values => values.Any(v => !string.IsNullOrWhiteSpace(v)))
                    .ToList();

                var maxColumns = _filteredRows.Count == 0 ? 0 : _filteredRows.Max(r => r.Length);
                var columns = Enumerable.Range(0, maxColumns)
                    .Select(i => new ColumnOption(i, $"Column {i + 1}"));
                AvailableColumns = new ObservableCollection<ColumnOption>(columns);

                if (AvailableColumns.Count > 0)
                {
                    SelectedNameColumn ??= AvailableColumns[0];
                    if (SelectedNameColumn.Index >= AvailableColumns.Count)
                    {
                        SelectedNameColumn = AvailableColumns[0];
                    }

                    if (SelectedSurnameColumn is not null && SelectedSurnameColumn.Index >= AvailableColumns.Count)
                    {
                        SelectedSurnameColumn = null;
                    }
                }
                else
                {
                    SelectedNameColumn = null;
                    SelectedSurnameColumn = null;
                }

                // Build tabular preview for UI DataGrid.
                var previewTable = new DataTable();
                for (var i = 0; i < maxColumns; i++)
                {
                    previewTable.Columns.Add($"Column {i + 1}", typeof(string));
                }

                foreach (var row in _filteredRows)
                {
                    var dr = previewTable.NewRow();
                    for (var i = 0; i < maxColumns; i++)
                    {
                        dr[i] = i < row.Length ? row[i] : string.Empty;
                    }
                    previewTable.Rows.Add(dr);
                }
                PreviewRows = previewTable.DefaultView;

                BuildParsedStudentsPreview();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load students file.\nError: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BuildParsedStudentsPreview()
        {
            if (SelectedNameColumn is null)
            {
                ParsedStudentsPreview = new ObservableCollection<StudentData>();
                return;
            }

            // Build parsed students preview from chosen name/surname columns.
            var result = new List<StudentData>();
            foreach (var row in _filteredRows)
            {
                var name = CsvHelper.GetValueAt(row, SelectedNameColumn.Index);
                var surname = SelectedSurnameColumn is null ? string.Empty : CsvHelper.GetValueAt(row, SelectedSurnameColumn.Index);

                var fullName = string.Join(" ", new[] { name, surname }.Where(x => !string.IsNullOrWhiteSpace(x))).Trim();
                if (string.IsNullOrWhiteSpace(fullName)) continue;

                result.Add(new StudentData { Name = fullName, TestId = null });
            }

            ParsedStudentsPreview = new ObservableCollection<StudentData>(result);
        }

        private static List<(int RowNumber, string[] Values)> LoadRowsFromCsv(string path, string separator)
        {
            var rows = new List<(int, string[])>();
            // Existing datasets are commonly exported as windows-1250.
            var enc = Encoding.GetEncoding(1250);
            var allLines = File.ReadAllLines(path, enc);
            var delimiter = CsvHelper.ResolveDelimiter(separator);

            for (var i = 0; i < allLines.Length; i++)
            {
                var line = allLines[i];
                rows.Add((i + 1, line.Split(delimiter).Select(x => x.Trim()).ToArray()));
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

    public class ColumnOption
    {
        public int Index { get; }
        public string Name { get; }

        public ColumnOption(int index, string name)
        {
            Index = index;
            Name = name;
        }

        public override string ToString() => Name;
    }
}
