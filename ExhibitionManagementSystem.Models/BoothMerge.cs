using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace ExhibitionManagementSystem.Models;

public class BoothMerge
{

    [Key] public int MergeID { get; set; }
    public int ExhibitionID { get; set; }
    [Required, StringLength(200)] public string MergedBoothLabel { get; set; }
    [Column(TypeName = "decimal(10,2)")] public decimal TotalAreaSqM { get; set; }
    public int? ReservationID { get; set; }
    public DateTime MergedAt { get; set; } = DateTime.UtcNow;
    [StringLength(450)] public string MergedByUserId { get; set; }
    [StringLength(500)] public string Notes { get; set; }

    [ForeignKey(nameof(ExhibitionID))] public virtual Exhibition Exhibition { get; set; }
    [ForeignKey(nameof(ReservationID))] public virtual BoothReservation Reservation { get; set; }
    [ForeignKey(nameof(MergedByUserId))] public virtual ApplicationUser MergedByUser { get; set; }
    public virtual ICollection<BoothMergeItem> MergeItems { get; set; } = new HashSet<BoothMergeItem>();

}
