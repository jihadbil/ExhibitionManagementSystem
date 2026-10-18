using System.Security.Claims;
using AutoMapper;
using ExhibitionManagementSystem.Controllers.Dashboard;
using ExhibitionManagementSystem.DataAccess;
using ExhibitionManagementSystem.DataAccess.Repositories.Implementations;
using ExhibitionManagementSystem.Models;
using ExhibitionManagementSystem.Models.DTOs.Exhibition;
using ExhibitionManagementSystem.Models.DTOs.Financial;
using ExhibitionManagementSystem.Models.DTOs.Mapping;
using ExhibitionManagementSystem.Models.DTOs.Pricing;
using ExhibitionManagementSystem.Models.DTOs.Visitor;
using ExhibitionManagementSystem.Models.DTOs.Venue;
using ExhibitionManagementSystem.Models.DTOs.Booth;
using ExhibitionManagementSystem.Models.Enums;
using ExhibitionManagementSystem.Services.Implementations;
using ExhibitionManagementSystem.Services.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace ExhibitionManagementSystem.Api.Tests.Regression;

public sealed class SystemAuditRepairTests
{
    internal static readonly IMapper Mapper = new MapperConfiguration(c => c.AddMaps(typeof(MappingProfile).Assembly), NullLoggerFactory.Instance).CreateMapper();
    private static async Task<Invoice> Seed(ApplicationDbContext db, InvoiceStatus status = InvoiceStatus.Issued)
    {
        var invoice = await WorkflowFixture.SeedAsync(db, status);
        var expo = await db.Exhibitions.SingleAsync();
        expo.StartDate = DateTime.UtcNow.Date.AddDays(-1); expo.EndDate = DateTime.UtcNow.Date.AddDays(5);
        await db.SaveChangesAsync();
        return invoice;
    }
    private static FinancialService Finance(ApplicationDbContext db, UnitOfWork uow) => new(uow, Mapper, new ConfigurationBuilder().Build(), db);

    [Theory]
    [InlineData(InvoiceStatus.Draft)] [InlineData(InvoiceStatus.Issued)] [InlineData(InvoiceStatus.PartiallyPaid)]
    [InlineData(InvoiceStatus.Paid)] [InlineData(InvoiceStatus.Cancelled)]
    public async Task ExhibitorWithAnyInvoice_CannotBeDeleted(InvoiceStatus status)
    {
        using var db = WorkflowFixture.CreateDb(); var invoice = await Seed(db, status);
        using var uow = new UnitOfWork(db, WorkflowFixture.Actor(invoice.TenantID));
        var result = await new ExhibitorService(uow, Mapper).DeleteAsync(invoice.TenantID, invoice.Reservation.ExhibitorID);
        Assert.Equal("EXHIBITOR_HAS_INVOICES", result.ErrorCode);
        Assert.False((await db.Exhibitors.IgnoreQueryFilters().SingleAsync()).IsDeleted);
        Assert.Single((await Finance(db, uow).GetInvoicesByTenantAsync(invoice.TenantID, 1, 20)).Data!.Items);
    }

    [Fact]
    public async Task ExhibitorWithoutDependencies_CanBeDeleted()
    {
        using var db = WorkflowFixture.CreateDb(); var invoice = await Seed(db);
        var extra = new Exhibitor { TenantID = invoice.TenantID, CompanyName = "Unlinked" }; db.Exhibitors.Add(extra); await db.SaveChangesAsync();
        using var uow = new UnitOfWork(db, WorkflowFixture.Actor(invoice.TenantID));
        Assert.True((await new ExhibitorService(uow, Mapper).DeleteAsync(invoice.TenantID, extra.ExhibitorID)).IsSuccess);
        Assert.False(await db.Exhibitors.AnyAsync(e => e.ExhibitorID == extra.ExhibitorID));
    }

