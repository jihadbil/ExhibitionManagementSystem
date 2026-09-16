using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace ExhibitionManagementSystem.DeskTop.Converters;

[ValueConversion(typeof(bool), typeof(Brush))]
public class BoolToStatusBrushConverter : IValueConverter
{
    private static readonly Brush AvailableBrush = new SolidColorBrush(Color.FromRgb(5, 150, 105)); // Green
    private static readonly Brush OccupiedBrush = new SolidColorBrush(Color.FromRgb(220, 38, 38));   // Red

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isAvailable && isAvailable)
        {
            return AvailableBrush;
        }
        return OccupiedBrush;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

[ValueConversion(typeof(bool), typeof(string))]
public class BoolToAvailabilityTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isAvailable && isAvailable)
        {
            return "شاغرة";
        }
        return "محجوزة";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

[ValueConversion(typeof(bool), typeof(Visibility))]
public class InverseBooleanToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool flag)
        {
            return flag ? Visibility.Collapsed : Visibility.Visible;
        }
        return Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}
