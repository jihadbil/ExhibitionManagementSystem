using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ExhibitionManagementSystem.Desktop.Converters
{
    public class InverseBoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool b)
            {
                return b ? Visibility.Collapsed : Visibility.Visible;
            }
            return Visibility.Visible;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is Visibility vis)
            {
                return vis != Visibility.Visible;
            }
            return true;
        }
    }
}
