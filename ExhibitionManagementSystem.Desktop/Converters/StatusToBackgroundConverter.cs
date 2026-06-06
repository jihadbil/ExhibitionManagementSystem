using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using ExhibitionManagementSystem.Desktop.Helpers;

namespace ExhibitionManagementSystem.Desktop.Converters
{
    public class StatusToBackgroundConverter : IValueConverter
    {
        private static readonly Dictionary<string, Brush> _brushMap = new(StringComparer.OrdinalIgnoreCase)
        {
            { "Active", ColorHelper.BrushFromHex("#D1FAE5") },
            { "Available", ColorHelper.BrushFromHex("#D1FAE5") },
            { "Reserved", ColorHelper.BrushFromHex("#E0E7FF") },
            { "UnderReview", ColorHelper.BrushFromHex("#FEF3C7") },
            { "Soon", ColorHelper.BrushFromHex("#DBEAFE") },
            { "Ended", ColorHelper.BrushFromHex("#F3F4F6") },
            { "Cancelled", ColorHelper.BrushFromHex("#FEE2E2") }
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
