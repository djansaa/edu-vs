using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EduVS.Data;
using EduVS.Helpers;
using EduVS.Models;
using EduVS.Views;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Serilog;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;

namespace EduVS.ViewModels
{
    public partial class TestsViewModel : BaseViewModel
    {
        public ObservableCollection<Test> Tests { get; } = new();
        public ObservableCollection<Subject> Subjects { get; } = new();

        [ObservableProperty]
        private Test? selectedTest;

        [ObservableProperty]
        private string? tempSubjectCode;
        [ObservableProperty]
        private string? tempTestName;
        [ObservableProperty]
        private string? tempTemplateAPath;
        [ObservableProperty]
        private string? tempTemplateBPath;


        public IRelayCommand OpenTestsFolderCommand { get; }
        public IAsyncRelayCommand LoadTestsCommand { get; }
        public IRelayCommand CancelSelectedTestCommand { get; }
        public IRelayCommand UpdateSelectedTestCommand { get; }
        public IRelayCommand DeleteSelectedTestCommand { get; }

        public IRelayCommand AddSubjectCommand { get; }
        public IRelayCommand BrowseTemplateACommand { get; }
        public IRelayCommand BrowseTemplateBCommand { get; }
        public IRelayCommand ShowTemplateACommand { get; }
        public IRelayCommand ShowTemplateBCommand { get; }
        public IRelayCommand AddNewTestCommand { get; }
        public IRelayCommand ClearNewTestCommand { get; }

        public TestsViewModel(ILogger<TestsViewModel> logger, AppDbContext db) : base(logger, db)
        {
            OpenTestsFolderCommand = new RelayCommand(OpenTestsFolder);
            LoadTestsCommand = new AsyncRelayCommand(LoadTestsAsync);
            CancelSelectedTestCommand = new RelayCommand(CancelSelectedTest);
            UpdateSelectedTestCommand = new AsyncRelayCommand(UpdateSelectedTestAsync);
            DeleteSelectedTestCommand = new AsyncRelayCommand(DeleteSelectedTest);


            AddSubjectCommand = new RelayCommand(AddSubject);
            BrowseTemplateACommand = new RelayCommand(BrowseTemplateA);
            BrowseTemplateBCommand = new RelayCommand(BrowseTemplateB);
            ShowTemplateACommand = new RelayCommand(ShowTemplateA);
            ShowTemplateBCommand = new RelayCommand(ShowTemplateB);
            AddNewTestCommand = new AsyncRelayCommand(AddNewTestAsync);
            ClearNewTestCommand = new RelayCommand(ClearNewTest);

            LoadSubjects();
        }

