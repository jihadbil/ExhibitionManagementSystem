using ExhibitionManagementSystem.Models.DTOs.Validation;
using System.ComponentModel.DataAnnotations;

namespace ExhibitionManagementSystem.Models.DTOs.Booth;

public class BoothCreateDto
{
    [System.ComponentModel.DataAnnotations.Range(1, int.MaxValue)]
    public int HallID { get; set; }

    [Required]
    [StringLength(20)]
    public string BoothNumber { get; set; } = string.Empty;

    [DecimalValue("0.01", "99999999.99")]
    public decimal OriginalAreaSqM { get; set; }
    [DecimalValue("-99999999.99", "99999999.99")]
    public decimal? PosX { get; set; }
    [DecimalValue("-99999999.99", "99999999.99")]
    public decimal? PosY { get; set; }
    [DecimalValue("0.01", "99999999.99")]
    public decimal? Width { get; set; }
    [DecimalValue("0.01", "99999999.99")]
    public decimal? Height { get; set; }
    [DecimalValue("-999.99", "999.99")]
    public decimal? RotationAngle { get; set; }
    public string? ShapeType { get; set; }
    public string ShapePolygonJSON { get; set; } = string.Empty;
    [System.ComponentModel.DataAnnotations.Range(1, int.MaxValue)]
    public int? AssignedPriceRuleID { get; set; }
}
