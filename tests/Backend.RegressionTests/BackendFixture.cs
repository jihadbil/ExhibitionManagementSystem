using System.Security.Claims;
using System.Security.Cryptography;
using ExhibitionManagementSystem.DataAccess;
using ExhibitionManagementSystem.Models;
using ExhibitionManagementSystem.Models.Enums;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Backend.RegressionTests;

public sealed class BackendFixture : IAsyncLifetime
{
    public WebApplicationFactory<Program> Factory { get; private set; } = null!;
    public string Password { get; } = Convert.ToBase64String(RandomNumberGenerator.GetBytes(24)) + "aA1!";

    public Task InitializeAsync()
    {
        var connection = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("BACKEND_TEST_CONNECTION")
            ?? throw new InvalidOperationException("Run tests/run-backend-tests.py to provide an isolated SQL Server."));
        if (!connection.InitialCatalog.StartsWith("BackendRegression_", StringComparison.Ordinal)
            || !(connection.DataSource.StartsWith("127.0.0.1,") || connection.DataSource.StartsWith("localhost,")))
            throw new InvalidOperationException("Regression tests require a dedicated local BackendRegression_ database.");
        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", connection.ConnectionString);
        Environment.SetEnvironmentVariable("JwtSettings__SecretKey", Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)));
        Environment.SetEnvironmentVariable("Bootstrap__AdminEmail", "bootstrap@regression.invalid");
        Environment.SetEnvironmentVariable("Bootstrap__AdminPassword", Password);
        Factory = new WebApplicationFactory<Program>().WithWebHostBuilder(b => b.UseEnvironment("Testing"));
        // Forces startup through real migrations/seeding, never EnsureCreated.
        using var client = Factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        return Task.CompletedTask;
    }

    public async Task DisposeAsync() => await Factory.DisposeAsync();

    public async Task<Scenario> CreateScenarioAsync()
    {
        var scope = Factory.Services.CreateScope();
        try
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var suffix = Guid.NewGuid().ToString("N");
            var a = new Tenant { CompanyName = "A " + suffix, Subdomain = "a" + suffix, BaseCurrency = "USD" };
            var b = new Tenant { CompanyName = "B " + suffix, Subdomain = "b" + suffix, BaseCurrency = "USD" };
            db.AddRange(a, b); await db.SaveChangesAsync();
            var manager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var admin = new ApplicationUser { UserName = "a" + suffix + "@test.invalid", Email = "a" + suffix + "@test.invalid", FullName = "Admin A", TenantID = a.TenantID, EmailConfirmed = true };
            var other = new ApplicationUser { UserName = "b" + suffix + "@test.invalid", Email = "b" + suffix + "@test.invalid", FullName = "User B", TenantID = b.TenantID, EmailConfirmed = true };
            Assert.True((await manager.CreateAsync(admin, Password)).Succeeded);
            Assert.True((await manager.CreateAsync(other, Password)).Succeeded);
            Assert.True((await manager.AddToRoleAsync(admin, "Admin")).Succeeded);
            Assert.True((await manager.AddToRoleAsync(other, "Staff")).Succeeded);
            var venue = Fill(new Venue { TenantID = a.TenantID, Name = "Venue A" });
            var otherVenue = Fill(new Venue { TenantID = b.TenantID, Name = "Venue B" });
            db.AddRange(venue, otherVenue); await db.SaveChangesAsync();
            var hall = Fill(new Hall { VenueID = venue.VenueID, HallName = "A" });
            var otherHall = Fill(new Hall { VenueID = otherVenue.VenueID, HallName = "B" });
            db.AddRange(hall, otherHall); await db.SaveChangesAsync();
            var booth = Fill(new Booth { HallID = hall.HallID, BoothNumber = "A1", OriginalAreaSqM = 10, CurrentAreaSqM = 10 });
            var otherBooth = Fill(new Booth { HallID = otherHall.HallID, BoothNumber = "B1", OriginalAreaSqM = 10, CurrentAreaSqM = 10 });
            var exhibition = Fill(new Exhibition { TenantID = a.TenantID, VenueID = venue.VenueID, Name = "Exhibition A", EntryCurrency = "USD", Status = ExhibitionStatus.Open, StartDate = DateTime.UtcNow.Date, EndDate = DateTime.UtcNow.Date.AddDays(3) });
            var otherExhibition = Fill(new Exhibition { TenantID = b.TenantID, VenueID = otherVenue.VenueID, Name = "Exhibition B", EntryCurrency = "USD", Status = ExhibitionStatus.Open, StartDate = DateTime.UtcNow.Date, EndDate = DateTime.UtcNow.Date.AddDays(3) });
            var exhibitor = Fill(new Exhibitor { TenantID = a.TenantID, CompanyName = "Exhibitor" });
            var visitor = Fill(new Visitor { TenantID = a.TenantID, FullName = "Visitor" });
            var service = Fill(new Service { TenantID = a.TenantID, ServiceName = "Service", DefaultPrice = 10 });
            db.AddRange(booth, otherBooth, exhibition, otherExhibition, exhibitor, visitor, service); await db.SaveChangesAsync();
            return new Scenario(scope, a, b, admin, other, booth, otherBooth, exhibition, otherExhibition, exhibitor, visitor, service);
        }
        catch { scope.Dispose(); throw; }
    }

    internal static T Fill<T>(T entity) where T : class
    {
        var nullability = new System.Reflection.NullabilityInfoContext();
        foreach (var p in typeof(T).GetProperties().Where(p => p.PropertyType == typeof(string) && p.CanWrite))
            if (p.GetValue(entity) == null && nullability.Create(p).WriteState != System.Reflection.NullabilityState.Nullable)
                p.SetValue(entity, "");
        return entity;
    }
}

