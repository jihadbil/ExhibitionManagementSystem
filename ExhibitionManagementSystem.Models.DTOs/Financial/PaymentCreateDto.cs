using System.ComponentModel.DataAnnotations;

namespace ExhibitionManagementSystem.Models.DTOs.Financial;

public class PaymentCreateDto
{
    public int InvoiceID { get; set; }
    public decimal Amount { get; set; }

    [Required]
    [StringLength(3)]
    public string CurrencyCode { get; set; } = string.Empty;

    [Required]
    public string Method { get; set; } = string.Empty;

    [StringLength(100)]
    public string ReferenceNo { get; set; } = string.Empty;

    [StringLength(500)]
    public string Notes { get; set; } = string.Empty;

    [Required]
    [StringLength(450)]
    public string ReceivedByUserId { get; set; } = string.Empty;
}
