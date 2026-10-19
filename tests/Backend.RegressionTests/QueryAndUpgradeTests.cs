using System.Data.Common;
using ExhibitionManagementSystem.DataAccess;
using ExhibitionManagementSystem.Models;
using ExhibitionManagementSystem.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Backend.RegressionTests;

public sealed partial class IntegrityTests
{
    [Fact]
    public async Task Listing_users_uses_three_queries_for_roles_regardless_of_page_population()
    {
        using var s = await fixture.CreateScenarioAsync();
        var manager = s.Services.GetRequiredService<UserManager<ApplicationUser>>();
        for (var i = 0; i < 10; i++)
        {
            var user = new ApplicationUser { UserName = Guid.NewGuid() + "@test.invalid", FullName = "Staff", TenantID = s.Tenant.TenantID };
            Assert.True((await manager.CreateAsync(user)).Succeeded);
            Assert.True((await manager.AddToRoleAsync(user, "Staff")).Succeeded);
        }
        var counter = new QueryCounter();
        await using var factory = fixture.Factory.WithWebHostBuilder(b => b.ConfigureTestServices(services =>
            services.AddDbContext<ApplicationDbContext>(options => options.AddInterceptors(counter))));
        using var scope = factory.Services.CreateScope();
        var auth = scope.ServiceProvider.GetRequiredService<IAuthService>();
        counter.Count = 0;
        var result = await auth.GetUsersAsync(s.Tenant.TenantID, 1, 20);
        Assert.True(result.IsSuccess, result.ErrorMessage);
        Assert.Equal(11, result.Data!.Items.Count);
        Assert.All(result.Data.Items, u => Assert.Single(u.Roles));
        Assert.Equal(3, counter.Count);
    }

    [Fact]
    public async Task Sponsorship_upgrade_stops_before_DDL_on_duplicate_spaces_and_preserves_history_after_reconciliation()
    {
        var connection = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("BACKEND_TEST_CONNECTION")!);
        connection.InitialCatalog += "_SponsorUpgrade";
        await using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(connection.ConnectionString).Options);
        var migrator = db.GetService<IMigrator>();
        await migrator.MigrateAsync(db.Database.GetMigrations().Single(m => m.EndsWith("HardenBackendIntegrity")));
        var currency = new Currency { CurrencyCode = "USD", CurrencyName = "Dollar", Symbol = "$" };
        var tenant = new Tenant { CompanyName = "Migration", Subdomain = "migration", BaseCurrency = "USD" };
        var venue = BackendFixture.Fill(new Venue { Tenant = tenant, Name = "Venue" });
        var exhibition = BackendFixture.Fill(new Exhibition { Tenant = tenant, Venue = venue, Name = "Exhibition", EntryCurrency = "USD" });
        var sponsor = new Sponsor { Tenant = tenant, CompanyName = "Sponsor" };
        var space = new AdvertisingSpace { Tenant = tenant, Exhibition = exhibition, SpaceCode = "A1", SpaceName = "Screen", CurrencyCode = "USD", IsAvailable = false };
        var contract = new SponsorshipContract { Tenant = tenant, Exhibition = exhibition, Sponsor = sponsor, ContractNumber = "SPN-1", CurrencyCode = "USD" };
        var first = new SponsorAdAssignment { Contract = contract, Space = space };
        var second = new SponsorAdAssignment { Contract = contract, Space = space };
        db.AddRange(currency, first, second); await db.SaveChangesAsync();
        var failure = await Assert.ThrowsAsync<SqlException>(() => migrator.MigrateAsync());
        Assert.Equal(51000, failure.Number);
        Assert.DoesNotContain(await db.Database.GetAppliedMigrationsAsync(), m => m.EndsWith("CompleteBackendHardening"));
        Assert.Equal(0, (await db.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS [Value] FROM sys.tables WHERE name='PasswordResetMessages'").ToListAsync()).Single());
        second.IsDeleted = true; second.DeletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(); await migrator.MigrateAsync();
        Assert.Single(await db.SponsorAdAssignments.AsNoTracking().ToListAsync());
        Assert.Equal(2, await db.SponsorAdAssignments.IgnoreQueryFilters().CountAsync());
        db.SponsorAdAssignments.Add(new SponsorAdAssignment { ContractID = contract.ContractID, SpaceID = space.SpaceID });
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    private sealed class QueryCounter : DbCommandInterceptor
    {
        public int Count { get; set; }
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command,
            CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            Count++; return ValueTask.FromResult(result);
        }
    }
}
