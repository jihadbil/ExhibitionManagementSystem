using ExhibitionManagementSystem.DataAccess;
using ExhibitionManagementSystem.DataAccess.MultiTenancy;
using ExhibitionManagementSystem.DataAccess.Repositories.Implementations;
using ExhibitionManagementSystem.Models;
using ExhibitionManagementSystem.Models.DTOs.Badge;
using ExhibitionManagementSystem.Models.DTOs.Exhibition;
using ExhibitionManagementSystem.Models.DTOs.Financial;
using ExhibitionManagementSystem.Models.Enums;
using ExhibitionManagementSystem.Services.Implementations;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ExhibitionManagementSystem.Api.Tests.Regression;

public sealed class Audit20261017RepairTests
{
    private sealed class Scope(int tenantId) : ITenantContext
    {
        public int? TenantId => tenantId;
        public bool CanBypassTenantIsolation => false;
    }

    [SqlServerTheory]
    [InlineData("ExhibitionContract")]
    [InlineData("ExhibitionContractHistory")]
    [InlineData("ExhibitionPackage")]
    [InlineData("ExhibitionSpace")]
    [InlineData("ExhibitionBadge")]
    [InlineData("EmptyExhibition")]
    [InlineData("MoveExhibition")]
    [InlineData("HallSchedule")]
    [InlineData("HallSpace")]
    [InlineData("HallBoothHistory")]
    [InlineData("EmptyHall")]
    [InlineData("InvoicePrecision")]
    [InlineData("ForeignBadge")]
    [InlineData("VisitorAttendance")]
    public async Task SqlSafetyInvariant(string scenario)
    {
        var name = "EMS_Repair_20261017_" + Guid.NewGuid().ToString("N");
        var connection = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("EMS_SQL_TEST_CONNECTION"))
        { InitialCatalog = name, TrustServerCertificate = true };
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(connection.ConnectionString).Options;
        await using var seed = new ApplicationDbContext(options, new WorkflowFixture.BypassContext());
        try
        {
            await seed.Database.MigrateAsync();
            var fixture = await WorkflowFixture.SeedAsync(seed, InvoiceStatus.Draft);
            var exhibition = await seed.Exhibitions.SingleAsync();
            exhibition.StartDate = DateTime.UtcNow.Date; exhibition.EndDate = DateTime.UtcNow.Date.AddDays(2);
            await seed.SaveChangesAsync();
            await using var db = new ApplicationDbContext(options, new Scope(fixture.TenantID));
            using var uow = new UnitOfWork(db, WorkflowFixture.Actor(fixture.TenantID));
            var mapper = SystemAuditRepairTests.Mapper;

            if (scenario.StartsWith("Exhibition", StringComparison.Ordinal) || scenario == "EmptyExhibition")
            {
                var target = NewExhibition(fixture.TenantID, exhibition.VenueID);
                seed.Exhibitions.Add(target);
                SponsorshipContract? contract = null;
                if (scenario is "ExhibitionContract" or "ExhibitionContractHistory")
                {
                    contract = new SponsorshipContract { TenantID = fixture.TenantID, Exhibition = target,
                        Sponsor = new Sponsor { TenantID = fixture.TenantID, CompanyName = "Synthetic sponsor" },
                        ContractNumber = "GUARD-1", CurrencyCode = "LYD", TotalAmount = 750,
                        IsDeleted = scenario == "ExhibitionContractHistory", CreatedByUserId = "workflow-user" };
                    seed.SponsorshipContracts.Add(contract);
                }
                if (scenario == "ExhibitionPackage")
                    seed.SponsorshipPackages.Add(new SponsorshipPackage { TenantID = fixture.TenantID,
                        Exhibition = target, PackageName = "Historical package", IsDeleted = true });
                if (scenario == "ExhibitionSpace")
                    seed.AdvertisingSpaces.Add(new AdvertisingSpace { TenantID = fixture.TenantID,
                        Exhibition = target, SpaceCode = "HISTORY", SpaceName = "Historical space", IsDeleted = true });
                if (scenario == "ExhibitionBadge")
                    seed.BadgeTemplates.Add(new BadgeTemplate { TenantID = fixture.TenantID,
                        Exhibition = target, TemplateName = "Historical template", IsDeleted = true });
                await seed.SaveChangesAsync();

                var result = await new ExhibitionService(uow, mapper).DeleteAsync(fixture.TenantID, target.ExhibitionID);
                Assert.Equal(scenario == "EmptyExhibition", result.IsSuccess);
                if (scenario != "EmptyExhibition") Assert.Equal("EXHIBITION_HAS_DEPENDENCIES", result.ErrorCode);
                db.ChangeTracker.Clear();
                Assert.Equal(scenario == "EmptyExhibition", (await db.Exhibitions.IgnoreQueryFilters()
                    .SingleAsync(e => e.ExhibitionID == target.ExhibitionID)).IsDeleted);
                if (scenario == "ExhibitionContract")
                    Assert.True((await new SponsorshipService(uow, mapper).GetContractByIdAsync(fixture.TenantID, contract!.ContractID)).IsSuccess);
            }
            else if (scenario == "MoveExhibition")
            {
                var target = NewExhibition(fixture.TenantID, exhibition.VenueID);
                var otherVenue = new Venue { TenantID = fixture.TenantID, Name = "Destination" };
                var hall = await seed.Halls.SingleAsync();
                seed.Venues.Add(otherVenue);
                seed.AdvertisingSpaces.Add(new AdvertisingSpace { TenantID = fixture.TenantID, Exhibition = target,
                    VenueID = exhibition.VenueID, HallID = hall.HallID, SpaceCode = "AD", SpaceName = "Billboard" });
                await seed.SaveChangesAsync();
                var result = await new ExhibitionService(uow, mapper).UpdateAsync(fixture.TenantID, target.ExhibitionID,
                    new ExhibitionUpdateDto { Name = target.Name, VenueID = otherVenue.VenueID,
                        StartDate = target.StartDate, EndDate = target.EndDate, Status = "Planning", EntryCurrency = "LYD" });
                Assert.Equal("EXHIBITION_VENUE_LOCKED", result.ErrorCode);
                db.ChangeTracker.Clear();
                Assert.Equal(exhibition.VenueID, (await db.Exhibitions.SingleAsync(e => e.ExhibitionID == target.ExhibitionID)).VenueID);
            }
            else if (scenario is "HallSchedule" or "HallSpace" or "HallBoothHistory" or "EmptyHall")
            {
                var hall = new Hall { VenueID = exhibition.VenueID, HallName = "Protected hall" };
                seed.Halls.Add(hall);
                if (scenario == "HallSchedule")
                    seed.ExhibitionSchedules.Add(new ExhibitionSchedule { ExhibitionID = exhibition.ExhibitionID, Hall = hall,
                        EventName = "Existing event", StartDateTime = DateTime.UtcNow, EndDateTime = DateTime.UtcNow.AddHours(1) });
                if (scenario == "HallSpace")
                    seed.AdvertisingSpaces.Add(new AdvertisingSpace { TenantID = fixture.TenantID, ExhibitionID = exhibition.ExhibitionID,
                        VenueID = exhibition.VenueID, Hall = hall, SpaceCode = "HALL-AD", SpaceName = "Historical billboard", IsDeleted = true });
                if (scenario == "HallBoothHistory")
                    seed.Booths.Add(new Booth { TenantID = fixture.TenantID, Hall = hall, BoothNumber = "OLD", IsDeleted = true });
                await seed.SaveChangesAsync();
                var result = await new HallService(uow, mapper).DeleteAsync(fixture.TenantID, hall.HallID);
                Assert.Equal(scenario == "EmptyHall", result.IsSuccess);
                if (scenario != "EmptyHall") Assert.Equal("HALL_HAS_DEPENDENCIES", result.ErrorCode);
                db.ChangeTracker.Clear();
                Assert.Equal(scenario == "EmptyHall", (await db.Halls.IgnoreQueryFilters().SingleAsync(h => h.HallID == hall.HallID)).IsDeleted);
                if (scenario == "HallSchedule")
                    Assert.NotNull((await db.ExhibitionSchedules.Include(s => s.Hall).SingleAsync()).Hall);
            }
            else if (scenario == "InvoicePrecision")
            {
                var service = new InvoiceItemService(uow, mapper);
                foreach (var (quantity, price) in new[] { (.015m, 100m), (1m, .000000001m),
                    (10000000000000000m, 1m), (1m, decimal.MaxValue), (9999999999999999m, 9999999999999999m) })
                {
                    var invalid = await service.CreateItemAsync(fixture.TenantID, fixture.InvoiceID,
                        new InvoiceItemCreateDto { ItemName = "Invalid", Quantity = quantity, UnitPrice = price });
                    Assert.Equal("INVALID_INVOICE_ITEM_VALUES", invalid.ErrorCode);
                }
                Assert.Equal(1, await db.InvoiceItems.CountAsync());
                var created = await service.CreateItemAsync(fixture.TenantID, fixture.InvoiceID,
                    new InvoiceItemCreateDto { ItemName = "Precise", Quantity = .02m, UnitPrice = 100 });
                Assert.True(created.IsSuccess, created.ErrorMessage);
                var invalidUpdate = await service.UpdateItemAsync(fixture.TenantID, fixture.InvoiceID, created.Data!.InvoiceItemID,
                    new InvoiceItemUpdateDto { InvoiceItemID = created.Data.InvoiceItemID, ItemName = "Invalid", Quantity = .015m, UnitPrice = 100 });
                Assert.Equal("INVALID_INVOICE_ITEM_VALUES", invalidUpdate.ErrorCode);
                var updated = await service.UpdateItemAsync(fixture.TenantID, fixture.InvoiceID, created.Data.InvoiceItemID,
                    new InvoiceItemUpdateDto { InvoiceItemID = created.Data.InvoiceItemID, ItemName = "Precise", Quantity = 3.33m, UnitPrice = .12345678m });
                Assert.True(updated.IsSuccess, updated.ErrorMessage);
                db.ChangeTracker.Clear();
                var saved = await db.InvoiceItems.SingleAsync(i => i.InvoiceItemID == created.Data.InvoiceItemID);
                Assert.Equal(updated.Data!.Quantity, saved.Quantity);
                Assert.Equal(updated.Data.UnitPrice, saved.UnitPrice);
                Assert.Equal(InvoiceFinancialSummary.Round(saved.Quantity * saved.UnitPrice), saved.TotalPrice);
                Assert.Equal(100 + saved.TotalPrice, (await db.Invoices.SingleAsync()).TotalAmount);
            }
            else if (scenario == "ForeignBadge")
            {
                var foreignTenant = new Tenant { CompanyName = "Foreign", Subdomain = "foreign", BaseCurrency = "LYD" };
                var foreignVenue = new Venue { Tenant = foreignTenant, Name = "Foreign venue" };
                var foreign = new Exhibition { Tenant = foreignTenant, Venue = foreignVenue, Name = "Foreign expo", EntryCurrency = "LYD" };
                seed.Exhibitions.Add(foreign); await seed.SaveChangesAsync();
                var service = new BadgeService(uow, mapper);
                var own = await service.CreateTemplateAsync(fixture.TenantID, new BadgeTemplateCreateDto
                { ExhibitionID = exhibition.ExhibitionID, TemplateName = "Own", IsDefault = true });
                Assert.True(own.IsSuccess, own.ErrorMessage);
                var created = await service.CreateTemplateAsync(fixture.TenantID, new BadgeTemplateCreateDto
                { ExhibitionID = foreign.ExhibitionID, TemplateName = "Foreign", IsDefault = true });
                Assert.Equal("EXHIBITION_NOT_FOUND", created.ErrorCode);
                var updated = await service.UpdateTemplateAsync(fixture.TenantID, own.Data!.TemplateID,
                    new BadgeTemplateUpdateDto { ExhibitionID = foreign.ExhibitionID, TemplateName = "Foreign", IsDefault = true });
                Assert.Equal("EXHIBITION_NOT_FOUND", updated.ErrorCode);
                db.ChangeTracker.Clear();
                var saved = await db.BadgeTemplates.SingleAsync();
                Assert.Equal(exhibition.ExhibitionID, saved.ExhibitionID);
                Assert.Equal("Own", saved.TemplateName);
                Assert.True(saved.IsDefault);
                var generic = await service.UpdateTemplateAsync(fixture.TenantID, saved.TemplateID,
                    new BadgeTemplateUpdateDto { ExhibitionID = null, TemplateName = "General", IsDefault = true });
                Assert.True(generic.IsSuccess, generic.ErrorMessage);
                Assert.Null(generic.Data!.ExhibitionID);
            }
            else if (scenario == "VisitorAttendance")
            {
                var visitor = new Visitor { TenantID = fixture.TenantID, FullName = "Attendee", Email = "attendee@example.invalid" };
                seed.Tickets.Add(new Ticket { TenantID = fixture.TenantID, ExhibitionID = exhibition.ExhibitionID,
                    Visitor = visitor, TicketType = "Regular", QRCode = "ENTRY-1", CurrencyCode = "LYD" });
                await seed.SaveChangesAsync();
                var reports = new ReportService(uow, mapper);
                var before = await reports.GenerateExhibitionReportAsync(fixture.TenantID, exhibition.ExhibitionID, "workflow-user");
                Assert.True(before.IsSuccess, before.ErrorMessage);
                Assert.Equal(0, before.Data!.TotalVisitors);
                var scan = await new TicketService(uow, mapper).ScanTicketAsync(fixture.TenantID, "ENTRY-1", "In", "Gate", "workflow-user");
                Assert.True(scan.IsSuccess, scan.ErrorMessage);
                seed.Tickets.Add(new Ticket { TenantID = fixture.TenantID, ExhibitionID = exhibition.ExhibitionID,
                    VisitorID = visitor.VisitorID, TicketType = "VIP", QRCode = "ENTRY-2", CurrencyCode = "LYD" });
                seed.Tickets.Add(new Ticket { TenantID = fixture.TenantID, ExhibitionID = exhibition.ExhibitionID,
                    Visitor = new Visitor { TenantID = fixture.TenantID, FullName = "No show" }, TicketType = "Regular", QRCode = "NO-SHOW", CurrencyCode = "LYD" });
                await seed.SaveChangesAsync();
                Assert.True((await new TicketService(uow, mapper).ScanTicketAsync(fixture.TenantID, "ENTRY-2", "In", "Gate", "workflow-user")).IsSuccess);
                db.ChangeTracker.Clear();
                var after = await reports.GenerateExhibitionReportAsync(fixture.TenantID, exhibition.ExhibitionID, "workflow-user");
                Assert.True(after.IsSuccess, after.ErrorMessage);
                Assert.Equal(1, after.Data!.TotalVisitors);
                Assert.Equal(2, await db.TicketScans.CountAsync());
            }
        }
        finally
        {
            Assert.Equal(name, new SqlConnectionStringBuilder(seed.Database.GetConnectionString()).InitialCatalog);
            await seed.Database.EnsureDeletedAsync();
        }
    }

    private static Exhibition NewExhibition(int tenantId, int venueId) => new()
    { TenantID = tenantId, VenueID = venueId, Name = "Independent exhibition", EntryCurrency = "LYD",
        StartDate = DateTime.UtcNow.Date, EndDate = DateTime.UtcNow.Date.AddDays(2) };
}
