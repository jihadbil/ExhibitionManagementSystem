using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace ExhibitionManagementSystem.Models;

public class VisitorRating
{
    [Key] public int RatingID { get; set; }
    public int VisitorID { get; set; }
    public int ExhibitionID { get; set; }
    public int? ExhibitorID { get; set; }
    public byte Score { get; set; } // تمثل tinyint وسيتم وضع قيد Check (1-5) في OnModelCreating
    [StringLength(1000)] public string Comment { get; set; }
    public DateTime RatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(VisitorID))] public virtual Visitor Visitor { get; set; }
    [ForeignKey(nameof(ExhibitionID))] public virtual Exhibition Exhibition { get; set; }
    [ForeignKey(nameof(ExhibitorID))] public virtual Exhibitor Exhibitor { get; set; }

}
