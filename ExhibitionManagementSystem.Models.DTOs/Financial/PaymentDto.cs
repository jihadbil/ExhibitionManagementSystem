using System;
using ExhibitionManagementSystem.Models.DTOs.Common;

namespace ExhibitionManagementSystem.Models.DTOs.Financial;

public class PaymentDto : AuditDto
{
    public int PaymentID { get; set; }
    public int InvoiceID { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime PaymentDate { get; set; }
    public decimal Amount { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public string CurrencySymbol { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public string ReferenceNo { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public string ReceivedByUserId { get; set; } = string.Empty;
    public string ReceivedByName { get; set; } = string.Empty;
}
