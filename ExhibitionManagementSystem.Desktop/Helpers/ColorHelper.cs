using System;
using System.Windows.Media;

namespace ExhibitionManagementSystem.Desktop.Helpers
{
    public static class ColorHelper
    {
        public static Color FromHex(string hex)
        {
            if (string.IsNullOrWhiteSpace(hex))
                throw new ArgumentException("Hex color string cannot be null or empty.", nameof(hex));

            hex = hex.Trim().TrimStart('#');

            if (hex.Length != 6 && hex.Length != 8)
                throw new ArgumentException("Hex color string must be 6 or 8 characters long.", nameof(hex));

            byte a = 255;
            int index = 0;

            if (hex.Length == 8)
            {
                a = Convert.ToByte(hex.Substring(0, 2), 16);
                index = 2;
            }

            byte r = Convert.ToByte(hex.Substring(index, 2), 16);
            byte g = Convert.ToByte(hex.Substring(index + 2, 2), 16);
            byte b = Convert.ToByte(hex.Substring(index + 4, 2), 16);

            return Color.FromArgb(a, r, g, b);
        }

        public static SolidColorBrush BrushFromHex(string hex)
        {
            var brush = new SolidColorBrush(FromHex(hex));
            brush.Freeze(); // Freeze for performance
            return brush;
        }
    }
}
