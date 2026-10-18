using System.ComponentModel.DataAnnotations;

namespace ExhibitionManagementSystem.Models.DTOs.Auth;

public sealed class CompanyRegistrationRequestDto
{
    [Required]
    [StringLength(200, MinimumLength = 2)]
    public string CompanyName { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    [RegularExpression("^[a-z0-9](?:[a-z0-9-]{0,61}[a-z0-9])?$",
        ErrorMessage = "النطاق المختصر يجب أن يحتوي أحرفاً لاتينية صغيرة وأرقاماً وشرطات فقط.")]
    public string Subdomain { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string AdminFullName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(256)]
    public string AdminEmail { get; set; } = string.Empty;

    [Phone]
    [StringLength(30)]
    public string? AdminPhoneNumber { get; set; }

    [Required]
    [StringLength(100, MinimumLength = 8)]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required]
    [Compare(nameof(Password), ErrorMessage = "كلمتا المرور غير متطابقتين.")]
    [DataType(DataType.Password)]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required]
    [StringLength(3, MinimumLength = 3)]
    [RegularExpression("^[A-Z]{3}$", ErrorMessage = "رمز العملة غير صالح.")]
    public string BaseCurrency { get; set; } = "LYD";

    [Range(typeof(bool), "true", "true",
        ErrorMessage = "يجب الموافقة على شروط الاستخدام وسياسة الخصوصية.")]
    public bool AcceptTerms { get; set; }
}

public sealed class CompanyRegistrationResponseDto
{
    public int TenantID { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string Subdomain { get; set; } = string.Empty;
    public string AdminEmail { get; set; } = string.Empty;
    public DateTime TrialEndsAt { get; set; }
    public string Message { get; set; } = string.Empty;
}
