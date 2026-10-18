using System.Collections.Concurrent;
using System.Data.Common;
using ExhibitionManagementSystem.DataAccess;
using ExhibitionManagementSystem.DataAccess.MultiTenancy;
using ExhibitionManagementSystem.DataAccess.Repositories.Implementations;
using ExhibitionManagementSystem.Models;
using ExhibitionManagementSystem.Models.DTOs.Exhibition;
using ExhibitionManagementSystem.Models.DTOs.Sponsorship;
using ExhibitionManagementSystem.Services.Implementations;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;

namespace ExhibitionManagementSystem.Api.Tests.Regression;

public sealed class ParentDeletionConcurrencyTests
{
    private sealed class Scope(int tenantId) : ITenantContext
    { public int? TenantId => tenantId; public bool CanBypassTenantIsolation => false; }

    private sealed class ParentReadBarrier(string table) : DbCommandInterceptor
    {
        private readonly ConcurrentDictionary<Guid, byte> _contexts = new();
        private readonly TaskCompletionSource _ready = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public override async ValueTask<DbDataReader> ReaderExecutedAsync(DbCommand command,
            CommandExecutedEventData eventData, DbDataReader result, CancellationToken cancellationToken = default)
        {
            if (command.CommandText.Contains($"FROM [{table}]", StringComparison.Ordinal) &&
                eventData.Context != null && _contexts.TryAdd(eventData.Context.ContextId.InstanceId, 0))
            {
                if (_contexts.Count == 2) _ready.TrySetResult();
                await _ready.Task.WaitAsync(TimeSpan.FromSeconds(20), cancellationToken);
            }
            return result;
        }
    }

    [SqlServerTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ParentDeletionAndConcurrentChildCreationCannotBothSucceed(bool hallCase)
    {
        var name = "EMS_ParentRace_20261017_" + Guid.NewGuid().ToString("N");
        var connection = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("EMS_SQL_TEST_CONNECTION"))
        { InitialCatalog = name, TrustServerCertificate = true };
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(connection.ConnectionString).Options;
        await using var seed = new ApplicationDbContext(options, new WorkflowFixture.BypassContext());
        try
        {
            await seed.Database.MigrateAsync();
            var fixture = await WorkflowFixture.SeedAsync(seed);
            var exhibition = await seed.Exhibitions.SingleAsync();
            exhibition.StartDate = DateTime.UtcNow.Date;
            exhibition.EndDate = DateTime.UtcNow.Date.AddDays(2);
            var targetExhibition = new Exhibition { TenantID = fixture.TenantID, VenueID = exhibition.VenueID,
                Name = "Concurrent parent", EntryCurrency = "LYD", StartDate = exhibition.StartDate, EndDate = exhibition.EndDate };
            var hall = new Hall { VenueID = exhibition.VenueID, HallName = "Concurrent parent hall" };
            var sponsor = new Sponsor { TenantID = fixture.TenantID, CompanyName = "Concurrent sponsor" };
            seed.AddRange(targetExhibition, hall, sponsor);
            await seed.SaveChangesAsync();
            var barrier = new ParentReadBarrier(hallCase ? "Halls" : "Exhibitions");
            var racingOptions = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(connection.ConnectionString)
                .AddInterceptors(barrier).Options;

            async Task<bool> DeleteParent()
            {
                await using var db = new ApplicationDbContext(racingOptions, new Scope(fixture.TenantID));
                using var uow = new UnitOfWork(db, WorkflowFixture.Actor(fixture.TenantID));
                return hallCase
                    ? (await new HallService(uow, SystemAuditRepairTests.Mapper).DeleteAsync(fixture.TenantID, hall.HallID)).IsSuccess
                    : (await new ExhibitionService(uow, SystemAuditRepairTests.Mapper).DeleteAsync(fixture.TenantID, targetExhibition.ExhibitionID)).IsSuccess;
            }
            async Task<bool> CreateChild()
            {
                await using var db = new ApplicationDbContext(racingOptions, new Scope(fixture.TenantID));
                using var uow = new UnitOfWork(db, WorkflowFixture.Actor(fixture.TenantID));
                return hallCase
                    ? (await new ExhibitionService(uow, SystemAuditRepairTests.Mapper).AddScheduleAsync(fixture.TenantID,
                        new ExhibitionScheduleCreateDto { ExhibitionID = exhibition.ExhibitionID, HallID = hall.HallID,
                            EventName = "Concurrent event", StartDateTime = DateTime.UtcNow.Date.AddHours(10),
                            EndDateTime = DateTime.UtcNow.Date.AddHours(11) })).IsSuccess
                    : (await new SponsorshipService(uow, SystemAuditRepairTests.Mapper).CreateContractAsync(fixture.TenantID,
                        "workflow-user", new SponsorshipContractCreateDto { ExhibitionID = targetExhibition.ExhibitionID,
                            SponsorID = sponsor.SponsorID, ContractNumber = "RACE-CONTRACT", TotalAmount = 750, CurrencyCode = "LYD" })).IsSuccess;
            }

            var results = await Task.WhenAll(DeleteParent(), CreateChild());
            Assert.Equal(1, results.Count(success => success));
            seed.ChangeTracker.Clear();
            if (hallCase)
            {
                var deleted = (await seed.Halls.IgnoreQueryFilters().SingleAsync(h => h.HallID == hall.HallID)).IsDeleted;
                var hasChild = await seed.ExhibitionSchedules.AnyAsync(s => s.HallID == hall.HallID);
                Assert.False(deleted && hasChild);
            }
            else
            {
                var deleted = (await seed.Exhibitions.IgnoreQueryFilters().SingleAsync(e => e.ExhibitionID == targetExhibition.ExhibitionID)).IsDeleted;
                var hasChild = await seed.SponsorshipContracts.IgnoreQueryFilters().AnyAsync(c => c.ExhibitionID == targetExhibition.ExhibitionID);
                Assert.False(deleted && hasChild);
            }
        }
        finally
        {
            Assert.Equal(name, new SqlConnectionStringBuilder(seed.Database.GetConnectionString()).InitialCatalog);
            await seed.Database.EnsureDeletedAsync();
        }
    }
}
