using EduVS.ViewModels;
using System.ComponentModel;
using System.Windows;

namespace EduVS.Views
{
    public partial class GenerateTestResultsStartProgressWindowView : Window
    {
        public GenerateTestResultsStartProgressViewModel ViewModel { get; }

        public GenerateTestResultsStartProgressWindowView(GenerateTestResultsStartProgressViewModel vm)
        {
            InitializeComponent();
            ViewModel = vm;
            DataContext = vm;
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (!ViewModel.CanClose)
            {
                e.Cancel = true;
                ViewModel.CancelCommand.Execute(null);
            }

            base.OnClosing(e);
        }
    }
}