    [Fact]
    public async Task VenueWithDependencies_CannotBeDeleted()
    {
        using var db = WorkflowFixture.CreateDb(); var invoice = await Seed(db);
        using var uow = new UnitOfWork(db, WorkflowFixture.Actor(invoice.TenantID));
        Assert.Equal("VENUE_HAS_DEPENDENCIES", (await new VenueService(uow, Mapper).DeleteAsync(invoice.TenantID, invoice.Reservation.Exhibition.VenueID)).ErrorCode);
        Assert.False((await db.Venues.SingleAsync()).IsDeleted);
    }

    [Fact]
    public async Task ExhibitionWithHistory_CannotBeDeleted()
    {
        using var db = WorkflowFixture.CreateDb(); var invoice = await Seed(db);
        using var uow = new UnitOfWork(db, WorkflowFixture.Actor(invoice.TenantID));
        Assert.Equal("EXHIBITION_HAS_DEPENDENCIES", (await new ExhibitionService(uow, Mapper).DeleteAsync(invoice.TenantID, invoice.Reservation.ExhibitionID)).ErrorCode);
        Assert.Single(await db.Exhibitions.ToListAsync()); Assert.Single(await db.Invoices.ToListAsync());
    }

    [Fact]
    public async Task ExhibitionWithReservation_CannotChangeVenue()
    {
        using var db = WorkflowFixture.CreateDb(); var invoice = await Seed(db);
        var venue = new Venue { TenantID = invoice.TenantID, Name = "Other" }; db.Venues.Add(venue); await db.SaveChangesAsync();
        using var uow = new UnitOfWork(db, WorkflowFixture.Actor(invoice.TenantID));
        var result = await new ExhibitionService(uow, Mapper).UpdateAsync(invoice.TenantID, invoice.Reservation.ExhibitionID,
            new() { Name = "Expo", VenueID = venue.VenueID, StartDate = DateTime.UtcNow.Date, EndDate = DateTime.UtcNow.Date.AddDays(2), Status = "Open" });
        Assert.Equal("EXHIBITION_VENUE_LOCKED", result.ErrorCode);
        Assert.NotEqual(venue.VenueID, (await db.Exhibitions.SingleAsync()).VenueID);
    }

    [Theory]
    [InlineData(-1, 1)] [InlineData(1, -1)] [InlineData(0, -2)]
    public async Task Exhibition_RejectsNegativeFeeAndReversedDates(int fee, int days)
    {
        using var db = WorkflowFixture.CreateDb(); var invoice = await Seed(db);
        using var uow = new UnitOfWork(db, WorkflowFixture.Actor(invoice.TenantID));
        var result = await new ExhibitionService(uow, Mapper).CreateAsync(invoice.TenantID, new() {
            Name = "Bad", VenueID = invoice.Reservation.Exhibition.VenueID, StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date.AddDays(days), EntryCurrency = "LYD", EntryFee = fee });
        Assert.Equal("INVALID_EXHIBITION_INPUT", result.ErrorCode); Assert.Single(await db.Exhibitions.ToListAsync());
    }

    [Fact]
    public async Task Schedule_RejectsHallAtAnotherVenue()
    {
        using var db = WorkflowFixture.CreateDb(); var invoice = await Seed(db);
        var hall = new Hall { HallName = "Foreign venue hall", Venue = new Venue { TenantID = invoice.TenantID, Name = "Other" } }; db.Halls.Add(hall); await db.SaveChangesAsync();
        using var uow = new UnitOfWork(db, WorkflowFixture.Actor(invoice.TenantID));
        var result = await new ExhibitionService(uow, Mapper).AddScheduleAsync(invoice.TenantID, new() {
            ExhibitionID = invoice.Reservation.ExhibitionID, HallID = hall.HallID, EventName = "Event", EventType = "Lecture",
            StartDateTime = DateTime.UtcNow, EndDateTime = DateTime.UtcNow.AddHours(1) });
        Assert.Equal("HALL_NOT_IN_EXHIBITION_VENUE", result.ErrorCode); Assert.Empty(await db.ExhibitionSchedules.ToListAsync());
    }

