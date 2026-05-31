using System.ComponentModel.DataAnnotations;

namespace ExhibitionManagementSystem.Models.DTOs.Reservation;

public class BoothReservationCreateDto
{
    public int ExhibitorID { get; set; }
    public int? BoothID { get; set; }
    public int ExhibitionID { get; set; }
    public int? MergeID { get; set; }

    [Required]
    public string BoothTypeSelected { get; set; } = string.Empty;

    public decimal RequestedAreaSqM { get; set; }

    [Required]
    public string ExhibitorCategory { get; set; } = string.Empty;

    [Required]
    [StringLength(3)]
    public string CurrencyCode { get; set; } = string.Empty;

    public string LogisticNotes { get; set; } = string.Empty;
}
