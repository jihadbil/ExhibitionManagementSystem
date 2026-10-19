using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ExhibitionManagementSystem.DataAccess;
using ExhibitionManagementSystem.Models;
using ExhibitionManagementSystem.Models.DTOs.Booth;
using ExhibitionManagementSystem.Models.Enums;
using ExhibitionManagementSystem.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Backend.RegressionTests;

public sealed partial class IntegrityTests
{
    [Fact]
    public async Task Assigned_booth_prices_convert_currency_and_reject_foreign_rules()
    {
        using var s = await fixture.CreateScenarioAsync(); using var actor = s.ActAs();
        var rule = BackendFixture.Fill(new BoothPriceRule { TenantID = s.Tenant.TenantID, ExhibitionID = s.Exhibition.ExhibitionID,
            PricePerSqM = 10, CurrencyCode = "USD", ValidFrom = DateTime.UtcNow.Date.AddDays(-1) });
        s.Db.Add(rule); await s.Db.SaveChangesAsync();
        s.Booth.AssignedPriceRuleID = rule.RuleID; await s.Db.SaveChangesAsync();
        var pricing = s.Services.GetRequiredService<IPricingService>();
        var result = await pricing.CalculateBoothPriceAsync(s.Tenant.TenantID, s.Exhibition.ExhibitionID,
            BoothType.Standard, ExhibitorCategory.Local, 10, s.Booth.BoothID, "LYD");
        Assert.True(result.IsSuccess, result.ErrorMessage); Assert.Equal(480, result.Data);
        rule.TenantID = s.OtherTenant.TenantID; await s.Db.SaveChangesAsync();
        Assert.False((await pricing.CalculateBoothPriceAsync(s.Tenant.TenantID, s.Exhibition.ExhibitionID,
            BoothType.Standard, ExhibitorCategory.Local, 10, s.Booth.BoothID, "USD")).IsSuccess);
    }

    [Fact]
    public async Task Dashboard_pending_does_not_subtract_paid_invoices_twice()
    {
        using var s = await fixture.CreateScenarioAsync(); using var actor = s.ActAs();
        var paid = await s.InvoiceAsync(); await s.InvoiceAsync();
        Assert.True((await s.Services.GetRequiredService<IFinancialService>().RecordPaymentAsync(s.Tenant.TenantID, s.Admin.Id,
            new() { IdempotencyKey = Guid.NewGuid().ToString("N"), InvoiceID = paid.InvoiceID, Amount = 100, CurrencyCode = "USD", Method = "Cash" })).IsSuccess);
        using var client = await ClientAsync(s);
        var response = await client.GetAsync("/api/Dashboard/stats");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var stats = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(100, stats.GetProperty("pendingRevenue").GetDecimal());
        Assert.Equal(100, stats.GetProperty("totalRevenue").GetDecimal());
    }

    [Fact]
    public async Task Report_converts_each_currency_and_counts_used_visitors()
    {
        using var s = await fixture.CreateScenarioAsync(); using var actor = s.ActAs();
        await s.InvoiceAsync(); var dinars = await s.InvoiceAsync();
        dinars.TotalAmount = 48; dinars.SubTotal = 48; dinars.CurrencyCode = "LYD";
        s.Db.Add(BackendFixture.Fill(new Ticket { VisitorID = s.Visitor.VisitorID, ExhibitionID = s.Exhibition.ExhibitionID,
            TicketType = "Test", QRCode = Guid.NewGuid().ToString(), CurrencyCode = "USD", Status = TicketStatus.Used }));
        await s.Db.SaveChangesAsync();
        var report = await s.Services.GetRequiredService<IReportService>().GenerateExhibitionReportAsync(s.Tenant.TenantID, s.Exhibition.ExhibitionID, s.Admin.Id);
        Assert.True(report.IsSuccess, report.ErrorMessage);
        Assert.Equal(110, report.Data!.TotalRevenue); Assert.Equal(1, report.Data.TotalVisitors); Assert.Equal("USD", report.Data.CurrencyCode);
    }

    [Fact]
    public async Task Populated_merge_can_be_unmerged_only_by_its_tenant()
    {
        using var s = await fixture.CreateScenarioAsync(); using var actor = s.ActAs();
        var second = BackendFixture.Fill(new Booth { HallID = s.Booth.HallID, BoothNumber = "A2", OriginalAreaSqM = 10, CurrentAreaSqM = 10 });
        s.Db.Add(second); await s.Db.SaveChangesAsync();
        using var mergeScope = fixture.Factory.Services.CreateScope();
        var booths = mergeScope.ServiceProvider.GetRequiredService<IBoothService>();
        var merged = await booths.MergeBoothsAsync(s.Tenant.TenantID, s.Admin.Id, new BoothMergeCreateDto
        {
            ExhibitionID = s.Exhibition.ExhibitionID, HallID = s.Booth.HallID,
            BoothIDs = [s.Booth.BoothID, second.BoothID], MergedBoothLabel = "Test merge"
        });
        Assert.True(merged.IsSuccess, merged.ErrorMessage);
        using var unmergeScope = fixture.Factory.Services.CreateScope();
        var unmerge = unmergeScope.ServiceProvider.GetRequiredService<IBoothService>();
        Assert.False((await unmerge.UnmergeBoothsAsync(s.OtherTenant.TenantID, merged.Data!.MergeID)).IsSuccess);
        var removed = await unmerge.UnmergeBoothsAsync(s.Tenant.TenantID, merged.Data.MergeID);
        Assert.True(removed.IsSuccess, removed.ErrorMessage);
        Assert.False(await s.Db.BoothMergeItems.AnyAsync(i => i.MergeID == merged.Data.MergeID));
        Assert.False(await s.Db.BoothMerges.AnyAsync(m => m.MergeID == merged.Data.MergeID));
    }

