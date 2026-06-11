using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace ExhibitionManagementSystem.Models;

/// <summary>
/// يمثل تقييمًا يقدمه زائر لمعرض أو عارض داخل معرض.
/// </summary>
public class VisitorRating
{
    /// <summary>
    /// المعرف الفريد للتقييم.
    /// </summary>
    [Key] public int RatingID { get; set; }

    /// <summary>
    /// معرف الزائر الذي قدم التقييم.
    /// </summary>
    public int VisitorID { get; set; }

    /// <summary>
    /// معرف المعرض الذي يرتبط به التقييم.
    /// </summary>
    public int ExhibitionID { get; set; }

    /// <summary>
    /// معرف العارض الذي تم تقييمه عند استهداف التقييم لعارض محدد.
    /// </summary>
    public int? ExhibitorID { get; set; }

    /// <summary>
    /// درجة التقييم المخزنة كقيمة صغيرة، ويتوقع تقييدها بين 1 و5 في إعدادات قاعدة البيانات.
    /// </summary>
    public byte Score { get; set; }

    /// <summary>
    /// تعليق نصي اختياري يوضح رأي الزائر.
    /// </summary>
    [StringLength(1000)] public string Comment { get; set; }

    /// <summary>
    /// تاريخ ووقت تسجيل التقييم.
    /// </summary>
    public DateTime RatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// الزائر الذي قدم التقييم.
    /// </summary>
    [ForeignKey(nameof(VisitorID))] public virtual Visitor Visitor { get; set; }

    /// <summary>
    /// المعرض المرتبط بالتقييم.
    /// </summary>
    [ForeignKey(nameof(ExhibitionID))] public virtual Exhibition Exhibition { get; set; }

    /// <summary>
    /// العارض المرتبط بالتقييم عند وجوده.
    /// </summary>
    [ForeignKey(nameof(ExhibitorID))] public virtual Exhibitor Exhibitor { get; set; }

}
