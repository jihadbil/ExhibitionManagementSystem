using System;
using ExhibitionManagementSystem.Models.DTOs.Common;

namespace ExhibitionManagementSystem.Models.DTOs.Pricing;

public class BoothPriceRuleDto : AuditDto
{
    public int RuleID { get; set; }
    public int TenantID { get; set; }
    public int? ExhibitionID { get; set; }
    public string? ExhibitionName { get; set; }
    public string? BoothType { get; set; }
    public string? ExhibitorCategory { get; set; }
    public string ProductCategory { get; set; } = string.Empty;
    public decimal PricePerSqM { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public decimal? MinAreaSqM { get; set; }
    public decimal? MaxAreaSqM { get; set; }
    public DateTime ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
    public string Notes { get; set; } = string.Empty;
}
