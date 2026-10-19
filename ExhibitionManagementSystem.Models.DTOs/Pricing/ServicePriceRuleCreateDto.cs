using ExhibitionManagementSystem.Models.DTOs.Validation;
using System;
using System.ComponentModel.DataAnnotations;

namespace ExhibitionManagementSystem.Models.DTOs.Pricing;

public class ServicePriceRuleCreateDto
{
    public int TenantID { get; set; }
    [System.ComponentModel.DataAnnotations.Range(1, int.MaxValue)]
    public int ServiceID { get; set; }
    [System.ComponentModel.DataAnnotations.Range(1, int.MaxValue)]
    public int? ExhibitionID { get; set; }
    [DefinedEnum(typeof(ExhibitionManagementSystem.Models.Enums.ExhibitorCategory))]
    public string? ExhibitorCategory { get; set; }
    [DecimalValue("0", "9999999999999999.99")]
    public decimal UnitPrice { get; set; }

    [Required]
    [StringLength(3)]
    public string CurrencyCode { get; set; } = string.Empty;

    public DateTime ValidFrom { get; set; }
    [DateAfter(nameof(ValidFrom))]
    public DateTime? ValidTo { get; set; }

    [StringLength(500)]
    public string Notes { get; set; } = string.Empty;
}
