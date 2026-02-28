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

namespace EduVS.ViewModels
{
    public partial class CreateNewStudentsViewModel : BaseViewModel
    {
        [ObservableProperty] private string sourceFilePath = string.Empty;
        [ObservableProperty] private string selectedSeparator = ";";
        [ObservableProperty] private int startRow = 1;
        [ObservableProperty] private bool useCombinedNameColumn = true;
        [ObservableProperty] private bool swapSurnameAndNameColumns;

        [ObservableProperty] private ObservableCollection<CsvColumnOption> availableColumns = new();
        [ObservableProperty] private CsvColumnOption? selectedCombinedNameColumn;
        [ObservableProperty] private CsvColumnOption? selectedNameColumn;
        [ObservableProperty] private CsvColumnOption? selectedSurnameColumn;

        [ObservableProperty] private DataView? previewRows;
        [ObservableProperty] private ObservableCollection<StudentData> parsedStudents = new();
        [ObservableProperty] private string className = string.Empty;

        private readonly List<string[]> _filteredRows = new();

        public CreateNewStudentsViewModel(ILogger<CreateNewStudentsViewModel> logger) : base(logger)
        {
        }

        partial void OnSourceFilePathChanged(string value) => RefreshPreview();
        partial void OnSelectedSeparatorChanged(string value) => RefreshPreview();
        partial void OnStartRowChanged(int value) => RefreshPreview();
        partial void OnUseCombinedNameColumnChanged(bool value) => BuildParsedStudentsPreview();
        partial void OnSwapSurnameAndNameColumnsChanged(bool value) => BuildParsedStudentsPreview();
        partial void OnSelectedCombinedNameColumnChanged(CsvColumnOption? value) => BuildParsedStudentsPreview();
        partial void OnSelectedNameColumnChanged(CsvColumnOption? value) => BuildParsedStudentsPreview();
        partial void OnSelectedSurnameColumnChanged(CsvColumnOption? value) => BuildParsedStudentsPreview();

        [RelayCommand]
        private void BrowseSourceFile()
        {
            var path = PdfPicker.PickStudentFile();
            if (path is null) return;

            SourceFilePath = path;
        }

        [RelayCommand]
        private void IncreaseStartRow()
        {
            StartRow++;
        }

        [RelayCommand]
        private void DecreaseStartRow()
        {
            if (StartRow > 1)
            {
                StartRow--;
            }
        }

        [RelayCommand]
        private void Cancel(Window? window)
        {
            window?.Close();
        }

