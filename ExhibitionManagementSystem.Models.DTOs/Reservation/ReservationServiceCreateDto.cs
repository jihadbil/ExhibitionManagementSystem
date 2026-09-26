using System.ComponentModel.DataAnnotations;

namespace ExhibitionManagementSystem.Models.DTOs.Reservation;

public class ReservationServiceCreateDto
{
    [Range(1, int.MaxValue)]
    public int ServiceID { get; set; }

    [Range(typeof(decimal), "0.01", "99999999.99")]
    public decimal Quantity { get; set; }

    [Range(typeof(decimal), "0", "9999999999999999.99")]
    public decimal UnitPrice { get; set; }

    [Required]
    [StringLength(3)]
    public string CurrencyCode { get; set; } = string.Empty;
}
