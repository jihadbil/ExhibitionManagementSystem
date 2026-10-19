using ExhibitionManagementSystem.Models.DTOs.Validation;
using System;
using System.ComponentModel.DataAnnotations;

namespace ExhibitionManagementSystem.Models.DTOs.Pricing;

public class BoothPriceRuleCreateDto
{
    public int TenantID { get; set; }
    [System.ComponentModel.DataAnnotations.Range(1, int.MaxValue)]
    public int? ExhibitionID { get; set; }
    [DefinedEnum(typeof(ExhibitionManagementSystem.Models.Enums.BoothType))]
    public string? BoothType { get; set; }
    [DefinedEnum(typeof(ExhibitionManagementSystem.Models.Enums.ExhibitorCategory))]
    public string? ExhibitorCategory { get; set; }

    [StringLength(100)]
    public string ProductCategory { get; set; } = string.Empty;

    [DecimalValue("0", "9999999999999999.99")]
    public decimal PricePerSqM { get; set; }

    [Required]
    [StringLength(3)]
    public string CurrencyCode { get; set; } = string.Empty;

    [DecimalValue("0.01", "99999999.99")]
    public decimal? MinAreaSqM { get; set; }
    [DecimalValue("0.01", "99999999.99")]
    public decimal? MaxAreaSqM { get; set; }
    public DateTime ValidFrom { get; set; }
    [DateAfter(nameof(ValidFrom))]
    public DateTime? ValidTo { get; set; }

    [StringLength(500)]
    public string Notes { get; set; } = string.Empty;

    [StringLength(150)]
    public string? RuleName { get; set; }
}
