using ExhibitionManagementSystem.DataAccess;
using ExhibitionManagementSystem.DataAccess.MultiTenancy;
using ExhibitionManagementSystem.DataAccess.Repositories.Implementations;
using ExhibitionManagementSystem.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ExhibitionManagementSystem.Api.Tests.Security;

public sealed class RepositoryTenantIsolationTests
{
    [Fact]
    public async Task IgnoringSoftDelete_PreservesTenantIsolation()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        await using (var seedContext = new ApplicationDbContext(options, new TenantContext(null, true)))
        {
            seedContext.Sponsors.AddRange(
                new Sponsor { SponsorID = 1, TenantID = 1, CompanyName = "Tenant 1 deleted", IsDeleted = true },
                new Sponsor { SponsorID = 2, TenantID = 2, CompanyName = "Tenant 2 deleted", IsDeleted = true },
                new Sponsor { SponsorID = 3, TenantID = 2, CompanyName = "Tenant 2 active" });
            await seedContext.SaveChangesAsync();
        }

        await using var tenantContext = new ApplicationDbContext(options, new TenantContext(1, false));
        var repository = new GenericRepository<Sponsor>(tenantContext);

        var deleted = await repository.GetDeletedAsync();
        var includingDeleted = await repository.AsQueryableIgnoringSoftDelete().ToListAsync();
        var otherTenantEntity = await repository.GetByIdAsync(3);

        Assert.Collection(deleted, sponsor => Assert.Equal(1, sponsor.TenantID));
        Assert.All(includingDeleted, sponsor => Assert.Equal(1, sponsor.TenantID));
        Assert.Null(otherTenantEntity);
    }

    private sealed class TenantContext(int? tenantId, bool canBypass) : ITenantContext
    {
        public int? TenantId { get; } = tenantId;
        public bool CanBypassTenantIsolation { get; } = canBypass;
    }
}
