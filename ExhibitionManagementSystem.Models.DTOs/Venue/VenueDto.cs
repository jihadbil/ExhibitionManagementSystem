using System;
using ExhibitionManagementSystem.Models.DTOs.Common;

namespace ExhibitionManagementSystem.Models.DTOs.Venue;

public class VenueDto : AuditDto
{
    public int VenueID { get; set; }
    public int TenantID { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public int? TotalCapacity { get; set; }
    public string MapImageURL { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int HallsCount { get; set; }
}
