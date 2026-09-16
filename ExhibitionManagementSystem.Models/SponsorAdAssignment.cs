using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ExhibitionManagementSystem.Models.Interfaces;

namespace ExhibitionManagementSystem.Models;

/// <summary>
/// يمثل تعيين وتخصيص مساحة إعلانية محددة لصالح راعي بموجب عقد رعاية.
/// </summary>
public class SponsorAdAssignment : IAuditableEntity, ISoftDeletable
{
    [Key]
    public int AssignmentID { get; set; }

    public int ContractID { get; set; }

    public int SpaceID { get; set; }

    public DateTime AssignedDate { get; set; } = DateTime.UtcNow;

    [StringLength(50)]
    public string AssetStatus { get; set; } = "Pending"; // Pending, Received, Approved, Displayed

    [StringLength(500)]
    public string? ArtworkFileUrl { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    public string? DeletedByUserId { get; set; }

    // Navigation Properties
    [ForeignKey(nameof(ContractID))]
    public virtual SponsorshipContract Contract { get; set; } = null!;

    [ForeignKey(nameof(SpaceID))]
    public virtual AdvertisingSpace Space { get; set; } = null!;
}
