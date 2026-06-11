using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using ExhibitionManagementSystem.Models.Enums;
using ExhibitionManagementSystem.Models.Interfaces;

namespace ExhibitionManagementSystem.Models;

/// <summary>
/// يمثل قاعدة تسعير لمساحة الأجنحة حسب المعرض أو الفئة أو نوع الجناح.
/// </summary>
public class BoothPriceRule : IAuditableEntity, ISoftDeletable
{
    /// <summary>
    /// المعرف الفريد لقاعدة تسعير الجناح.
    /// </summary>
    [Key] public int RuleID { get; set; }

    /// <summary>
    /// معرف المستأجر الذي يملك قاعدة التسعير.
    /// </summary>
    public int TenantID { get; set; }

    /// <summary>
    /// معرف المعرض الذي تطبق عليه القاعدة عند تخصيصها لمعرض محدد.
    /// </summary>
    public int? ExhibitionID { get; set; }

    /// <summary>
    /// نوع الجناح الذي تطبق عليه قاعدة التسعير.
    /// </summary>
    public BoothType? BoothType { get; set; }

    /// <summary>
    /// فئة العارض التي تطبق عليها قاعدة التسعير.
    /// </summary>
    public ExhibitorCategory? ExhibitorCategory { get; set; }

    /// <summary>
    /// فئة المنتج التي يمكن استخدامها لتخصيص السعر.
    /// </summary>
    [StringLength(100)] public string ProductCategory { get; set; }

    /// <summary>
    /// سعر المتر المربع حسب القاعدة.
    /// </summary>
    [Column(TypeName = "decimal(18,2)")] public decimal PricePerSqM { get; set; }

    /// <summary>
    /// رمز العملة المستخدمة في السعر.
    /// </summary>
    [Required, StringLength(3)] public string CurrencyCode { get; set; }

    /// <summary>
    /// الحد الأدنى للمساحة التي تطبق عليها القاعدة.
    /// </summary>
    [Column(TypeName = "decimal(10,2)")] public decimal? MinAreaSqM { get; set; }

    /// <summary>
    /// الحد الأقصى للمساحة التي تطبق عليها القاعدة.
    /// </summary>
    [Column(TypeName = "decimal(10,2)")] public decimal? MaxAreaSqM { get; set; }

    /// <summary>
    /// تاريخ بداية صلاحية قاعدة التسعير.
    /// </summary>
    [Column(TypeName = "date")] public DateTime ValidFrom { get; set; }

    /// <summary>
    /// تاريخ نهاية صلاحية قاعدة التسعير.
    /// </summary>
    [Column(TypeName = "date")] public DateTime? ValidTo { get; set; }

    /// <summary>
    /// ملاحظات إضافية حول قاعدة التسعير.
    /// </summary>
    [StringLength(500)] public string Notes { get; set; }

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
    /// المستأجر المرتبط بقاعدة التسعير.
    /// </summary>
    [ForeignKey(nameof(TenantID))] public virtual Tenant Tenant { get; set; }

    /// <summary>
    /// المعرض المرتبط بقاعدة التسعير.
    /// </summary>
    [ForeignKey(nameof(ExhibitionID))] public virtual Exhibition Exhibition { get; set; }

    /// <summary>
    /// العملة المرتبطة بقاعدة التسعير.
    /// </summary>
    [ForeignKey(nameof(CurrencyCode))] public virtual Currency Currency { get; set; }

}
