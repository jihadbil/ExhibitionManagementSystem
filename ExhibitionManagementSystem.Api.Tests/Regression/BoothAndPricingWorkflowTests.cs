using ExhibitionManagementSystem.DataAccess.Repositories.Implementations;
using ExhibitionManagementSystem.Models;
using ExhibitionManagementSystem.Models.DTOs.Booth;
using ExhibitionManagementSystem.Models.DTOs.Pricing;
using ExhibitionManagementSystem.Models.Enums;
using ExhibitionManagementSystem.Services.Implementations;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ExhibitionManagementSystem.Api.Tests.Regression;

public sealed class BoothAndPricingWorkflowTests
{
    [Fact]
    public async Task ProductRule_AcceptsTrustedExhibitorSector()
    {
        using var db = WorkflowFixture.CreateDb();
        var invoice = await WorkflowFixture.SeedAsync(db, InvoiceStatus.Draft);
        var exhibitor = await db.Exhibitors.SingleAsync();
        exhibitor.Sector = "Technology";
        var rule = new BoothPriceRule { TenantID = invoice.TenantID, ProductCategory = "Technology",
            PricePerSqM = 10, CurrencyCode = "USD", ValidFrom = DateTime.UtcNow.Date.AddDays(-1) };
        var booth = new Booth { TenantID = invoice.TenantID, HallID = (await db.Halls.SingleAsync()).HallID,
            BoothNumber = "B-2", OriginalAreaSqM = 10, CurrentAreaSqM = 10, AssignedPriceRule = rule };
        db.Booths.Add(booth);
        db.ExchangeRates.Add(new ExchangeRate { FromCurrency = "USD", ToCurrency = "LYD", Rate = 5,
            RateDate = DateTime.UtcNow.Date, Source = "Test", CreatedByUserId = "workflow-user" });
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        using var uow = new UnitOfWork(db, WorkflowFixture.Actor(invoice.TenantID));
        var result = await new PricingService(uow, WorkflowFixture.Mapper).CalculateBoothPriceDetailsAsync(
            invoice.TenantID, invoice.Reservation.ExhibitionID, booth.BoothType, exhibitor.ExhibitorCategory,
            booth.CurrentAreaSqM, booth.BoothID, exhibitor.Sector);
        Assert.True(result.IsSuccess, result.ErrorMessage);
        Assert.Equal(100, result.Data!.Amount);
    }

    [Fact]
    public async Task Merge_UsesCurrentAreaAndIsScopedToExhibitionAndCanBeRemoved()
    {
        using var db = WorkflowFixture.CreateDb();
        var invoice = await WorkflowFixture.SeedAsync(db, InvoiceStatus.Draft);
        var hall = await db.Halls.SingleAsync();
        var secondExpo = new Exhibition { TenantID = invoice.TenantID, VenueID = hall.VenueID, Name = "Other expo", EntryCurrency = "LYD" };
        var booths = new[] {
            new Booth { TenantID = invoice.TenantID, HallID = hall.HallID, BoothNumber = "B-2", OriginalAreaSqM = 10, CurrentAreaSqM = 4 },
            new Booth { TenantID = invoice.TenantID, HallID = hall.HallID, BoothNumber = "B-3", OriginalAreaSqM = 10, CurrentAreaSqM = 4 }
        };
        db.Booths.AddRange(booths);
        db.Exhibitions.Add(secondExpo);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        using var uow = new UnitOfWork(db, WorkflowFixture.Actor(invoice.TenantID));
        var service = new BoothService(uow, WorkflowFixture.Mapper);
        var result = await service.MergeBoothsAsync(invoice.TenantID, "workflow-user", new BoothMergeCreateDto
        { HallID = hall.HallID, ExhibitionID = invoice.Reservation.ExhibitionID, BoothIDs = booths.Select(b => b.BoothID).ToList(), MergedBoothLabel = "B-2+B-3" });
        Assert.True(result.IsSuccess, result.ErrorMessage);
        Assert.Equal(8, result.Data!.MergedAreaSqM);
        var inOtherExpo = await service.GetAvailableAsync(invoice.TenantID, hall.HallID, secondExpo.ExhibitionID);
        Assert.Contains(inOtherExpo.Data!, b => b.BoothID == booths[0].BoothID);
        var inMergedExpo = await service.GetAvailableAsync(invoice.TenantID, hall.HallID, invoice.Reservation.ExhibitionID);
        Assert.DoesNotContain(inMergedExpo.Data!, b => b.BoothID == booths[0].BoothID);
        var removed = await service.UnmergeBoothsAsync(invoice.TenantID, result.Data.MergeID);
        Assert.True(removed.IsSuccess, removed.ErrorMessage);
        Assert.Empty(db.BoothMergeItems);
    }

