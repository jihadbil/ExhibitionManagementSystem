namespace ExhibitionManagementSystem.Models.DTOs.Booth;

public class BoothSummaryDto
{
    public int BoothID { get; set; }
    public int HallID { get; set; }
    public string BoothNumber { get; set; } = string.Empty;
    public decimal CurrentAreaSqM { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool IsMerged { get; set; }
}
