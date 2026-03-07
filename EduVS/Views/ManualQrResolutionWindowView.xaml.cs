using EduVS.ViewModels;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace EduVS.Views
{
    public partial class ManualQrResolutionWindowView : Window
    {
        private static readonly Regex DigitsOnly = new("^[0-9]+$");

        public ManualQrResolutionViewModel ViewModel { get; }

        public ManualQrResolutionWindowView(ManualQrResolutionViewModel vm)
        {
            InitializeComponent();
            ViewModel = vm;
            DataContext = vm;

            DataObject.AddPastingHandler(TestIdTextBox, OnNumericTextBoxPaste);
            DataObject.AddPastingHandler(PageTextBox, OnNumericTextBoxPaste);
        }

        private void NumericTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !DigitsOnly.IsMatch(e.Text);
        }

        private void OnNumericTextBoxPaste(object sender, DataObjectPastingEventArgs e)
        {
            if (!e.SourceDataObject.GetDataPresent(DataFormats.Text))
            {
                e.CancelCommand();
                return;
            }

            var text = e.SourceDataObject.GetData(DataFormats.Text) as string ?? string.Empty;
            if (!DigitsOnly.IsMatch(text))
            {
                e.CancelCommand();
            }
        }
    }
}
