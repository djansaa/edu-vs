using CommunityToolkit.Mvvm.ComponentModel;

namespace EduVS.Models
{
    public partial class StudentData : ObservableObject
    {
        [ObservableProperty] private string name = "";
        [ObservableProperty] private int? testId;
    }
}