        [RelayCommand]
        private void Confirm(Window? window)
        {
            if (string.IsNullOrWhiteSpace(ClassName))
            {
                MessageBox.Show("Class name is required.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (ParsedStudents.Count == 0)
            {
                MessageBox.Show("No students to save. Check selected columns and start row.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var suggestedName = SanitizeFileName(ClassName.Trim());
            if (string.IsNullOrWhiteSpace(suggestedName))
            {
                MessageBox.Show("Class name contains only invalid file name characters.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var outputPath = PdfPicker.PickCsvSavePath($"{suggestedName}.csv");
            if (outputPath is null) return;

            var lines = new List<string> { "\"Surname\";\"Name\"" };
            lines.AddRange(ParsedStudents.Select(student =>
                $"{EscapeCsvValue(student.Surname)};{EscapeCsvValue(student.Name)}"));

            File.WriteAllLines(outputPath, lines, Encoding.UTF8);

            MessageBox.Show("Students CSV file has been created.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            window?.Close();
        }

        private void RefreshPreview()
        {
            PreviewRows = null;
            ParsedStudents = new ObservableCollection<StudentData>();
            AvailableColumns = new ObservableCollection<CsvColumnOption>();
            _filteredRows.Clear();

            if (string.IsNullOrWhiteSpace(SourceFilePath) || !File.Exists(SourceFilePath))
            {
                return;
            }

            try
            {
                var rows = LoadRowsFromCsv(SourceFilePath, SelectedSeparator)
                    .Where(r => r.RowNumber >= Math.Max(1, StartRow))
                    .Select(r => r.Values)
                    .ToList();

                _filteredRows.Clear();
                _filteredRows.AddRange(rows);

                var maxColumns = _filteredRows.Any() ? _filteredRows.Max(r => r.Length) : 0;
                AvailableColumns = new ObservableCollection<CsvColumnOption>(
                    Enumerable.Range(0, maxColumns)
                        .Select(i => new CsvColumnOption(i, $"Column {i + 1}")));

                if (SelectedCombinedNameColumn is null && AvailableColumns.Any())
                {
                    SelectedCombinedNameColumn = AvailableColumns.FirstOrDefault();
                }

                if (SelectedNameColumn is null && AvailableColumns.Any())
                {
                    SelectedNameColumn = AvailableColumns.FirstOrDefault();
                }

                if (SelectedSurnameColumn is null && AvailableColumns.Count > 1)
                {
                    SelectedSurnameColumn = AvailableColumns[1];
                }

                var table = new DataTable();
                for (var i = 0; i < maxColumns; i++)
                {
                    table.Columns.Add($"Column {i + 1}", typeof(string));
                }

                foreach (var row in _filteredRows)
                {
                    var dr = table.NewRow();
                    for (var i = 0; i < maxColumns; i++)
                    {
                        dr[i] = i < row.Length ? row[i] : string.Empty;
                    }
                    table.Rows.Add(dr);
                }

                PreviewRows = table.DefaultView;
                BuildParsedStudentsPreview();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load CSV file.\nError: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BuildParsedStudentsPreview()
        {
            if (!_filteredRows.Any())
            {
                ParsedStudents = new ObservableCollection<StudentData>();
                return;
            }

            var students = new List<StudentData>();
            foreach (var row in _filteredRows)
            {
                if (TryBuildStudent(row, out var student))
                {
                    students.Add(student);
                }
            }

            ParsedStudents = new ObservableCollection<StudentData>(students);
        }

        private bool TryBuildStudent(string[] row, out StudentData student)
        {
            student = new StudentData();

            if (UseCombinedNameColumn)
            {
                if (SelectedCombinedNameColumn is null)
                {
                    return false;
                }

                var fullName = CsvHelper.GetValueAt(row, SelectedCombinedNameColumn.Index);
                var (combinedSurname, combinedName) = SplitCombinedName(fullName);
                if (string.IsNullOrWhiteSpace(combinedSurname) && string.IsNullOrWhiteSpace(combinedName))
                {
                    return false;
                }

                student = new StudentData
                {
                    Surname = combinedSurname,
                    Name = combinedName,
                    TestId = null
                };
                return true;
            }

            if (SelectedNameColumn is null && SelectedSurnameColumn is null)
            {
                return false;
            }

            var name = SelectedNameColumn is null ? string.Empty : CsvHelper.GetValueAt(row, SelectedNameColumn.Index);
            var surname = SelectedSurnameColumn is null ? string.Empty : CsvHelper.GetValueAt(row, SelectedSurnameColumn.Index);
            if (string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(surname))
            {
                return false;
            }

            student = new StudentData
            {
                Name = name,
                Surname = surname,
                TestId = null
            };
            return true;
        }

        private (string Surname, string Name) SplitCombinedName(string fullName)
        {
            var parts = fullName
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (parts.Length == 0)
            {
                return (string.Empty, string.Empty);
            }

            if (parts.Length == 1)
            {
                return SwapSurnameAndNameColumns
                    ? (string.Empty, parts[0])
                    : (parts[0], string.Empty);
            }

            var firstValue = parts[0];
            var remainingValue = string.Join(" ", parts.Skip(1));

            return SwapSurnameAndNameColumns
                ? (remainingValue, firstValue)
                : (firstValue, remainingValue);
        }

        private static List<(int RowNumber, string[] Values)> LoadRowsFromCsv(string path, string separator)
        {
            var rows = new List<(int RowNumber, string[] Values)>();
            var delimiter = CsvHelper.ResolveDelimiter(separator);

            using var reader = new StreamReader(path, Encoding.GetEncoding(1250), detectEncodingFromByteOrderMarks: true);
            var rowNumber = 0;
            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine() ?? string.Empty;
                rowNumber++;
                var values = line.Split(delimiter).Select(NormalizeCsvCell).ToArray();
                rows.Add((rowNumber, values));
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

        private static string SanitizeFileName(string name)
        {
            foreach (var c in Path.GetInvalidFileNameChars())
            {
                name = name.Replace(c.ToString(), string.Empty);
            }
            return name;
        }

        private static string EscapeCsvValue(string value)
        {
            var escaped = value.Replace("\"", "\"\"");
            return $"\"{escaped}\"";
        }
    }

    public class CsvColumnOption
    {
        public int Index { get; }
        public string Name { get; }

        public CsvColumnOption(int index, string name)
        {
            Index = index;
            Name = name;
        }

        public override string ToString() => Name;
    }
}
