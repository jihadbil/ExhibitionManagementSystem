using ExhibitionManagementSystem.DataAccess;
using ExhibitionManagementSystem.DataAccess.MultiTenancy;
using ExhibitionManagementSystem.DataAccess.Repositories.Implementations;
using ExhibitionManagementSystem.Models;
using ExhibitionManagementSystem.Models.DTOs.Booth;
using ExhibitionManagementSystem.Models.DTOs.Exhibition;
using ExhibitionManagementSystem.Models.DTOs.Financial;
using ExhibitionManagementSystem.Models.DTOs.Hall;
using ExhibitionManagementSystem.Models.DTOs.Sponsorship;
using ExhibitionManagementSystem.Models.DTOs.Venue;
using ExhibitionManagementSystem.Models.Enums;
using ExhibitionManagementSystem.Services.Implementations;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;
using Xunit.Abstractions;

namespace ExhibitionManagementSystem.Api.Tests.Regression;

public sealed class SqlServerTheoryAttribute : TheoryAttribute
{
    public SqlServerTheoryAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("EMS_SQL_TEST_CONNECTION")))
            Skip = "Set EMS_SQL_TEST_CONNECTION to run these invariants against disposable SQL databases.";
    }
}

public sealed class DeepAuditRepairTests(ITestOutputHelper output)
{
    private sealed class Scope(int tenantId) : ITenantContext
    {
        public int? TenantId => tenantId;
        public bool CanBypassTenantIsolation => false;
    }

