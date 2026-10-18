using System.Data.Common;
using ExhibitionManagementSystem.DataAccess;
using ExhibitionManagementSystem.DataAccess.MultiTenancy;
using ExhibitionManagementSystem.DataAccess.Repositories.Implementations;
using ExhibitionManagementSystem.Models;
using ExhibitionManagementSystem.Models.DTOs.Sponsorship;
using ExhibitionManagementSystem.Services.Implementations;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;

namespace ExhibitionManagementSystem.Api.Tests.Regression;

public sealed class SponsorshipConcurrencyTests
{
    private sealed class Scope(int tenantId) : ITenantContext
    { public int? TenantId => tenantId; public bool CanBypassTenantIsolation => false; }

    private sealed class AdmissionBarrier : DbCommandInterceptor
    {
        private int _arrivals;
        private readonly TaskCompletionSource _bothRead = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public override async ValueTask<DbDataReader> ReaderExecutedAsync(DbCommand command,
            CommandExecutedEventData eventData, DbDataReader result, CancellationToken cancellationToken = default)
        {
            if (command.CommandText.Contains("COUNT", StringComparison.Ordinal) &&
                command.CommandText.Contains("SponsorshipContracts", StringComparison.Ordinal))
            {
                if (Interlocked.Increment(ref _arrivals) == 2) _bothRead.TrySetResult();
                await _bothRead.Task.WaitAsync(TimeSpan.FromSeconds(20), cancellationToken);
            }
            return result;
        }
    }

    [SqlServerFact]
    public async Task ConcurrentContractRequestsCannotOversellTheLastPackageSlot()
    {
        var name = "EMS_SponsorshipRace_" + Guid.NewGuid().ToString("N");
        var connection = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("EMS_SQL_TEST_CONNECTION"))
        { InitialCatalog = name, TrustServerCertificate = true };
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(connection.ConnectionString).Options;
        await using var seed = new ApplicationDbContext(options, new WorkflowFixture.BypassContext());
        try
        {
            await seed.Database.MigrateAsync();
            var fixture = await WorkflowFixture.SeedAsync(seed);
            var sponsor = new Sponsor { TenantID = fixture.TenantID, CompanyName = "Synthetic sponsor" };
            var package = new SponsorshipPackage { TenantID = fixture.TenantID, ExhibitionID = fixture.Reservation.ExhibitionID,
                PackageName = "One slot", CurrencyCode = "LYD", Price = 100, MaxSponsorsCount = 1, IsActive = true };
            seed.Sponsors.Add(sponsor); seed.SponsorshipPackages.Add(package); await seed.SaveChangesAsync();
            var barrier = new AdmissionBarrier();
            var racingOptions = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(connection.ConnectionString).AddInterceptors(barrier).Options;
            async Task<bool> Admit(string number)
            {
                await using var db = new ApplicationDbContext(racingOptions, new Scope(fixture.TenantID));
                using var uow = new UnitOfWork(db, WorkflowFixture.Actor(fixture.TenantID));
                var result = await new SponsorshipService(uow, WorkflowFixture.Mapper).CreateContractAsync(fixture.TenantID,
                    "workflow-user", new SponsorshipContractCreateDto { ExhibitionID = fixture.Reservation.ExhibitionID,
                        SponsorID = sponsor.SponsorID, PackageID = package.PackageID, ContractNumber = number, CurrencyCode = "LYD", TotalAmount = 100 });
                return result.IsSuccess;
            }
            var results = await Task.WhenAll(Admit("RACE-ONE"), Admit("RACE-TWO"));
            Assert.Equal(1, results.Count(success => success));
            Assert.Equal(1, await seed.SponsorshipContracts.CountAsync(c => c.PackageID == package.PackageID));
        }
        finally
        {
            Assert.Equal(name, new SqlConnectionStringBuilder(seed.Database.GetConnectionString()).InitialCatalog);
            await seed.Database.EnsureDeletedAsync();
        }
    }
}
