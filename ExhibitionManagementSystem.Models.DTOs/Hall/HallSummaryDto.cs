namespace ExhibitionManagementSystem.Models.DTOs.Hall;

public class HallSummaryDto
{
    public int HallID { get; set; }
    public int VenueID { get; set; }
    public string HallName { get; set; } = string.Empty;
    public decimal? AreaSqM { get; set; }
    public bool IsActive { get; set; }
    public int BoothsCount { get; set; }
}
