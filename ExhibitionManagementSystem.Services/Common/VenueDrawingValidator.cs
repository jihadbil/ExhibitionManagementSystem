using System.Text.Json;
using System.Text.RegularExpressions;

namespace ExhibitionManagementSystem.Services.Common;

/// <summary>Validates the v4 drawing before storing it; older plans retain their existing validation.</summary>
internal static partial class VenueDrawingValidator
{
    private static readonly HashSet<string> Kinds = ["hall", "wall", "line", "rectangle", "ellipse", "polygon", "freehand", "text", "door", "dimension", "exit", "column"];
    private static readonly HashSet<string> Layers = ["halls", "structure", "movement", "annotations"];
    private readonly record struct Point(double X, double Y);

    public static bool IsValid(JsonElement root, HashSet<int> ownedHallIds, double pageWidth, double pageHeight)
    {
        if (!root.TryGetProperty("schemaVersion", out var version) || !version.TryGetInt32(out var schema) || schema != 4)
            return !root.TryGetProperty("shapes", out _);
        if (!root.TryGetProperty("shapes", out var shapes) || shapes.ValueKind != JsonValueKind.Array || shapes.GetArrayLength() > 3000
            || !root.TryGetProperty("grid", out var grid) || !Number(grid, "size", out var gridSize) || gridSize < 0.1 || gridSize > 10 || !Boolean(grid, "snap") || !Boolean(grid, "visible")
            || !root.TryGetProperty("layers", out var layers) || layers.ValueKind != JsonValueKind.Object)
            return false;
        foreach (var name in Layers)
            if (!layers.TryGetProperty(name, out var layerState) || !Boolean(layerState, "visible") || !Boolean(layerState, "locked")) return false;
        var ids = new HashSet<string>(StringComparer.Ordinal);
        var hallIds = new HashSet<int>();
        var totalPoints = 0;
        foreach (var shape in shapes.EnumerateArray())
        {
            if (shape.ValueKind != JsonValueKind.Object || !Text(shape, "id", out var id) || id.Length is < 1 or > 100 || !ids.Add(id)
                || !Text(shape, "kind", out var kind) || !Kinds.Contains(kind)
                || !Text(shape, "layer", out var layer) || !Layers.Contains(layer)
                || !Number(shape, "x", out var x) || !Number(shape, "y", out var y)
                || !Number(shape, "width", out var width) || width <= 0 || width > 10000
                || !Number(shape, "height", out var height) || height <= 0 || height > 10000
                || !Number(shape, "rotation", out var rotation) || rotation < 0 || rotation >= 360
                || !Number(shape, "strokeWidth", out var strokeWidth) || strokeWidth < 0.02 || strokeWidth > 2
                || !Number(shape, "fontSize", out var fontSize) || fontSize < 0.2 || fontSize > 10
                || !Text(shape, "stroke", out var stroke) || !Color().IsMatch(stroke)
                || !Text(shape, "fill", out var fill) || (fill != "none" && !Color().IsMatch(fill))
                || !Text(shape, "label", out var label) || label.Length > 200 || !Boolean(shape, "locked")
                || !shape.TryGetProperty("points", out var pointsJson) || pointsJson.ValueKind != JsonValueKind.Array
                || pointsJson.GetArrayLength() is < 2 or > 2000)
                return false;
            var points = new List<Point>();
            foreach (var point in pointsJson.EnumerateArray())
            {
                if (!Number(point, "x", out var px) || !Number(point, "y", out var py)
                    || px < -0.005 || py < -0.005 || px > width + 0.005 || py > height + 0.005) return false;
                points.Add(new Point(px, py));
            }
            totalPoints += points.Count;
            if (totalPoints > 50000) return false;
            if (kind is "hall" or "polygon")
            {
                if (points.Count is < 3 or > 512 || Area(points) < 0.1 || SelfIntersects(points)) return false;
            }
            else if (kind is "wall" or "line" or "dimension" or "freehand")
            {
                var length = points.Skip(1).Select((p, i) => Math.Sqrt(Math.Pow(p.X - points[i].X, 2) + Math.Pow(p.Y - points[i].Y, 2))).Sum();
                if (length < 0.1) return false;
            }
            if (kind == "hall")
            {
                if (layer != "halls" || !shape.TryGetProperty("hallID", out var hallIdJson) || !hallIdJson.TryGetInt32(out var hallId)
                    || !ownedHallIds.Contains(hallId) || !hallIds.Add(hallId)) return false;
            }
            var radians = rotation * Math.PI / 180;
            if (kind is "ellipse" or "column")
            {
                var rx = width / 2; var ry = height / 2;
                var extentX = Math.Sqrt(Math.Pow(rx * Math.Cos(radians), 2) + Math.Pow(ry * Math.Sin(radians), 2));
                var extentY = Math.Sqrt(Math.Pow(rx * Math.Sin(radians), 2) + Math.Pow(ry * Math.Cos(radians), 2));
                if (!Inside(x + rx - extentX, y + ry - extentY, pageWidth, pageHeight) || !Inside(x + rx + extentX, y + ry + extentY, pageWidth, pageHeight)) return false;
            }
            else
            {
                var path = kind is "hall" or "polygon" or "wall" or "line" or "dimension" or "freehand";
                var geometry = path ? points : [new Point(0, 0), new Point(width, 0), new Point(width, height), new Point(0, height)];
                foreach (var p in geometry)
                {
                    var dx = p.X - width / 2; var dy = p.Y - height / 2;
                    if (!Inside(x + width / 2 + dx * Math.Cos(radians) - dy * Math.Sin(radians), y + height / 2 + dx * Math.Sin(radians) + dy * Math.Cos(radians), pageWidth, pageHeight)) return false;
                }
            }
        }
        var exportedHalls = new List<int>();
        foreach (var hall in root.GetProperty("halls").EnumerateArray())
        {
            if (hall.ValueKind != JsonValueKind.Object || !hall.TryGetProperty("hallID", out var hallId) || hallId.ValueKind != JsonValueKind.Number || !hallId.TryGetInt32(out var id)) return false;
            exportedHalls.Add(id);
        }
        return exportedHalls.Count == hallIds.Count && exportedHalls.Distinct().Count() == exportedHalls.Count && hallIds.SetEquals(exportedHalls);
    }

