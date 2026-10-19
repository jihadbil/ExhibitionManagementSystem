using System.Text.Json;
using ExhibitionManagementSystem.DataAccess;
using ExhibitionManagementSystem.Models;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ExhibitionManagementSystem.Services.Messaging;

public interface IPasswordResetDelivery
{
    bool IsAvailable { get; }
    Task QueueAsync(ApplicationUser user, string token, CancellationToken cancellationToken = default);
}

internal sealed record PasswordResetPayload(string Email, string Token);

public sealed class PasswordResetDelivery(ApplicationDbContext db, IDataProtectionProvider protection,
    IOptions<PasswordResetOptions> options, TimeProvider clock) : IPasswordResetDelivery
{
    internal const string ProtectionPurpose = "ExhibitionManagementSystem.PasswordResetOutbox.v1";
    public bool IsAvailable => options.Value.Enabled;

    public async Task QueueAsync(ApplicationUser user, string token, CancellationToken cancellationToken = default)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        if (!IsAvailable) throw new InvalidOperationException("Password reset delivery is disabled.");
        if (await db.PasswordResetMessages.AnyAsync(m => m.UserId == user.Id && m.CompletedAt == null
            && m.CreatedAt > now.AddMinutes(-1), cancellationToken)) return;
        db.PasswordResetMessages.Add(new PasswordResetMessage
        {
            UserId = user.Id, SecurityStamp = user.SecurityStamp ?? string.Empty,
            ProtectedPayload = protection.CreateProtector(ProtectionPurpose)
                .Protect(JsonSerializer.Serialize(new PasswordResetPayload(user.Email!, token))),
            CreatedAt = now, ExpiresAt = now.AddMinutes(30), NextAttemptAt = now
        });
        // This anonymous identity operation intentionally bypasses business audit
        // serialization: neither token nor mail payload belongs in audit logs.
        await db.SaveChangesAsync(cancellationToken);
    }
}
