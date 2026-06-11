using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using ExhibitionManagementSystem.Models.Interfaces;

namespace ExhibitionManagementSystem.Models;

/// <summary>
/// يمثل موقعًا يستضيف المعارض ويحتوي على قاعات.
/// </summary>
public class Venue : IAuditableEntity, ISoftDeletable
{

    /// <summary>
    /// المعرف الفريد للموقع.
    /// </summary>
    [Key] public int VenueID { get; set; }

    /// <summary>
    /// معرف المستأجر الذي يدير الموقع.
    /// </summary>
    public int TenantID { get; set; }

    /// <summary>
    /// اسم الموقع.
    /// </summary>
    [Required, StringLength(200)] public string Name { get; set; }

    /// <summary>
    /// العنوان التفصيلي للموقع.
    /// </summary>
    [StringLength(500)] public string Address { get; set; }

    /// <summary>
    /// المدينة التي يقع فيها الموقع.
    /// </summary>
    [StringLength(100)] public string City { get; set; }

    /// <summary>
    /// الدولة التي يقع فيها الموقع.
    /// </summary>
    [StringLength(100)] public string Country { get; set; }

    /// <summary>
    /// الطاقة الاستيعابية الإجمالية للموقع.
    /// </summary>
    public int? TotalCapacity { get; set; }

    /// <summary>
    /// رابط صورة خريطة الموقع.
    /// </summary>
    [StringLength(500)] public string MapImageURL { get; set; }

    /// <summary>
    /// يحدد ما إذا كان الموقع نشطًا ومتاحًا للاستخدام.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// تاريخ إنشاء سجل الموقع.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// تاريخ آخر تعديل على سجل الموقع.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// يحدد ما إذا كان الموقع محذوفًا حذفًا ناعمًا.
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
    /// المستأجر المرتبط بالموقع.
    /// </summary>
    [ForeignKey(nameof(TenantID))] public virtual Tenant Tenant { get; set; }

    /// <summary>
    /// القاعات الموجودة داخل الموقع.
    /// </summary>
    public virtual ICollection<Hall> Halls { get; set; } = new HashSet<Hall>();

}
