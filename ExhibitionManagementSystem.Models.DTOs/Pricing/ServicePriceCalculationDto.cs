namespace ExhibitionManagementSystem.Models.DTOs.Pricing;

/// <summary>
/// نتيجة تسعير خدمة مع الاحتفاظ بعملة المصدر وقاعدة التسعير المستخدمة.
/// </summary>
public sealed class ServicePriceCalculationDto
{
    public decimal UnitPrice { get; set; }
    public decimal TotalAmount { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public int? PriceRuleID { get; set; }
}
