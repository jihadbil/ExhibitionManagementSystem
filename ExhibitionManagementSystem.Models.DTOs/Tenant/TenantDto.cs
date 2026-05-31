using System;
using ExhibitionManagementSystem.Models.DTOs.Common;

namespace ExhibitionManagementSystem.Models.DTOs.Tenant;

public class TenantDto : AuditDto
{
    public int TenantID { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string Subdomain { get; set; } = string.Empty;
    public string Plan { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime? TrialEndsAt { get; set; }
    public string BaseCurrency { get; set; } = string.Empty;
    public string CurrencySymbol { get; set; } = string.Empty;
}
