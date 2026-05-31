using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using ExhibitionManagementSystem.Models.Enums;
using ExhibitionManagementSystem.Models.Interfaces;

namespace ExhibitionManagementSystem.Models;

public class Exhibition : IAuditableEntity, ISoftDeletable
{

    [Key] public int ExhibitionID { get; set; }
    public int TenantID { get; set; }
    public int VenueID { get; set; }
    [Required, StringLength(200)] public string Name { get; set; }
    [StringLength(100)] public string Type { get; set; }
    [StringLength(50)] public string Edition { get; set; }
    [Column(TypeName = "date")] public DateTime StartDate { get; set; }
    [Column(TypeName = "date")] public DateTime EndDate { get; set; }
    public ExhibitionStatus Status { get; set; }
    public string Description { get; set; }
    public int? ExpectedVisitors { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal? EntryFee { get; set; }
    [StringLength(3)] public string EntryCurrency { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    public string? DeletedByUserId { get; set; }

    [ForeignKey(nameof(TenantID))] public virtual Tenant Tenant { get; set; }
    [ForeignKey(nameof(VenueID))] public virtual Venue Venue { get; set; }
    [ForeignKey(nameof(EntryCurrency))] public virtual Currency Currency { get; set; }
    public virtual ICollection<ExhibitionSchedule> ExhibitionSchedules { get; set; } = new HashSet<ExhibitionSchedule>();
    public virtual ICollection<BoothReservation> BoothReservations { get; set; } = new HashSet<BoothReservation>();
}
