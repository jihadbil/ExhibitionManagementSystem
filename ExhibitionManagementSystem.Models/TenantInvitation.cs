using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ExhibitionManagementSystem.Models.Interfaces;

namespace ExhibitionManagementSystem.Models;

/// <summary>
/// دعوة أحادية الاستخدام لإنشاء أول حساب إداري للمستأجر.
/// لا يُخزن رمز الدعوة الخام مطلقًا؛ يُخزن ملخص SHA-256 فقط.
/// </summary>
public sealed class TenantInvitation : IAuditableEntity
{
    [Key]
    public Guid InvitationID { get; set; } = Guid.NewGuid();

    public int TenantID { get; set; }

    [Required, StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(256)]
    public string NormalizedEmail { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required, StringLength(64)]
    public string TokenHash { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }
    public DateTime? AcceptedAt { get; set; }
    public DateTime? RevokedAt { get; set; }

    [StringLength(450)]
    public string? AcceptedByUserId { get; set; }

    [Required, StringLength(450)]
    public string CreatedByUserId { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    [Timestamp]
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    [ForeignKey(nameof(TenantID))]
    public Tenant Tenant { get; set; } = null!;
}
