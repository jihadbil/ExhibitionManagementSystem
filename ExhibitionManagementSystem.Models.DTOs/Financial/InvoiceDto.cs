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
    public string CurrencyCode { get; set; } = string.Empty;
    public string CurrencySymbol { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? DueDate { get; set; }
    public string Notes { get; set; } = string.Empty;
    public List<PaymentDto> Payments { get; set; } = [];
}
