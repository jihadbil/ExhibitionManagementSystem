using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace ExhibitionManagementSystem.DeskTop.Converters;

public class ExhibitionTypeToGradientConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        string type = value?.ToString() ?? string.Empty;
        string key = type switch
        {
            "Tech" => "TechGradientBrush",
            "Medical" => "MedicalGradientBrush",
            "Industrial" => "IndustrialGradientBrush",
            "Commercial" => "CommercialGradientBrush",
            "Educational" => "EducationalGradientBrush",
            "Automotive" => "AutomotiveGradientBrush",
            _ => "PrimaryGradientBrush"
        };

        if (Application.Current != null && Application.Current.Resources.Contains(key))
        {
            return (Brush)Application.Current.Resources[key];
        }

        return Brushes.Transparent;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return Binding.DoNothing;
    }
}
