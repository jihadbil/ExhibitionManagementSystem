using System;
using ExhibitionManagementSystem.Models.DTOs.Common;

namespace ExhibitionManagementSystem.Models.DTOs.Pricing;

public class ServicePriceRuleDto : AuditDto
{
    public int RuleID { get; set; }
    public int TenantID { get; set; }
    public int ServiceID { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public int? ExhibitionID { get; set; }
    public string? ExhibitionName { get; set; }
    public string? ExhibitorCategory { get; set; }
    public decimal UnitPrice { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public DateTime ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
    public string Notes { get; set; } = string.Empty;
}
