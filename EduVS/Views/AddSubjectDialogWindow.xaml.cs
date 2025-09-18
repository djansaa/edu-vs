using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace EduVS.Views
{
    /// <summary>
    /// Interaction logic for AddSubjectDialogWindow.xaml
    /// </summary>
    public partial class AddSubjectDialogWindow : Window
    {
        public string SubjectCode { get; private set; } = string.Empty;
        public string SubjectName { get; private set; } = string.Empty;

        public AddSubjectDialogWindow()
        {
            InitializeComponent();
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            SubjectCode = CodeTextBox.Text.Trim().ToUpper();
            SubjectName = NameTextBox.Text.Trim();

            bool hasError = false;

            if (string.IsNullOrWhiteSpace(SubjectCode))
            {
                CodeTextBox.Background = Brushes.MistyRose;
                hasError = true;
            }
            else
            {
                CodeTextBox.ClearValue(TextBox.BackgroundProperty);
            }

            if (string.IsNullOrWhiteSpace(SubjectName))
            {
                NameTextBox.Background = Brushes.MistyRose;
                hasError = true;
            }
            else
            {
                NameTextBox.ClearValue(TextBox.BackgroundProperty);
            }

            if (hasError) return;

            DialogResult = true;
            Close();
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox tb && !string.IsNullOrWhiteSpace(tb.Text))
                tb.ClearValue(TextBox.BackgroundProperty);
        }
    }
}
