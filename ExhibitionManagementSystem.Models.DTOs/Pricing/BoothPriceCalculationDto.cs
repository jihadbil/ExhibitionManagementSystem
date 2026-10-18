namespace ExhibitionManagementSystem.Models.DTOs.Pricing;

public sealed class BoothPriceCalculationDto
{
    public decimal Amount { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public int RuleID { get; set; }
}
