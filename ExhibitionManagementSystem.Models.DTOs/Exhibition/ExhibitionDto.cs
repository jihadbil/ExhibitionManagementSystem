using System;
using ExhibitionManagementSystem.Models.DTOs.Common;

namespace ExhibitionManagementSystem.Models.DTOs.Exhibition;

public class ExhibitionDto : AuditDto
{
    public int ExhibitionID { get; set; }
    public int TenantID { get; set; }
    public int VenueID { get; set; }
    public string VenueName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Edition { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int? ExpectedVisitors { get; set; }
    public decimal? EntryFee { get; set; }
    public string EntryCurrency { get; set; } = string.Empty;
    public string CurrencySymbol { get; set; } = string.Empty;
}
