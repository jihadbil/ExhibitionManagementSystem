using ExhibitionManagementSystem.DataAccess;
using ExhibitionManagementSystem.DataAccess.MultiTenancy;
using ExhibitionManagementSystem.DataAccess.Repositories.Implementations;
using ExhibitionManagementSystem.Models;
using ExhibitionManagementSystem.Services.Implementations;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ExhibitionManagementSystem.Api.Tests.Security;

public sealed class TenantMutationGuardTests
{
    [Fact]
    public async Task Unmerge_DoesNotLoadAnotherTenantsMerge()
    {
        await using var context = CreateContext();
        context.Exhibitions.Add(new Exhibition
        {
            ExhibitionID = 101,
            TenantID = 1,
            Name = "Tenant one exhibition"
        });
        context.BoothMerges.Add(new BoothMerge
        {
            MergeID = 501,
            ExhibitionID = 101,
            MergedBoothLabel = "A-1+A-2",
            MergedByUserId = "tenant-one-user"
        });
        await context.SaveChangesAsync();

        using var unitOfWork = new UnitOfWork(context, new HttpContextAccessor());
        var service = new BoothService(unitOfWork, null!);

        var result = await service.UnmergeBoothsAsync(tenantId: 2, mergeId: 501);

        Assert.False(result.IsSuccess);
        Assert.Equal("MERGE_NOT_FOUND", result.ErrorCode);
        Assert.NotNull(await context.BoothMerges.FindAsync(501));
    }

    [Fact]
    public async Task RemoveSpaceAssignment_DoesNotLoadAnotherTenantsAssignment()
    {
        await using var context = CreateContext();
        context.SponsorshipContracts.Add(new SponsorshipContract
        {
            ContractID = 301,
            TenantID = 1,
            ExhibitionID = 101,
            SponsorID = 201,
            ContractNumber = "TENANT-1-CONTRACT",
            CurrencyCode = "LYD"
        });
        context.AdvertisingSpaces.Add(new AdvertisingSpace
        {
            SpaceID = 401,
            TenantID = 1,
            ExhibitionID = 101,
            SpaceCode = "SCREEN-1",
            SpaceName = "Main screen",
            CurrencyCode = "LYD",
            IsAvailable = false
        });
        context.SponsorAdAssignments.Add(new SponsorAdAssignment
        {
            AssignmentID = 601,
            ContractID = 301,
            SpaceID = 401
        });
        await context.SaveChangesAsync();

        using var unitOfWork = new UnitOfWork(context, new HttpContextAccessor());
        var service = new SponsorshipService(unitOfWork, null!);

        var result = await service.RemoveSpaceAssignmentAsync(
            tenantId: 2,
            assignmentId: 601,
            userId: "tenant-two-user");

        Assert.False(result.IsSuccess);
        Assert.Equal("ASSIGNMENT_NOT_FOUND", result.ErrorCode);
        Assert.NotNull(await context.SponsorAdAssignments.FindAsync(601));
        Assert.False((await context.AdvertisingSpaces.FindAsync(401))!.IsAvailable);
    }

    [Fact]
    public async Task AssignSpace_RejectsSpaceFromAnotherExhibition()
    {
        await using var context = CreateContext();
        context.SponsorshipContracts.Add(new SponsorshipContract
        {
            ContractID = 302,
            TenantID = 1,
            ExhibitionID = 101,
            SponsorID = 201,
            ContractNumber = "EXHIBITION-101-CONTRACT",
            CurrencyCode = "LYD"
        });
        context.AdvertisingSpaces.Add(new AdvertisingSpace
        {
            SpaceID = 402,
            TenantID = 1,
            ExhibitionID = 102,
            SpaceCode = "OTHER-EXHIBITION",
            SpaceName = "Other exhibition screen",
            CurrencyCode = "LYD",
            IsAvailable = true
        });
        await context.SaveChangesAsync();

        using var unitOfWork = new UnitOfWork(context, new HttpContextAccessor());
        var service = new SponsorshipService(unitOfWork, null!);

        var result = await service.AssignSpaceAsync(1, new()
        {
            ContractID = 302,
            SpaceID = 402
        });

        Assert.False(result.IsSuccess);
        Assert.Equal("SPACE_NOT_FOUND", result.ErrorCode);
        Assert.True((await context.AdvertisingSpaces.FindAsync(402))!.IsAvailable);
        Assert.Empty(context.SponsorAdAssignments);
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new ApplicationDbContext(options, new BypassTenantContext());
    }

    private sealed class BypassTenantContext : ITenantContext
    {
        public int? TenantId => null;
        public bool CanBypassTenantIsolation => true;
    }
}
