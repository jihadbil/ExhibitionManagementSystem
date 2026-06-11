using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ExhibitionManagementSystem.Models.Interfaces;

namespace ExhibitionManagementSystem.Models;

/// <summary>
/// يمثل مصروفًا ماليًا مرتبطًا بمعرض ومستأجر.
/// </summary>
public class Expense : IAuditableEntity, ISoftDeletable
{
    /// <summary>
    /// المعرف الفريد للمصروف.
    /// </summary>
    [Key] public int ExpenseID { get; set; }

    /// <summary>
    /// معرف المستأجر الذي سجل المصروف.
    /// </summary>
    public int TenantID { get; set; }

    /// <summary>
    /// معرف المعرض المرتبط بالمصروف.
    /// </summary>
    public int ExhibitionID { get; set; }

    /// <summary>
    /// وصف المصروف.
    /// </summary>
    [Required, StringLength(200)] public string Description { get; set; }

    /// <summary>
    /// تصنيف المصروف المالي.
    /// </summary>
    [Required, StringLength(100)] public string Category { get; set; }

    /// <summary>
    /// قيمة المصروف.
    /// </summary>
    [Column(TypeName = "decimal(18,2)")] public decimal Amount { get; set; }

    /// <summary>
    /// رمز العملة المستخدمة في المصروف.
    /// </summary>
    [Required, StringLength(3)] public string CurrencyCode { get; set; }

    /// <summary>
    /// تاريخ المصروف.
    /// </summary>
    [Column(TypeName = "date")] public DateTime ExpenseDate { get; set; }

    /// <summary>
    /// ملاحظات إضافية حول المصروف.
    /// </summary>
    [StringLength(500)] public string? Notes { get; set; }

    /// <summary>
    /// معرف المستخدم الذي أنشأ سجل المصروف.
    /// </summary>
    [StringLength(450)] public string? CreatedByUserId { get; set; }

    /// <summary>
    /// تاريخ إنشاء سجل المصروف.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// تاريخ آخر تعديل على سجل المصروف.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// يحدد ما إذا كان المصروف محذوفًا حذفًا ناعمًا.
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
    /// المستأجر المرتبط بالمصروف.
    /// </summary>
    [ForeignKey(nameof(TenantID))] public virtual Tenant Tenant { get; set; }

    /// <summary>
    /// المعرض المرتبط بالمصروف.
    /// </summary>
    [ForeignKey(nameof(ExhibitionID))] public virtual Exhibition Exhibition { get; set; }

    /// <summary>
    /// العملة المستخدمة في المصروف.
    /// </summary>
    [ForeignKey(nameof(CurrencyCode))] public virtual Currency Currency { get; set; }

    /// <summary>
    /// المستخدم الذي أنشأ سجل المصروف.
    /// </summary>
    [ForeignKey(nameof(CreatedByUserId))] public virtual ApplicationUser? CreatedByUser { get; set; }
}
