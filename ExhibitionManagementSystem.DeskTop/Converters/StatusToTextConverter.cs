using System;
using System.Globalization;
using System.Windows.Data;

namespace ExhibitionManagementSystem.DeskTop.Converters;

[ValueConversion(typeof(string), typeof(string))]
public class StatusToTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null)
            return string.Empty;

        string status = value.ToString() ?? string.Empty;

        return status.ToLower() switch
        {
            "active" or "open" => "نشط",
            "available" => "متاح",
            "upcoming" => "قادم",
            "reserved" => "محجوز",
            "pending" or "pendingreview" => "قيد المراجعة",
            "cancelled" => "ملغي",
            "ended" or "closed" => "منتهي",
            _ => status
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return Binding.DoNothing;
    }
}
