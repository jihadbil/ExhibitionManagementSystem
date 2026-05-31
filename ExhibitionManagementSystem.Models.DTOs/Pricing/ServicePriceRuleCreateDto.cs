using System;
using System.ComponentModel.DataAnnotations;

namespace ExhibitionManagementSystem.Models.DTOs.Pricing;

public class ServicePriceRuleCreateDto
{
    public int TenantID { get; set; }
    public int ServiceID { get; set; }
    public int? ExhibitionID { get; set; }
    public string? ExhibitorCategory { get; set; }
    public decimal UnitPrice { get; set; }

    [Required]
    [StringLength(3)]
    public string CurrencyCode { get; set; } = string.Empty;

    public DateTime ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }

    [StringLength(500)]
    public string Notes { get; set; } = string.Empty;
}