public sealed record Scenario(IServiceScope Scope, Tenant Tenant, Tenant OtherTenant, ApplicationUser Admin,
    ApplicationUser OtherUser, Booth Booth, Booth OtherBooth, Exhibition Exhibition, Exhibition OtherExhibition,
    Exhibitor Exhibitor, Visitor Visitor, Service Service) : IDisposable
{
    public IServiceProvider Services => Scope.ServiceProvider;
    public ApplicationDbContext Db => Services.GetRequiredService<ApplicationDbContext>();

    public IDisposable ActAs(string? id = null)
    {
        var accessor = Services.GetRequiredService<IHttpContextAccessor>();
        var old = accessor.HttpContext;
        accessor.HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim(ClaimTypes.NameIdentifier, id ?? Admin.Id), new Claim("TenantId", Tenant.TenantID.ToString()),
            new Claim(ClaimTypes.Role, "Admin")], "test")) };
        return new RestoreContext(() => accessor.HttpContext = old);
    }

    public async Task<Invoice> InvoiceAsync(InvoiceStatus status = InvoiceStatus.Issued)
    {
        var reservation = BackendFixture.Fill(new BoothReservation { ExhibitionID = Exhibition.ExhibitionID,
            ExhibitorID = Exhibitor.ExhibitorID, CreatedByUserId = Admin.Id, CurrencyCode = "USD",
            BoothAmount = 100, TotalAmount = 100, AmountInBaseCurrency = 100, ExchangeRateUsed = 1,
            Status = ReservationStatus.Confirmed });
        var invoice = BackendFixture.Fill(new Invoice { TenantID = Tenant.TenantID, Reservation = reservation,
            InvoiceNumber = "TEST-" + Guid.NewGuid().ToString("N"), SubTotal = 100, TotalAmount = 100,
            CurrencyCode = "USD", Status = status });
        invoice.InvoiceItems.Add(new InvoiceItem { ItemName = "Booth", Quantity = 1, UnitPrice = 100, TotalPrice = 100 });
        Db.Add(invoice); await Db.SaveChangesAsync(); return invoice;
    }

    public void Dispose() => Scope.Dispose();
    private sealed class RestoreContext(Action restore) : IDisposable { public void Dispose() => restore(); }
}
