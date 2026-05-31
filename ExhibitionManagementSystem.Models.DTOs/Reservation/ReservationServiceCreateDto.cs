using System.ComponentModel.DataAnnotations;

namespace ExhibitionManagementSystem.Models.DTOs.Reservation;

public class ReservationServiceCreateDto
{
    public int ServiceID { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }

    [Required]
    [StringLength(3)]
    public string CurrencyCode { get; set; } = string.Empty;
}