    [Fact]
    public async Task Concurrent_payments_do_not_exceed_the_invoice_balance()
    {
        using var s = await fixture.CreateScenarioAsync(); using var actor = s.ActAs();
        var invoice = await s.InvoiceAsync();
        using var client = await ClientAsync(s);
        var responses = await Task.WhenAll(Enumerable.Range(0, 2).Select(_ => client.PostAsJsonAsync("/api/Financial/payments",
            new { idempotencyKey = Guid.NewGuid().ToString("N"), invoiceID = invoice.InvoiceID, amount = 60, currencyCode = "USD", method = "Cash" })));
        Assert.Single(responses, r => r.IsSuccessStatusCode);
        var rejected = Assert.Single(responses, r => !r.IsSuccessStatusCode);
        Assert.True(rejected.StatusCode is HttpStatusCode.Conflict or HttpStatusCode.BadRequest);
        if (rejected.StatusCode == HttpStatusCode.BadRequest)
            Assert.Equal("PAYMENT_AMOUNT_EXCEEDS_REMAINING", (await rejected.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("errorCode").GetString());
        Assert.Equal(60, await s.Db.Payments.Where(p => p.InvoiceID == invoice.InvoiceID).SumAsync(p => p.Amount));
        foreach (var response in responses) response.Dispose();
    }

    [Fact]
    public async Task Password_changes_revoke_refresh_tokens()
    {
        using var s = await fixture.CreateScenarioAsync(); using var actor = s.ActAs();
        var auth = s.Services.GetRequiredService<IAuthService>();
        var login = await auth.LoginAsync(new() { Email = s.Admin.Email!, Password = fixture.Password });
        var changed = await auth.ChangePasswordAsync(s.Admin.Id, new() { CurrentPassword = fixture.Password, NewPassword = fixture.Password + "New1!", ConfirmNewPassword = fixture.Password + "New1!" });
        Assert.True(changed.IsSuccess, changed.ErrorMessage);
        Assert.False((await auth.RefreshTokenAsync(new() { AccessToken = login.Data!.AccessToken, RefreshToken = login.Data.RefreshToken })).IsSuccess);
    }

    [Fact]
    public async Task Invoice_creation_derives_totals_from_the_booking_instead_of_the_request()
    {
        using var s = await fixture.CreateScenarioAsync(); using var actor = s.ActAs();
        var reservation = BackendFixture.Fill(new BoothReservation { ExhibitionID = s.Exhibition.ExhibitionID,
            ExhibitorID = s.Exhibitor.ExhibitorID, CreatedByUserId = s.Admin.Id, CurrencyCode = "USD",
            BoothAmount = 100, TotalAmount = 100, ExchangeRateUsed = 1, Status = ReservationStatus.Confirmed });
        s.Db.Add(reservation); await s.Db.SaveChangesAsync();
        var created = await s.Services.GetRequiredService<IFinancialService>().CreateInvoiceAsync(s.Tenant.TenantID,
            new() { ReservationID = reservation.ReservationID, InvoiceNumber = "CREATE-" + Guid.NewGuid().ToString("N"),
                SubTotal = 1, TotalAmount = 1, TaxRate = 10, CurrencyCode = "USD" });
        Assert.True(created.IsSuccess, created.ErrorMessage);
        Assert.Equal(100, created.Data!.SubTotal); Assert.Equal(10, created.Data.TaxAmount); Assert.Equal(110, created.Data.TotalAmount);
        Assert.Single(created.Data.Items);
    }

    [Fact]
    public async Task Upgrade_stops_on_conflicting_shadow_keys_and_preserves_financial_data_after_reconciliation()
    {
        var connection = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("BACKEND_TEST_CONNECTION")!);
        connection.InitialCatalog += "_Upgrade";
        await using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(connection.ConnectionString).Options);
        var previous = db.Database.GetMigrations().TakeWhile(m => !m.EndsWith("HardenBackendIntegrity")).Last();
        var migrator = db.GetService<IMigrator>();
        await migrator.MigrateAsync(previous);
        var currency = new Currency { CurrencyCode = "USD", CurrencyName = "Dollar", Symbol = "$" };
        await LegacyInsert(db, currency);
        var tenant = new Tenant { CompanyName = "Upgrade test", Subdomain = "upgrade", BaseCurrency = "USD" };
        await LegacyInsert(db, tenant);
        var user = new ApplicationUser { FullName = "Upgrade test", TenantID = tenant.TenantID, UserName = "upgrade@test.invalid" };
        await LegacyInsert(db, user);
        var venue = BackendFixture.Fill(new Venue { TenantID = tenant.TenantID, Name = "Upgrade venue" }); await LegacyInsert(db, venue);
        var exhibition = BackendFixture.Fill(new Exhibition { TenantID = tenant.TenantID, VenueID = venue.VenueID, EntryCurrency = "USD", Name = "Upgrade exhibition" }); await LegacyInsert(db, exhibition);
        var exhibitor = BackendFixture.Fill(new Exhibitor { TenantID = tenant.TenantID, CompanyName = "Upgrade exhibitor" }); await LegacyInsert(db, exhibitor);
        async Task<Invoice> OldInvoice(string number)
        {
            var reservation = BackendFixture.Fill(new BoothReservation { ExhibitionID = exhibition.ExhibitionID, ExhibitorID = exhibitor.ExhibitorID,
                CreatedByUserId = user.Id, CurrencyCode = "USD", BoothAmount = 100, TotalAmount = 100 }); await LegacyInsert(db, reservation);
            var invoice = BackendFixture.Fill(new Invoice { TenantID = tenant.TenantID, ReservationID = reservation.ReservationID,
                InvoiceNumber = number, CurrencyCode = "USD", TotalAmount = 100, SubTotal = 100 }); await LegacyInsert(db, invoice); return invoice;
        }
        var first = await OldInvoice("INV-0001-2026-04242"); var second = await OldInvoice("OLD-2");
        var payment = BackendFixture.Fill(new Payment { InvoiceID = first.InvoiceID, Amount = 60, CurrencyCode = "USD", Method = PaymentMethod.Cash,
            Status = PaymentStatus.Completed, ReceivedByUserId = user.Id }); await LegacyInsert(db, payment);
        await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE Payments SET InvoiceID1={second.InvoiceID} WHERE PaymentID={payment.PaymentID}");
        var failure = await Assert.ThrowsAsync<SqlException>(() => migrator.MigrateAsync());
        Assert.Equal(51000, failure.Number);
        Assert.DoesNotContain((await db.Database.GetAppliedMigrationsAsync()), m => m.EndsWith("HardenBackendIntegrity"));
        await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE Payments SET InvoiceID1={first.InvoiceID} WHERE PaymentID={payment.PaymentID}");
        await migrator.MigrateAsync();
        var loaded = await db.Invoices.Include(i => i.Payments).SingleAsync(i => i.InvoiceID == first.InvoiceID);
        Assert.Equal(60, Assert.Single(loaded.Payments).Amount);
        Assert.Equal(second.InvoiceNumber, (await db.Invoices.SingleAsync(i => i.InvoiceID == second.InvoiceID)).InvoiceNumber);
        Assert.Equal(8, loaded.RowVersion.Length);
        var next = await db.Database.SqlQueryRaw<long>("SELECT NEXT VALUE FOR [InvoiceNumbers] AS [Value]").ToListAsync();
        Assert.True(next.Single() > 4242);

    }

    // Builds legacy fixture rows through parameterized SQL because the current
    // EF model correctly expects RowVersion columns absent from the old schema.
    private static async Task LegacyInsert<T>(ApplicationDbContext db, T entity) where T : class
    {
        var type = db.Model.FindEntityType(typeof(T))!;
        var key = type.FindPrimaryKey()!.Properties.Single();
        var columns = type.GetProperties().Where(p => !p.IsShadowProperty() && p.Name != "RowVersion" && p.Name != "IdempotencyKey"
            && !(p.IsPrimaryKey() && p.ClrType == typeof(int))).ToList();
        var connection = db.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open) await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = $"INSERT INTO [{type.GetTableName()}] (" + string.Join(',', columns.Select(p => "[" + p.GetColumnName() + "]"))
            + $") OUTPUT INSERTED.[{key.GetColumnName()}] VALUES (" + string.Join(',', columns.Select((_, i) => "@p" + i)) + ")";
        for (var i = 0; i < columns.Count; i++)
        {
            var p = columns[i]; var value = p.PropertyInfo!.GetValue(entity);
            var converter = p.GetTypeMapping().Converter;
            var parameter = command.CreateParameter(); parameter.ParameterName = "@p" + i;
            parameter.Value = (converter == null ? value : converter.ConvertToProvider(value)) ?? DBNull.Value;
            if (value is DateTime) parameter.DbType = System.Data.DbType.DateTime2;
            command.Parameters.Add(parameter);
        }
        var id = await command.ExecuteScalarAsync();
        if (key.ClrType == typeof(int)) key.PropertyInfo!.SetValue(entity, Convert.ToInt32(id));
    }
}
