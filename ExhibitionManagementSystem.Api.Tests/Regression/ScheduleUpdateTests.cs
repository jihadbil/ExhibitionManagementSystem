using AutoMapper;
using ExhibitionManagementSystem.DataAccess.Repositories.Implementations;
using ExhibitionManagementSystem.Models;
using ExhibitionManagementSystem.Models.DTOs.Exhibition;
using ExhibitionManagementSystem.Models.DTOs.Mapping.Profiles;
using ExhibitionManagementSystem.Models.Enums;
using ExhibitionManagementSystem.Services.Implementations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ExhibitionManagementSystem.Api.Tests.Regression;

public sealed class ScheduleUpdateTests
{
    private static readonly IMapper Mapper = new MapperConfiguration(
        configuration => configuration.AddProfile<ExhibitionMappingProfile>(),
        NullLoggerFactory.Instance).CreateMapper();

    [Fact]
    public async Task UpdateSchedule_ChangesFieldsWithinTheOwningExhibition()
    {
        await using var db = WorkflowFixture.CreateDb();
        var fixture = await SeedScheduleAsync(db);
        using var unitOfWork = new UnitOfWork(db, WorkflowFixture.Actor(fixture.TenantId));
        var service = new ExhibitionService(unitOfWork, Mapper);

        var result = await service.UpdateScheduleAsync(
            fixture.TenantId,
            fixture.ExhibitionId,
            fixture.ScheduleId,
            ValidUpdate(fixture.HallId));

        Assert.True(result.IsSuccess, result.ErrorMessage);
        Assert.Equal("الفعالية المعدلة", result.Data!.EventName);
        Assert.Equal("Workshop", result.Data.EventType);
        Assert.Equal("المتحدث الجديد", result.Data.SpeakerName);
        Assert.Equal(250, result.Data.MaxAttendees);
        Assert.Equal(fixture.HallId, result.Data.HallID);
    }

    [Fact]
    public async Task UpdateSchedule_RejectsCapacityBelowCurrentRegistrations()
    {
        await using var db = WorkflowFixture.CreateDb();
        var fixture = await SeedScheduleAsync(db);
        db.ScheduleRegistrations.Add(new ScheduleRegistration
        {
            ScheduleID = fixture.ScheduleId,
            Visitor = new Visitor
            {
                TenantID = fixture.TenantId,
                FullName = "زائر مسجل"
            },
            Status = RegistrationStatus.Registered
        });
        await db.SaveChangesAsync();
        using var unitOfWork = new UnitOfWork(db, WorkflowFixture.Actor(fixture.TenantId));
        var service = new ExhibitionService(unitOfWork, Mapper);
        var update = ValidUpdate(fixture.HallId);
        update.MaxAttendees = 0;

        var result = await service.UpdateScheduleAsync(
            fixture.TenantId,
            fixture.ExhibitionId,
            fixture.ScheduleId,
            update);

        Assert.False(result.IsSuccess);
        Assert.Equal("SCHEDULE_CAPACITY_CONFLICT", result.ErrorCode);
        Assert.Equal("الفعالية الأصلية", (await db.ExhibitionSchedules.FindAsync(fixture.ScheduleId))!.EventName);
    }

    [Fact]
    public async Task UpdateSchedule_RejectsWrongExhibitionAndTenant()
    {
        await using var db = WorkflowFixture.CreateDb();
        var fixture = await SeedScheduleAsync(db);
        using var unitOfWork = new UnitOfWork(db, WorkflowFixture.Actor(fixture.TenantId));
        var service = new ExhibitionService(unitOfWork, Mapper);

        var wrongExhibition = await service.UpdateScheduleAsync(
            fixture.TenantId,
            fixture.ExhibitionId + 100,
            fixture.ScheduleId,
            ValidUpdate(fixture.HallId));
        var wrongTenant = await service.UpdateScheduleAsync(
            fixture.TenantId + 100,
            fixture.ExhibitionId,
            fixture.ScheduleId,
            ValidUpdate(fixture.HallId));

        Assert.Equal("SCHEDULE_NOT_FOUND", wrongExhibition.ErrorCode);
        Assert.Equal("SCHEDULE_NOT_FOUND", wrongTenant.ErrorCode);
    }

    private static ExhibitionScheduleUpdateDto ValidUpdate(int hallId) => new()
    {
        HallID = hallId,
        EventName = "الفعالية المعدلة",
        EventType = "Workshop",
        StartDateTime = DateTime.UtcNow.Date.AddDays(2).AddHours(10),
        EndDateTime = DateTime.UtcNow.Date.AddDays(2).AddHours(12),
        SpeakerName = "المتحدث الجديد",
        MaxAttendees = 250,
        Description = "وصف محدث",
        IsPublic = false
    };

    private static async Task<(int TenantId, int ExhibitionId, int ScheduleId, int HallId)> SeedScheduleAsync(
        ExhibitionManagementSystem.DataAccess.ApplicationDbContext db)
    {
        var tenant = new Tenant
        {
            CompanyName = "منظم المعرض",
            Subdomain = Guid.NewGuid().ToString("N"),
            BaseCurrency = "LYD"
        };
        var venue = new Venue { Tenant = tenant, Name = "موقع المعرض" };
        var hall = new Hall { Venue = venue, HallName = "القاعة الأولى" };
        var exhibition = new Exhibition
        {
            Tenant = tenant,
            Venue = venue,
            Name = "المعرض التجريبي",
            StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date.AddDays(10),
            Status = ExhibitionStatus.Open,
            EntryCurrency = "LYD"
        };
        var schedule = new ExhibitionSchedule
        {
            Exhibition = exhibition,
            Hall = hall,
            EventName = "الفعالية الأصلية",
            EventType = EventType.Lecture,
            StartDateTime = DateTime.UtcNow.Date.AddDays(1).AddHours(10),
            EndDateTime = DateTime.UtcNow.Date.AddDays(1).AddHours(11),
            MaxAttendees = 100
        };
        db.Currencies.Add(new Currency
        {
            CurrencyCode = "LYD",
            CurrencyName = "دينار ليبي",
            Symbol = "د.ل"
        });
        db.ExhibitionSchedules.Add(schedule);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        return (tenant.TenantID, exhibition.ExhibitionID, schedule.ScheduleID, hall.HallID);
    }
}
