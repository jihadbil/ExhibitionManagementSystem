using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using AutoMapper;
using ExhibitionManagementSystem.Controllers.Visitor;
using ExhibitionManagementSystem.DataAccess;
using ExhibitionManagementSystem.DataAccess.MultiTenancy;
using ExhibitionManagementSystem.DataAccess.Repositories.Implementations;
using ExhibitionManagementSystem.DataAccess.Repositories.Interfaces;
using ExhibitionManagementSystem.Models;
using ExhibitionManagementSystem.Models.Enums;
using ExhibitionManagementSystem.Security;
using ExhibitionManagementSystem.Services.Implementations;
using ExhibitionManagementSystem.Services.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace ExhibitionManagementSystem.Api.Tests.Regression;

// Exercises real MVC model binding and authorization over loopback HTTP.
// Test-only authentication never runs through Program or the production seeder.
public sealed class AuditRepairHttpTests
{
    [Fact]
    public async Task ScanEndpoint_AcceptsValidRequestAndRejectsBadDirectionWithout500()
    {
        await using var host = await Host.Start();
        host.Client.DefaultRequestHeaders.Add("Audit-Role", "GateOperator");
        var invalid = await host.Client.PostAsJsonAsync("/api/tickets/scan", new { qrCode = "AUDIT-TODAY", direction = "Entry", location = "Gate" });
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        var valid = await host.Client.PostAsJsonAsync("/api/tickets/scan", new { qrCode = "AUDIT-TODAY", direction = "In", location = "Gate" });
        Assert.Equal(HttpStatusCode.OK, valid.StatusCode);
        var body = await valid.Content.ReadFromJsonAsync<JsonElement>(); Assert.Equal("In", body.GetProperty("scanDirection").GetString());
        var duplicate = await host.Client.PostAsJsonAsync("/api/tickets/scan", new { qrCode = "AUDIT-TODAY", direction = "In", location = "Gate" });
        Assert.False(duplicate.IsSuccessStatusCode); Assert.NotEqual(HttpStatusCode.InternalServerError, duplicate.StatusCode);
    }

    [Theory]
    [InlineData("Viewer", false)] [InlineData("GateOperator", false)] [InlineData("SuperAdmin", false)]
    [InlineData("Finance", true)] [InlineData("TenantAdmin", true)]
    public async Task FinancialDashboard_IsRoleProtected(string role, bool allowed)
    {
        await using var host = await Host.Start(); host.Client.DefaultRequestHeaders.Add("Audit-Role", role);
        var stats = await host.Client.GetFromJsonAsync<JsonElement>("/api/dashboard/stats");
        Assert.Equal(allowed, stats.GetProperty("canViewFinancials").GetBoolean());
        Assert.Equal(allowed ? JsonValueKind.Number : JsonValueKind.Null, stats.GetProperty("totalRevenue").ValueKind);
        var chart = await host.Client.GetAsync("/api/dashboard/revenue-chart");
        Assert.Equal(allowed ? HttpStatusCode.OK : HttpStatusCode.Forbidden, chart.StatusCode);
    }

