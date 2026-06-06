using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ExhibitionManagementSystem.Desktop.Converters
{
    public class PercentageToWidthConverter : IMultiValueConverter
    {
        public object Convert(object[]? values, Type targetType, object? parameter, CultureInfo culture)
        {
            if (values != null && values.Length >= 2)
            {
                double percentage = 0.0;
                double totalWidth = 0.0;

                if (values[0] is double d1) percentage = d1;
                else if (values[0] is IConvertible c1) percentage = System.Convert.ToDouble(c1);

                if (values[1] is double d2) totalWidth = d2;
                else if (values[1] is IConvertible c2) totalWidth = System.Convert.ToDouble(c2);

                if (percentage < 0) percentage = 0;
                if (percentage > 100) percentage = 100;

                return totalWidth * (percentage / 100.0);
            }
            return 0.0;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