    [Theory]
    [InlineData(ExhibitionStatus.Cancelled)] [InlineData(ExhibitionStatus.Closed)]
    public async Task Tickets_RejectClosedExhibition(ExhibitionStatus status)
    {
        using var db = WorkflowFixture.CreateDb(); var invoice = await Seed(db);
        (await db.Exhibitions.SingleAsync()).Status = status;
        var visitor = new Visitor { TenantID = invoice.TenantID, FullName = "Visitor" }; db.Visitors.Add(visitor); await db.SaveChangesAsync();
        using var uow = new UnitOfWork(db, WorkflowFixture.Actor(invoice.TenantID));
        Assert.Equal("EXHIBITION_NOT_ACCEPTING_TICKETS", (await new TicketService(uow, Mapper).IssueTicketAsync(invoice.TenantID,
            new() { VisitorID = visitor.VisitorID, ExhibitionID = invoice.Reservation.ExhibitionID, TicketType = "Regular", CurrencyCode = "LYD" })).ErrorCode);
    }

    [Fact]
    public async Task TicketForFutureDay_CannotEnterToday()
    {
        using var db = WorkflowFixture.CreateDb(); var invoice = await Seed(db);
        var visitor = new Visitor { TenantID = invoice.TenantID, FullName = "Visitor" }; db.Visitors.Add(visitor); await db.SaveChangesAsync();
        using var uow = new UnitOfWork(db, WorkflowFixture.Actor(invoice.TenantID)); var service = new TicketService(uow, Mapper);
        var issued = await service.IssueTicketAsync(invoice.TenantID, new() { VisitorID = visitor.VisitorID,
            ExhibitionID = invoice.Reservation.ExhibitionID, TicketType = "Regular", CurrencyCode = "LYD", ValidDate = DateTime.UtcNow.Date.AddDays(1) });
        Assert.True(issued.IsSuccess, issued.ErrorMessage);
        Assert.Equal("TICKET_NOT_YET_VALID", (await service.ScanTicketAsync(invoice.TenantID, issued.Data!.QRCode, "In", "Gate", "workflow-user")).ErrorCode);
        Assert.Empty(await db.TicketScans.ToListAsync());
    }

    [Theory]
    [InlineData("[]")] [InlineData("null")] [InlineData("123")] [InlineData("{\"bounds\":[],\"halls\":[]}")]
    [InlineData("{\"bounds\":{\"width\":\"bad\",\"height\":10},\"halls\":[]}")]
    public async Task SitePlanWrongTypes_ReturnValidationError(string json)
    {
        using var db = WorkflowFixture.CreateDb(); var invoice = await Seed(db);
        using var uow = new UnitOfWork(db, WorkflowFixture.Actor(invoice.TenantID));
        Assert.False((await new VenueService(uow, Mapper).SaveSitePlanAsync(invoice.TenantID, invoice.Reservation.Exhibition.VenueID, new VenueSitePlanSaveDto { PlanJSON = json })).IsSuccess);
    }

    [Fact]
    public async Task VisitorWithoutTicket_CannotGetUnregisteredBadge()
    {
        using var db = WorkflowFixture.CreateDb(); var invoice = await Seed(db);
        var visitor = new Visitor { TenantID = invoice.TenantID, FullName = "Visitor" }; db.Visitors.Add(visitor); await db.SaveChangesAsync();
        using var uow = new UnitOfWork(db, WorkflowFixture.Actor(invoice.TenantID));
        Assert.Equal("TICKET_REQUIRED", (await new BadgeService(uow, Mapper).GenerateBadgePayloadForVisitorAsync(invoice.TenantID, visitor.VisitorID, invoice.Reservation.ExhibitionID)).ErrorCode);
        Assert.Empty(await db.Tickets.ToListAsync());
    }

