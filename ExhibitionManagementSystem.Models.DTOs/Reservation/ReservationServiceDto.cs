namespace ExhibitionManagementSystem.Models.DTOs.Reservation;

public class ReservationServiceDto
{
    public int ReservationServiceID { get; set; }
    public int ReservationID { get; set; }
    public int ServiceID { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public decimal TotalPrice { get; set; }
}
