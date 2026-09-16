using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace ExhibitionManagementSystem.DeskTop.Converters;

public class SelectionAndCollisionToBorderConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values == null || values.Length < 2)
            return DependencyProperty.UnsetValue;

        bool isSelected = values[0] is bool b1 && b1;
        bool hasCollision = values[1] is bool b2 && b2;

        if (hasCollision)
        {
            return new SolidColorBrush(Color.FromRgb(239, 68, 68)); // #EF4444
        }

        if (isSelected)
        {
            return new SolidColorBrush(Color.FromRgb(79, 70, 229)); // #4F46E5 (Indigo)
        }

        // Classic blueprint dark slate border
        return new SolidColorBrush(Color.FromRgb(71, 85, 105)); // #475569 (slate-600)
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
