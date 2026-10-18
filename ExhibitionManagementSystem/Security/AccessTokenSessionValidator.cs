using System.Security.Claims;
using ExhibitionManagementSystem.DataAccess;
using ExhibitionManagementSystem.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ExhibitionManagementSystem.Security;

public sealed class AccessTokenSessionValidator(ApplicationDbContext db, UserManager<ApplicationUser> users)
{
    public async Task<bool> ValidateAsync(ClaimsPrincipal principal)
    {
        var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? principal.FindFirstValue("nameid")
                     ?? principal.FindFirstValue("sub");
        if (string.IsNullOrEmpty(userId) ||
            !int.TryParse(principal.FindFirstValue("TenantId"), out var tenantId) ||
            !Guid.TryParse(principal.FindFirstValue("session_id"), out var sessionId)) return false;

        var user = await users.FindByIdAsync(userId);
        if (user == null || !user.IsActive || user.TenantID != tenantId ||
            !string.Equals(user.SecurityStamp, principal.FindFirstValue("security_stamp"), StringComparison.Ordinal)) return false;

        if (!await db.Tenants.IgnoreQueryFilters().AsNoTracking().AnyAsync(t =>
            t.TenantID == tenantId && t.IsActive)) return false;
        if (!await db.RefreshSessions.AsNoTracking().AnyAsync(s =>
            s.RefreshSessionID == sessionId && s.UserId == userId && s.TenantID == tenantId &&
            s.RevokedAt == null && s.ExpiresAt > DateTime.UtcNow)) return false;

        var tokenRoles = principal.FindAll(ClaimTypes.Role)
            .Concat(principal.FindAll("role"))
            .Select(c => c.Value)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var currentRoles = await users.GetRolesAsync(user);
        return tokenRoles.SetEquals(currentRoles);
    }
}
