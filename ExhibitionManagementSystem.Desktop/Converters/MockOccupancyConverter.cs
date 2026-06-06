using System;
using System.Globalization;
using System.Windows.Data;

namespace ExhibitionManagementSystem.Desktop.Converters
{
    public class MockOccupancyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int id)
            {
                // Generate a consistent occupancy percentage
                return Math.Round(60.0 + (id * 4.3) % 35.0, 1);
            }
            return 75.0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
