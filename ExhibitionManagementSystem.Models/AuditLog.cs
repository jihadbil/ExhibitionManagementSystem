using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ExhibitionManagementSystem.Models;

/// <summary>
/// يمثل سجل تدقيق لتتبع العمليات التي تتم على سجلات النظام.
/// </summary>
public class AuditLog
{

    /// <summary>
    /// المعرف الفريد لسجل التدقيق.
    /// </summary>
    [Key] public long LogID { get; set; }

    /// <summary>
    /// معرف المستأجر الذي تمت العملية ضمن نطاقه.
    /// </summary>
    public int TenantID { get; set; }

    /// <summary>
    /// معرف المستخدم الذي نفذ العملية.
    /// </summary>
    [StringLength(450)] public string UserId { get; set; }

    /// <summary>
    /// اسم الجدول أو الكيان الذي تأثر بالعملية.
    /// </summary>
    [Required, StringLength(100)] public string TableName { get; set; }

    /// <summary>
    /// معرف السجل الذي تأثر بالعملية.
    /// </summary>
    [Required, StringLength(100)] public string RecordID { get; set; }

    /// <summary>
    /// نوع العملية المنفذة مثل إضافة أو تعديل أو حذف.
    /// </summary>
    [Required, StringLength(20)] public string Action { get; set; }

    /// <summary>
    /// القيم السابقة للسجل قبل تنفيذ العملية.
    /// </summary>
    public string OldValues { get; set; }

    /// <summary>
    /// القيم الجديدة للسجل بعد تنفيذ العملية.
    /// </summary>
    public string NewValues { get; set; }

    /// <summary>
    /// تاريخ ووقت تنفيذ العملية.
    /// </summary>
    public DateTime ActionAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// عنوان IP الذي صدرت منه العملية.
    /// </summary>
    [StringLength(50)] public string IPAddress { get; set; }

    /// <summary>
    /// المستأجر المرتبط بسجل التدقيق.
    /// </summary>
    [ForeignKey(nameof(TenantID))] public virtual Tenant Tenant { get; set; }

    /// <summary>
    /// المستخدم المرتبط بسجل التدقيق.
    /// </summary>
    [ForeignKey(nameof(UserId))] public virtual ApplicationUser User { get; set; }

}