    [SqlServerTheory]
    [InlineData("ForeignPackageExhibition")]
    [InlineData("ForeignAdvertisingHall")]
    [InlineData("DeleteSponsorWithContract")]
    [InlineData("DeletePackageWithContract")]
    [InlineData("OversellSponsorshipPackage")]
    [InlineData("PolygonBoothUnchangedUpdate")]
    [InlineData("HallInvalidPlan")]
    [InlineData("VenueShrinkAroundBooths")]
    [InlineData("VenueWallOverBooths")]
    [InlineData("ExhibitionDatesWithSchedule")]
    [InlineData("DeleteRegisteredSchedule")]
    [InlineData("FractionalCentPayment")]
    [InlineData("FractionalCentFullPayment")]
    [InlineData("CancelledPackageCapacity")]
    [InlineData("ForeignSpaceExhibition")]
    [InlineData("ForeignSpaceHallUpdate")]
    [InlineData("HallMalformedJsonWithoutBooths")]
    [InlineData("HallShrinkAroundBooths")]
    [InlineData("ValidBoothCreate")]
    public async Task SafetyInvariant(string scenario)
    {
        var databaseName = "EMS_DeepAudit_20261012_" + Guid.NewGuid().ToString("N");
        var connection = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("EMS_SQL_TEST_CONNECTION"))
        { InitialCatalog = databaseName, TrustServerCertificate = true };
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(connection.ConnectionString).Options;
        await using var seed = new ApplicationDbContext(options, new WorkflowFixture.BypassContext());
        try
        {
            await seed.Database.MigrateAsync();
            var invoice = await WorkflowFixture.SeedAsync(seed);
            var expo = await seed.Exhibitions.SingleAsync();
            expo.StartDate = DateTime.UtcNow.Date.AddDays(-1);
            expo.EndDate = DateTime.UtcNow.Date.AddDays(5);
            var booth = await seed.Booths.SingleAsync();
            var reservation = await seed.BoothReservations.SingleAsync();
            reservation.RequestedAreaSqM = 10;
            reservation.Status = ReservationStatus.Confirmed;
            booth.Width = 2; booth.Height = 10;
            booth.PosX = 0; booth.PosY = 0;
            booth.ShapeType = BoothShapeType.Polygon;
            booth.ShapePolygonJSON = "[{\"x\":0,\"y\":0},{\"x\":2,\"y\":0},{\"x\":0,\"y\":10}]";
            var hall = await seed.Halls.SingleAsync();
            hall.FloorPlanWidth = 20; hall.FloorPlanHeight = 20;
            var otherTenant = new Tenant { CompanyName = "Other synthetic tenant", BaseCurrency = "LYD", Subdomain = "other-" + Guid.NewGuid().ToString("N") };
            var foreignVenue = new Venue { Tenant = otherTenant, Name = "Foreign synthetic venue" };
            var foreignHall = new Hall { Venue = foreignVenue, HallName = "Foreign confidential hall" };
            var foreignExpo = new Exhibition { Tenant = otherTenant, Venue = foreignVenue, EntryCurrency = "LYD", Name = "Other synthetic expo", StartDate = expo.StartDate, EndDate = expo.EndDate };
            seed.Halls.Add(foreignHall); seed.Exhibitions.Add(foreignExpo);
            var sponsor = new Sponsor { TenantID = invoice.TenantID, CompanyName = "Synthetic sponsor" };
            var package = new SponsorshipPackage { TenantID = invoice.TenantID, ExhibitionID = expo.ExhibitionID, PackageName = "One sponsor", MaxSponsorsCount = 1, Price = 100, CurrencyCode = "LYD", IsActive = true };
            var contract = new SponsorshipContract { TenantID = invoice.TenantID, ExhibitionID = expo.ExhibitionID, Sponsor = sponsor, Package = package, ContractNumber = "AUDIT-EXISTING", CurrencyCode = "LYD", TotalAmount = 100, CreatedByUserId = "workflow-user" };
            seed.SponsorshipContracts.Add(contract);
            var schedule = new ExhibitionSchedule { ExhibitionID = expo.ExhibitionID, EventName = "Existing schedule", StartDateTime = DateTime.UtcNow.Date.AddDays(4).AddHours(10), EndDateTime = DateTime.UtcNow.Date.AddDays(4).AddHours(11), HallID = hall.HallID };
            seed.ExhibitionSchedules.Add(schedule);
            await seed.SaveChangesAsync();
            seed.ChangeTracker.Clear();
            await using var db = new ApplicationDbContext(options, new Scope(invoice.TenantID));
            using var uow = new UnitOfWork(db, WorkflowFixture.Actor(invoice.TenantID));
            var mapper = SystemAuditRepairTests.Mapper;
            var sponsorship = new SponsorshipService(uow, mapper);
            output.WriteLine($"Scenario={scenario}; disposable database={databaseName}");
            switch (scenario)
            {
                case "ForeignSpaceExhibition":
                case "ForeignSpaceHallUpdate":
                {
                    var created = await sponsorship.CreateSpaceAsync(invoice.TenantID, new AdvertisingSpaceCreateDto
                    { ExhibitionID = expo.ExhibitionID, HallID = hall.HallID, VenueID = expo.VenueID, SpaceCode = "OWN", SpaceName = "Own space", CurrencyCode = "LYD" });
                    Assert.True(created.IsSuccess, created.ErrorMessage);
                    var result = await sponsorship.UpdateSpaceAsync(invoice.TenantID, created.Data!.SpaceID, new AdvertisingSpaceUpdateDto
                    {
                        ExhibitionID = scenario == "ForeignSpaceExhibition" ? foreignExpo.ExhibitionID : expo.ExhibitionID,
                        HallID = scenario == "ForeignSpaceHallUpdate" ? foreignHall.HallID : hall.HallID,
                        SpaceCode = "OWN", SpaceName = "Updated", CurrencyCode = "LYD"
                    });
                    Assert.False(result.IsSuccess);
                    var saved = await seed.AdvertisingSpaces.SingleAsync();
                    Assert.Equal(expo.ExhibitionID, saved.ExhibitionID);
                    Assert.Equal(hall.HallID, saved.HallID);
                    break;
                }
                case "HallMalformedJsonWithoutBooths":
                {
                    var emptyHall = new Hall { VenueID = expo.VenueID, HallName = "Empty hall", FloorPlanWidth = 20, FloorPlanHeight = 20 };
                    seed.Halls.Add(emptyHall); await seed.SaveChangesAsync();
                    var result = await new HallService(uow, mapper).UpdateAsync(invoice.TenantID, emptyHall.HallID,
                        new HallUpdateDto { HallName = "Empty hall", FloorPlanWidth = 20, FloorPlanHeight = 20, FloorPlanJSON = "not-json", IsActive = true });
                    Assert.False(result.IsSuccess);
                    Assert.Equal("INVALID_FLOOR_PLAN", result.ErrorCode);
                    break;
                }
                case "HallShrinkAroundBooths":
                {
                    var result = await new HallService(uow, mapper).UpdateAsync(invoice.TenantID, hall.HallID,
                        new HallUpdateDto { HallName = "Hall", FloorPlanWidth = 1, FloorPlanHeight = 1, IsActive = true });
                    Assert.False(result.IsSuccess);
                    Assert.Equal("BOOTH_OUT_OF_BOUNDS", result.ErrorCode);
                    break;
                }
                case "ValidBoothCreate":
                {
                    var result = await new BoothService(uow, mapper).CreateAsync(invoice.TenantID, new BoothCreateDto
                    { HallID = hall.HallID, BoothNumber = "B-2", OriginalAreaSqM = 9, Width = 3, Height = 3, PosX = 5, PosY = 5 });
                    Assert.True(result.IsSuccess, result.ErrorMessage);
                    Assert.Equal(9m, result.Data!.CurrentAreaSqM);
                    Assert.Equal(2, await seed.Booths.CountAsync());
                    break;
                }
                case "ForeignPackageExhibition":
                {
                    var result = await sponsorship.UpdatePackageAsync(invoice.TenantID, package.PackageID, new SponsorshipPackageUpdateDto { ExhibitionID = foreignExpo.ExhibitionID, PackageName = "Moved", MaxSponsorsCount = 1, Price = 100, CurrencyCode = "LYD" });
                    output.WriteLine($"Accepted={result.IsSuccess}; foreign exhibition ID={foreignExpo.ExhibitionID}; stored exhibition ID={(await seed.SponsorshipPackages.SingleAsync()).ExhibitionID}");
                    Assert.False(result.IsSuccess, "Cross-tenant package reassignment must be rejected.");
                    break;
                }
                case "ForeignAdvertisingHall":
                {
                    var result = await sponsorship.CreateSpaceAsync(invoice.TenantID, new AdvertisingSpaceCreateDto { ExhibitionID = expo.ExhibitionID, HallID = foreignHall.HallID, VenueID = foreignVenue.VenueID, SpaceCode = "FOREIGN", SpaceName = "Space", CurrencyCode = "LYD" });
                    output.WriteLine($"Accepted={result.IsSuccess}; leaked hall name={result.Data?.HallName}");
                    Assert.False(result.IsSuccess, "Foreign hall references must be rejected before mapping confidential names.");
                    break;
                }
                case "DeleteSponsorWithContract":
                case "DeletePackageWithContract":
                {
                    var result = scenario == "DeleteSponsorWithContract" ? await sponsorship.DeleteSponsorAsync(invoice.TenantID, sponsor.SponsorID, "workflow-user") : await sponsorship.DeletePackageAsync(invoice.TenantID, package.PackageID, "workflow-user");
                    var visible = await sponsorship.GetContractsByExhibitionAsync(invoice.TenantID, expo.ExhibitionID);
                    if (scenario == "DeleteSponsorWithContract")
                    {
                        Assert.False(result.IsSuccess);
                        Assert.Equal("SPONSOR_HAS_CONTRACTS", result.ErrorCode);
                    }
                    output.WriteLine($"Delete accepted={result.IsSuccess}; visible contracts={visible.Data!.Count}; stored contracts={await seed.SponsorshipContracts.IgnoreQueryFilters().CountAsync()}");
                    Assert.Single(visible.Data!);
                    break;
                }
                case "OversellSponsorshipPackage":
                {
                    var result = await sponsorship.CreateContractAsync(invoice.TenantID, "workflow-user", new SponsorshipContractCreateDto { ExhibitionID = expo.ExhibitionID, SponsorID = sponsor.SponsorID, PackageID = package.PackageID, ContractNumber = "AUDIT-SECOND", CurrencyCode = "LYD", TotalAmount = 100 });
                    output.WriteLine($"Second contract accepted={result.IsSuccess}; contracts={await seed.SponsorshipContracts.CountAsync()}; capacity=1");
                    Assert.False(result.IsSuccess, "A fully allocated package must reject a second contract.");
                    break;
                }
                case "PolygonBoothUnchangedUpdate":
                {
                    var result = await new BoothService(uow, mapper).UpdateAsync(invoice.TenantID, booth.BoothID, new BoothUpdateDto { BoothNumber = "B-1", Status = "Available", BoothType = "Standard", ShapeType = "Polygon", ShapePolygonJSON = booth.ShapePolygonJSON, OriginalAreaSqM = 10, Width = 2, Height = 10 });
                    output.WriteLine($"Accepted={result.IsSuccess}; area before=10; area after={result.Data?.CurrentAreaSqM}; reservation area={(await seed.BoothReservations.AsNoTracking().SingleAsync()).RequestedAreaSqM}");
                    Assert.True(result.IsSuccess, result.ErrorMessage);
                    Assert.Equal(10m, result.Data!.CurrentAreaSqM);
                    break;
                }
                case "HallInvalidPlan":
                {
                    var result = await new HallService(uow, mapper).UpdateAsync(invoice.TenantID, hall.HallID, new HallUpdateDto { HallName = "Hall", FloorPlanJSON = "not-json", FloorPlanWidth = -1, FloorPlanHeight = 20, IsActive = true });
                    output.WriteLine($"Accepted={result.IsSuccess}; width after={result.Data?.FloorPlanWidth}; stored plan={result.Data?.FloorPlanJSON}");
                    Assert.False(result.IsSuccess, "Hall update must validate plan syntax and dimensions.");
                    break;
                }
                case "VenueShrinkAroundBooths":
                case "VenueWallOverBooths":
                {
                    var size = scenario == "VenueWallOverBooths" ? 20 : 1;
                    var shape = new { id = "audit-hall", kind = "hall", layer = "halls", hallID = hall.HallID, x = 0, y = 0, width = size, height = size, rotation = 0, points = new[] { new { x = 0, y = 0 }, new { x = size, y = 0 }, new { x = size, y = size }, new { x = 0, y = size } }, label = "Hall", fill = "#eff6ff", stroke = "#2563eb", strokeWidth = .12, fontSize = 1.6, locked = false };
                    var wall = new { id = "audit-wall", kind = "wall", layer = "structure", x = 0, y = 2, width = 2, height = .01, rotation = 0, points = new[] { new { x = 0, y = 0 }, new { x = 2, y = 0 } }, label = "Wall", fill = "#eff6ff", stroke = "#2563eb", strokeWidth = .3, fontSize = 1.6, locked = false };
                    var shapes = scenario == "VenueWallOverBooths" ? new object[] { shape, wall } : new object[] { shape };
                    var plan = System.Text.Json.JsonSerializer.Serialize(new { schemaVersion = 4, unit = "meter", bounds = new { width = 20, height = 20 }, grid = new { size = 1, snap = true, visible = true }, layers = new[] { "halls", "structure", "movement", "annotations" }.ToDictionary(l => l, _ => new { visible = true, locked = false }), shapes, halls = new[] { new { hallID = hall.HallID, x = 0, y = 0, width = size, height = size } }, hallDoors = Array.Empty<object>(), elements = Array.Empty<object>() });
                    var result = await new VenueService(uow, mapper).SaveSitePlanAsync(invoice.TenantID, expo.VenueID, new VenueSitePlanSaveDto { PlanJSON = plan, ExpectedVersion = 0 });
                    output.WriteLine($"Accepted={result.IsSuccess}; hall bounds after=1x1; existing reserved booth bounds=2x10");
                    Assert.False(result.IsSuccess, "Shrinking a hall around its existing reserved booth must be rejected.");
                    Assert.Equal(scenario == "VenueWallOverBooths" ? "BOOTH_BLOCKS_ACCESS_AREA" : "BOOTH_OUT_OF_BOUNDS", result.ErrorCode);
                    break;
                }
                case "ExhibitionDatesWithSchedule":
                {
                    var result = await new ExhibitionService(uow, mapper).UpdateAsync(invoice.TenantID, expo.ExhibitionID, new ExhibitionUpdateDto { VenueID = expo.VenueID, Name = "Shortened", StartDate = expo.StartDate, EndDate = DateTime.UtcNow.Date.AddDays(1), Status = "Open", EntryCurrency = "LYD" });
                    output.WriteLine($"Accepted={result.IsSuccess}; schedule day=+4; exhibition end day=+1");
                    Assert.False(result.IsSuccess, "Exhibition dates cannot exclude an existing schedule.");
                    break;
                }
                case "DeleteRegisteredSchedule":
                {
                    seed.ScheduleRegistrations.Add(new ScheduleRegistration { ScheduleID = schedule.ScheduleID, Visitor = new Visitor { TenantID = invoice.TenantID, FullName = "Registered visitor" } });
                    await seed.SaveChangesAsync();
                    var result = await new ExhibitionService(uow, mapper).RemoveScheduleAsync(invoice.TenantID, schedule.ScheduleID);
                    Assert.False(result.IsSuccess);
                    Assert.Equal("SCHEDULE_HAS_REGISTRATIONS", result.ErrorCode);
                    Assert.Equal(1, await seed.ScheduleRegistrations.CountAsync());
                    Assert.True(await seed.ExhibitionSchedules.AnyAsync(s => s.ScheduleID == schedule.ScheduleID));
                    break;
                }
                case "FractionalCentPayment":
                case "FractionalCentFullPayment":
                {
                    var finance = new FinancialService(uow, mapper, new ConfigurationBuilder().Build(), db);
                    var amount = scenario == "FractionalCentFullPayment" ? 99.995m : .015m;
                    var first = await finance.RecordPaymentAsync(invoice.TenantID, "workflow-user", new PaymentCreateDto { InvoiceID = invoice.InvoiceID, Amount = amount, CurrencyCode = "USD", Method = "Cash" });
                    output.WriteLine($"Accepted={first.IsSuccess}; requested={amount}; response={first.Data?.Amount}; stored={await seed.Payments.SumAsync(p => p.Amount)}; stored base={await seed.Payments.SumAsync(p => p.AmountInBaseCurrency)}; status={(await seed.Invoices.SingleAsync()).Status}");
                    Assert.False(first.IsSuccess, "Unsupported currency precision must be rejected or normalized consistently before saving.");
                    break;
                }
                case "CancelledPackageCapacity":
                {
                    await sponsorship.UpdateContractStatusAsync(invoice.TenantID, contract.ContractID, "Cancelled");
                    db.ChangeTracker.Clear();
                    var result = await sponsorship.GetPackageByIdAsync(invoice.TenantID, package.PackageID);
                    output.WriteLine($"Cancelled contract; allocated={result.Data!.AllocatedSponsorsCount}; sold out={result.Data.IsSoldOut}");
                    Assert.False(result.Data.IsSoldOut, "Cancelled contracts must release package capacity.");
                    break;
                }
            }
        }
        finally
        {
            // Only the exact uniquely created review database is eligible for cleanup.
            Assert.Equal(databaseName, new SqlConnectionStringBuilder(seed.Database.GetConnectionString()).InitialCatalog);
            await seed.Database.EnsureDeletedAsync();
        }
    }

    [Fact]
    public async Task GateOperatorMustNotReadFinancialReservationAmounts()
    {
        await using var host = await AuditRepairHttpTests.Host.Start();
        var client = host.Client;
        client.DefaultRequestHeaders.Add("Audit-Role", "GateOperator");
        var financial = await client.GetAsync("/api/financial/invoices");
        var reservations = await client.GetAsync("/api/exhibitors/1/reservations");
        output.WriteLine($"Financial HTTP={(int)financial.StatusCode}; exhibitor reservations HTTP={(int)reservations.StatusCode}; body={await reservations.Content.ReadAsStringAsync()}");
        Assert.Equal(System.Net.HttpStatusCode.Forbidden, reservations.StatusCode);
        client.DefaultRequestHeaders.Remove("Audit-Role");
        client.DefaultRequestHeaders.Add("Audit-Role", "EventManager");
        Assert.Equal(System.Net.HttpStatusCode.OK, (await client.GetAsync("/api/exhibitors/1/reservations")).StatusCode);
    }
}
