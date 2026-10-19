using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ExhibitionManagementSystem.DataAccess;
using ExhibitionManagementSystem.DataAccess.Repositories.Interfaces;
using ExhibitionManagementSystem.Models;
using ExhibitionManagementSystem.Models.DTOs.Auth;
using ExhibitionManagementSystem.Models.DTOs.Badge;
using ExhibitionManagementSystem.Models.DTOs.Booth;
using ExhibitionManagementSystem.Models.DTOs.Financial;
using ExhibitionManagementSystem.Models.DTOs.Reservation;
using ExhibitionManagementSystem.Models.Enums;
using ExhibitionManagementSystem.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Backend.RegressionTests;

public sealed partial class IntegrityTests(BackendFixture fixture) : IClassFixture<BackendFixture>
{
    private async Task<HttpClient> ClientAsync(Scenario s)
    {
        var client = fixture.Factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        var login = await client.PostAsJsonAsync("/api/Auth/login", new { email = s.Admin.Email, password = fixture.Password });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var token = (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    [Fact]
    public async Task Anonymous_registration_cannot_create_a_platform_administrator()
    {
        using var s = await fixture.CreateScenarioAsync();
        using var client = fixture.Factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        var email = Guid.NewGuid() + "@test.invalid";
        var response = await client.PostAsJsonAsync("/api/Auth/register", new RegisterRequestDto
        {
            Email = email, FullName = "Attacker", Password = fixture.Password, ConfirmPassword = fixture.Password,
            TenantID = s.OtherTenant.TenantID, InitialRole = "SuperAdmin"
        });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.False(await s.Db.Users.AnyAsync(u => u.Email == email));
    }

    [Fact]
    public async Task Tenant_admin_cannot_read_disable_delete_or_grant_roles_to_another_tenant()
    {
        using var s = await fixture.CreateScenarioAsync();
        using var client = await ClientAsync(s);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/Users/" + s.OtherUser.Id)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PatchAsJsonAsync("/api/Users/" + s.OtherUser.Id + "/status", false)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.DeleteAsync("/api/Users/" + s.OtherUser.Id)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsJsonAsync("/api/Roles/assign", new { userId = s.OtherUser.Id, roleName = "Admin" })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync("/api/Roles/assign", new { userId = s.Admin.Id, roleName = "SuperAdmin" })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PutAsJsonAsync("/api/Currencies/rates", new { fromCurrency = "USD", toCurrency = "LYD", rate = 0 })).StatusCode);
    }

    [Fact]
    public async Task Authenticated_registration_uses_the_callers_tenant_and_rejects_platform_roles()
    {
        using var s = await fixture.CreateScenarioAsync();
        using var client = await ClientAsync(s);
        var dto = new RegisterRequestDto { Email = Guid.NewGuid() + "@test.invalid", FullName = "New staff",
            Password = fixture.Password, ConfirmPassword = fixture.Password, TenantID = s.OtherTenant.TenantID, InitialRole = "SuperAdmin" };
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync("/api/Auth/register", dto)).StatusCode);
        dto.InitialRole = "Staff";
        var response = await client.PostAsJsonAsync("/api/Auth/register", dto);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var user = await s.Db.Users.SingleAsync(u => u.Email == dto.Email);
        Assert.Equal(s.Tenant.TenantID, user.TenantID);
    }

    [Fact]
    public async Task Disabling_account_revokes_refresh_and_existing_access_tokens()
    {
        using var s = await fixture.CreateScenarioAsync();
        using var actor = s.ActAs();
        var auth = s.Services.GetRequiredService<IAuthService>();
        var login = await auth.LoginAsync(new() { Email = s.Admin.Email!, Password = fixture.Password });
        Assert.True(login.IsSuccess);
        using var client = fixture.Factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        client.DefaultRequestHeaders.Authorization = new("Bearer", login.Data!.AccessToken);
        Assert.True((await auth.UpdateUserStatusAsync(s.Tenant.TenantID, s.Admin.Id, false)).IsSuccess);
        Assert.False((await auth.RefreshTokenAsync(new() { AccessToken = login.Data.AccessToken, RefreshToken = login.Data.RefreshToken })).IsSuccess);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/Profile")).StatusCode);
    }

    [Fact]
    public async Task Disabled_tenant_cannot_login_or_refresh()
    {
        using var s = await fixture.CreateScenarioAsync();
        using var actor = s.ActAs();
        var auth = s.Services.GetRequiredService<IAuthService>();
        var login = await auth.LoginAsync(new() { Email = s.Admin.Email!, Password = fixture.Password });
        Assert.True((await s.Services.GetRequiredService<ITenantService>().DeleteAsync(s.Tenant.TenantID)).IsSuccess);
        Assert.False(s.Tenant.IsActive);
        Assert.False((await auth.LoginAsync(new() { Email = s.Admin.Email!, Password = fixture.Password })).IsSuccess);
        Assert.False((await auth.RefreshTokenAsync(new() { AccessToken = login.Data!.AccessToken, RefreshToken = login.Data.RefreshToken })).IsSuccess);
    }

