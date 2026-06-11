using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using ExhibitionManagementSystem.Models.Interfaces;

namespace ExhibitionManagementSystem.Models;

/// <summary>
/// يمثل منتجًا يعرضه عارض ضمن معرض.
/// </summary>
public class Product : IAuditableEntity, ISoftDeletable
{
    /// <summary>
    /// المعرف الفريد للمنتج.
    /// </summary>
    [Key] public int ProductID { get; set; }

    /// <summary>
    /// معرف العارض صاحب المنتج.
    /// </summary>
    public int ExhibitorID { get; set; }

    /// <summary>
    /// معرف المعرض الذي يعرض فيه المنتج.
    /// </summary>
    public int ExhibitionID { get; set; }

    /// <summary>
    /// اسم المنتج.
    /// </summary>
    [Required, StringLength(200)] public string ProductName { get; set; }

    /// <summary>
    /// تصنيف المنتج.
    /// </summary>
    [StringLength(100)] public string Category { get; set; }

    /// <summary>
    /// وصف المنتج.
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// رابط صورة المنتج.
    /// </summary>
    [StringLength(500)] public string ImageURL { get; set; }

    /// <summary>
    /// تاريخ إنشاء سجل المنتج.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// تاريخ آخر تعديل على سجل المنتج.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// يحدد ما إذا كان المنتج محذوفًا حذفًا ناعمًا.
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
    /// العارض صاحب المنتج.
    /// </summary>
    [ForeignKey(nameof(ExhibitorID))] public virtual Exhibitor Exhibitor { get; set; }

    /// <summary>
    /// المعرض الذي يعرض فيه المنتج.
    /// </summary>
    [ForeignKey(nameof(ExhibitionID))] public virtual Exhibition Exhibition { get; set; }

}