    [Fact]
    public async Task StaffBadge_IsPersistedReusedAndScannableAndStopsWithReservation()
    {
        using var db = WorkflowFixture.CreateDb(); var invoice = await Seed(db);
        (await db.BoothReservations.SingleAsync()).Status = ReservationStatus.Confirmed;
        var staff = new BoothStaff { ReservationID = invoice.ReservationID, StaffName = "Staff" }; db.BoothStaffs.Add(staff); await db.SaveChangesAsync();
        using var uow = new UnitOfWork(db, WorkflowFixture.Actor(invoice.TenantID)); var badges = new BadgeService(uow, Mapper);
        Assert.Equal("STAFF_BADGE_REQUIRED", (await badges.GenerateBadgePayloadForStaffAsync(invoice.TenantID, staff.StaffID)).ErrorCode);
        var issued = await badges.IssueStaffBadgeAsync(invoice.TenantID, staff.StaffID); Assert.True(issued.IsSuccess, issued.ErrorMessage);
        var repeated = await badges.IssueStaffBadgeAsync(invoice.TenantID, staff.StaffID); Assert.True(repeated.IsSuccess, repeated.ErrorMessage);
        Assert.Equal(issued.Data!.QRCode, repeated.Data!.QRCode); Assert.Single(await db.Tickets.ToListAsync());
        Assert.Equal("Exhibitor", (await db.Tickets.SingleAsync()).TicketType);
        Assert.True((await badges.GenerateBadgePayloadByQRCodeAsync(invoice.TenantID, issued.Data.QRCode)).IsSuccess);
        var tickets = new TicketService(uow, Mapper);
        Assert.True((await tickets.ScanTicketAsync(invoice.TenantID, issued.Data.QRCode, "In", "Gate", "workflow-user")).IsSuccess);
        (await db.BoothReservations.SingleAsync()).Status = ReservationStatus.Cancelled; await db.SaveChangesAsync();
        Assert.Equal("STAFF_ACCESS_NOT_ACTIVE", (await tickets.ScanTicketAsync(invoice.TenantID, issued.Data.QRCode, "Out", "Gate", "workflow-user")).ErrorCode);
    }

    [Fact]
    public async Task BookedBooth_CannotChangeArea()
    {
        using var db = WorkflowFixture.CreateDb(); var invoice = await Seed(db);
        using var uow = new UnitOfWork(db, WorkflowFixture.Actor(invoice.TenantID));
        var result = await new BoothService(uow, Mapper).UpdateAsync(invoice.TenantID, invoice.Reservation.BoothID!.Value,
            new BoothUpdateDto { BoothNumber = "B-1", Status = "Available", Width = 20, Height = 20 });
        Assert.Equal("BOOTH_GEOMETRY_LOCKED", result.ErrorCode); Assert.Equal(10, (await db.Booths.SingleAsync()).CurrentAreaSqM);
    }

    [Fact]
    public async Task Package_ConvertsCurrencyAndRejectsMissingAndDuplicateServicesBeforeSaving()
    {
        using var db = WorkflowFixture.CreateDb(); var invoice = await Seed(db);
        var service = new Service { TenantID = invoice.TenantID, ServiceName = "USD service", DefaultPrice = 10, DefaultCurrencyCode = "USD" };
        db.Services.Add(service); db.ExchangeRates.Add(new ExchangeRate { FromCurrency = "USD", ToCurrency = "LYD", Rate = 5, RateDate = DateTime.UtcNow.Date, CreatedByUserId = "workflow-user" }); await db.SaveChangesAsync();
        using var uow = new UnitOfWork(db, WorkflowFixture.Actor(invoice.TenantID)); var pricing = new PricingService(uow, Mapper);
        var dto = new PricingPackageCreateDto { PackageName = "Package", CurrencyCode = "LYD", TotalPrice = 50, ValidFrom = DateTime.UtcNow.Date, ServiceIDs = [service.ServiceID, int.MaxValue] };
        Assert.Equal("SERVICE_NOT_AVAILABLE", (await pricing.CreatePackageAsync(invoice.TenantID, dto)).ErrorCode); Assert.Empty(await db.PricingPackages.ToListAsync());
        dto.ServiceIDs = [service.ServiceID, service.ServiceID];
        Assert.Equal("INVALID_PACKAGE_INPUT", (await pricing.CreatePackageAsync(invoice.TenantID, dto)).ErrorCode); Assert.Empty(await db.PricingPackages.ToListAsync());
        dto.ServiceIDs = [service.ServiceID]; var created = await pricing.CreatePackageAsync(invoice.TenantID, dto); Assert.True(created.IsSuccess, created.ErrorMessage);
        Assert.Equal(50, (await db.PricingPackages.Include(p => p.PackageServices).SingleAsync()).PackageServices.Single().UnitPrice);
        Assert.Single(await db.AuditLogs.Where(a => a.TableName == "PackageServicePricing").ToListAsync());
    }

