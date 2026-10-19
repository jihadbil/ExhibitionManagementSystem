using ExhibitionManagementSystem.Models.DTOs.Validation;
using System.ComponentModel.DataAnnotations;

namespace ExhibitionManagementSystem.Models.DTOs.Booth;

public class BoothUpdateDto
{
    [Required]
    [StringLength(20)]
    public string BoothNumber { get; set; } = string.Empty;

    [Required]
    [DefinedEnum(typeof(ExhibitionManagementSystem.Models.Enums.BoothStatus))]
    public string Status { get; set; } = string.Empty;

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
    public int? AssignedPriceRuleID { get; set; }
}
