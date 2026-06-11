using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace ExhibitionManagementSystem.Models;

/// <summary>
/// يمثل عملية دمج عدة أجنحة في جناح واحد داخل معرض.
/// </summary>
public class BoothMerge
{

    /// <summary>
    /// المعرف الفريد لعملية الدمج.
    /// </summary>
    [Key] public int MergeID { get; set; }

    /// <summary>
    /// معرف المعرض الذي تمت فيه عملية الدمج.
    /// </summary>
    public int ExhibitionID { get; set; }

    /// <summary>
    /// التسمية النهائية للجناح المدمج.
    /// </summary>
    [Required, StringLength(200)] public string MergedBoothLabel { get; set; }

    /// <summary>
    /// إجمالي مساحة الأجنحة المدمجة بالمتر المربع.
    /// </summary>
    [Column(TypeName = "decimal(10,2)")] public decimal TotalAreaSqM { get; set; }

    /// <summary>
    /// معرف الحجز المرتبط بالجناح المدمج عند تخصيصه.
    /// </summary>
    public int? ReservationID { get; set; }

    /// <summary>
    /// تاريخ ووقت تنفيذ عملية الدمج.
    /// </summary>
    public DateTime MergedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// معرف المستخدم الذي نفذ عملية الدمج.
    /// </summary>
    [StringLength(450)] public string MergedByUserId { get; set; }

    /// <summary>
    /// ملاحظات إضافية حول عملية الدمج.
    /// </summary>
    [StringLength(500)] public string Notes { get; set; }

    /// <summary>
    /// المعرض المرتبط بعملية الدمج.
    /// </summary>
    [ForeignKey(nameof(ExhibitionID))] public virtual Exhibition Exhibition { get; set; }

    /// <summary>
    /// الحجز المرتبط بالجناح المدمج.
    /// </summary>
    [ForeignKey(nameof(ReservationID))] public virtual BoothReservation Reservation { get; set; }

    /// <summary>
    /// المستخدم الذي نفذ عملية الدمج.
    /// </summary>
    [ForeignKey(nameof(MergedByUserId))] public virtual ApplicationUser MergedByUser { get; set; }

    /// <summary>
    /// العناصر التي توضح الأجنحة الداخلة في عملية الدمج.
    /// </summary>
    public virtual ICollection<BoothMergeItem> MergeItems { get; set; } = new HashSet<BoothMergeItem>();

}
