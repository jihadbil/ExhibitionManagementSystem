using ExhibitionManagementSystem.Models.DTOs.Validation;
using System.ComponentModel.DataAnnotations;

namespace ExhibitionManagementSystem.Models.DTOs.Financial;

public class PaymentCreateDto
{
    [Required, StringLength(100)]
    [RegularExpression("^[A-Za-z0-9._:-]+$")]
    public string IdempotencyKey { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.Range(1, int.MaxValue)]
    public int InvoiceID { get; set; }
    [Range(typeof(decimal), "0.01", "9999999999999999.99")]
    [DecimalValue("0.01", "9999999999999999.99")]
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
}
