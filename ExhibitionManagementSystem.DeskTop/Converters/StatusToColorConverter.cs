using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace ExhibitionManagementSystem.DeskTop.Converters;

[ValueConversion(typeof(string), typeof(Brush))]
public class StatusToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null)
            return (Brush)Application.Current.Resources["TextMutedBrush"];

        string status = value.ToString() ?? string.Empty;

        return status.ToLower() switch
        {
            "active" or "available" or "نشط" or "متاح" or "open" => (Brush)Application.Current.Resources["SuccessBrush"],
            "upcoming" or "reserved" or "قادم" or "محجوز" => (Brush)Application.Current.Resources["InfoBrush"],
            "pending" or "pendingreview" or "قيد المراجعة" or "قيدالمراجعة" => (Brush)Application.Current.Resources["WarningBrush"],
            "cancelled" or "ended" or "ملغي" or "منتهي" or "closed" => (Brush)Application.Current.Resources["DangerBrush"],
            _ => (Brush)Application.Current.Resources["TextMutedBrush"]
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return Binding.DoNothing;
    }
}
