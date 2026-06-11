using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using ExhibitionManagementSystem.Models.Enums;
using ExhibitionManagementSystem.Models.Interfaces;

namespace ExhibitionManagementSystem.Models;

/// <summary>
/// يمثل دفعة مالية مسجلة على فاتورة.
/// </summary>
public class Payment : IAuditableEntity
{
    /// <summary>
    /// المعرف الفريد للدفعة.
    /// </summary>
    [Key] public int PaymentID { get; set; }

    /// <summary>
    /// معرف الفاتورة المرتبطة بالدفعة.
    /// </summary>
    public int InvoiceID { get; set; }

    /// <summary>
    /// تاريخ ووقت تسجيل الدفعة.
    /// </summary>
    public DateTime PaymentDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// قيمة الدفعة.
    /// </summary>
    [Column(TypeName = "decimal(18,2)")] public decimal Amount { get; set; }

    /// <summary>
    /// رمز العملة المستخدمة في الدفعة.
    /// </summary>
    [Required, StringLength(3)] public string CurrencyCode { get; set; }

    /// <summary>
    /// طريقة الدفع المستخدمة.
    /// </summary>
    public PaymentMethod Method { get; set; }

    /// <summary>
    /// رقم مرجعي للدفع مثل رقم التحويل أو الشيك.
    /// </summary>
    [StringLength(100)] public string ReferenceNo { get; set; }

    /// <summary>
    /// حالة الدفعة الحالية.
    /// </summary>
    public PaymentStatus Status { get; set; }

    /// <summary>
    /// ملاحظات إضافية على الدفعة.
    /// </summary>
    [StringLength(500)] public string Notes { get; set; }

    /// <summary>
    /// معرف المستخدم الذي استلم أو سجل الدفعة.
    /// </summary>
    [StringLength(450)] public string ReceivedByUserId { get; set; }

    /// <summary>
    /// تاريخ إنشاء سجل الدفعة.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// تاريخ آخر تعديل على سجل الدفعة.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// الفاتورة المرتبطة بالدفعة.
    /// </summary>
    [ForeignKey(nameof(InvoiceID))] public virtual Invoice Invoice { get; set; }

    /// <summary>
    /// العملة المستخدمة في الدفعة.
    /// </summary>
    [ForeignKey(nameof(CurrencyCode))] public virtual Currency Currency { get; set; }

    /// <summary>
    /// المستخدم الذي استلم أو سجل الدفعة.
    /// </summary>
    [ForeignKey(nameof(ReceivedByUserId))] public virtual ApplicationUser ReceivedByUser { get; set; }

}
