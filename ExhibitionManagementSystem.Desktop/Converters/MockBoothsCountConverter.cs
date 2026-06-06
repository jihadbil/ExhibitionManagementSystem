using System;
using System.Globalization;
using System.Windows.Data;

namespace ExhibitionManagementSystem.Desktop.Converters
{
    public class MockBoothsCountConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int id)
            {
                // Generate a consistent mock number of booths based on the ID
                return 30 + (id * 7) % 50;
            }
            return 25;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
