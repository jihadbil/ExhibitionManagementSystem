using System;
using System.Globalization;
using System.Windows.Data;

namespace ExhibitionManagementSystem.Desktop.Converters
{
    public class DateToRangeConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length >= 2 && values[0] is DateTime start && values[1] is DateTime end)
            {
                var arCulture = new CultureInfo("ar-SA");
                string startDay = start.ToString("dd", arCulture);
                string endDay = end.ToString("dd", arCulture);
                string monthYear = start.ToString("MMMM yyyy", arCulture);
                
                // If they are in the same month and year
                if (start.Month == end.Month && start.Year == end.Year)
                {
                    return $"{startDay} - {endDay} {monthYear}";
                }
                else
                {
                    return $"{start.ToString("dd MMMM", arCulture)} - {end.ToString("dd MMMM yyyy", arCulture)}";
                }
            }
            return string.Empty;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
