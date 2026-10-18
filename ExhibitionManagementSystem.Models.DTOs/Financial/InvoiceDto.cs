using System;
using System.Collections.Generic;
using ExhibitionManagementSystem.Models.DTOs.Common;

namespace ExhibitionManagementSystem.Models.DTOs.Financial;

public class InvoiceDto : AuditDto
{
    public int InvoiceID { get; set; }
    public int TenantID { get; set; }
    public int ReservationID { get; set; }
    public string ExhibitorName { get; set; } = string.Empty;
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    public decimal SubTotal { get; set; }
    public decimal TaxRate { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal AdjustedTotalAmount { get; set; }
    public decimal RefundedAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public decimal AdjustedTotalAmountInBaseCurrency { get; set; }
    public decimal PaidAmountInBaseCurrency { get; set; }
    public decimal RemainingAmountInBaseCurrency { get; set; }
    public bool IsOverdue { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public string BaseCurrencyCode { get; set; } = string.Empty;
    public decimal ExchangeRateUsed { get; set; }
    public DateTime? ExchangeRateDate { get; set; }
    public decimal TotalAmountInBaseCurrency { get; set; }
    public string CurrencySymbol { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? DueDate { get; set; }
    public string Notes { get; set; } = string.Empty;
    public List<PaymentDto> Payments { get; set; } = [];
    public List<InvoiceItemDto> Items { get; set; } = [];
    public List<CreditNoteDto> CreditNotes { get; set; } = [];
}
