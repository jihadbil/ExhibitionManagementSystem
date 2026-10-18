using System.Security.Claims;
using AutoMapper;
using ExhibitionManagementSystem.DataAccess;
using ExhibitionManagementSystem.DataAccess.MultiTenancy;
using ExhibitionManagementSystem.Models;
using ExhibitionManagementSystem.Models.DTOs.Mapping.Profiles;
using ExhibitionManagementSystem.Models.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace ExhibitionManagementSystem.Api.Tests.Regression;

public static class WorkflowFixture
{
    public static IMapper Mapper { get; } = new MapperConfiguration(cfg =>
    {
        cfg.AddProfile<FinancialMappingProfile>();
        cfg.AddProfile<ReservationMappingProfile>();
        cfg.AddProfile<BoothMappingProfile>();
        cfg.AddProfile<PricingMappingProfile>();
        cfg.AddProfile<SponsorshipMappingProfile>();
        cfg.AddProfile<TenantMappingProfile>();
    }, NullLoggerFactory.Instance).CreateMapper();

    public static ApplicationDbContext CreateDb() => new(
        new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options,
        new BypassContext());

    public static HttpContextAccessor Actor(int tenantId) => new()
    {
        HttpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity([
                new Claim(ClaimTypes.NameIdentifier, "workflow-user"), new Claim("TenantId", tenantId.ToString())
            ], "Test"))
        }
    };

    public static async Task<Invoice> SeedAsync(ApplicationDbContext db, InvoiceStatus status = InvoiceStatus.Issued, DateTime? invoiceDate = null)
    {
        foreach (var code in new[] { "LYD", "USD", "EUR" })
            if (!await db.Currencies.AnyAsync(c => c.CurrencyCode == code))
                db.Currencies.Add(new Currency { CurrencyCode = code, CurrencyName = code, Symbol = code });
        var tenant = new Tenant { CompanyName = "Workflow tenant", BaseCurrency = "LYD", Subdomain = Guid.NewGuid().ToString("N") };
        var user = new ApplicationUser { Id = "workflow-user", Tenant = tenant, FullName = "Workflow user", UserName = "workflow-user", SecurityStamp = "stamp" };
        var venue = new Venue { Tenant = tenant, Name = "Venue" };
        var hall = new Hall { Venue = venue, HallName = "Hall" };
        var booth = new Booth { Hall = hall, BoothNumber = "B-1", OriginalAreaSqM = 10, CurrentAreaSqM = 10, Tenant = tenant };
        var exhibition = new Exhibition { Tenant = tenant, Venue = venue, Name = "Expo", Status = ExhibitionStatus.Open, EntryCurrency = "LYD" };
        var exhibitor = new Exhibitor { Tenant = tenant, CompanyName = "Exhibitor" };
        var reservation = new BoothReservation
        {
            Tenant = tenant, Exhibition = exhibition, Exhibitor = exhibitor, Booth = booth,
            CurrencyCode = "USD", BaseCurrencyCode = "LYD", BoothSourceCurrencyCode = "USD",
            BoothAmount = 100, TotalAmount = 100, ExchangeRateUsed = 5, AmountInBaseCurrency = 500,
            CreatedByUser = user, Status = status == InvoiceStatus.Cancelled ? ReservationStatus.Cancelled : ReservationStatus.Pending
        };
        var invoice = new Invoice
        {
            Tenant = tenant, Reservation = reservation, InvoiceNumber = "WORKFLOW-1", Status = status,
            InvoiceDate = invoiceDate ?? DateTime.UtcNow, CreatedAt = invoiceDate ?? DateTime.UtcNow,
            CurrencyCode = "USD", BaseCurrencyCode = "LYD", SubTotal = 100, TotalAmount = 100,
            ExchangeRateUsed = 5, TotalAmountInBaseCurrency = 500, DueDate = DateTime.UtcNow.AddDays(-1)
        };
        invoice.InvoiceItems.Add(new InvoiceItem { ItemName = "Booth", Quantity = 1, UnitPrice = 100, TotalPrice = 100 });
        db.Invoices.Add(invoice);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        return invoice;
    }

    public sealed class BypassContext : ITenantContext
    {
        public int? TenantId => null;
        public bool CanBypassTenantIsolation => true;
    }
}
