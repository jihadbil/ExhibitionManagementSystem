using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace ExhibitionManagementSystem.Desktop.Converters
{
    public class StringToGradientConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string typeName && !string.IsNullOrWhiteSpace(typeName))
            {
                string key = typeName.Trim() + "GradientBrush";
                if (Application.Current.TryFindResource(key) is Brush brush)
                {
                    return brush;
                }
            }

            return Application.Current.TryFindResource("PrimaryGradientBrush") as Brush 
                   ?? Brushes.Gray;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
