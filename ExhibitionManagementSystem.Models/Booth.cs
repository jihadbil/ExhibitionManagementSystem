using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ExhibitionManagementSystem.Models.Enums;
using System.Text;
using ExhibitionManagementSystem.Models.Interfaces;

namespace ExhibitionManagementSystem.Models;

/// <summary>
/// يمثل جناحًا داخل قاعة معرض مع بيانات المساحة والمخطط والحالة.
/// </summary>
public class Booth : IAuditableEntity, ISoftDeletable
{

    /// <summary>
    /// المعرف الفريد للجناح.
    /// </summary>
    [Key] public int BoothID { get; set; }

    /// <summary>
    /// معرف القاعة التي يقع فيها الجناح.
    /// </summary>
    public int HallID { get; set; }

    /// <summary>
    /// الرقم أو الرمز التشغيلي للجناح داخل القاعة.
    /// </summary>
    [Required, StringLength(20)] public string BoothNumber { get; set; }

    /// <summary>
    /// المساحة الأصلية للجناح بالمتر المربع قبل أي دمج أو تعديل.
    /// </summary>
    [Column(TypeName = "decimal(10,2)")] public decimal OriginalAreaSqM { get; set; }

    /// <summary>
    /// المساحة الحالية للجناح بالمتر المربع بعد التعديلات.
    /// </summary>
    [Column(TypeName = "decimal(10,2)")] public decimal CurrentAreaSqM { get; set; }

    /// <summary>
    /// حالة الجناح الحالية مثل متاح أو محجوز أو مدمج.
    /// </summary>
    public BoothStatus Status { get; set; }

    /// <summary>
    /// يحدد ما إذا كان الجناح جزءًا من عملية دمج.
    /// </summary>
    public bool IsMerged { get; set; } = false;

    /// <summary>
    /// معرف عملية الدمج المرتبطة بالجناح عند وجودها.
    /// </summary>
    public int? MergeID { get; set; }

    /// <summary>
    /// إحداثي الجناح الأفقي على مخطط القاعة.
    /// </summary>
    [Column(TypeName = "decimal(10,2)")] public decimal? PosX { get; set; }

    /// <summary>
    /// إحداثي الجناح الرأسي على مخطط القاعة.
    /// </summary>
    [Column(TypeName = "decimal(10,2)")] public decimal? PosY { get; set; }

    /// <summary>
    /// عرض الجناح على مخطط القاعة.
    /// </summary>
    [Column(TypeName = "decimal(10,2)")] public decimal? Width { get; set; }

    /// <summary>
    /// ارتفاع الجناح على مخطط القاعة.
    /// </summary>
    [Column(TypeName = "decimal(10,2)")] public decimal? Height { get; set; }

    /// <summary>
    /// زاوية دوران الجناح على المخطط بالدرجات.
    /// </summary>
    [Column(TypeName = "decimal(5,2)")] public decimal? RotationAngle { get; set; } = 0;

    /// <summary>
    /// نوع شكل الجناح على المخطط.
    /// </summary>
    public BoothShapeType? ShapeType { get; set; } = BoothShapeType.Rect;

    /// <summary>
    /// تمثيل JSON لنقاط الشكل عندما يكون الجناح مضلعًا.
    /// </summary>
    public string ShapePolygonJSON { get; set; }

    /// <summary>
    /// تاريخ إنشاء سجل الجناح.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// تاريخ آخر تعديل على سجل الجناح.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// يحدد ما إذا كان الجناح محذوفًا حذفًا ناعمًا.
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
    /// القاعة التي تحتوي على الجناح.
    /// </summary>
    [ForeignKey(nameof(HallID))] public virtual Hall Hall { get; set; }

    /// <summary>
    /// عملية الدمج المرتبطة بالجناح.
    /// </summary>
    [ForeignKey(nameof(MergeID))] public virtual BoothMerge BoothMerge { get; set; }

}
