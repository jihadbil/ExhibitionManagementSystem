using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ExhibitionManagementSystem.Models;

/// <summary>
/// جلسة تحديث مستقلة لجهاز واحد. لا تطبق مرشح Tenant لأنها تُقرأ أثناء طلب تحديث مجهول،
/// لذلك يجب أن تتضمن كل استعلاماتها UserId وTenantID صراحة.
/// </summary>
public sealed class RefreshSession
{
    [Key]
    public Guid RefreshSessionID { get; set; } = Guid.NewGuid();

    [Required, StringLength(450)]
    public string UserId { get; set; } = string.Empty;

    public int TenantID { get; set; }

    [Required, StringLength(128)]
    public string TokenHash { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public Guid? ReplacedBySessionID { get; set; }

    [StringLength(50)]
    public string? CreatedByIp { get; set; }

    [StringLength(256)]
    public string? DeviceDescription { get; set; }

    [StringLength(100)]
    public string? RevocationReason { get; set; }

    [Timestamp]
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    [ForeignKey(nameof(UserId))]
    public ApplicationUser User { get; set; } = null!;
}
