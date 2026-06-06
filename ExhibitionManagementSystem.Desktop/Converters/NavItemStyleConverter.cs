using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ExhibitionManagementSystem.Desktop.Converters
{
    public class NavItemStyleConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length >= 2 && values[0] is string selected && values[1] is string page && selected == page)
            {
                return Application.Current.TryFindResource("ActiveNavItemStyle") ?? DependencyProperty.UnsetValue;
            }
            return Application.Current.TryFindResource("SecondaryButtonStyle") ?? DependencyProperty.UnsetValue;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
