using System.ComponentModel.DataAnnotations;

namespace ExhibitionManagementSystem.Models.DTOs.Financial;

public sealed class CreditNoteCreateDto
{
    [Required]
    [RegularExpression("^(Credit|Refund)$", ErrorMessage = "Type must be Credit or Refund.")]
    public string Type { get; set; } = string.Empty;

    [Range(typeof(decimal), "0.01", "79228162514264337593543950335")]
    public decimal Amount { get; set; }

    [Required, StringLength(500, MinimumLength = 3)]
    public string Reason { get; set; } = string.Empty;
}
