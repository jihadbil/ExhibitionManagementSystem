using System.Text.Json;
using ExhibitionManagementSystem.DataAccess;
using ExhibitionManagementSystem.DataAccess.Repositories.Implementations;
using ExhibitionManagementSystem.Models;
using ExhibitionManagementSystem.Models.DTOs.Booth;
using ExhibitionManagementSystem.Models.DTOs.Reservation;
using ExhibitionManagementSystem.Models.Enums;
using ExhibitionManagementSystem.Services.Implementations;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;
using ReservationHandler = ExhibitionManagementSystem.Services.Implementations.ReservationService;

namespace ExhibitionManagementSystem.Api.Tests.Regression;

public sealed class SqlServerAuditRepairTests
{
    [SqlServerFact]
    public async Task SqlServer_MergedPricingFxDeletionGuardsAndStaffPermits()
    {
        var builder = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("EMS_SQL_TEST_CONNECTION")) {
            InitialCatalog = "EMS_AuditRepair_Test_" + Guid.NewGuid().ToString("N"), TrustServerCertificate = true };
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(builder.ConnectionString).Options;
        try
        {
            using var db = new ApplicationDbContext(options, new WorkflowFixture.BypassContext());
            await db.Database.MigrateAsync(); var original = await WorkflowFixture.SeedAsync(db);
            var expo = await db.Exhibitions.SingleAsync(); expo.StartDate = DateTime.UtcNow.Date.AddDays(-1); expo.EndDate = DateTime.UtcNow.Date.AddDays(5);
            var hall = await db.Halls.SingleAsync(); hall.FloorPlanWidth = 50; hall.FloorPlanHeight = 50;
            var originalBooth = await db.Booths.SingleAsync(); originalBooth.Width = 2; originalBooth.Height = 5;
            db.ExchangeRates.AddRange(
                new ExchangeRate { FromCurrency = "USD", ToCurrency = "LYD", Rate = 5, RateDate = DateTime.UtcNow.Date, Source = "Test", CreatedByUserId = "workflow-user" },
                new ExchangeRate { FromCurrency = "EUR", ToCurrency = "LYD", Rate = 6, RateDate = DateTime.UtcNow.Date, Source = "Test", CreatedByUserId = "workflow-user" });
            var usdRule = new BoothPriceRule { TenantID = original.TenantID, ExhibitionID = expo.ExhibitionID, BoothType = BoothType.VIP, PricePerSqM = 100, CurrencyCode = "USD", ValidFrom = DateTime.UtcNow.Date.AddDays(-1) };
            var eurRule = new BoothPriceRule { TenantID = original.TenantID, ExhibitionID = expo.ExhibitionID, BoothType = BoothType.VIP, PricePerSqM = 100, CurrencyCode = "EUR", ValidFrom = DateTime.UtcNow.Date.AddDays(-1) };
            db.BoothPriceRules.AddRange(usdRule, eurRule, new BoothPriceRule { TenantID = original.TenantID, ExhibitionID = expo.ExhibitionID, BoothType = BoothType.Standard, PricePerSqM = 1, CurrencyCode = "LYD", ValidFrom = DateTime.UtcNow.Date.AddDays(-1) });
            await db.SaveChangesAsync();
            var booths = new[] {
                new Booth { TenantID = original.TenantID, HallID = hall.HallID, BoothNumber = "VIP-2", BoothType = BoothType.VIP, CurrentAreaSqM = 10, OriginalAreaSqM = 10, Width = 2, Height = 5, PosX = 10, AssignedPriceRuleID = usdRule.RuleID },
                new Booth { TenantID = original.TenantID, HallID = hall.HallID, BoothNumber = "VIP-3", BoothType = BoothType.VIP, CurrentAreaSqM = 10, OriginalAreaSqM = 10, Width = 2, Height = 5, PosX = 20, AssignedPriceRuleID = eurRule.RuleID }
            };
            db.Booths.AddRange(booths); await db.SaveChangesAsync();
            using var uow = new UnitOfWork(db, WorkflowFixture.Actor(original.TenantID));
            var mapper = SystemAuditRepairTests.Mapper;
            var boothService = new BoothService(uow, mapper);
            var merged = await boothService.MergeBoothsAsync(original.TenantID, "workflow-user", new BoothMergeCreateDto {
                HallID = hall.HallID, ExhibitionID = expo.ExhibitionID, BoothIDs = booths.Select(b => b.BoothID).ToList(), MergedBoothLabel = "VIP merge" });
            Assert.True(merged.IsSuccess, merged.ErrorMessage);
            var resized = await boothService.UpdateAsync(original.TenantID, booths[0].BoothID, new BoothUpdateDto {
                BoothNumber = booths[0].BoothNumber, Status = "Available", Width = 20, Height = 20, AssignedPriceRuleID = usdRule.RuleID });
            Assert.Equal("BOOTH_GEOMETRY_LOCKED", resized.ErrorCode);
            var layout = new BoothLayoutSaveDto { Booths = new[] { originalBooth, booths[0], booths[1] }.Select(b => new BoothLayoutItemDto {
                BoothID = b.BoothID, BoothNumber = b.BoothNumber, PosX = b.PosX ?? 0, PosY = b.PosY ?? 0,
                Width = b.BoothID == booths[0].BoothID ? 3 : b.Width!.Value, Height = b.Height!.Value,
                ShapeType = "Rect", BoothType = b.BoothType.ToString(), AssignedPriceRuleID = b.AssignedPriceRuleID }).ToList() };
            Assert.Equal("BOOTH_GEOMETRY_LOCKED", (await boothService.SaveLayoutAsync(original.TenantID, "workflow-user", hall.HallID, layout)).ErrorCode);
            var financial = new FinancialService(uow, mapper, new ConfigurationBuilder().Build(), db);
            var created = await new ReservationHandler(uow, mapper, new PricingService(uow, mapper), financial).CreateAsync(original.TenantID, "workflow-user", new BoothReservationCreateDto {
                ExhibitionID = expo.ExhibitionID, ExhibitorID = original.Reservation.ExhibitorID, MergeID = merged.Data!.MergeID,
                BoothTypeSelected = "Standard", ExhibitorCategory = "Local", RequestedAreaSqM = 1, CurrencyCode = "LYD" });
            Assert.True(created.IsSuccess, created.ErrorMessage);
            Assert.Equal(11000, created.Data!.BoothAmount); Assert.Equal("VIP", created.Data.BoothTypeSelected); Assert.Equal(20, created.Data.AllocatedAreaSqM);
            db.ChangeTracker.Clear();
            var audit = await db.AuditLogs.SingleAsync(a => a.TableName == "ReservationBoothPricing");
            using var snapshot = JsonDocument.Parse(audit.NewValues); Assert.Equal(2, snapshot.RootElement.GetArrayLength());
            Assert.Equal("USD", snapshot.RootElement[0].GetProperty("SourceCurrency").GetString());
            Assert.Equal("EUR", snapshot.RootElement[1].GetProperty("SourceCurrency").GetString());
            Assert.Equal("EXHIBITOR_HAS_INVOICES", (await new ExhibitorService(uow, mapper).DeleteAsync(original.TenantID, original.Reservation.ExhibitorID)).ErrorCode);
            Assert.Equal(2, (await financial.GetInvoicePageAsync(original.TenantID, 1, 20)).Data!.AllCount);
            Assert.Equal(1, (await financial.GetInvoicePageAsync(original.TenantID, 1, 20, "Overdue")).Data!.TotalCount);
            var service = new Service { TenantID = original.TenantID, ServiceName = "USD service", DefaultPrice = 10, DefaultCurrencyCode = "USD" };
            db.Services.Add(service); await db.SaveChangesAsync();
            var package = await new PricingService(uow, mapper).CreatePackageAsync(original.TenantID, new() {
                PackageName = "LYD package", CurrencyCode = "LYD", TotalPrice = 50, ValidFrom = DateTime.UtcNow.Date, ServiceIDs = [service.ServiceID] });
            Assert.True(package.IsSuccess, package.ErrorMessage); Assert.Equal(50, (await db.PackageServices.SingleAsync()).UnitPrice);
            var reservation = await db.BoothReservations.SingleAsync(r => r.ReservationID == original.ReservationID); reservation.Status = ReservationStatus.Confirmed;
            var staff = new BoothStaff { ReservationID = reservation.ReservationID, StaffName = "Audit staff" }; db.BoothStaffs.Add(staff); await db.SaveChangesAsync();
            var badges = new BadgeService(uow, mapper); var permit = await badges.IssueStaffBadgeAsync(original.TenantID, staff.StaffID);
            Assert.True(permit.IsSuccess, permit.ErrorMessage); Assert.Equal(permit.Data!.QRCode, (await badges.IssueStaffBadgeAsync(original.TenantID, staff.StaffID)).Data!.QRCode);
            Assert.True((await new TicketService(uow, mapper).ScanTicketAsync(original.TenantID, permit.Data.QRCode, "In", "Gate", "workflow-user")).IsSuccess);

            var raceBooths = new[] { new Booth { TenantID = original.TenantID, HallID = hall.HallID, BoothNumber = "Race-1", CurrentAreaSqM = 1, OriginalAreaSqM = 1 },
                new Booth { TenantID = original.TenantID, HallID = hall.HallID, BoothNumber = "Race-2", CurrentAreaSqM = 1, OriginalAreaSqM = 1 } };
            db.Booths.AddRange(raceBooths); await db.SaveChangesAsync();
            async Task<bool> RaceReservation()
            {
                using var raceDb = new ApplicationDbContext(options, new WorkflowFixture.BypassContext());
                using var raceUow = new UnitOfWork(raceDb, WorkflowFixture.Actor(original.TenantID));
                return (await new ReservationHandler(raceUow, mapper, new PricingService(raceUow, mapper),
                    new FinancialService(raceUow, mapper, new ConfigurationBuilder().Build(), raceDb)).CreateAsync(original.TenantID, "workflow-user", new() {
                        ExhibitorID = original.Reservation.ExhibitorID, ExhibitionID = expo.ExhibitionID, BoothID = raceBooths[0].BoothID,
                        BoothTypeSelected = "Standard", ExhibitorCategory = "Local", RequestedAreaSqM = 1, CurrencyCode = "LYD" })).IsSuccess;
            }
            async Task<bool> RaceMerge()
            {
                using var raceDb = new ApplicationDbContext(options, new WorkflowFixture.BypassContext());
                using var raceUow = new UnitOfWork(raceDb, WorkflowFixture.Actor(original.TenantID));
                return (await new BoothService(raceUow, mapper).MergeBoothsAsync(original.TenantID, "workflow-user", new() {
                    ExhibitionID = expo.ExhibitionID, HallID = hall.HallID, BoothIDs = raceBooths.Select(b => b.BoothID).ToList(), MergedBoothLabel = "Race" })).IsSuccess;
            }
            var outcomes = await Task.WhenAll(RaceReservation(), RaceMerge());
            Assert.Single(outcomes, succeeded => succeeded);
            db.ChangeTracker.Clear();
            var directCount = await db.BoothReservations.CountAsync(r => r.BoothID == raceBooths[0].BoothID && r.Status != ReservationStatus.Cancelled);
            var mergeCount = await db.BoothMergeItems.CountAsync(i => i.BoothID == raceBooths[0].BoothID && i.ExhibitionID == expo.ExhibitionID);
            Assert.Equal(1, directCount + mergeCount);
        }
        finally
        {
            using var cleanup = new ApplicationDbContext(options, new WorkflowFixture.BypassContext());
            Assert.StartsWith("EMS_AuditRepair_Test_", builder.InitialCatalog);
            await cleanup.Database.EnsureDeletedAsync();
        }
    }
}
