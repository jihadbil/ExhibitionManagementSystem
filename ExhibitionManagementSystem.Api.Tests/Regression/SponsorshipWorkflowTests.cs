using ExhibitionManagementSystem.DataAccess.Repositories.Implementations;
using ExhibitionManagementSystem.Models;
using ExhibitionManagementSystem.Models.Enums;
using ExhibitionManagementSystem.Services.Implementations;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ExhibitionManagementSystem.Api.Tests.Regression;

public sealed class SponsorshipWorkflowTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CancelOrDeleteContract_ReleasesItsSpace(bool delete)
    {
        using var db = WorkflowFixture.CreateDb();
        var invoice = await WorkflowFixture.SeedAsync(db);
        var contract = new SponsorshipContract { TenantID = invoice.TenantID, ExhibitionID = invoice.Reservation.ExhibitionID,
            Sponsor = new Sponsor { TenantID = invoice.TenantID, CompanyName = "Sponsor" }, ContractNumber = "SP-1", CurrencyCode = "LYD" };
        var space = new AdvertisingSpace { TenantID = invoice.TenantID, ExhibitionID = invoice.Reservation.ExhibitionID,
            SpaceCode = "SP-1", SpaceName = "Space", CurrencyCode = "LYD", IsAvailable = false };
        db.SponsorAdAssignments.Add(new SponsorAdAssignment { Contract = contract, Space = space });
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        using var uow = new UnitOfWork(db, WorkflowFixture.Actor(invoice.TenantID));
        var service = new SponsorshipService(uow, WorkflowFixture.Mapper);
        if (delete)
        {
            var result = await service.DeleteContractAsync(invoice.TenantID, contract.ContractID, "workflow-user");
            Assert.True(result.IsSuccess, result.ErrorMessage);
        }
        else
        {
            var result = await service.UpdateContractStatusAsync(invoice.TenantID, contract.ContractID, "Cancelled");
            Assert.True(result.IsSuccess, result.ErrorMessage);
        }
        db.ChangeTracker.Clear();
        Assert.True((await db.AdvertisingSpaces.SingleAsync()).IsAvailable);
        Assert.Empty(db.SponsorAdAssignments);
        Assert.True((await db.SponsorAdAssignments.IgnoreQueryFilters().SingleAsync()).IsDeleted);
    }
}