    [Fact]
    public async Task NumericUnknownPaymentMethod_IsRejected()
    {
        using var db = WorkflowFixture.CreateDb(); var invoice = await Seed(db);
        using var uow = new UnitOfWork(db, WorkflowFixture.Actor(invoice.TenantID));
        Assert.Equal("INVALID_PAYMENT_METHOD", (await Finance(db, uow).RecordPaymentAsync(invoice.TenantID, "workflow-user",
            new PaymentCreateDto { InvoiceID = invoice.InvoiceID, Amount = 1, CurrencyCode = "USD", Method = "999" })).ErrorCode);
        Assert.Empty(await db.Payments.ToListAsync());
    }

    [Theory]
    [InlineData(-100, 1, "Active")] [InlineData(100, 50, "Active")] [InlineData(100, 1, "999")]
    public async Task SubscriptionInvalidInputs_AreRejected(int fee, int years, string status)
    {
        using var db = WorkflowFixture.CreateDb(); var invoice = await Seed(db);
        using var uow = new UnitOfWork(db, WorkflowFixture.Actor(invoice.TenantID));
        Assert.False((await new AdminService(uow, Mapper).CreateSubscriptionAsync(invoice.TenantID,
            new() { PlanName = "Enterprise", Price = fee, EndDate = DateTime.UtcNow.Date.AddYears(years), Status = status, CurrencyCode = "LYD" })).IsSuccess);
        Assert.Empty(await db.TenantSubscriptions.ToListAsync());
    }

    [Fact]
    public async Task TenantAdmin_CannotGrantDifferentPlanAndSuperAdminCanRecordIt()
    {
        using var db = WorkflowFixture.CreateDb(); var invoice = await Seed(db);
        var actor = WorkflowFixture.Actor(invoice.TenantID);
        ((ClaimsIdentity)actor.HttpContext!.User.Identity!).AddClaim(new Claim(ClaimTypes.Role, "TenantAdmin"));
        using var uow = new UnitOfWork(db, actor);
        var dto = new ExhibitionManagementSystem.Models.DTOs.Admin.TenantSubscriptionDto {
            PlanName = "Enterprise", Status = "Active", Price = 100, CurrencyCode = "LYD", EndDate = DateTime.UtcNow.Date.AddYears(1) };
        var admin = new AdminService(uow, Mapper);
        Assert.Equal("SUBSCRIPTION_PLAN_CHANGE_NOT_ALLOWED", (await admin.CreateSubscriptionAsync(invoice.TenantID, dto)).ErrorCode);
        Assert.Empty(await db.TenantSubscriptions.ToListAsync());
        ((ClaimsIdentity)actor.HttpContext.User.Identity!).AddClaim(new Claim(ClaimTypes.Role, "SuperAdmin"));
        Assert.True((await admin.CreateSubscriptionAsync(invoice.TenantID, dto)).IsSuccess);
    }

    [Fact]
    public void CurrentPlan_ExcludesFutureAndExpiredSubscriptions()
    {
        var today = DateTime.UtcNow.Date;
        var tenant = new Tenant { TenantSubscriptions = [
            new TenantSubscription { Plan = "Current", Status = SubscriptionStatus.Active, StartDate = today.AddDays(-5), EndDate = today.AddDays(5) },
            new TenantSubscription { Plan = "Future", Status = SubscriptionStatus.Active, StartDate = today.AddDays(10), EndDate = today.AddDays(20) },
            new TenantSubscription { Plan = "Expired", Status = SubscriptionStatus.Active, StartDate = today.AddDays(-2), EndDate = today.AddDays(-1) }
        ] };
        Assert.Equal("Current", tenant.CurrentPlan);
    }

