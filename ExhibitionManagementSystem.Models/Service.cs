using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using ExhibitionManagementSystem.Models.Interfaces;

namespace ExhibitionManagementSystem.Models;

/// <summary>
/// يمثل خدمة يمكن تقديمها للعارضين أو إضافتها إلى الحجوزات.
/// </summary>
public class Service : IAuditableEntity, ISoftDeletable, ITenantEntity
{

    /// <summary>
    /// المعرف الفريد للخدمة.
    /// </summary>
    [Key] public int ServiceID { get; set; }

    /// <summary>
    /// معرف المستأجر الذي يملك الخدمة.
    /// </summary>
    public int TenantID { get; set; }

    /// <summary>
    /// اسم الخدمة.
    /// </summary>
    [Required, StringLength(200)] public string ServiceName { get; set; } = string.Empty;

    /// <summary>
    /// تصنيف الخدمة.
    /// </summary>
    [StringLength(100)] public string Category { get; set; } = string.Empty;

    /// <summary>
    /// وحدة قياس الخدمة مثل قطعة أو يوم أو متر.
    /// </summary>
    [StringLength(50)] public string Unit { get; set; } = string.Empty;

    /// <summary>
    /// السعر الافتراضي للخدمة عند عدم وجود قاعدة تسعير مخصصة.
    /// </summary>
    [Column(TypeName = "decimal(18,2)")] public decimal? DefaultPrice { get; set; }

    /// <summary>
    /// عملة السعر الافتراضي للخدمة.
    /// </summary>
    [Required, StringLength(3)] public string DefaultCurrencyCode { get; set; } = "LYD";

    /// <summary>
    /// يحدد ما إذا كانت الخدمة إلزامية ضمن الحجز.
    /// </summary>
    public bool IsMandatory { get; set; } = false;

    /// <summary>
    /// وصف الخدمة.
    /// </summary>
    [StringLength(500)] public string Description { get; set; } = string.Empty;

    /// <summary>
    /// يحدد ما إذا كانت الخدمة نشطة ومتاحة للاستخدام.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// تاريخ إنشاء سجل الخدمة.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// تاريخ آخر تعديل على سجل الخدمة.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// يحدد ما إذا كانت الخدمة محذوفة حذفًا ناعمًا.
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
    /// المستأجر المرتبط بالخدمة.
    /// </summary>
    [ForeignKey(nameof(TenantID))] public virtual Tenant Tenant { get; set; } = null!;

    [ForeignKey(nameof(DefaultCurrencyCode))] public virtual Currency DefaultCurrency { get; set; } = null!;

}
