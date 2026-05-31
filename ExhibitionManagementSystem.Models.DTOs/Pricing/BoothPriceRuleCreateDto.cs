using System;
using System.ComponentModel.DataAnnotations;

namespace ExhibitionManagementSystem.Models.DTOs.Pricing;

public class BoothPriceRuleCreateDto
{
    public int TenantID { get; set; }
    public int? ExhibitionID { get; set; }
    public string? BoothType { get; set; }
    public string? ExhibitorCategory { get; set; }

    [StringLength(100)]
    public string ProductCategory { get; set; } = string.Empty;

    public decimal PricePerSqM { get; set; }

    [Required]
    [StringLength(3)]
    public string CurrencyCode { get; set; } = string.Empty;

    public decimal? MinAreaSqM { get; set; }
    public decimal? MaxAreaSqM { get; set; }
    public DateTime ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }

    [StringLength(500)]
    public string Notes { get; set; } = string.Empty;
}
