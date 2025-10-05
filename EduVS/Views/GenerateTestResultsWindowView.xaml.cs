using EduVS.ViewModels;
using System.Windows;

namespace EduVS.Views
{
    /// <summary>
    /// Interaction logic for GenerateTestResultsWindowView.xaml
    /// </summary>
    public partial class GenerateTestResultsWindowView : Window
    {
        public GenerateTestResultsWindowView(GenerateTestResultsViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;
        }
    }
}
