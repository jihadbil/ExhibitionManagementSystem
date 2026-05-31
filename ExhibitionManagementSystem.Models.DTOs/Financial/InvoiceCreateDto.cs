using System;
using System.ComponentModel.DataAnnotations;

namespace ExhibitionManagementSystem.Models.DTOs.Financial;

public class InvoiceCreateDto
{
    public int TenantID { get; set; }
    public int ReservationID { get; set; }

    [Required]
    [StringLength(50)]
    public string InvoiceNumber { get; set; } = string.Empty;

    public decimal SubTotal { get; set; }
    public decimal TaxRate { get; set; }
    public decimal TotalAmount { get; set; }

    [Required]
    [StringLength(3)]
    public string CurrencyCode { get; set; } = string.Empty;

    public DateTime? DueDate { get; set; }

    [StringLength(500)]
    public string Notes { get; set; } = string.Empty;
}
