using System.ComponentModel.DataAnnotations;

namespace ExhibitionManagementSystem.Models.DTOs.Booth;

public sealed class BoothLayoutSaveDto
{
    [Required]
    public List<BoothLayoutItemDto> Booths { get; set; } = [];

    public List<int> DeletedBoothIDs { get; set; } = [];

    public string? FloorPlanJSON { get; set; }
    public string? ExpectedFloorPlanJSON { get; set; }
    public long? ExpectedSitePlanVersion { get; set; }
}

public sealed class BoothLayoutItemDto
{
    public int? BoothID { get; set; }

    [Required, StringLength(20)]
    public string BoothNumber { get; set; } = string.Empty;

    [Range(typeof(decimal), "0", "100000")]
    public decimal PosX { get; set; }

    [Range(typeof(decimal), "0", "100000")]
    public decimal PosY { get; set; }

    [Range(typeof(decimal), "0.1", "100000")]
    public decimal Width { get; set; }

    [Range(typeof(decimal), "0.1", "100000")]
    public decimal Height { get; set; }

    [Range(typeof(decimal), "-360", "360")]
    public decimal RotationAngle { get; set; }

    public string ShapeType { get; set; } = "Rect";
    public string BoothType { get; set; } = "Standard";
    public string ShapePolygonJSON { get; set; } = string.Empty;
    public int? AssignedPriceRuleID { get; set; }
}

public sealed class BoothLayoutSaveResultDto
{
    public IList<BoothDto> Booths { get; set; } = [];
    public DateTime SavedAtUtc { get; set; }
    public string FloorPlanJSON { get; set; } = string.Empty;
}
