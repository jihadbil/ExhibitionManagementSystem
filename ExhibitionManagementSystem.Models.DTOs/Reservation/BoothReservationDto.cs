using System;
using System.Collections.Generic;
using ExhibitionManagementSystem.Models.DTOs.Common;

namespace ExhibitionManagementSystem.Models.DTOs.Reservation;

public class BoothReservationDto : AuditDto
{
    public int ReservationID { get; set; }
    public int ExhibitorID { get; set; }
    public string ExhibitorName { get; set; } = string.Empty;
    public int? BoothID { get; set; }
    public string BoothNumber { get; set; } = string.Empty;
    public int ExhibitionID { get; set; }
    public string ExhibitionName { get; set; } = string.Empty;
    public int? MergeID { get; set; }
    public string BoothTypeSelected { get; set; } = string.Empty;
    public decimal RequestedAreaSqM { get; set; }
    public decimal AllocatedAreaSqM { get; set; }
    public string ExhibitorCategory { get; set; } = string.Empty;
    public decimal BoothAmount { get; set; }
    public decimal ServicesAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public string CurrencySymbol { get; set; } = string.Empty;
    public decimal ExchangeRateUsed { get; set; }
    public decimal AmountInBaseCurrency { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime ReservationDate { get; set; }
    public string LogisticNotes { get; set; } = string.Empty;
    public string CreatedByUserId { get; set; } = string.Empty;
    public List<ReservationServiceDto> Services { get; set; } = [];
}
