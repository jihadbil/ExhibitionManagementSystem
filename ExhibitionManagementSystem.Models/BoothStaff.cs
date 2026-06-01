using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using ExhibitionManagementSystem.Models.Interfaces;

namespace ExhibitionManagementSystem.Models;

public class BoothStaff : IAuditableEntity, ISoftDeletable
{
    [Key] public int StaffID { get; set; }
    public int ReservationID { get; set; }
    [Required, StringLength(100)] public string StaffName { get; set; }
    [StringLength(50)] public string Role { get; set; }
    [StringLength(20)] public string Phone { get; set; }
    [StringLength(200)] public string Email { get; set; }
    public bool BadgeIssued { get; set; } = false;
    [StringLength(50)] public string BadgeNumber { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    public string? DeletedByUserId { get; set; }

    [ForeignKey(nameof(ReservationID))] public virtual BoothReservation Reservation { get; set; }
}
