using ExhibitionManagementSystem.Models.DTOs.Validation;
using System.ComponentModel.DataAnnotations;

namespace ExhibitionManagementSystem.Models.DTOs.Reservation;

public class BoothReservationCreateDto
{
    [System.ComponentModel.DataAnnotations.Range(1, int.MaxValue)]
    public int ExhibitorID { get; set; }
    [System.ComponentModel.DataAnnotations.Range(1, int.MaxValue)]
    public int? BoothID { get; set; }
    [System.ComponentModel.DataAnnotations.Range(1, int.MaxValue)]
    public int ExhibitionID { get; set; }
    [System.ComponentModel.DataAnnotations.Range(1, int.MaxValue)]
    public int? MergeID { get; set; }

    [Required]
    [DefinedEnum(typeof(ExhibitionManagementSystem.Models.Enums.BoothType))]
    public string BoothTypeSelected { get; set; } = string.Empty;

    [DecimalValue("0.01", "99999999.99")]
    public decimal RequestedAreaSqM { get; set; }

    [Required]
    [DefinedEnum(typeof(ExhibitionManagementSystem.Models.Enums.ExhibitorCategory))]
    public string ExhibitorCategory { get; set; } = string.Empty;

    [Required]
    [StringLength(3)]
    public string CurrencyCode { get; set; } = string.Empty;

    public string LogisticNotes { get; set; } = string.Empty;
}
