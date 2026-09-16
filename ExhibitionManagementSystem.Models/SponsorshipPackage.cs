using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ExhibitionManagementSystem.Models.Enums;
using ExhibitionManagementSystem.Models.Interfaces;

namespace ExhibitionManagementSystem.Models;

/// <summary>
/// يمثل باقة رعاية محددة معروضة للرعاة في معرض معين.
/// </summary>
public class SponsorshipPackage : IAuditableEntity, ISoftDeletable
{
    [Key]
    public int PackageID { get; set; }

    public int TenantID { get; set; }

    public int ExhibitionID { get; set; }

    [Required, StringLength(150)]
    public string PackageName { get; set; } = string.Empty;

    public SponsorshipLevel Level { get; set; } = SponsorshipLevel.Gold;

    [Column(TypeName = "decimal(18,2)")]
    public decimal Price { get; set; }

    [Required, StringLength(3)]
    public string CurrencyCode { get; set; } = "LYD";

    /// <summary>
    /// الحد الأقصى لعدد الرعاة المسموح بهم في هذه الباقة (مثلاً راعي رئيسي واحد فقط، أو 3 رعاة بلاتينيين).
    /// </summary>
    public int MaxSponsorsCount { get; set; } = 1;

    /// <summary>
    /// وصف المزايا والامتيازات المشمولة في الباقة (تذاكر VIP، لافتات، كلمات افتتاحية، مساحات إعلانية).
    /// </summary>
    public string? EntitlementsDescription { get; set; }

    public bool IsActive { get; set; } = true;

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

    [ForeignKey(nameof(CurrencyCode))]
    public virtual Currency Currency { get; set; } = null!;

    public virtual ICollection<SponsorshipContract> Contracts { get; set; } = new HashSet<SponsorshipContract>();
}
