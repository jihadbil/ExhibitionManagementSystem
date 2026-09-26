using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using ExhibitionManagementSystem.Models.Interfaces;

namespace ExhibitionManagementSystem.Models;

/// <summary>
/// يمثل زائرًا مسجلًا لدى مستأجر ويمكنه امتلاك تذاكر للمعارض.
/// </summary>
public class Visitor : IAuditableEntity, ISoftDeletable, ITenantEntity
{
    /// <summary>
    /// المعرف الفريد للزائر.
    /// </summary>
    [Key] public int VisitorID { get; set; }

    /// <summary>
    /// معرف المستأجر الذي سُجل الزائر ضمنه.
    /// </summary>
    public int TenantID { get; set; }

    /// <summary>
    /// الاسم الكامل للزائر.
    /// </summary>
    [Required, StringLength(100)] public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// رقم هاتف الزائر.
    /// </summary>
    [StringLength(20)] public string Phone { get; set; } = string.Empty;

    /// <summary>
    /// البريد الإلكتروني للزائر.
    /// </summary>
    [StringLength(200)] public string Email { get; set; } = string.Empty;

    /// <summary>
    /// جنسية الزائر.
    /// </summary>
    [StringLength(100)] public string Nationality { get; set; } = string.Empty;

    /// <summary>
    /// نوع الزائر أو تصنيفه.
    /// </summary>
    [StringLength(50)] public string VisitorType { get; set; } = string.Empty;

    /// <summary>
    /// تاريخ ووقت تسجيل الزائر.
    /// </summary>
    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// تاريخ إنشاء سجل الزائر.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// تاريخ آخر تعديل على سجل الزائر.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// يحدد ما إذا كان الزائر محذوفًا حذفًا ناعمًا.
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
    /// معرف حساب المستخدم المرتبط بالزائر عند وجوده.
    /// </summary>
    [StringLength(450)] public string? UserId { get; set; }

    /// <summary>
    /// المستأجر المرتبط بالزائر.
    /// </summary>
    [ForeignKey(nameof(TenantID))] public virtual Tenant Tenant { get; set; } = null!;

    /// <summary>
    /// حساب المستخدم المرتبط بالزائر.
    /// </summary>
    [ForeignKey(nameof(UserId))] public virtual ApplicationUser? User { get; set; }

    /// <summary>
    /// التذاكر الصادرة للزائر.
    /// </summary>
    public virtual ICollection<Ticket> Tickets { get; set; } = new HashSet<Ticket>();
}
