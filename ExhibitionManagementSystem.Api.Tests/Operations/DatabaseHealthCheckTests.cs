using ExhibitionManagementSystem.DataAccess;
using ExhibitionManagementSystem.DataAccess.MultiTenancy;
using ExhibitionManagementSystem.Health;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ExhibitionManagementSystem.Api.Tests.Operations;

public sealed class DatabaseHealthCheckTests
{
    [Fact]
    public async Task CheckHealth_ReturnsHealthyWhenDatabaseIsReachable()
    {
        await using var context = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
                .Options,
            new StubTenantContext(1));
        var check = new DatabaseHealthCheck(
            context,
            NullLogger<DatabaseHealthCheck>.Instance);

        var result = await check.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Healthy, result.Status);
    }

    private sealed record StubTenantContext(
        int? TenantId,
        bool CanBypassTenantIsolation = false) : ITenantContext;
}
