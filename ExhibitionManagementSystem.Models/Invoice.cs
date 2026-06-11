using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using ExhibitionManagementSystem.Models.Enums;
using ExhibitionManagementSystem.Models.Interfaces;

namespace ExhibitionManagementSystem.Models;

/// <summary>
/// يمثل فاتورة مالية ناتجة عن حجز جناح.
/// </summary>
public class Invoice : IAuditableEntity, ISoftDeletable
{
    /// <summary>
    /// المعرف الفريد للفاتورة.
    /// </summary>
    [Key] public int InvoiceID { get; set; }

    /// <summary>
    /// معرف المستأجر صاحب الفاتورة.
    /// </summary>
    public int TenantID { get; set; }

    /// <summary>
    /// معرف الحجز الذي صدرت عنه الفاتورة.
    /// </summary>
    public int ReservationID { get; set; }

    /// <summary>
    /// رقم الفاتورة الظاهر للمستخدمين.
    /// </summary>
    [Required, StringLength(50)] public string InvoiceNumber { get; set; }

    /// <summary>
    /// تاريخ إصدار الفاتورة.
    /// </summary>
    public DateTime InvoiceDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// قيمة الفاتورة قبل الضريبة.
    /// </summary>
    [Column(TypeName = "decimal(18,2)")] public decimal SubTotal { get; set; }

    /// <summary>
    /// نسبة الضريبة المطبقة على الفاتورة.
    /// </summary>
    [Column(TypeName = "decimal(5,2)")] public decimal TaxRate { get; set; } = 0;

    /// <summary>
    /// قيمة الضريبة المحتسبة.
    /// </summary>
    [Column(TypeName = "decimal(18,2)")] public decimal TaxAmount { get; set; } = 0;

    /// <summary>
    /// إجمالي قيمة الفاتورة بعد الضريبة.
    /// </summary>
    [Column(TypeName = "decimal(18,2)")] public decimal TotalAmount { get; set; }

    /// <summary>
    /// رمز العملة المستخدمة في الفاتورة.
    /// </summary>
    [Required, StringLength(3)] public string CurrencyCode { get; set; }

    /// <summary>
    /// حالة الفاتورة الحالية.
    /// </summary>
    public InvoiceStatus Status { get; set; }

    /// <summary>
    /// تاريخ استحقاق سداد الفاتورة.
    /// </summary>
    [Column(TypeName = "date")] public DateTime? DueDate { get; set; }

    /// <summary>
    /// ملاحظات إضافية على الفاتورة.
    /// </summary>
    [StringLength(500)] public string Notes { get; set; }

    /// <summary>
    /// تاريخ إنشاء سجل الفاتورة.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// تاريخ آخر تعديل على سجل الفاتورة.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// يحدد ما إذا كانت الفاتورة محذوفة حذفًا ناعمًا.
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
    /// المستأجر المرتبط بالفاتورة.
    /// </summary>
    [ForeignKey(nameof(TenantID))] public virtual Tenant Tenant { get; set; }

    /// <summary>
    /// الحجز المرتبط بالفاتورة.
    /// </summary>
    [ForeignKey(nameof(ReservationID))] public virtual BoothReservation Reservation { get; set; }

    /// <summary>
    /// العملة المستخدمة في الفاتورة.
    /// </summary>
    [ForeignKey(nameof(CurrencyCode))] public virtual Currency Currency { get; set; }

    /// <summary>
    /// المدفوعات المسجلة على الفاتورة.
    /// </summary>
    public virtual ICollection<Payment> Payments { get; set; } = new HashSet<Payment>();
}
