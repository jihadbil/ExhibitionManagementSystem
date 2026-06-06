using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using ExhibitionManagementSystem.Desktop.Helpers;

namespace ExhibitionManagementSystem.Desktop.Converters
{
    public class StatusToBrushConverter : IValueConverter
    {
        private static readonly Dictionary<string, Brush> _brushMap = new(StringComparer.OrdinalIgnoreCase)
        {
            { "Active", ColorHelper.BrushFromHex("#059669") },
            { "Available", ColorHelper.BrushFromHex("#059669") },
            { "Reserved", ColorHelper.BrushFromHex("#4F46E5") },
            { "UnderReview", ColorHelper.BrushFromHex("#D97706") },
            { "Soon", ColorHelper.BrushFromHex("#2563EB") },
            { "Ended", ColorHelper.BrushFromHex("#6B7280") },
            { "Cancelled", ColorHelper.BrushFromHex("#DC2626") }
        };

        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string status && _brushMap.TryGetValue(status, out var brush))
            {
                return brush;
            }
            return Brushes.Transparent;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
