using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace ExhibitionManagementSystem.Models;

public class ReservationService
{
    [Key] public int ReservationServiceID { get; set; }
    public int ReservationID { get; set; }
    public int ServiceID { get; set; }
    [Column(TypeName = "decimal(10,2)")] public decimal Quantity { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal UnitPrice { get; set; }
    [Required, StringLength(3)] public string CurrencyCode { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal TotalPrice { get; set; }

    [ForeignKey(nameof(ReservationID))] public virtual BoothReservation Reservation { get; set; }
    [ForeignKey(nameof(ServiceID))] public virtual Service Service { get; set; }
    [ForeignKey(nameof(CurrencyCode))] public virtual Currency Currency { get; set; }

}
