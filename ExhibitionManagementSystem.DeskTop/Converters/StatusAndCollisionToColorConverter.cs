using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace ExhibitionManagementSystem.DeskTop.Converters;

public class StatusAndCollisionToColorConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values == null || values.Length < 2)
            return DependencyProperty.UnsetValue;

        bool hasCollision = values[1] is bool b && b;

        if (hasCollision)
        {
            // Light red tint for collision area
            return new SolidColorBrush(Color.FromRgb(254, 226, 226)); // #FEE2E2
        }

        // Classic clean white floorplan background
        return new SolidColorBrush(Colors.White);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
