using System;
using System.Globalization;
using System.Windows.Data;
using MaterialDesignThemes.Wpf;

namespace ExhibitionManagementSystem.Desktop.Converters
{
    public class TrendDirectionToIconConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string direction && string.Equals(direction, "Up", StringComparison.OrdinalIgnoreCase))
            {
                return PackIconKind.TrendingUp;
            }
            return PackIconKind.TrendingDown;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
