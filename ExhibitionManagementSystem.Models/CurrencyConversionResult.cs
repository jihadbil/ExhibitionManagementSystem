namespace ExhibitionManagementSystem.Models;

/// <summary>
/// لقطة نتيجة تحويل مالي قابلة للحفظ مع المعاملة.
/// </summary>
public sealed class CurrencyConversionResult
{
    public decimal SourceAmount { get; init; }
    public string SourceCurrencyCode { get; init; } = string.Empty;
    public decimal TargetAmount { get; init; }
    public string TargetCurrencyCode { get; init; } = string.Empty;
    public decimal ExchangeRate { get; init; }
    public DateTime RateDate { get; init; }
    public string Source { get; init; } = string.Empty;
}
