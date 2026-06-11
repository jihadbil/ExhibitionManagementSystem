using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using ExhibitionManagementSystem.Models.Enums;
using ExhibitionManagementSystem.Models.Interfaces;

namespace ExhibitionManagementSystem.Models;

/// <summary>
/// يمثل حجز جناح أو مساحة لعارض داخل معرض.
/// </summary>
public class BoothReservation : IAuditableEntity, ISoftDeletable
{

    /// <summary>
    /// المعرف الفريد للحجز.
    /// </summary>
    [Key] public int ReservationID { get; set; }

    /// <summary>
    /// معرف العارض صاحب الحجز.
    /// </summary>
    public int ExhibitorID { get; set; }

    /// <summary>
    /// معرف الجناح المحجوز عند تخصيص جناح فردي.
    /// </summary>
    public int? BoothID { get; set; }

    /// <summary>
    /// معرف المعرض الذي تم الحجز ضمنه.
    /// </summary>
    public int ExhibitionID { get; set; }

    /// <summary>
    /// معرف عملية الدمج عند حجز جناح مدمج.
    /// </summary>
    public int? MergeID { get; set; }

    /// <summary>
    /// نوع الجناح الذي اختاره العارض.
    /// </summary>
    public BoothType BoothTypeSelected { get; set; }

    /// <summary>
    /// المساحة المطلوبة من العارض بالمتر المربع.
    /// </summary>
    [Column(TypeName = "decimal(10,2)")] public decimal RequestedAreaSqM { get; set; }

    /// <summary>
    /// المساحة التي تم تخصيصها فعليًا بالمتر المربع.
    /// </summary>
    [Column(TypeName = "decimal(10,2)")] public decimal AllocatedAreaSqM { get; set; }

    /// <summary>
    /// فئة العارض المستخدمة في التسعير والتقارير.
    /// </summary>
    public ExhibitorCategory ExhibitorCategory { get; set; }

    /// <summary>
    /// قيمة الجناح قبل إضافة الخدمات.
    /// </summary>
    [Column(TypeName = "decimal(18,2)")] public decimal BoothAmount { get; set; }

    /// <summary>
    /// إجمالي قيمة الخدمات المرتبطة بالحجز.
    /// </summary>
    [Column(TypeName = "decimal(18,2)")] public decimal ServicesAmount { get; set; } = 0;

    /// <summary>
    /// القيمة الإجمالية للحجز بعد احتساب الجناح والخدمات.
    /// </summary>
    [Column(TypeName = "decimal(18,2)")] public decimal TotalAmount { get; set; }

    /// <summary>
    /// رمز عملة الحجز.
    /// </summary>
    [Required, StringLength(3)] public string CurrencyCode { get; set; }

    /// <summary>
    /// سعر الصرف المستخدم عند تحويل قيمة الحجز إلى العملة الأساسية.
    /// </summary>
    [Column(TypeName = "decimal(18,6)")] public decimal ExchangeRateUsed { get; set; } = 1;

    /// <summary>
    /// قيمة الحجز بعد تحويلها إلى العملة الأساسية للمستأجر.
    /// </summary>
    [Column(TypeName = "decimal(18,2)")] public decimal AmountInBaseCurrency { get; set; }

    /// <summary>
    /// حالة الحجز الحالية.
    /// </summary>
    public ReservationStatus Status { get; set; }

    /// <summary>
    /// تاريخ ووقت إنشاء الحجز.
    /// </summary>
    public DateTime ReservationDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// ملاحظات لوجستية مرتبطة بتجهيز الجناح أو متطلبات العارض.
    /// </summary>
    public string LogisticNotes { get; set; }

    /// <summary>
    /// معرف المستخدم الذي أنشأ الحجز.
    /// </summary>
    [StringLength(450)] public string CreatedByUserId { get; set; }

    /// <summary>
    /// تاريخ إنشاء سجل الحجز.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// تاريخ آخر تعديل على سجل الحجز.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// يحدد ما إذا كان الحجز محذوفًا حذفًا ناعمًا.
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
    /// العارض صاحب الحجز.
    /// </summary>
    [ForeignKey(nameof(ExhibitorID))] public virtual Exhibitor Exhibitor { get; set; }

    /// <summary>
    /// الجناح المرتبط بالحجز عند وجود جناح فردي.
    /// </summary>
    [ForeignKey(nameof(BoothID))] public virtual Booth Booth { get; set; }

    /// <summary>
    /// المعرض المرتبط بالحجز.
    /// </summary>
    [ForeignKey(nameof(ExhibitionID))] public virtual Exhibition Exhibition { get; set; }

    /// <summary>
    /// عملية الدمج المرتبطة بالحجز عند حجز جناح مدمج.
    /// </summary>
    [ForeignKey(nameof(MergeID))] public virtual BoothMerge BoothMerge { get; set; }

    /// <summary>
    /// العملة المستخدمة في الحجز.
    /// </summary>
    [ForeignKey(nameof(CurrencyCode))] public virtual Currency Currency { get; set; }

    /// <summary>
    /// المستخدم الذي أنشأ الحجز.
    /// </summary>
    [ForeignKey(nameof(CreatedByUserId))] public virtual ApplicationUser CreatedByUser { get; set; }

    /// <summary>
    /// الفاتورة الناتجة عن الحجز.
    /// </summary>
    public virtual Invoice Invoice { get; set; }

    /// <summary>
    /// الخدمات الإضافية المطلوبة ضمن الحجز.
    /// </summary>
    public virtual ICollection<ReservationService> ReservationServices { get; set; } = new HashSet<ReservationService>();
}
