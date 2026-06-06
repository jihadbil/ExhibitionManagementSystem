using System;
using System.Globalization;
using System.Windows.Data;

namespace ExhibitionManagementSystem.Desktop.Converters
{
    public class ActiveBoolToStatusConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool isActive)
            {
                return isActive ? "Active" : "Cancelled";
            }
            return "Cancelled";
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