    [Fact]
    public async Task InvoiceFilterAndCounts_AreComputedBeforePagination()
    {
        using var db = WorkflowFixture.CreateDb(); var invoice = await Seed(db, InvoiceStatus.Paid);
        db.Invoices.Add(new Invoice { TenantID = invoice.TenantID, InvoiceNumber = "WORKFLOW-2", Status = InvoiceStatus.Draft,
            CurrencyCode = "USD", BaseCurrencyCode = "LYD", TotalAmount = 20, SubTotal = 20, ExchangeRateUsed = 5,
            TotalAmountInBaseCurrency = 100, DueDate = DateTime.UtcNow.AddDays(1), Reservation = new BoothReservation {
                TenantID = invoice.TenantID, ExhibitorID = invoice.Reservation.ExhibitorID, ExhibitionID = invoice.Reservation.ExhibitionID,
                CreatedByUserId = "workflow-user", CurrencyCode = "USD", BaseCurrencyCode = "LYD", BoothSourceCurrencyCode = "USD",
                Booth = new Booth { TenantID = invoice.TenantID, HallID = invoice.Reservation.Booth.HallID, BoothNumber = "B-2" }
            } });
        await db.SaveChangesAsync();
        using var uow = new UnitOfWork(db, WorkflowFixture.Actor(invoice.TenantID));
        var page = await Finance(db, uow).GetInvoicePageAsync(invoice.TenantID, 1, 1, "Paid");
        Assert.True(page.IsSuccess); Assert.Equal(2, page.Data!.AllCount); Assert.Equal(1, page.Data.PaidCount); Assert.Single(page.Data.Items);
        var empty = await Finance(db, uow).GetInvoicePageAsync(invoice.TenantID, 1, 1, "Draft");
        Assert.Equal(1, empty.Data!.TotalCount); Assert.Equal(2, empty.Data.AllCount); Assert.Single(empty.Data.Items);
        var cancelled = await Finance(db, uow).GetInvoicePageAsync(invoice.TenantID, 1, 1, "Cancelled");
        Assert.Empty(cancelled.Data!.Items); Assert.Equal(2, cancelled.Data.AllCount);
    }

    [Fact]
    public async Task DashboardWithoutFinanceRole_DoesNotReturnFinancialAmounts()
    {
        using var db = WorkflowFixture.CreateDb(); var invoice = await Seed(db);
        var controller = new DashboardController(db) { ControllerContext = new() { HttpContext = WorkflowFixture.Actor(invoice.TenantID).HttpContext! } };
        var stats = Assert.IsType<ExhibitionManagementSystem.Models.DTOs.Dashboard.DashboardStatsDto>(Assert.IsType<OkObjectResult>((await controller.GetStats()).Result).Value);
        Assert.Null(stats.TotalRevenue); Assert.Null(stats.PendingRevenue); Assert.False(stats.CanViewFinancials); Assert.Equal(1, stats.OccupiedBooths);
    }

    [Fact]
    public async Task TenantDeletion_DeactivatesAndRevokesSessionsWithoutDeletingHistory()
    {
        using var db = WorkflowFixture.CreateDb(); var invoice = await Seed(db);
        db.RefreshSessions.Add(new RefreshSession { TenantID = invoice.TenantID, UserId = "workflow-user", TokenHash = "synthetic", ExpiresAt = DateTime.UtcNow.AddDays(1) }); await db.SaveChangesAsync();
        using var uow = new UnitOfWork(db, WorkflowFixture.Actor(invoice.TenantID));
        var service = new TenantService(uow, Mapper, db, null!, null!, null!, Options.Create(new TenantInvitationOptions()), NullLogger<TenantService>.Instance);
        Assert.True((await service.DeleteAsync(invoice.TenantID)).IsSuccess); Assert.False((await db.Tenants.SingleAsync()).IsActive);
        Assert.NotNull((await db.RefreshSessions.SingleAsync()).RevokedAt); Assert.Single(await db.Invoices.ToListAsync());
    }
}
