using System;
using System.ComponentModel.DataAnnotations;

namespace ExhibitionManagementSystem.Models.DTOs.Tenant;

public class TenantCreateDto : IValidatableObject
{
    [Required]
    [StringLength(200)]
    public string CompanyName { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    [RegularExpression("^[a-z0-9](?:[a-z0-9-]{0,61}[a-z0-9])?$",
        ErrorMessage = "النطاق الفرعي يجب أن يحتوي أحرفًا لاتينية صغيرة وأرقامًا وشرطات فقط.")]
    public string Subdomain { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string Plan { get; set; } = string.Empty;

    [Required]
    [StringLength(3, MinimumLength = 3)]
    [RegularExpression("^[A-Z]{3}$")]
    public string BaseCurrency { get; set; } = string.Empty;

    public DateTime? TrialEndsAt { get; set; }

    [Required]
    [EmailAddress]
    [StringLength(256)]
    public string AdminEmail { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string AdminFullName { get; set; } = string.Empty;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (TrialEndsAt.HasValue && TrialEndsAt.Value.Date <= DateTime.UtcNow.Date)
        {
            yield return new ValidationResult(
                "يجب أن يكون تاريخ انتهاء الفترة التجريبية في المستقبل.",
                [nameof(TrialEndsAt)]);
        }
    }
}
