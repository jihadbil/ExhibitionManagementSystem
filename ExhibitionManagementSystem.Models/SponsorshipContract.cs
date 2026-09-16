using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ExhibitionManagementSystem.Models.Enums;
using ExhibitionManagementSystem.Models.Interfaces;

namespace ExhibitionManagementSystem.Models;

/// <summary>
/// يمثل عقد أو اتفاقية رعاية موثقة بين إدارة المعرض وجهة راعية.
/// </summary>
public class SponsorshipContract : IAuditableEntity, ISoftDeletable
{
    [Key]
    public int ContractID { get; set; }

    public int TenantID { get; set; }

    public int ExhibitionID { get; set; }

    public int SponsorID { get; set; }

    public int? PackageID { get; set; }

    [Required, StringLength(50)]
    public string ContractNumber { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalAmount { get; set; }

    [Required, StringLength(3)]
    public string CurrencyCode { get; set; } = "LYD";

    public SponsorshipStatus Status { get; set; } = SponsorshipStatus.Confirmed;

    public DateTime SignedDate { get; set; } = DateTime.UtcNow;

    public DateTime? PaymentDueDate { get; set; }

    public string? SpecialTerms { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }

    [StringLength(450)]
    public string? CreatedByUserId { get; set; }

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

    [ForeignKey(nameof(SponsorID))]
    public virtual Sponsor Sponsor { get; set; } = null!;

    [ForeignKey(nameof(PackageID))]
    public virtual SponsorshipPackage? Package { get; set; }

    [ForeignKey(nameof(CurrencyCode))]
    public virtual Currency Currency { get; set; } = null!;

    public virtual ICollection<SponsorAdAssignment> AdAssignments { get; set; } = new HashSet<SponsorAdAssignment>();
}
