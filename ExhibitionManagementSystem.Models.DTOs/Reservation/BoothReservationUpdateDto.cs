using System.ComponentModel.DataAnnotations;

namespace ExhibitionManagementSystem.Models.DTOs.Reservation;

public class BoothReservationUpdateDto
{
    [Required]
    public string Status { get; set; } = string.Empty;

    public decimal AllocatedAreaSqM { get; set; }
    public decimal BoothAmount { get; set; }
    public decimal ServicesAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string LogisticNotes { get; set; } = string.Empty;
    public int? BoothID { get; set; }
    public int? MergeID { get; set; }
}
