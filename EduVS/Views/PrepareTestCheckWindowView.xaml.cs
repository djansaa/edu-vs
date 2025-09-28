using EduVS.ViewModels;
using System.Windows;

namespace EduVS.Views
{
    /// <summary>
    /// Interaction logic for PrepareTestCheckWindowView.xaml
    /// </summary>
    public partial class PrepareTestCheckWindowView : Window
    {
        public PrepareTestCheckWindowView(PrepareTestCheckViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;
        }
    }
}
