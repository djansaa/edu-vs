using System.Globalization;
using System.Windows.Data;

namespace EduVS.ViewHelpers
{
    public class NonNegativeIntConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => value?.ToString() ?? "0";

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string s && int.TryParse(s, out var n) && n >= 0)
                return n;
            return 0;
        }
    }
}
