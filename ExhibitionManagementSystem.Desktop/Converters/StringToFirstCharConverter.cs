using System;
using System.Globalization;
using System.Windows.Data;

namespace ExhibitionManagementSystem.Desktop.Converters
{
    public class StringToFirstCharConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string str && !string.IsNullOrWhiteSpace(str))
            {
                // Trim and get the first character, capitalized
                var trimmed = str.Trim();
                if (trimmed.Length > 0)
                {
                    return trimmed[0].ToString().ToUpper();
                }
            }
            return "?";
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
