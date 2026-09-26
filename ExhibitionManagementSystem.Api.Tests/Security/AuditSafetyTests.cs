using System.Security.Claims;
using ExhibitionManagementSystem.DataAccess;
using ExhibitionManagementSystem.DataAccess.MultiTenancy;
using ExhibitionManagementSystem.DataAccess.Repositories.Implementations;
using ExhibitionManagementSystem.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ExhibitionManagementSystem.Api.Tests.Security;

public sealed class AuditSafetyTests
{
    [Fact]
    public async Task SaveChanges_DoesNotGuessActorOrTenant()
    {
        await using var context = CreateContext(1);
        context.Venues.Add(CreateVenue(1));
        var unitOfWork = new UnitOfWork(context, new HttpContextAccessor());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => unitOfWork.SaveChangesAsync());

        Assert.Contains("authenticated user", exception.Message);
        Assert.Empty(context.AuditLogs.IgnoreQueryFilters());
    }

    [Fact]
    public async Task SaveChanges_RedactsCredentialAndTokenProperties()
    {
        await using var context = CreateContext(1);
        var accessor = AuthenticatedContext("actor-id", 1);
        var unitOfWork = new UnitOfWork(context, accessor);
        context.Users.Add(new ApplicationUser
        {
            Id = "new-user",
            TenantID = 1,
            UserName = "new-user@example.test",
            FullName = "Test User",
            PasswordHash = "must-not-be-audited",
            SecurityStamp = "must-not-be-audited"
        });

        await unitOfWork.SaveChangesAsync();

        var audit = Assert.Single(context.AuditLogs.IgnoreQueryFilters());
        Assert.DoesNotContain("Password", audit.NewValues, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("SecurityStamp", audit.NewValues, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("RefreshToken", audit.NewValues, StringComparison.OrdinalIgnoreCase);
    }

    private static ApplicationDbContext CreateContext(int tenantId) =>
        new(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
                .Options,
            new StubTenantContext(tenantId));

    private static HttpContextAccessor AuthenticatedContext(string userId, int tenantId)
    {
        var identity = new ClaimsIdentity(
            new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim("TenantId", tenantId.ToString())
            },
            "Test");

        return new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(identity)
            }
        };
    }

    private static Venue CreateVenue(int tenantId) => new()
    {
        TenantID = tenantId,
        Name = "Venue",
        Address = "Address",
        City = "City",
        Country = "Country",
        MapImageURL = string.Empty
    };

    private sealed record StubTenantContext(
        int? TenantId,
        bool CanBypassTenantIsolation = false) : ITenantContext;
}
