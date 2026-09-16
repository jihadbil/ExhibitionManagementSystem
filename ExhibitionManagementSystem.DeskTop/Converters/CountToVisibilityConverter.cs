using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ExhibitionManagementSystem.DeskTop.Converters;

[ValueConversion(typeof(int), typeof(Visibility))]
public class CountToVisibilityConverter : IValueConverter
{
    public bool Inverse { get; set; }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        int count = 0;
        if (value is int intValue)
        {
            count = intValue;
        }
        else if (value is System.Collections.ICollection collection)
        {
            count = collection.Count;
        }

        bool isEmpty = count == 0;
        bool result = isEmpty;

        if (Inverse)
        {
            result = !isEmpty;
        }

        return result ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return Binding.DoNothing;
    }
}
