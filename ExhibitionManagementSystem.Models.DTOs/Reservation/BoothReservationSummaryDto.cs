using System;

namespace ExhibitionManagementSystem.Models.DTOs.Reservation;

public class BoothReservationSummaryDto
{
    public int ReservationID { get; set; }
    public string ExhibitorName { get; set; } = string.Empty;
    public string BoothNumber { get; set; } = string.Empty;
    public string ExhibitionName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime ReservationDate { get; set; }
}
