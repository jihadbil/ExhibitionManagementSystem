using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ExhibitionManagementSystem.Models.Enums;
using ExhibitionManagementSystem.Models.Interfaces;

namespace ExhibitionManagementSystem.Models;

/// <summary>
/// يمثل مساحة أو موقعاً إعلانياً متاحاً للرعاية داخل المعرض أو القاعات أو الوسائط الرقمية.
/// </summary>
public class AdvertisingSpace : IAuditableEntity, ISoftDeletable
{
    [Key]
    public int SpaceID { get; set; }

    public int TenantID { get; set; }

    public int ExhibitionID { get; set; }

    public int? VenueID { get; set; }

    public int? HallID { get; set; }

    [Required, StringLength(50)]
    public string SpaceCode { get; set; } = string.Empty;

    [Required, StringLength(150)]
    public string SpaceName { get; set; } = string.Empty;

    public AdvertisingSpaceType SpaceType { get; set; } = AdvertisingSpaceType.DigitalScreen;

    [StringLength(100)]
    public string? DimensionsDescription { get; set; } // مثال: 1920x1080px أو 3m x 2m

    [Column(TypeName = "decimal(18,2)")]
    public decimal BasePrice { get; set; }

    [Required, StringLength(3)]
    public string CurrencyCode { get; set; } = "LYD";

    public bool IsAvailable { get; set; } = true;

    [StringLength(500)]
    public string? LocationNotes { get; set; }

    [StringLength(500)]
    public string? PhotoUrl { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    public string? DeletedByUserId { get; set; }

    // Navigation Properties
    [ForeignKey(nameof(TenantID))]
    public virtual Tenant Tenant { get; set; } = null!;

    [ForeignKey(nameof(ExhibitionID))]
    public virtual Exhibition Exhibition { get; set; } = null!;

    [ForeignKey(nameof(VenueID))]
    public virtual Venue? Venue { get; set; }

    [ForeignKey(nameof(HallID))]
    public virtual Hall? Hall { get; set; }

    [ForeignKey(nameof(CurrencyCode))]
    public virtual Currency Currency { get; set; } = null!;

    public virtual ICollection<SponsorAdAssignment> AdAssignments { get; set; } = new HashSet<SponsorAdAssignment>();
}
