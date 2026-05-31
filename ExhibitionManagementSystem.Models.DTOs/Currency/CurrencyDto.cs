namespace ExhibitionManagementSystem.Models.DTOs.Currency;

public class CurrencyDto
{
    public string CurrencyCode { get; set; } = string.Empty;
    public string CurrencyName { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
