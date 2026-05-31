using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using ExhibitionManagementSystem.Models.Interfaces;

namespace ExhibitionManagementSystem.Models;

public class Hall : IAuditableEntity, ISoftDeletable
{

    [Key] public int HallID { get; set; }
    public int VenueID { get; set; }
    [Required, StringLength(200)] public string HallName { get; set; }
    [Column(TypeName = "decimal(10,2)")] public decimal? AreaSqM { get; set; }
    public int? MaxBooths { get; set; }
    [Column(TypeName = "decimal(10,2)")] public decimal? FloorPlanWidth { get; set; }
    [Column(TypeName = "decimal(10,2)")] public decimal? FloorPlanHeight { get; set; }
    public string FloorPlanJSON { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    public string? DeletedByUserId { get; set; }

    [ForeignKey(nameof(VenueID))] public virtual Venue Venue { get; set; }
    public virtual ICollection<Booth> Booths { get; set; } = new HashSet<Booth>();

}
