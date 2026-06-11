using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ExhibitionManagementSystem.DeskTop.Converters;

[ValueConversion(typeof(object), typeof(Visibility))]
public class NullToVisibilityConverter : IValueConverter
{
    public bool Inverse { get; set; }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool isNull = value == null;
        bool result = isNull;

        if (Inverse)
        {
            result = !isNull;
        }

        // If result is true (it is null, or not null but inversed), we want it Collapsed
        // Standard behavior: null -> Collapsed (result = true -> Collapsed), not null -> Visible (result = false -> Visible)
        // Inverse behavior: null -> Visible (result = false -> Visible), not null -> Collapsed (result = true -> Collapsed)
        return result ? Visibility.Collapsed : Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return Binding.DoNothing;
    }
}
