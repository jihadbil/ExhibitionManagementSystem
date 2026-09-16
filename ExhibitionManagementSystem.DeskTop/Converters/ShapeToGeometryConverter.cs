using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace ExhibitionManagementSystem.DeskTop.Converters;

public class ShapeToGeometryConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values == null || values.Length < 3)
            return Geometry.Empty;

        string shapeType = values[0]?.ToString() ?? "Rectangle";
        double width = values[1] is double w ? w : 80;
        double height = values[2] is double h ? h : 60;

        // Prevent negative or zero dimension geometry crashes
        if (width <= 0) width = 80;
        if (height <= 0) height = 60;

        return shapeType.ToLower() switch
        {
            "triangle" or "مثلث" => PathGeometry.Parse($"M 0,{height} L {width / 2},0 L {width},{height} Z"),
            "lshape" or "حرف l" or "زاوية قائمة" => PathGeometry.Parse($"M 0,0 L {width},0 L {width},{height / 2} L {width / 2},{height / 2} L {width / 2},{height} L 0,{height} Z"),
            "trapezoid" or "شبه منحرف" => PathGeometry.Parse($"M {width * 0.25},0 L {width * 0.75},0 L {width},{height} L 0,{height} Z"),
            _ => new RectangleGeometry(new Rect(0, 0, width, height), 6, 6)
        };
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
