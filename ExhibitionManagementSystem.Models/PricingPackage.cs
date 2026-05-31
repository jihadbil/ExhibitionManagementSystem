using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using ExhibitionManagementSystem.Models.Interfaces;

namespace ExhibitionManagementSystem.Models;

public class PricingPackage : IAuditableEntity, ISoftDeletable
{
    [Key] public int PackageID { get; set; }
    public int TenantID { get; set; }
    [Required, StringLength(200)] public string PackageName { get; set; }
    [StringLength(500)] public string Description { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal TotalPrice { get; set; }
    [Required, StringLength(3)] public string CurrencyCode { get; set; }
    [Column(TypeName = "date")] public DateTime ValidFrom { get; set; }
    [Column(TypeName = "date")] public DateTime? ValidTo { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    public string? DeletedByUserId { get; set; }

    [ForeignKey(nameof(TenantID))] public virtual Tenant Tenant { get; set; }
    [ForeignKey(nameof(CurrencyCode))] public virtual Currency Currency { get; set; }
    public virtual ICollection<PackageService> PackageServices { get; set; } = new HashSet<PackageService>();
}
