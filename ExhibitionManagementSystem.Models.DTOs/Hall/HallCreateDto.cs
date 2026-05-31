using System.ComponentModel.DataAnnotations;

namespace ExhibitionManagementSystem.Models.DTOs.Hall;

public class HallCreateDto
{
    public int VenueID { get; set; }

    [Required]
    [StringLength(200)]
    public string HallName { get; set; } = string.Empty;

    public decimal? AreaSqM { get; set; }
    public int? MaxBooths { get; set; }
    public decimal? FloorPlanWidth { get; set; }
    public decimal? FloorPlanHeight { get; set; }
    public string FloorPlanJSON { get; set; } = string.Empty;
}
