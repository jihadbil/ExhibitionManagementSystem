using ExhibitionManagementSystem.Models.DTOs.Validation;
using System.ComponentModel.DataAnnotations;

namespace ExhibitionManagementSystem.Models.DTOs.Reservation;

public class ReservationServiceCreateDto
{
    [System.ComponentModel.DataAnnotations.Range(1, int.MaxValue)]
    public int ServiceID { get; set; }
    [DecimalValue("0.01", "99999999.99")]
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }

    [Required]
    [StringLength(3)]
    public string CurrencyCode { get; set; } = string.Empty;
}
