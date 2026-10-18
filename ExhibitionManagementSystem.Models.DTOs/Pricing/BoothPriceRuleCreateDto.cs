using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using ExhibitionManagementSystem.Models.Enums;

namespace ExhibitionManagementSystem.Models.DTOs.Pricing;

public class BoothPriceRuleCreateDto : IValidatableObject
{
    public int TenantID { get; set; }
    public int? ExhibitionID { get; set; }
    public string? BoothType { get; set; }
    public string? ExhibitorCategory { get; set; }

    [StringLength(100)]
    public string ProductCategory { get; set; } = string.Empty;

    [Range(typeof(decimal), "0.01", "9999999999999999.99")]
    public decimal PricePerSqM { get; set; }

    [Required]
    [StringLength(3)]
    public string CurrencyCode { get; set; } = string.Empty;

    [Range(typeof(decimal), "0", "99999999.99")]
    public decimal? MinAreaSqM { get; set; }

    [Range(typeof(decimal), "0.01", "99999999.99")]
    public decimal? MaxAreaSqM { get; set; }
    public DateTime ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }

    [StringLength(500)]
    public string Notes { get; set; } = string.Empty;

    [StringLength(150)]
    public string? RuleName { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!string.IsNullOrWhiteSpace(BoothType) &&
            !Enum.TryParse<ExhibitionManagementSystem.Models.Enums.BoothType>(BoothType, true, out _))
        {
            yield return new ValidationResult("نوع الجناح غير صالح.", [nameof(BoothType)]);
        }

        if (!string.IsNullOrWhiteSpace(ExhibitorCategory) &&
            !Enum.TryParse<ExhibitionManagementSystem.Models.Enums.ExhibitorCategory>(ExhibitorCategory, true, out _))
        {
            yield return new ValidationResult("فئة العارض غير صالحة.", [nameof(ExhibitorCategory)]);
        }

        if (MinAreaSqM.HasValue && MaxAreaSqM.HasValue && MinAreaSqM > MaxAreaSqM)
        {
            yield return new ValidationResult(
                "الحد الأدنى للمساحة يجب ألا يتجاوز الحد الأقصى.",
                [nameof(MinAreaSqM), nameof(MaxAreaSqM)]);
        }

        if (ValidTo.HasValue && ValidTo.Value.Date < ValidFrom.Date)
        {
            yield return new ValidationResult(
                "تاريخ انتهاء القاعدة يجب ألا يسبق تاريخ بدايتها.",
                [nameof(ValidFrom), nameof(ValidTo)]);
        }
    }
}
