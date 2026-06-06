using System.Windows;
using System.Windows.Media;

namespace ExhibitionManagementSystem.Desktop.Helpers
{
    public static class GradientHelper
    {
        public static LinearGradientBrush CreateHorizontal(string fromHex, string toHex)
        {
            var brush = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0.5),
                EndPoint = new Point(1, 0.5)
            };
            brush.GradientStops.Add(new GradientStop(ColorHelper.FromHex(fromHex), 0.0));
            brush.GradientStops.Add(new GradientStop(ColorHelper.FromHex(toHex), 1.0));
            brush.Freeze(); // Freeze for performance
            return brush;
        }
    }
}
