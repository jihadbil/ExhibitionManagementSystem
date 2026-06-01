using System;

namespace ExhibitionManagementSystem.Models.DTOs.Financial;

public class ExchangeRateDto
{
    public int ExchangeRateID { get; set; }
    public string FromCurrency { get; set; } = string.Empty;
    public string ToCurrency { get; set; } = string.Empty;
    public decimal Rate { get; set; }
    public DateTime RateDate { get; set; }
    public string Source { get; set; } = string.Empty;
}
