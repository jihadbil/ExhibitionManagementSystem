using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace ExhibitionManagementSystem.Desktop.Converters
{
    public class BoothStatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string status)
            {
                return status switch
                {
                    "متاح" or "Available" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#16A34A")),
                    "محجوز" or "Reserved" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D97706")),
                    "قيد المراجعة" or "UnderReview" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2563EB")),
                    _ => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6B7280"))
                };
            }
            return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6B7280"));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
