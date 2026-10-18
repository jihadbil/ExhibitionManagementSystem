using ExhibitionManagementSystem.DataAccess.Repositories.Implementations;
using ExhibitionManagementSystem.Models;
using ExhibitionManagementSystem.Models.DTOs.Exhibition;
using ExhibitionManagementSystem.Services.Implementations;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ExhibitionManagementSystem.Api.Tests.Regression;

public sealed class ExhibitionDateSafetyTests
{
    [Fact]
    public async Task ExistingTicketDayPreventsShorteningButAllowsExtendingTheExhibition()
    {
        using var db = WorkflowFixture.CreateDb();
        var fixture = await WorkflowFixture.SeedAsync(db);
        var exhibition = await db.Exhibitions.SingleAsync();
        exhibition.StartDate = DateTime.UtcNow.Date.AddDays(-1);
        exhibition.EndDate = DateTime.UtcNow.Date.AddDays(5);
        db.Tickets.Add(new Ticket { TenantID = fixture.TenantID, ExhibitionID = exhibition.ExhibitionID,
            Visitor = new Visitor { TenantID = fixture.TenantID, FullName = "Synthetic visitor" },
            TicketType = "Regular", QRCode = "SYNTHETIC-DAY-FOUR", CurrencyCode = "LYD", ValidDate = DateTime.UtcNow.Date.AddDays(4) });
        await db.SaveChangesAsync();
        using var uow = new UnitOfWork(db, WorkflowFixture.Actor(fixture.TenantID));
        var service = new ExhibitionService(uow, SystemAuditRepairTests.Mapper);
        var update = new ExhibitionUpdateDto { VenueID = exhibition.VenueID, Name = "Exhibition",
            StartDate = exhibition.StartDate, EndDate = DateTime.UtcNow.Date.AddDays(1), Status = "Open", EntryCurrency = "LYD" };
        var shortened = await service.UpdateAsync(fixture.TenantID, exhibition.ExhibitionID, update);
        Assert.Equal("EXHIBITION_DATES_LOCKED", shortened.ErrorCode);
        Assert.Equal(DateTime.UtcNow.Date.AddDays(5), (await db.Exhibitions.SingleAsync()).EndDate);
        update.EndDate = DateTime.UtcNow.Date.AddDays(6);
        Assert.True((await service.UpdateAsync(fixture.TenantID, exhibition.ExhibitionID, update)).IsSuccess);
        Assert.Equal(DateTime.UtcNow.Date.AddDays(6), (await db.Exhibitions.SingleAsync()).EndDate);
    }
}
