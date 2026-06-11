using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using ExhibitionManagementSystem.Models.Interfaces;

namespace ExhibitionManagementSystem.Models;

/// <summary>
/// يمثل قاعة داخل موقع معرض مع مخططها ومساحتها وأجنحتها.
/// </summary>
public class Hall : IAuditableEntity, ISoftDeletable
{

    /// <summary>
    /// المعرف الفريد للقاعة.
    /// </summary>
    [Key] public int HallID { get; set; }

    /// <summary>
    /// معرف الموقع الذي تتبع له القاعة.
    /// </summary>
    public int VenueID { get; set; }

    /// <summary>
    /// اسم القاعة.
    /// </summary>
    [Required, StringLength(200)] public string HallName { get; set; }

    /// <summary>
    /// مساحة القاعة بالمتر المربع.
    /// </summary>
    [Column(TypeName = "decimal(10,2)")] public decimal? AreaSqM { get; set; }

    /// <summary>
    /// الحد الأقصى المتوقع لعدد الأجنحة داخل القاعة.
    /// </summary>
    public int? MaxBooths { get; set; }

    /// <summary>
    /// عرض مخطط القاعة المستخدم في واجهة التخطيط.
    /// </summary>
    [Column(TypeName = "decimal(10,2)")] public decimal? FloorPlanWidth { get; set; }

    /// <summary>
    /// ارتفاع مخطط القاعة المستخدم في واجهة التخطيط.
    /// </summary>
    [Column(TypeName = "decimal(10,2)")] public decimal? FloorPlanHeight { get; set; }

    /// <summary>
    /// تمثيل JSON لمخطط القاعة وتفاصيل توزيعها.
    /// </summary>
    public string FloorPlanJSON { get; set; }

    /// <summary>
    /// يحدد ما إذا كانت القاعة متاحة للاستخدام.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// تاريخ إنشاء سجل القاعة.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// تاريخ آخر تعديل على سجل القاعة.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// يحدد ما إذا كانت القاعة محذوفة حذفًا ناعمًا.
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
    /// الموقع الذي يحتوي على القاعة.
    /// </summary>
    [ForeignKey(nameof(VenueID))] public virtual Venue Venue { get; set; }

    /// <summary>
    /// الأجنحة الموجودة داخل القاعة.
    /// </summary>
    public virtual ICollection<Booth> Booths { get; set; } = new HashSet<Booth>();

}
