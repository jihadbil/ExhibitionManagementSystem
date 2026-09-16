using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ExhibitionManagementSystem.Models.Interfaces;

namespace ExhibitionManagementSystem.Models;

/// <summary>
/// يمثل شركة أو جهة راعية للمعارض والفعاليات.
/// </summary>
public class Sponsor : IAuditableEntity, ISoftDeletable
{
    [Key]
    public int SponsorID { get; set; }

    public int TenantID { get; set; }

    [Required, StringLength(200)]
    public string CompanyName { get; set; } = string.Empty;

    [StringLength(100)]
    public string? ContactPerson { get; set; }

    [StringLength(20)]
    public string? Phone { get; set; }

    [StringLength(200)]
    public string? Email { get; set; }

    [StringLength(500)]
    public string? WebsiteUrl { get; set; }

    [StringLength(500)]
    public string? LogoUrl { get; set; }

    public string? CompanyProfile { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    public string? DeletedByUserId { get; set; }

    // Navigation Properties
    [ForeignKey(nameof(TenantID))]
    public virtual Tenant Tenant { get; set; } = null!;

    public virtual ICollection<SponsorshipContract> Contracts { get; set; } = new HashSet<SponsorshipContract>();
}
