using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ExhibitionManagementSystem.Models.DTOs.Pricing;

public class ServicePriceRuleUpdateDto
{
    public string ExhibitorCategory { get; set; } = string.Empty;
    [Column(TypeName = "decimal(18,2)")] public decimal UnitPrice { get; set; }
    [StringLength(3)] public string CurrencyCode { get; set; } = string.Empty;
    public int? MinQuantity { get; set; }
    public int? MaxQuantity { get; set; }
    public bool IsActive { get; set; } = true;
}
