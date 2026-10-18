using System.Text.Json;
using ExhibitionManagementSystem.Models;
using ExhibitionManagementSystem.Models.DTOs.Booth;

namespace ExhibitionManagementSystem.Services.Common;

internal static class HallDrawingSafety
{
    public static bool IsPositioned(Booth booth) => booth.PosX.HasValue && booth.PosY.HasValue &&
        booth.Width.HasValue && booth.Height.HasValue;

    public static BoothLayoutItemDto Item(Booth booth) => new()
    {
        BoothID = booth.BoothID, BoothNumber = booth.BoothNumber,
        PosX = booth.PosX ?? 0, PosY = booth.PosY ?? 0,
        Width = booth.Width ?? 0, Height = booth.Height ?? 0,
        RotationAngle = booth.RotationAngle ?? 0,
        ShapeType = booth.ShapeType?.ToString() ?? "Rect",
        ShapePolygonJSON = booth.ShapePolygonJSON
    };

    public static (string Message, string Code)? Validate(IEnumerable<Booth> booths, string? siteJson,
        int hallId, decimal? width, decimal? height, string? floorJson)
    {
        try
        {
            var obstacles = ReadObstacles(floorJson, width, height);
            var positioned = booths.Where(IsPositioned).ToList();
            if (positioned.Count == 0) return null;
            var boundary = BoothDrawingGeometry.HallBoundary(siteJson, hallId, width ?? 0, height ?? 0);
            if (boundary.Width <= 0 || boundary.Height <= 0)
                return ("يجب تحديد حدود القاعة قبل حفظ الأجنحة", "HALL_DIMENSIONS_REQUIRED");
            obstacles.AddRange(BoothDrawingGeometry.SiteObstacles(siteJson, hallId, boundary.Points));
            var geometry = new List<(Booth Booth, IReadOnlyList<DrawingPoint> Points)>();
            foreach (var booth in positioned)
            {
                if (booth.Width <= 0 || booth.Height <= 0 || booth.PosX < 0 || booth.PosY < 0)
                    return ("أبعاد أو إحداثيات الجناح غير صالحة", "INVALID_BOOTH_GEOMETRY");
                var points = BoothDrawingGeometry.BoothPoints(Item(booth));
                if (!BoothDrawingGeometry.Inside(points, boundary.Points))
                    return ($"الجناح {booth.BoothNumber} خارج حدود القاعة الجديدة", "BOOTH_OUT_OF_BOUNDS");
                if (obstacles.Any(obstacle => BoothDrawingGeometry.Overlap(points, obstacle)))
                    return ($"الجناح {booth.BoothNumber} يتداخل مع جدار أو منطقة محجوزة", "BOOTH_BLOCKS_ACCESS_AREA");
                if (geometry.Any(other => BoothDrawingGeometry.Overlap(points, other.Points)))
                    return ($"الجناح {booth.BoothNumber} يتداخل مع جناح آخر", "BOOTH_COLLISION");
                geometry.Add((booth, points));
            }
            return null;
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException or KeyNotFoundException or OverflowException)
        { return ("بنية المخطط أو هندسة الأجنحة غير صالحة", "INVALID_FLOOR_PLAN"); }
    }

    private static List<IReadOnlyList<DrawingPoint>> ReadObstacles(string? json, decimal? width, decimal? height)
    {
        var result = new List<IReadOnlyList<DrawingPoint>>();
        if (string.IsNullOrWhiteSpace(json)) return result;
        if (json.Length > 2_000_000) throw new JsonException("Plan exceeds the supported size.");
        using var document = JsonDocument.Parse(json);
        if (document.RootElement.ValueKind != JsonValueKind.Object) throw new JsonException("Plan must be an object.");
        if (!document.RootElement.TryGetProperty("elements", out var elements)) return result;
        if (elements.ValueKind != JsonValueKind.Array) throw new JsonException("Invalid plan elements.");
        foreach (var element in elements.EnumerateArray())
        {
            if (element.ValueKind != JsonValueKind.Object) throw new JsonException("Invalid plan element.");
            if (!element.TryGetProperty("type", out var type) || type.ValueKind != JsonValueKind.String)
                throw new JsonException("Missing element type.");
            if (type.GetString() is not ("aisle" or "column" or "restricted")) continue;
            var x = element.GetProperty("x").GetDecimal(); var y = element.GetProperty("y").GetDecimal();
            var w = element.GetProperty("width").GetDecimal(); var h = element.GetProperty("height").GetDecimal();
            var rotation = element.TryGetProperty("rotation", out var angle) ? angle.GetDecimal() : 0;
            if (x < 0 || y < 0 || w <= 0 || h <= 0) throw new JsonException("Invalid element dimensions.");
            var points = BoothDrawingGeometry.Rectangle(x, y, w, h, rotation);
            if (width > 0 && height > 0 && points.Any(p => p.X < 0 || p.Y < 0 || p.X > width || p.Y > height))
                throw new JsonException("Element outside hall bounds.");
            result.Add(points);
        }
        return result;
    }
}
