namespace ExhibitionManagementSystem.Models.DTOs.Exhibitor;

public class ExhibitorSummaryDto
{
    public int ExhibitorID { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string Sector { get; set; } = string.Empty;
    public string Nationality { get; set; } = string.Empty;
    public string ExhibitorCategory { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
