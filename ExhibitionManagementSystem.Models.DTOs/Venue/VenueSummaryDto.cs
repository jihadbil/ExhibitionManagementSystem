namespace ExhibitionManagementSystem.Models.DTOs.Venue;

public class VenueSummaryDto
{
    public int VenueID { get; set; }
    public string Name { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int HallsCount { get; set; }
}
