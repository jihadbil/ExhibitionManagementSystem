using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using ExhibitionManagementSystem.Desktop.Helpers;

namespace ExhibitionManagementSystem.Desktop.Converters
{
    public class StatusToColorConverter : IValueConverter
    {
        private static readonly Dictionary<string, Color> _colorMap = new(StringComparer.OrdinalIgnoreCase)
        {
            { "Active", ColorHelper.FromHex("#059669") },
            { "Available", ColorHelper.FromHex("#059669") },
            { "Reserved", ColorHelper.FromHex("#4F46E5") },
            { "UnderReview", ColorHelper.FromHex("#D97706") },
            { "Soon", ColorHelper.FromHex("#2563EB") },
            { "Ended", ColorHelper.FromHex("#6B7280") },
            { "Cancelled", ColorHelper.FromHex("#DC2626") }
        };

        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string status && _colorMap.TryGetValue(status, out var color))
            {
                return color;
            }
            return Colors.Transparent;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
