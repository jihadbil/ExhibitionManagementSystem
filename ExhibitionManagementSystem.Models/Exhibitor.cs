using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using ExhibitionManagementSystem.Models.Enums;
using ExhibitionManagementSystem.Models.Interfaces;

namespace ExhibitionManagementSystem.Models;

/// <summary>
/// يمثل عارضًا أو شركة مشاركة في المعارض.
/// </summary>
public class Exhibitor : IAuditableEntity, ISoftDeletable, ITenantEntity
{
    /// <summary>
    /// المعرف الفريد للعارض.
    /// </summary>
    [Key] public int ExhibitorID { get; set; }

    /// <summary>
    /// معرف المستأجر الذي يتبع له العارض.
    /// </summary>
    public int TenantID { get; set; }

    /// <summary>
    /// اسم الشركة العارضة.
    /// </summary>
    [Required, StringLength(200)] public string CompanyName { get; set; } = string.Empty;

    /// <summary>
    /// اسم مسؤول التواصل لدى الشركة.
    /// </summary>
    [StringLength(100)] public string ContactPerson { get; set; } = string.Empty;

    /// <summary>
    /// رقم هاتف العارض.
    /// </summary>
    [StringLength(20)] public string Phone { get; set; } = string.Empty;

    /// <summary>
    /// البريد الإلكتروني للعارض.
    /// </summary>
    [StringLength(200)] public string Email { get; set; } = string.Empty;

    /// <summary>
    /// القطاع أو المجال الذي يعمل فيه العارض.
    /// </summary>
    [StringLength(100)] public string Sector { get; set; } = string.Empty;

    /// <summary>
    /// جنسية أو بلد العارض.
    /// </summary>
    [StringLength(100)] public string Nationality { get; set; } = string.Empty;

    /// <summary>
    /// فئة العارض المستخدمة في التصنيف والتسعير.
    /// </summary>
    public ExhibitorCategory ExhibitorCategory { get; set; }

    /// <summary>
    /// عملة الفوترة المفضلة للعارض. يمكن تجاوزها على مستوى الحجز.
    /// </summary>
    [Required, StringLength(3)] public string PreferredCurrencyCode { get; set; } = "LYD";

    /// <summary>
    /// رابط شعار الشركة العارضة.
    /// </summary>
    [StringLength(500)] public string LogoURL { get; set; } = string.Empty;

    /// <summary>
    /// نبذة تعريفية عن الشركة العارضة.
    /// </summary>
    public string CompanyProfile { get; set; } = string.Empty;

    /// <summary>
    /// يحدد ما إذا كان العارض نشطًا.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// تاريخ إنشاء سجل العارض.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// تاريخ آخر تعديل على سجل العارض.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// يحدد ما إذا كان العارض محذوفًا حذفًا ناعمًا.
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
    /// معرف حساب المستخدم المرتبط بالعارض عند وجوده.
    /// </summary>
    [StringLength(450)] public string? UserId { get; set; }

    /// <summary>
    /// المستأجر المرتبط بالعارض.
    /// </summary>
    [ForeignKey(nameof(TenantID))] public virtual Tenant Tenant { get; set; } = null!;

    /// <summary>
    /// حساب المستخدم المرتبط بالعارض.
    /// </summary>
    [ForeignKey(nameof(UserId))] public virtual ApplicationUser? User { get; set; }

    [ForeignKey(nameof(PreferredCurrencyCode))] public virtual Currency PreferredCurrency { get; set; } = null!;

    /// <summary>
    /// حجوزات الأجنحة الخاصة بالعارض.
    /// </summary>
    public virtual ICollection<BoothReservation> BoothReservations { get; set; } = new HashSet<BoothReservation>();
}
