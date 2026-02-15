using EduVS.ViewModels;
using System.Windows;

namespace EduVS.Views
{
    public partial class CreateNewStudentsWindowView : Window
    {
        public CreateNewStudentsWindowView(CreateNewStudentsViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;
        }
    }
}
