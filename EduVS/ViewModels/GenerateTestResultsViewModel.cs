using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EduVS.Data;
using EduVS.Helpers;
using EduVS.Models;
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
        [ObservableProperty] private string studentsCsvPath = string.Empty;

        [ObservableProperty] private ObservableCollection<StudentData> students = new();
        [ObservableProperty] private StudentData? selectedStudent;

        [ObservableProperty] private ObservableCollection<TestData> tests = new();
        [ObservableProperty] private TestData? selectedTest;

        [ObservableProperty] private bool isStarted;

        private readonly PdfManager _pdf = new();
        private string? _combinedPdfPath;
        private Dictionary<int, List<int>> _pagesByTestId = new();

        public GenerateTestResultsViewModel(ILogger<GenerateTestResultsViewModel> logger, AppDbContext db) : base(logger, db) { }

        [RelayCommand]
        private void BrowsePdfs()
        {
            var paths = PdfPicker.PickPdfs();
            if (paths is null || !paths.Any()) return;

            SelectedPdfPaths = new ObservableCollection<string>(paths);
        }

        [RelayCommand]
        private void BrowseStudentsCsv()
        {
            var path = PdfPicker.PickCsv();
            if (path is null) return;
            StudentsCsvPath = path;
        }

        [RelayCommand]
        private void Start()
        {
            if (SelectedPdfPaths is null || !SelectedPdfPaths.Any())
            {
                MessageBox.Show("You must choose input PDFs files.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (string.IsNullOrEmpty(StudentsCsvPath))
            {
                MessageBox.Show("You must choose input CSV file with students.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // load students from CSV into collection
            foreach (var student in LoadStudentsFromCsv(StudentsCsvPath)) Students.Add(student);

            // 1) merge
            var temp = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"combined_{Guid.NewGuid():N}.pdf");
            _combinedPdfPath = _pdf.MergePdfs(SelectedPdfPaths, temp);

            // 2) scan
            var qrTopRight = new System.Drawing.RectangleF(0.55f, 0f, 0.45f, 0.45f);
            var nameBoxTL = new System.Drawing.RectangleF(0.10f, 0.0095f, 0.49f, 0.06f);

            var (testsFound, pagesMap) = _pdf.Scan(_combinedPdfPath, qrTopRight, nameBoxTL);
            Tests = new ObservableCollection<TestData>(testsFound);
            SelectedTest = Tests.FirstOrDefault();
            _pagesByTestId = new Dictionary<int, List<int>>(pagesMap);

            IsStarted = true;
        }

        private IEnumerable<StudentData> LoadStudentsFromCsv(string path)
        {
            var students = new List<StudentData>();
            try
            {
                var enc = Encoding.GetEncoding(1250); // windows-1250
                var all = File.ReadAllLines(path, enc);

                // skip first three lines and last one
                var lines = all.Skip(3).Take(Math.Max(0, all.Length - 4));

                foreach (var line in lines)
                {
                    if (CsvHelper.TryParse(line, out var student) && student is not null)
                    {
                        students.Add(student);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load students from CSV file.\nError: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            return students;
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
