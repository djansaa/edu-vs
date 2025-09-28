using EduVS.ViewModels;
using System.Windows;

namespace EduVS.Views
{
    /// <summary>
    /// Interaction logic for GenerateTestWindowView.xaml
    /// </summary>
    public partial class GenerateTestWindowView : Window
    {
        public GenerateTestWindowView(GenerateTestViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;
        }
    }
}
