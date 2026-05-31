using System;
using ExhibitionManagementSystem.Models.DTOs.Common;

namespace ExhibitionManagementSystem.Models.DTOs.Hall;

public class HallDto : AuditDto
{
    public int HallID { get; set; }
    public int VenueID { get; set; }
    public string VenueName { get; set; } = string.Empty;
    public string HallName { get; set; } = string.Empty;
    public decimal? AreaSqM { get; set; }
    public int? MaxBooths { get; set; }
    public decimal? FloorPlanWidth { get; set; }
    public decimal? FloorPlanHeight { get; set; }
    public string FloorPlanJSON { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int BoothsCount { get; set; }
}
