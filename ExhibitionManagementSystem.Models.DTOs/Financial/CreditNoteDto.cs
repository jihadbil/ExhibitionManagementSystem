namespace ExhibitionManagementSystem.Models.DTOs.Financial;

public sealed class CreditNoteDto
{
    public int CreditNoteID { get; set; }
    public int InvoiceID { get; set; }
    public string CreditNoteNumber { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string CreatedByUserId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
