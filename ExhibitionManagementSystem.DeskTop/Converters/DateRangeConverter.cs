using System;
using System.Globalization;
using System.Windows.Data;

namespace ExhibitionManagementSystem.DeskTop.Converters;

public class DateRangeConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length >= 2 && values[0] is DateTime start && values[1] is DateTime end)
        {
            if (start == DateTime.MinValue || end == DateTime.MinValue)
            {
                return "غير محدد";
            }

            try
            {
                // استخدام ثقافة عربية تستخدم التقويم الميلادي افتراضياً لتجنب مشاكل تقويم أم القرى (الهجري)
                var ar = new CultureInfo("ar-EG");
                return $"{start.ToString("d MMM", ar)} — {end.ToString("d MMM yyyy", ar)}";
            }
            catch
            {
                return $"{start:yyyy-MM-dd} — {end:yyyy-MM-dd}";
            }
        }
        return string.Empty;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
