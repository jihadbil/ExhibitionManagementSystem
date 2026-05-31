using System.ComponentModel.DataAnnotations;

namespace ExhibitionManagementSystem.Models.DTOs.Booth;

public class BoothCreateDto
{
    public int HallID { get; set; }

    [Required]
    [StringLength(20)]
    public string BoothNumber { get; set; } = string.Empty;

    public decimal OriginalAreaSqM { get; set; }
    public decimal? PosX { get; set; }
    public decimal? PosY { get; set; }
    public decimal? Width { get; set; }
    public decimal? Height { get; set; }
    public decimal? RotationAngle { get; set; }
    public string? ShapeType { get; set; }
    public string ShapePolygonJSON { get; set; } = string.Empty;
}
