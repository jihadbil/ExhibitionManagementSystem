using System.ComponentModel.DataAnnotations;

namespace ExhibitionManagementSystem.Models.DTOs.Venue;

public sealed class VenueSitePlanDto
{
    public int VenueID { get; set; }
    public string PlanJSON { get; set; } = string.Empty;
    public DateTime? UpdatedAt { get; set; }
    public long Version { get; set; }
}

public sealed class VenueSitePlanSaveDto
{
    [Required]
    public string PlanJSON { get; set; } = string.Empty;
    public long? ExpectedVersion { get; set; }
}