    [Fact]
    public async Task Merge_RejectsHallFromDifferentVenue()
    {
        using var db = WorkflowFixture.CreateDb();
        var invoice = await WorkflowFixture.SeedAsync(db);
        var venue = new Venue { TenantID = invoice.TenantID, Name = "Other venue" };
        var hall = new Hall { Venue = venue, HallName = "Other hall" };
        db.Halls.Add(hall);
        await db.SaveChangesAsync();
        using var uow = new UnitOfWork(db, WorkflowFixture.Actor(invoice.TenantID));
        var result = await new BoothService(uow, WorkflowFixture.Mapper).MergeBoothsAsync(invoice.TenantID, "workflow-user", new BoothMergeCreateDto
        { HallID = hall.HallID, ExhibitionID = invoice.Reservation.ExhibitionID, BoothIDs = [10, 11] });
        Assert.Equal("HALL_NOT_IN_EXHIBITION_VENUE", result.ErrorCode);
    }

    [Theory]
    [InlineData(-10, null, "INVALID_PRICE")]
    [InlineData(10, "Unknown", "INVALID_EXHIBITOR_CATEGORY")]
    public async Task ServiceRule_RejectsInvalidInputs(decimal price, string? category, string error)
    {
        using var db = WorkflowFixture.CreateDb();
        using var uow = new UnitOfWork(db, WorkflowFixture.Actor(1));
        var result = await new PricingService(uow, WorkflowFixture.Mapper).CreateServicePriceRuleAsync(1, new ServicePriceRuleCreateDto
        { ServiceID = 1, CurrencyCode = "USD", UnitPrice = price, ExhibitorCategory = category });
        Assert.Equal(error, result.ErrorCode);
        Assert.Empty(db.ServicePriceRules);
    }

    [Fact]
    public async Task ServiceRule_PrefersLatestApplicableRule()
    {
        using var db = WorkflowFixture.CreateDb();
        db.ServicePriceRules.AddRange(
            new ServicePriceRule { ServiceID = 1, TenantID = 1, UnitPrice = 10, CurrencyCode = "USD", ValidFrom = DateTime.UtcNow.Date.AddDays(-10) },
            new ServicePriceRule { ServiceID = 1, TenantID = 1, UnitPrice = 20, CurrencyCode = "USD", ValidFrom = DateTime.UtcNow.Date.AddDays(-1) });
        await db.SaveChangesAsync();
        var rule = await new ServicePriceRuleRepository(db).GetApplicableRuleAsync(1, null, null, DateTime.UtcNow);
        Assert.Equal(20, rule!.UnitPrice);
    }

    [Fact]
    public async Task BridgeConversion_RoundsOnlyAfterMultiplyingRates()
    {
        using var db = WorkflowFixture.CreateDb();
        db.ExchangeRates.AddRange(
            new ExchangeRate { FromCurrency = "EUR", ToCurrency = "LYD", Rate = .333333m, RateDate = DateTime.UtcNow.Date, Source = "Test", CreatedByUserId = "workflow-user" },
            new ExchangeRate { FromCurrency = "LYD", ToCurrency = "USD", Rate = 3, RateDate = DateTime.UtcNow.Date, Source = "Test", CreatedByUserId = "workflow-user" });
        await db.SaveChangesAsync();
        var result = await new ExchangeRateRepository(db).ConvertWithDetailsAsync("EUR", "USD", 1, null, "LYD");
        Assert.Equal(1, result!.TargetAmount);
        Assert.Equal(InvoiceFinancialSummary.Round(result.SourceAmount * result.ExchangeRate), result.TargetAmount);
    }
}
