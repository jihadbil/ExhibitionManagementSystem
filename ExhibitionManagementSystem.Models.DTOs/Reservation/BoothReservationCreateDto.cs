using System.ComponentModel.DataAnnotations;

namespace ExhibitionManagementSystem.Models.DTOs.Reservation;

public class BoothReservationCreateDto : IValidatableObject
{
    [Range(1, int.MaxValue)]
    public int ExhibitorID { get; set; }

    [Range(1, int.MaxValue)]
    public int? BoothID { get; set; }

    [Range(1, int.MaxValue)]
    public int ExhibitionID { get; set; }

    [Range(1, int.MaxValue)]
    public int? MergeID { get; set; }

    [Required]
    public string BoothTypeSelected { get; set; } = string.Empty;

    [Range(typeof(decimal), "0.01", "99999999.99")]
    public decimal RequestedAreaSqM { get; set; }

    [Required]
    public string ExhibitorCategory { get; set; } = string.Empty;

    [Required]
    [StringLength(3)]
    public string CurrencyCode { get; set; } = string.Empty;

    [StringLength(500)]
    public string? LogisticNotes { get; set; }

    public List<ReservationServiceCreateDto> Services { get; set; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (BoothID.HasValue == MergeID.HasValue)
        {
            yield return new ValidationResult(
                "يجب اختيار جناح أو دمج واحد فقط.",
                [nameof(BoothID), nameof(MergeID)]);
        }
    }
}
