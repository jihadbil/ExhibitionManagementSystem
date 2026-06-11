using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using ExhibitionManagementSystem.Models.Interfaces;

namespace ExhibitionManagementSystem.Models;

/// <summary>
/// يمثل باقة تسعير تحتوي على مجموعة خدمات وسعر إجمالي.
/// </summary>
public class PricingPackage : IAuditableEntity, ISoftDeletable
{
    /// <summary>
    /// المعرف الفريد لباقة التسعير.
    /// </summary>
    [Key] public int PackageID { get; set; }

    /// <summary>
    /// معرف المستأجر الذي يملك الباقة.
    /// </summary>
    public int TenantID { get; set; }

    /// <summary>
    /// اسم باقة التسعير.
    /// </summary>
    [Required, StringLength(200)] public string PackageName { get; set; }

    /// <summary>
    /// وصف محتوى الباقة.
    /// </summary>
    [StringLength(500)] public string Description { get; set; }

    /// <summary>
    /// السعر الإجمالي للباقة.
    /// </summary>
    [Column(TypeName = "decimal(18,2)")] public decimal TotalPrice { get; set; }

    /// <summary>
    /// رمز العملة المستخدمة في سعر الباقة.
    /// </summary>
    [Required, StringLength(3)] public string CurrencyCode { get; set; }

    /// <summary>
    /// تاريخ بداية صلاحية الباقة.
    /// </summary>
    [Column(TypeName = "date")] public DateTime ValidFrom { get; set; }

    /// <summary>
    /// تاريخ نهاية صلاحية الباقة.
    /// </summary>
    [Column(TypeName = "date")] public DateTime? ValidTo { get; set; }

    /// <summary>
    /// يحدد ما إذا كانت الباقة نشطة ومتاحة للاستخدام.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// تاريخ إنشاء سجل الباقة.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// تاريخ آخر تعديل على سجل الباقة.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// يحدد ما إذا كانت الباقة محذوفة حذفًا ناعمًا.
    /// </summary>
    public bool IsDeleted { get; set; } = false;

    /// <summary>
    /// تاريخ تنفيذ الحذف الناعم.
    /// </summary>
    public DateTime? DeletedAt { get; set; }

    /// <summary>
    /// معرف المستخدم الذي نفذ الحذف الناعم.
    /// </summary>
    public string? DeletedByUserId { get; set; }

    /// <summary>
    /// المستأجر المرتبط بالباقة.
    /// </summary>
    [ForeignKey(nameof(TenantID))] public virtual Tenant Tenant { get; set; }

    /// <summary>
    /// العملة المستخدمة في سعر الباقة.
    /// </summary>
    [ForeignKey(nameof(CurrencyCode))] public virtual Currency Currency { get; set; }

    /// <summary>
    /// الخدمات المضمنة في الباقة.
    /// </summary>
    public virtual ICollection<PackageService> PackageServices { get; set; } = new HashSet<PackageService>();
}
