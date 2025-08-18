using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EduVS.Data;
using EduVS.Models;
using EduVS.Views;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Serilog;
using System.Collections.ObjectModel;
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

        public IAsyncRelayCommand LoadTestsCommand { get; }
        public IRelayCommand AddSubjectCommand { get; }

        public TestsViewModel(ILogger<TestsViewModel> logger, AppDbContext db) : base(logger, db)
        {
            LoadTestsCommand = new AsyncRelayCommand(LoadTestsAsync);
            AddSubjectCommand = new RelayCommand(AddSubject);

            LoadSubjects();
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
                    Log.Information("Subject added: {Code} - {Name}", code, name);
                }
                else
                {
                    MessageBox.Show("Subject code already exists.", "Duplicate", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

    }
}