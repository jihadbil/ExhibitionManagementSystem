using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace ExhibitionManagementSystem.Models;

/// <summary>
/// يمثل جناحًا واحدًا داخل عملية دمج أجنحة.
/// </summary>
public class BoothMergeItem
{
    /// <summary>
    /// المعرف الفريد لعنصر الدمج.
    /// </summary>
    [Key] public int ItemID { get; set; }

    /// <summary>
    /// معرف عملية الدمج التي ينتمي إليها العنصر.
    /// </summary>
    public int MergeID { get; set; }

    /// <summary>
    /// معرف الجناح المضاف إلى عملية الدمج.
    /// </summary>
    public int BoothID { get; set; }

    /// <summary>
    /// ترتيب الجناح داخل مجموعة الدمج.
    /// </summary>
    public int SequenceOrder { get; set; }

    /// <summary>
    /// مساحة الجناح الأصلية وقت إضافته إلى الدمج.
    /// </summary>
    [Column(TypeName = "decimal(10,2)")] public decimal OriginalAreaSqM { get; set; }

    /// <summary>
    /// عملية الدمج المرتبطة بهذا العنصر.
    /// </summary>
    [ForeignKey(nameof(MergeID))] public virtual BoothMerge Merge { get; set; }

    /// <summary>
    /// الجناح المرتبط بهذا العنصر.
    /// </summary>
    [ForeignKey(nameof(BoothID))] public virtual Booth Booth { get; set; }



}