    private static bool Inside(double x, double y, double w, double h) => x >= -0.005 && y >= -0.005 && x <= w + 0.005 && y <= h + 0.005;
    private static bool Number(JsonElement element, string name, out double value)
    {
        value = 0;
        return element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out var field) && field.ValueKind == JsonValueKind.Number && field.TryGetDouble(out value) && double.IsFinite(value);
    }
    private static bool Text(JsonElement element, string name, out string value)
    {
        value = string.Empty;
        if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(name, out var field) || field.ValueKind != JsonValueKind.String) return false;
        value = field.GetString()!; return true;
    }
    private static bool Boolean(JsonElement element, string name) => element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out var field) && field.ValueKind is JsonValueKind.True or JsonValueKind.False;
    private static double Area(List<Point> points) => Math.Abs(points.Select((p, i) => p.X * points[(i + 1) % points.Count].Y - points[(i + 1) % points.Count].X * p.Y).Sum()) / 2;
    private static double Cross(Point a, Point b, Point c) => (b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X);
    private static bool OnSegment(Point a, Point b, Point p) => Math.Abs(Cross(a, b, p)) < 1e-8 && p.X >= Math.Min(a.X, b.X) - 1e-8 && p.X <= Math.Max(a.X, b.X) + 1e-8 && p.Y >= Math.Min(a.Y, b.Y) - 1e-8 && p.Y <= Math.Max(a.Y, b.Y) + 1e-8;
    private static bool SelfIntersects(List<Point> p)
    {
        for (var i = 0; i < p.Count; i++) for (var j = i + 1; j < p.Count; j++)
        {
            if (j == i + 1 || (i == 0 && j == p.Count - 1)) continue;
            var a = p[i]; var b = p[(i + 1) % p.Count]; var c = p[j]; var d = p[(j + 1) % p.Count];
            if ((Cross(a, b, c) * Cross(a, b, d) < 0 && Cross(c, d, a) * Cross(c, d, b) < 0) || OnSegment(a, b, c) || OnSegment(a, b, d) || OnSegment(c, d, a) || OnSegment(c, d, b)) return true;
        }
        return false;
    }
    [GeneratedRegex("^#[0-9a-fA-F]{6}$")]
    private static partial Regex Color();
}