        private void OpenTestsFolder()
        {
            var baseDir = AppContext.BaseDirectory; // app folder
            var testsDir = Path.Combine(baseDir, "test-templates");
            if (!Directory.Exists(testsDir)) Directory.CreateDirectory(testsDir);
            try
            {
                Log.Information("Opening tests folder: {TestsDir}", testsDir);
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = testsDir,
                    UseShellExecute = true,
                    Verb = "open"
                });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to open tests folder: {TestsDir}", testsDir);
                MessageBox.Show("Failed to open folder.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task DeleteSelectedTest()
        {
            if (SelectedTest is null) return;

            if (MessageBox.Show("Delete selected test? PDF templates will also be removed from app folder.", "Confirm delete", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;

            var test = SelectedTest; // local reference

            try
            {
                var fullA = ResolvePath(test.TemplateAPath);
                var fullB = ResolvePath(test.TemplateBPath);

                // delete files
                SafeDeleteFile(fullA);
                SafeDeleteFile(fullB);

                // DB
                _db.Tests.Remove(test);
                await _db.SaveChangesAsync();

                SelectedTest = null;

                // try to delete parent dir if empty
                var dirA = fullA is null ? null : Path.GetDirectoryName(fullA);
                var dirB = fullB is null ? null : Path.GetDirectoryName(fullB);
                if (dirA == dirB) TryDeleteDirIfEmpty(dirA);
                else { TryDeleteDirIfEmpty(dirA); TryDeleteDirIfEmpty(dirB); }

                MessageBox.Show("Test was successfully deleted.", "OK", MessageBoxButton.OK, MessageBoxImage.Information);

                await LoadTestsAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "DeleteSelectedTest failed");
                MessageBox.Show("Delete failed.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        partial void OnSelectedTestChanged(Test? value)
        {
            if (value is null)
            {
                TempSubjectCode = null;
                TempTestName = null;
                TempTemplateAPath = null;
                TempTemplateBPath = null;
                return;
            }

            TempSubjectCode = value.SubjectCode;
            TempTestName = value.Name;
            TempTemplateAPath = value.TemplateAPath;
            TempTemplateBPath = value.TemplateBPath;
        }

        private async Task UpdateSelectedTestAsync()
        {
            if (SelectedTest is null) return;

            if (string.IsNullOrWhiteSpace(TempSubjectCode) ||
                string.IsNullOrWhiteSpace(TempTestName) ||
                string.IsNullOrWhiteSpace(TempTemplateAPath))
            {
                MessageBox.Show("Please fill in all required fields.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (MessageBox.Show("Update selected test?", "Confirm update", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;

            string? newA = null, newB = null;
            try
            {
                // Build the same target folder pattern as in AddNewTestAsync
                var now = DateTime.Now;
                var folderName = $"{MakeSafeName(TempSubjectCode)}_{MakeSafeName(TempTestName)}_{now:yyyy_MM_dd}";
                var baseDir = AppContext.BaseDirectory;
                var targetDir = Path.Combine(baseDir, "test-templates", folderName);

                // Decide whether we need to copy A and/or B
                var srcA = Resolve(TempTemplateAPath)!;
                var oldA = Resolve(SelectedTest.TemplateAPath);

                var needCopyA = !IsSameFile(srcA, oldA);

                var needCopyB = false;
                string? srcB = null;
                var oldB = Resolve(SelectedTest.TemplateBPath);
                if (!string.IsNullOrWhiteSpace(TempTemplateBPath))
                {
                    srcB = Resolve(TempTemplateBPath);
                    needCopyB = !IsSameFile(srcB, oldB);
                }

                // Only create folder if we actually copy something
                if (needCopyA || needCopyB)
                    Directory.CreateDirectory(targetDir);

                Log.Information("Updating test {@Test}, needCopyA={NeedCopyA}, needCopyB={NeedCopyB}", SelectedTest, needCopyA, needCopyB);

                // Base filename part (same as in create)
                var baseName = $"{MakeSafeName(TempSubjectCode)}_{MakeSafeName(TempTestName)}";

                // --- Copy A if changed (like in AddNewTestAsync) ---
                if (needCopyA)
                {
                    var destA = NextIndexedPath(targetDir, baseName, "A");
                    File.Copy(srcA, destA, overwrite: false);
                    newA = destA; // remember for cleanup on failure
                }

                // --- Copy B if provided and changed (like in AddNewTestAsync) ---
                if (needCopyB && srcB is not null)
                {
                    var destB = NextIndexedPath(targetDir, baseName, "B");
                    File.Copy(srcB, destB, overwrite: false);
                    newB = destB;
                }

                // Update entity fields AFTER files are ready
                SelectedTest.SubjectCode = TempSubjectCode!;
                SelectedTest.Name = TempTestName!;
                if (needCopyA && newA is not null)
                {
                    // Remove previous A (if any) and set new path
                    SafeDeleteFile(oldA);
                    SelectedTest.TemplateAPath = newA;
                }
                if (needCopyB && newB is not null)
                {
                    SafeDeleteFile(oldB);
                    SelectedTest.TemplateBPath = newB;
                }

                await _db.SaveChangesAsync();

                Log.Information("Updated test {@Test}", SelectedTest);
                MessageBox.Show("Test updated.", "OK", MessageBoxButton.OK, MessageBoxImage.Information);

                await LoadTestsAsync();

            }
            catch (Exception ex)
            {
                Log.Error(ex, "UpdateSelectedTest failed");

                // Cleanup any newly copied files because DB update didn't finish
                SafeDeleteFile(newA);
                SafeDeleteFile(newB);

                MessageBox.Show("Update failed.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelSelectedTest()
        {
            SelectedTest = null;
            TempSubjectCode = null;
            TempTestName = null;
            TempTemplateAPath = null;
            TempTemplateBPath = null;
        }

        private void ClearNewTest()
        {
            TempSubjectCode = null;
            TempTestName = null;
            TempTemplateAPath = null;
            TempTemplateBPath = null;
        }

        private async Task AddNewTestAsync()
        {
            if (string.IsNullOrWhiteSpace(TempSubjectCode) ||
                string.IsNullOrWhiteSpace(TempTestName) ||
                string.IsNullOrWhiteSpace(TempTemplateAPath))
            {
                MessageBox.Show("Please fill in all required fields.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string? destA = null, destB = null;
            try
            {
                var now = DateTime.Now;
                var folderName = $"{MakeSafeName(TempSubjectCode)}_{MakeSafeName(TempTestName)}_{now:yyyy_MM_dd}";
                var targetDir = Path.Combine(AppContext.BaseDirectory, "test-templates", folderName);
                Directory.CreateDirectory(targetDir);

                var fileName = $"{MakeSafeName(TempSubjectCode)}_{MakeSafeName(TempTestName)}";
                destA = Path.Combine(targetDir, fileName + "_A.pdf");
                destB = Path.Combine(targetDir, fileName + "_B.pdf");

                int i = 1;
                while (File.Exists(destA)) destA = Path.Combine(targetDir, $"{fileName}_A{i++}.pdf");
                i = 1;
                while (File.Exists(destB)) destB = Path.Combine(targetDir, $"{fileName}_B{i++}.pdf");

                File.Copy(TempTemplateAPath!, destA, overwrite: false);
                if (!string.IsNullOrWhiteSpace(TempTemplateBPath))
                    File.Copy(TempTemplateBPath!, destB, overwrite: false);
                else
                    destB = null; // <-- important

                // add test to db
                var newTest = new Test
                {
                    SubjectCode = TempSubjectCode,
                    Name = TempTestName,
                    TemplateAPath = destA,
                    TemplateBPath = destB,
                    TimeStamp = now
                };

                Log.Information("Adding new test: {@Test}", newTest);

                await _db.Tests.AddAsync(newTest);
                await _db.SaveChangesAsync();

                // clear temp
                TempSubjectCode = null;
                TempTestName = null;
                TempTemplateAPath = null;
                TempTemplateBPath = null;

                MessageBox.Show("New test added.", "OK", MessageBoxButton.OK, MessageBoxImage.Information);

                await LoadTestsAsync();

            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to add new test");
                // cleanup copied files
                try { if (destA != null && File.Exists(destA)) File.Delete(destA); } catch { }
                try { if (destB != null && File.Exists(destB)) File.Delete(destB); } catch { }
                MessageBox.Show("Add failed.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BrowseTemplateA()
        {
            var fileName = PdfPicker.PickPdf();
            if (fileName != null) TempTemplateAPath = fileName;
        }

        private void BrowseTemplateB()
        {
            var fileName = PdfPicker.PickPdf();
            if (fileName != null) TempTemplateBPath = fileName;
        }

        private void ShowTemplateA()
        {
        }

        private void ShowTemplateB()
        {
        }

        private async Task LoadTestsAsync()
        {
            // load all tests from db
            try
            {
                Log.Information("Loading tests from db");

                var tests = await _db.Tests
                    .Include(t => t.Subject)
                    .OrderByDescending(t => t.TimeStamp)
                    .ToListAsync();

                Tests.Clear();
                foreach (var t in tests) Tests.Add(t);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to load tests");
                MessageBox.Show("Loading failed.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadSubjects()
        {
            Subjects.Clear();
            foreach (var s in _db.Subjects.OrderBy(s => s.Code)) Subjects.Add(s);
        }

        private void AddSubject()
        {
            var window = new AddSubjectDialogWindow();

            // show dialog next to the cursor
            var pos = Mouse.GetPosition(Application.Current.MainWindow);
            var screenPoint = Application.Current.MainWindow.PointToScreen(pos);
            window.Left = screenPoint.X;
            window.Top = screenPoint.Y;


            if (window.ShowDialog() == true)
            {
                var code = window.SubjectCode;
                var name = window.SubjectName;

                if (!_db.Subjects.Any(s => s.Code == code))
                {
                    var subject = new Subject { Code = code, Name = name };
                    _db.Subjects.Add(subject);
                    _db.SaveChanges();

                    Subjects.Add(subject);

                    TempSubjectCode = subject.Code;
                    Log.Information("Subject added: {Code} - {Name}", code, name);
                }
                else
                {
                    MessageBox.Show("Subject code already exists.", "Duplicate", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        private static string MakeSafeName(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return "unnamed";
            // delete diacritics
            var normalized = s.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();
            foreach (var ch in normalized)
                if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(ch) != System.Globalization.UnicodeCategory.NonSpacingMark)
                    sb.Append(ch);
            var noDia = sb.ToString().Normalize(NormalizationForm.FormC);

            // forbidden chars -> _
            var invalid = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
            var re = new Regex($"[{Regex.Escape(invalid)}]");
            var safe = re.Replace(noDia, "_");

            // whitespace -> _
            safe = Regex.Replace(safe, @"\s+", "_");
            return safe.Trim('_');
        }

        private static string? Resolve(string? p) =>
            string.IsNullOrWhiteSpace(p) ? null :
            Path.IsPathRooted(p) ? Path.GetFullPath(p)
                                 : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, p));

        private static bool IsSameFile(string? a, string? b)
        {
            var fa = Resolve(a); var fb = Resolve(b);
            return fa != null && fb != null &&
                   string.Equals(fa, fb, StringComparison.OrdinalIgnoreCase);
        }

        private static void SafeDeleteFile(string? path)
        {
            try { if (!string.IsNullOrWhiteSpace(path) && File.Exists(path)) File.Delete(path); }
            catch (Exception ex) { Log.Warning(ex, "Cleanup: failed to delete {Path}", path); }
        }

        private static string NextIndexedPath(string dir, string baseName, string suffix /* "A" or "B" */)
        {
            // Builds ...\{baseName}_{suffix}.pdf, then ..._{suffix}1.pdf, ..._{suffix}2.pdf, ...
            var candidate = Path.Combine(dir, $"{baseName}_{suffix}.pdf");
            if (!File.Exists(candidate)) return candidate;
            int i = 1;
            do
            {
                candidate = Path.Combine(dir, $"{baseName}_{suffix}{i}.pdf");
                i++;
            } while (File.Exists(candidate));
            return candidate;
        }

        string? ResolvePath(string? p)
        {
            if (string.IsNullOrWhiteSpace(p)) return null;
            if (Path.IsPathRooted(p)) return p;
            return Path.Combine(AppContext.BaseDirectory, p);
        }

        void TryDeleteDirIfEmpty(string? d)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(d) && Directory.Exists(d) &&
                    !Directory.EnumerateFileSystemEntries(d).Any())
                    Directory.Delete(d);
            }
            catch (Exception ex) { Log.Warning(ex, "Cleanup: failed to delete dir {Dir}", d); }
        }

    }
}