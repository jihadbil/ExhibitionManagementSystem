using System.ComponentModel.DataAnnotations;

namespace ExhibitionManagementSystem.Models;

/// <summary>
/// نتيجة طلب حساس قابلة للإعادة بأمان عند تكرار مفتاح Idempotency نفسه.
/// لا يخضع هذا السجل لمرشح المستأجر العام؛ كل وصول إليه يقيّد TenantID صراحة.
/// </summary>
public sealed class IdempotencyRecord
{
    public long IdempotencyRecordID { get; set; }
    public int TenantID { get; set; }

    [Required, StringLength(450)]
    public string UserId { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string Operation { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string Key { get; set; } = string.Empty;

    [Required, StringLength(64)]
    public string RequestHash { get; set; } = string.Empty;

    [Required, StringLength(20)]
    public string State { get; set; } = "Pending";

    public int? StatusCode { get; set; }

    [StringLength(200)]
    public string? ContentType { get; set; }

    public string? ResponseBody { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}
