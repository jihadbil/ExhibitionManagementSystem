using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace ExhibitionManagementSystem.Models;

public class BoothStaff
{
    [Key] public int StaffID { get; set; }
    public int ReservationID { get; set; }
    [Required, StringLength(100)] public string StaffName { get; set; }
    [StringLength(50)] public string Role { get; set; }
    [StringLength(20)] public string Phone { get; set; }
    [StringLength(200)] public string Email { get; set; }
    public bool BadgeIssued { get; set; } = false;
    [StringLength(50)] public string BadgeNumber { get; set; }

    [ForeignKey(nameof(ReservationID))] public virtual BoothReservation Reservation { get; set; }

}
