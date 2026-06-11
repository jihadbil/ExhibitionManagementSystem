using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using ExhibitionManagementSystem.Models.Enums;
using ExhibitionManagementSystem.Models.Interfaces;

namespace ExhibitionManagementSystem.Models;

/// <summary>
/// يمثل قاعدة تسعير مخصصة لخدمة حسب المستأجر أو المعرض أو فئة العارض.
/// </summary>
public class ServicePriceRule : IAuditableEntity, ISoftDeletable
{
    /// <summary>
    /// المعرف الفريد لقاعدة تسعير الخدمة.
    /// </summary>
    [Key] public int PriceRuleID { get; set; }

    /// <summary>
    /// معرف الخدمة التي تطبق عليها القاعدة.
    /// </summary>
    public int ServiceID { get; set; }

    /// <summary>
    /// معرف المستأجر الذي يملك قاعدة التسعير.
    /// </summary>
    public int TenantID { get; set; }

    /// <summary>
    /// معرف المعرض الذي تطبق عليه القاعدة عند تخصيصها لمعرض محدد.
    /// </summary>
    public int? ExhibitionID { get; set; }

    /// <summary>
    /// فئة العارض التي تطبق عليها القاعدة.
    /// </summary>
    public ExhibitorCategory? ExhibitorCategory { get; set; }

    /// <summary>
    /// سعر وحدة الخدمة حسب القاعدة.
    /// </summary>
    [Column(TypeName = "decimal(18,2)")] public decimal UnitPrice { get; set; }

    /// <summary>
    /// رمز العملة المستخدمة في السعر.
    /// </summary>
    [Required, StringLength(3)] public string CurrencyCode { get; set; }

    /// <summary>
    /// تاريخ بداية صلاحية قاعدة التسعير.
    /// </summary>
    [Column(TypeName = "date")] public DateTime ValidFrom { get; set; }

    /// <summary>
    /// تاريخ نهاية صلاحية قاعدة التسعير.
    /// </summary>
    [Column(TypeName = "date")] public DateTime? ValidTo { get; set; }

    /// <summary>
    /// تاريخ إنشاء قاعدة التسعير.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// تاريخ آخر تعديل على قاعدة التسعير.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// يحدد ما إذا كانت قاعدة التسعير محذوفة حذفًا ناعمًا.
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
    /// الخدمة المرتبطة بقاعدة التسعير.
    /// </summary>
    [ForeignKey(nameof(ServiceID))] public virtual Service Service { get; set; }

    /// <summary>
    /// المستأجر المرتبط بقاعدة التسعير.
    /// </summary>
    [ForeignKey(nameof(TenantID))] public virtual Tenant Tenant { get; set; }

    /// <summary>
    /// المعرض المرتبط بقاعدة التسعير.
    /// </summary>
    [ForeignKey(nameof(ExhibitionID))] public virtual Exhibition Exhibition { get; set; }

    /// <summary>
    /// العملة المستخدمة في قاعدة التسعير.
    /// </summary>
    [ForeignKey(nameof(CurrencyCode))] public virtual Currency Currency { get; set; }

}
