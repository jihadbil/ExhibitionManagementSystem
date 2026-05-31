using System;

namespace ExhibitionManagementSystem.Models.DTOs.Exhibition;

public class ExhibitionSummaryDto
{
    public int ExhibitionID { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string VenueName { get; set; } = string.Empty;
}
