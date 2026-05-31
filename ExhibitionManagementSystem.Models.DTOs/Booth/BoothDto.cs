using System;
using ExhibitionManagementSystem.Models.DTOs.Common;

namespace ExhibitionManagementSystem.Models.DTOs.Booth;

public class BoothDto : AuditDto
{
    public int BoothID { get; set; }
    public int HallID { get; set; }
    public string HallName { get; set; } = string.Empty;
    public string BoothNumber { get; set; } = string.Empty;
    public decimal OriginalAreaSqM { get; set; }
    public decimal CurrentAreaSqM { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool IsMerged { get; set; }
    public int? MergeID { get; set; }
    public decimal? PosX { get; set; }
    public decimal? PosY { get; set; }
    public decimal? Width { get; set; }
    public decimal? Height { get; set; }
    public decimal? RotationAngle { get; set; }
    public string? ShapeType { get; set; }
    public string ShapePolygonJSON { get; set; } = string.Empty;
}
