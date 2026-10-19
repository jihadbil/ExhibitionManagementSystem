using System.Text.Json;
using ExhibitionManagementSystem.DataAccess;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ExhibitionManagementSystem.Services.Messaging;

public sealed class PasswordResetDispatcher(ApplicationDbContext db, IDataProtectionProvider protection,
    IPasswordResetEmailTransport transport, IOptions<PasswordResetOptions> options, TimeProvider clock,
    ILogger<PasswordResetDispatcher> logger)
{
    public async Task ProcessBatchAsync(CancellationToken cancellationToken = default)
    {
        if (!options.Value.Enabled) return;
        for (var i = 0; i < 10; i++)
        {
            var now = clock.GetUtcNow().UtcDateTime;
            var message = await db.PasswordResetMessages.Where(m => m.CompletedAt == null && m.NextAttemptAt <= now
                && (m.LeasedUntil == null || m.LeasedUntil <= now)).OrderBy(m => m.Id).FirstOrDefaultAsync(cancellationToken);
            if (message == null) return;
            message.LeasedUntil = now.AddMinutes(5);
            message.Attempts++;
            try { await db.SaveChangesAsync(cancellationToken); }
            catch (DbUpdateConcurrencyException) { db.ChangeTracker.Clear(); continue; }

            try
            {
                var user = await db.Users.AsNoTracking().Include(u => u.Tenant)
                    .SingleOrDefaultAsync(u => u.Id == message.UserId, cancellationToken);
                if (message.ExpiresAt <= now || user == null || !user.IsActive || !user.Tenant.IsActive
                    || user.SecurityStamp != message.SecurityStamp)
                {
                    message.LastErrorCode = "EXPIRED_OR_REVOKED";
                    message.CompletedAt = now;
                }
                else
                {
                    var payload = JsonSerializer.Deserialize<PasswordResetPayload>(protection
                        .CreateProtector(PasswordResetDelivery.ProtectionPurpose).Unprotect(message.ProtectedPayload))!;
                    if (!string.Equals(payload.Email, user.Email, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException("Password reset recipient changed.");
                    var link = new UriBuilder(options.Value.ResetUrl);
                    link.Query = (string.IsNullOrEmpty(link.Query) ? "" : link.Query.TrimStart('?') + "&")
                        + "email=" + Uri.EscapeDataString(payload.Email) + "&token=" + Uri.EscapeDataString(payload.Token);
                    await transport.SendAsync(payload.Email, link.Uri, cancellationToken);
                    message.SentAt = now;
                    message.CompletedAt = now;
                    message.LastErrorCode = null;
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
            catch (Exception ex)
            {
                logger.LogWarning("Password reset delivery attempt {Attempt} failed ({ErrorType})", message.Attempts, ex.GetType().Name);
                message.LastErrorCode = "DELIVERY_FAILED";
                message.NextAttemptAt = now.AddMinutes(Math.Min(10, Math.Pow(2, message.Attempts - 1)));
                if (message.Attempts >= 5) message.CompletedAt = now;
            }
            if (message.CompletedAt.HasValue) message.ProtectedPayload = string.Empty;
            message.LeasedUntil = null;
            await db.SaveChangesAsync(cancellationToken);
        }
    }
}