    [Fact]
    public async Task Migrations_cover_model_and_navigation_keys_are_not_shadow_duplicates()
    {
        using var scope = fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.False(db.Database.HasPendingModelChanges());
        Assert.DoesNotContain(db.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()).SelectMany(f => f.Properties),
            p => p.IsShadowProperty() && p.Name.EndsWith("1", StringComparison.Ordinal));
        Assert.Contains((await db.Database.GetAppliedMigrationsAsync()), m => m.EndsWith("HardenBackendIntegrity"));
    }

    [Theory]
    [InlineData(-10, "USD")]
    [InlineData(0, "USD")]
    [InlineData(100, "LYD")]
    public async Task Invalid_payments_do_not_change_invoice_or_create_records(int amount, string currency)
    {
        using var s = await fixture.CreateScenarioAsync(); using var actor = s.ActAs();
        var invoice = await s.InvoiceAsync();
        var result = await s.Services.GetRequiredService<IFinancialService>().RecordPaymentAsync(s.Tenant.TenantID, s.Admin.Id,
            new() { IdempotencyKey = Guid.NewGuid().ToString("N"), InvoiceID = invoice.InvoiceID, Amount = amount, CurrencyCode = currency, Method = "Cash" });
        Assert.False(result.IsSuccess);
        Assert.False(await s.Db.Payments.AnyAsync(p => p.InvoiceID == invoice.InvoiceID));
        Assert.Equal(InvoiceStatus.Issued, invoice.Status);
    }

    [Fact]
    public async Task Separate_requests_cannot_collect_120_for_an_invoice_of_100()
    {
        using var s = await fixture.CreateScenarioAsync(); using var actor = s.ActAs();
        var invoice = await s.InvoiceAsync();
        for (var n = 0; n < 2; n++)
        {
            using var fresh = fixture.Factory.Services.CreateScope();
            var result = await fresh.ServiceProvider.GetRequiredService<IFinancialService>().RecordPaymentAsync(s.Tenant.TenantID, s.Admin.Id,
                new() { IdempotencyKey = Guid.NewGuid().ToString("N"), InvoiceID = invoice.InvoiceID, Amount = 60, CurrencyCode = "USD", Method = "Cash" });
            Assert.Equal(n == 0, result.IsSuccess);
        }
        using var verify = fixture.Factory.Services.CreateScope();
        var db = verify.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var loaded = await db.Invoices.Include(i => i.Payments).SingleAsync(i => i.InvoiceID == invoice.InvoiceID);
        Assert.Single(loaded.Payments);
        Assert.Equal(60, loaded.Payments.Sum(p => p.Amount));
        Assert.Equal(InvoiceStatus.PartiallyPaid, loaded.Status);
    }

    [Fact]
    public async Task Paid_invoice_items_are_immutable()
    {
        using var s = await fixture.CreateScenarioAsync(); using var actor = s.ActAs();
        var invoice = await s.InvoiceAsync();
        Assert.True((await s.Services.GetRequiredService<IFinancialService>().RecordPaymentAsync(s.Tenant.TenantID, s.Admin.Id,
            new() { IdempotencyKey = Guid.NewGuid().ToString("N"), InvoiceID = invoice.InvoiceID, Amount = 100, CurrencyCode = "USD", Method = "Cash" })).IsSuccess);
        var items = s.Services.GetRequiredService<IInvoiceItemService>();
        Assert.False((await items.CreateItemAsync(s.Tenant.TenantID, invoice.InvoiceID, new() { ItemName = "Extra", Quantity = 1, UnitPrice = 25 })).IsSuccess);
        Assert.False((await items.DeleteItemAsync(s.Tenant.TenantID, invoice.InvoiceItems.Single().InvoiceItemID)).IsSuccess);
        Assert.Equal(100, invoice.TotalAmount);
        Assert.False((await s.Services.GetRequiredService<IReservationService>().CancelAsync(s.Tenant.TenantID, invoice.ReservationID)).IsSuccess);
        Assert.False((await s.Services.GetRequiredService<IReservationService>().AddServiceToReservationAsync(s.Tenant.TenantID,
            invoice.ReservationID, new() { ServiceID = s.Service.ServiceID, Quantity = 1, CurrencyCode = "USD" })).IsSuccess);
    }

    [Fact]
    public async Task Fractional_services_are_priced_and_removed_without_orphan_errors()
    {
        using var s = await fixture.CreateScenarioAsync(); using var actor = s.ActAs();
        var invoice = await s.InvoiceAsync(); var service = s.Services.GetRequiredService<IReservationService>();
        var added = await service.AddServiceToReservationAsync(s.Tenant.TenantID, invoice.ReservationID,
            new() { ServiceID = s.Service.ServiceID, Quantity = 1.5m, CurrencyCode = "USD" });
        Assert.True(added.IsSuccess, added.ErrorMessage); Assert.Equal(15, added.Data!.TotalPrice);
        Assert.True((await service.RemoveServiceFromReservationAsync(s.Tenant.TenantID, invoice.ReservationID, added.Data.ReservationServiceID)).IsSuccess);
        Assert.False(await s.Db.ReservationServices.AnyAsync(r => r.ReservationServiceID == added.Data.ReservationServiceID));
        Assert.Equal(100, invoice.TotalAmount);
    }

    [Fact]
    public async Task Cancelled_bookings_cannot_be_reapproved_or_moved_across_tenants()
    {
        using var s = await fixture.CreateScenarioAsync(); using var actor = s.ActAs();
        var invoice = await s.InvoiceAsync(InvoiceStatus.Cancelled);
        invoice.Reservation.Status = ReservationStatus.Cancelled; await s.Db.SaveChangesAsync();
        var service = s.Services.GetRequiredService<IReservationService>();
        Assert.False((await service.ApproveAsync(s.Tenant.TenantID, invoice.ReservationID)).IsSuccess);
        Assert.False((await service.UpdateAsync(s.Tenant.TenantID, invoice.ReservationID,
            new() { Status = "Confirmed", BoothID = s.OtherBooth.BoothID })).IsSuccess);
        Assert.Equal(BoothStatus.Available, s.OtherBooth.Status);
    }

    [Fact]
    public async Task Templates_cannot_reference_another_tenants_exhibition()
    {
        using var s = await fixture.CreateScenarioAsync(); using var actor = s.ActAs();
        var result = await s.Services.GetRequiredService<IBadgeService>().CreateTemplateAsync(s.Tenant.TenantID,
            new() { TemplateName = "Forbidden", ExhibitionID = s.OtherExhibition.ExhibitionID });
        Assert.False(result.IsSuccess);
        Assert.False(await s.Db.BadgeTemplates.AnyAsync(t => t.TenantID == s.Tenant.TenantID));
    }

    [Fact]
    public async Task Future_tickets_are_rejected_and_correct_visitor_navigation_loads()
    {
        using var s = await fixture.CreateScenarioAsync(); using var actor = s.ActAs();
        var ticket = BackendFixture.Fill(new Ticket { VisitorID = s.Visitor.VisitorID, ExhibitionID = s.Exhibition.ExhibitionID,
            TicketType = "Test", QRCode = Guid.NewGuid().ToString(), CurrencyCode = "USD", Status = TicketStatus.Active, ValidDate = DateTime.UtcNow.Date.AddDays(1) });
        s.Db.Add(ticket); await s.Db.SaveChangesAsync();
        Assert.False((await s.Services.GetRequiredService<ITicketService>().ScanTicketAsync(s.Tenant.TenantID, ticket.QRCode, "In", "Test", s.Admin.Id)).IsSuccess);
        using var scope = fixture.Factory.Services.CreateScope();
        var visitor = await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Visitors.Include(v => v.Tickets).SingleAsync(v => v.VisitorID == s.Visitor.VisitorID);
        Assert.Single(visitor.Tickets);
    }

    [Fact]
    public async Task Audit_failure_rolls_back_business_write()
    {
        using var s = await fixture.CreateScenarioAsync(); using var actor = s.ActAs("missing-audit-user");
        var name = "Atomic-" + Guid.NewGuid();
        var uow = s.Services.GetRequiredService<IUnitOfWork>();
        await uow.Services.AddAsync(BackendFixture.Fill(new Service { TenantID = s.Tenant.TenantID, ServiceName = name }));
        await Assert.ThrowsAsync<DbUpdateException>(() => uow.SaveChangesAsync());
        Assert.False(await s.Db.Services.AnyAsync(x => x.ServiceName == name));
    }

    [Fact]
    public async Task Zero_exchange_rates_are_rejected()
    {
        using var s = await fixture.CreateScenarioAsync(); using var actor = s.ActAs();
        Assert.False((await s.Services.GetRequiredService<ICurrencyService>().UpsertExchangeRateAsync(s.Admin.Id,
            new() { FromCurrency = "USD", ToCurrency = "LYD", Rate = 0, RateDate = DateTime.UtcNow.Date })).IsSuccess);
    }

    [Fact]
    public async Task Advertising_spaces_cannot_reference_another_tenants_hall()
    {
        using var s = await fixture.CreateScenarioAsync(); using var actor = s.ActAs();
        var result = await s.Services.GetRequiredService<ISponsorshipService>().CreateSpaceAsync(s.Tenant.TenantID,
            new() { ExhibitionID = s.Exhibition.ExhibitionID, HallID = s.OtherBooth.HallID, SpaceCode = "Bad", SpaceName = "Bad", CurrencyCode = "USD" });
        Assert.False(result.IsSuccess);
        Assert.False(await s.Db.AdvertisingSpaces.AnyAsync(a => a.TenantID == s.Tenant.TenantID));
    }

    [Fact]
    public async Task Demo_seeding_refuses_the_normal_application_database()
    {
        using var scope = fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var count = await db.Tenants.CountAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(() => DataSeederTest.SeedDataAsync(fixture.Factory.Services));
        Assert.Equal(count, await db.Tenants.CountAsync());
    }
}
