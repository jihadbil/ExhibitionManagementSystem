using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ExhibitionManagementSystem.Models.DTOs.Pricing;

public class PricingPackageUpdateDto
{
    [Required, StringLength(100)] public string PackageName { get; set; } = string.Empty;
    [StringLength(500)] public string? Description { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal BasePrice { get; set; }
    [StringLength(3)] public string CurrencyCode { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
