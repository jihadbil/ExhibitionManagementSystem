using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using ExhibitionManagementSystem.Models.Enums;
using ExhibitionManagementSystem.Models.Interfaces;

namespace ExhibitionManagementSystem.Models;

/// <summary>
/// يمثل معرضًا يتم تنظيمه لمستأجر في موقع محدد.
/// </summary>
public class Exhibition : IAuditableEntity, ISoftDeletable
{

    /// <summary>
    /// المعرف الفريد للمعرض.
    /// </summary>
    [Key] public int ExhibitionID { get; set; }

    /// <summary>
    /// معرف المستأجر المنظم للمعرض.
    /// </summary>
    public int TenantID { get; set; }

    /// <summary>
    /// معرف الموقع الذي يستضيف المعرض.
    /// </summary>
    public int VenueID { get; set; }

    /// <summary>
    /// اسم المعرض.
    /// </summary>
    [Required, StringLength(200)] public string Name { get; set; }

    /// <summary>
    /// نوع أو تصنيف المعرض.
    /// </summary>
    [StringLength(100)] public string Type { get; set; }

    /// <summary>
    /// رقم أو وصف نسخة المعرض.
    /// </summary>
    [StringLength(50)] public string Edition { get; set; }

    /// <summary>
    /// تاريخ بداية المعرض.
    /// </summary>
    [Column(TypeName = "date")] public DateTime StartDate { get; set; }

    /// <summary>
    /// تاريخ نهاية المعرض.
    /// </summary>
    [Column(TypeName = "date")] public DateTime EndDate { get; set; }

    /// <summary>
    /// حالة المعرض الحالية.
    /// </summary>
    public ExhibitionStatus Status { get; set; }

    /// <summary>
    /// وصف تفصيلي للمعرض.
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// العدد المتوقع للزوار.
    /// </summary>
    public int? ExpectedVisitors { get; set; }

    /// <summary>
    /// رسوم دخول المعرض عند وجودها.
    /// </summary>
    [Column(TypeName = "decimal(18,2)")] public decimal? EntryFee { get; set; }

    /// <summary>
    /// رمز العملة المستخدمة لرسوم الدخول.
    /// </summary>
    [StringLength(3)] public string EntryCurrency { get; set; }

    /// <summary>
    /// تاريخ إنشاء سجل المعرض.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// تاريخ آخر تعديل على سجل المعرض.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// يحدد ما إذا كان المعرض محذوفًا حذفًا ناعمًا.
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
    /// المستأجر المنظم للمعرض.
    /// </summary>
    [ForeignKey(nameof(TenantID))] public virtual Tenant Tenant { get; set; }

    /// <summary>
    /// الموقع الذي يستضيف المعرض.
    /// </summary>
    [ForeignKey(nameof(VenueID))] public virtual Venue Venue { get; set; }

    /// <summary>
    /// العملة المرتبطة برسوم الدخول.
    /// </summary>
    [ForeignKey(nameof(EntryCurrency))] public virtual Currency Currency { get; set; }

    /// <summary>
    /// جدول الفعاليات المرتبطة بالمعرض.
    /// </summary>
    public virtual ICollection<ExhibitionSchedule> ExhibitionSchedules { get; set; } = new HashSet<ExhibitionSchedule>();

    /// <summary>
    /// حجوزات الأجنحة المرتبطة بالمعرض.
    /// </summary>
    public virtual ICollection<BoothReservation> BoothReservations { get; set; } = new HashSet<BoothReservation>();
}
