using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using ExhibitionManagementSystem.Models.Interfaces;
using ExhibitionManagementSystem.Models.Enums;

namespace ExhibitionManagementSystem.Models;

/// <summary>
/// يمثل تذكرة دخول زائر إلى معرض.
/// </summary>
public class Ticket : IAuditableEntity, ISoftDeletable
{
    /// <summary>
    /// المعرف الفريد للتذكرة.
    /// </summary>
    [Key] public int TicketID { get; set; }

    /// <summary>
    /// معرف الزائر صاحب التذكرة.
    /// </summary>
    public int VisitorID { get; set; }

    /// <summary>
    /// معرف المعرض المرتبط بالتذكرة.
    /// </summary>
    public int ExhibitionID { get; set; }

    /// <summary>
    /// نوع التذكرة.
    /// </summary>
    [Required, StringLength(50)] public string TicketType { get; set; }

    /// <summary>
    /// سعر التذكرة.
    /// </summary>
    [Column(TypeName = "decimal(18,2)")] public decimal Price { get; set; } = 0;

    /// <summary>
    /// رمز العملة المستخدمة في سعر التذكرة.
    /// </summary>
    [StringLength(3)] public string? CurrencyCode { get; set; }

    /// <summary>
    /// قيمة رمز QR الخاص بالتذكرة.
    /// </summary>
    [Required, StringLength(500)] public string QRCode { get; set; }

    /// <summary>
    /// تاريخ صلاحية التذكرة عند تقييدها بيوم محدد.
    /// </summary>
    [Column(TypeName = "date")] public DateTime? ValidDate { get; set; }

    /// <summary>
    /// حالة التذكرة الحالية.
    /// </summary>
    public TicketStatus Status { get; set; }

    /// <summary>
    /// تاريخ ووقت إصدار التذكرة.
    /// </summary>
    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// تاريخ إنشاء سجل التذكرة.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// تاريخ آخر تعديل على سجل التذكرة.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// يحدد ما إذا كانت التذكرة محذوفة حذفًا ناعمًا.
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
    /// الزائر صاحب التذكرة.
    /// </summary>
    [ForeignKey(nameof(VisitorID))] public virtual Visitor Visitor { get; set; }

    /// <summary>
    /// المعرض المرتبط بالتذكرة.
    /// </summary>
    [ForeignKey(nameof(ExhibitionID))] public virtual Exhibition Exhibition { get; set; }

    /// <summary>
    /// العملة المستخدمة في سعر التذكرة.
    /// </summary>
    [ForeignKey(nameof(CurrencyCode))] public virtual Currency Currency { get; set; }

    /// <summary>
    /// عمليات مسح الدخول والخروج المرتبطة بالتذكرة.
    /// </summary>
    public virtual ICollection<TicketScan> TicketScans { get; set; } = new HashSet<TicketScan>();
}
