namespace ExhibitionManagementSystem.Models.DTOs.Service;

public class ServiceSummaryDto
{
    public int ServiceID { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public decimal? DefaultPrice { get; set; }
    public bool IsMandatory { get; set; }
    public bool IsActive { get; set; }
}
