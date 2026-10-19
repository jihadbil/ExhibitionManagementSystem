using System.ComponentModel.DataAnnotations;

namespace ExhibitionManagementSystem.Models;

public sealed class PasswordResetMessage
{
    public long Id { get; set; }
    [Required, MaxLength(450)] public string UserId { get; set; } = string.Empty;
    [Required, MaxLength(128)] public string SecurityStamp { get; set; } = string.Empty;
    [Required] public string ProtectedPayload { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime NextAttemptAt { get; set; }
    public DateTime? LeasedUntil { get; set; }
    public DateTime? SentAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int Attempts { get; set; }
    [MaxLength(50)] public string? LastErrorCode { get; set; }
    [Timestamp] public byte[] RowVersion { get; set; } = [];
}