    [Fact]
    public async Task EventManager_CanCreateScheduleButCannotReadFinance()
    {
        await using var host = await Host.Start(); host.Client.DefaultRequestHeaders.Add("Audit-Role", "EventManager");
        var response = await host.Client.PostAsJsonAsync("/api/exhibitions/1/schedules", new {
            hallID = 1, eventName = "Audit event", eventType = "Lecture",
            startDateTime = DateTime.UtcNow.Date.AddHours(10), endDateTime = DateTime.UtcNow.Date.AddHours(11) });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await host.Client.GetAsync("/api/financial/invoices")).StatusCode);
    }

    [Fact]
    public async Task InvoiceLinkedExhibitorDelete_ReturnsConflictAndLeavesInvoiceVisible()
    {
        await using var host = await Host.Start(); host.Client.DefaultRequestHeaders.Add("Audit-Role", "TenantAdmin");
        var deleted = await host.Client.DeleteAsync("/api/exhibitors/1"); Assert.Equal(HttpStatusCode.Conflict, deleted.StatusCode);
        var body = await deleted.Content.ReadFromJsonAsync<JsonElement>(); Assert.Equal("EXHIBITOR_HAS_INVOICES", body.GetProperty("errorCode").GetString());
        var page = await host.Client.GetFromJsonAsync<JsonElement>("/api/financial/invoices?pageSize=1&status=Issued");
        Assert.Equal(1, page.GetProperty("items").GetArrayLength()); Assert.Equal(1, page.GetProperty("allCount").GetInt32());
        Assert.Equal(HttpStatusCode.BadRequest, (await host.Client.GetAsync("/api/financial/invoices?status=999")).StatusCode);
    }

    [Fact]
    public async Task SubscriptionPlanChange_RequiresSuperAdminButAuditAccessStaysTenantScoped()
    {
        await using var host = await Host.Start(); host.Client.DefaultRequestHeaders.Add("Audit-Role", "TenantAdmin");
        var dto = new { planName = "Enterprise", status = "Active", price = 100, currencyCode = "LYD", endDate = DateTime.UtcNow.Date.AddYears(1) };
        var forbidden = await host.Client.PostAsJsonAsync("/api/admin/subscriptions", dto);
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
        host.Client.DefaultRequestHeaders.Remove("Audit-Role"); host.Client.DefaultRequestHeaders.Add("Audit-Role", "SuperAdmin");
        Assert.Equal(HttpStatusCode.OK, (await host.Client.PostAsJsonAsync("/api/admin/subscriptions", dto)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await host.Client.GetAsync("/api/admin/audit-logs")).StatusCode);
    }

    [Fact]
    public async Task AnonymousDashboard_IsUnauthorized()
    {
        await using var host = await Host.Start();
        Assert.Equal(HttpStatusCode.Unauthorized, (await host.Client.GetAsync("/api/dashboard/stats")).StatusCode);
    }

    internal sealed class Host(WebApplication app, HttpClient client) : IAsyncDisposable
    {
        public HttpClient Client { get; } = client;
        public static async Task<Host> Start()
        {
            var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Development" });
            builder.Configuration["AllowedHosts"] = "127.0.0.1";
            builder.Logging.ClearProviders(); builder.WebHost.UseUrls("http://127.0.0.1:0");
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddScoped<ITenantContext>(_ => new WorkflowFixture.BypassContext());
            var databaseName = Guid.NewGuid().ToString("N");
            builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseInMemoryDatabase(databaseName));
            builder.Services.AddScoped<IUnitOfWork, UnitOfWork>(); builder.Services.AddSingleton<IMapper>(SystemAuditRepairTests.Mapper);
            builder.Services.AddScoped<ITicketService, TicketService>(); builder.Services.AddScoped<IFinancialService, FinancialService>();
            builder.Services.AddScoped<IExhibitionService, ExhibitionService>(); builder.Services.AddScoped<IExhibitorService, ExhibitorService>();
            builder.Services.AddScoped<IAdminService, AdminService>();
            builder.Services.AddAuthentication("Audit").AddScheme<AuthenticationSchemeOptions, AuditAuthHandler>("Audit", _ => { });
            builder.Services.AddAuthorization(AuthorizationPolicies.Configure);
            builder.Services.AddControllers().AddApplicationPart(typeof(TicketsController).Assembly);
            var app = builder.Build();
            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>(); var invoice = await WorkflowFixture.SeedAsync(db);
                var exhibition = await db.Exhibitions.SingleAsync(); exhibition.StartDate = DateTime.UtcNow.Date.AddDays(-1); exhibition.EndDate = DateTime.UtcNow.Date.AddDays(5);
                db.Tickets.Add(new Ticket { TenantID = invoice.TenantID, ExhibitionID = exhibition.ExhibitionID, TicketType = "Regular",
                    QRCode = "AUDIT-TODAY", CurrencyCode = "LYD", ValidDate = DateTime.UtcNow.Date,
                    Visitor = new Visitor { TenantID = invoice.TenantID, FullName = "Synthetic visitor" } });
                await db.SaveChangesAsync();
            }
            app.UseAuthentication(); app.UseAuthorization(); app.MapControllers(); await app.StartAsync();
            var url = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
            return new Host(app, new HttpClient(new HttpClientHandler { UseProxy = false }) { BaseAddress = new Uri(url) });
        }
        public async ValueTask DisposeAsync() { Client.Dispose(); await app.StopAsync(); await app.DisposeAsync(); }
    }

    private sealed class AuditAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var role = Request.Headers["Audit-Role"].ToString();
            if (string.IsNullOrWhiteSpace(role)) return Task.FromResult(AuthenticateResult.NoResult());
            var claims = new[] { new Claim(ClaimTypes.NameIdentifier, "workflow-user"), new Claim("TenantId", "1"), new Claim(ClaimTypes.Role, role) };
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(new ClaimsIdentity(claims, Scheme.Name)), Scheme.Name)));
        }
    }
}
