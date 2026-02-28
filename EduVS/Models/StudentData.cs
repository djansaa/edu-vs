using CommunityToolkit.Mvvm.ComponentModel;

namespace EduVS.Models
{
    public partial class StudentData : ObservableObject
    {
        [NotifyPropertyChangedFor(nameof(DisplayName))]
        [ObservableProperty] private string name = "";

        [NotifyPropertyChangedFor(nameof(DisplayName))]
        [ObservableProperty] private string surname = "";

        [ObservableProperty] private int? testId;

        public string DisplayName => string.Join(" ", new[] { Surname, Name }.Where(x => !string.IsNullOrWhiteSpace(x)));
    }
}
