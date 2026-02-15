using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EduVS.Helpers;
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

        [ObservableProperty] private ObservableCollection<CsvColumnOption> availableColumns = new();
        [ObservableProperty] private CsvColumnOption? selectedCombinedNameColumn;
        [ObservableProperty] private CsvColumnOption? selectedNameColumn;
        [ObservableProperty] private CsvColumnOption? selectedSurnameColumn;

        [ObservableProperty] private DataView? previewRows;
        [ObservableProperty] private ObservableCollection<string> parsedStudents = new();
        [ObservableProperty] private string className = string.Empty;

        private readonly List<string[]> _filteredRows = new();

        public CreateNewStudentsViewModel(ILogger<CreateNewStudentsViewModel> logger) : base(logger)
        {
        }

        partial void OnSourceFilePathChanged(string value) => RefreshPreview();
        partial void OnSelectedSeparatorChanged(string value) => RefreshPreview();
        partial void OnStartRowChanged(int value) => RefreshPreview();
        partial void OnUseCombinedNameColumnChanged(bool value) => BuildParsedStudentsPreview();
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

            File.WriteAllLines(outputPath, ParsedStudents.Select(EscapeCsvValue), Encoding.UTF8);

            MessageBox.Show("Students CSV file has been created.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            window?.Close();
        }

        private void RefreshPreview()
        {
            PreviewRows = null;
            ParsedStudents = new ObservableCollection<string>();
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
                ParsedStudents = new ObservableCollection<string>();
                return;
            }

            var students = new List<string>();
            foreach (var row in _filteredRows)
            {
                string fullName;
                if (UseCombinedNameColumn)
                {
                    if (SelectedCombinedNameColumn is null) continue;
                    fullName = CsvHelper.GetValueAt(row, SelectedCombinedNameColumn.Index);
                }
                else
                {
                    if (SelectedNameColumn is null) continue;
                    var firstName = CsvHelper.GetValueAt(row, SelectedNameColumn.Index);
                    var surname = SelectedSurnameColumn is null ? string.Empty : CsvHelper.GetValueAt(row, SelectedSurnameColumn.Index);
                    fullName = string.Join(" ", new[] { firstName, surname }.Where(x => !string.IsNullOrWhiteSpace(x))).Trim();
                }

                if (!string.IsNullOrWhiteSpace(fullName))
                {
                    students.Add(fullName);
                }
            }

            ParsedStudents = new ObservableCollection<string>(students);
        }

        private static List<(int RowNumber, string[] Values)> LoadRowsFromCsv(string path, string separator)
        {
            var rows = new List<(int RowNumber, string[] Values)>();
            var encoding = Encoding.GetEncoding(1250);
            var delimiter = CsvHelper.ResolveDelimiter(separator);

            var allLines = File.ReadAllLines(path, encoding);
            for (var i = 0; i < allLines.Length; i++)
            {
                var values = allLines[i].Split(delimiter).Select(v => v.Trim()).ToArray();
                rows.Add((i + 1, values));
            }

            return rows;
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
