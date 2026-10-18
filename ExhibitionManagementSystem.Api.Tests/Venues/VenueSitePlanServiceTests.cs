using System.Security.Claims;
using System.Text.Json;
using AutoMapper;
using ExhibitionManagementSystem.DataAccess;
using ExhibitionManagementSystem.DataAccess.MultiTenancy;
using ExhibitionManagementSystem.DataAccess.Repositories.Implementations;
using ExhibitionManagementSystem.Models;
using ExhibitionManagementSystem.Models.DTOs.Mapping.Profiles;
using ExhibitionManagementSystem.Models.DTOs.Venue;
using ExhibitionManagementSystem.Services.Implementations;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ExhibitionManagementSystem.Api.Tests.Venues;

public sealed class VenueSitePlanServiceTests
{
    private const string ValidPlan = """
        {"bounds":{"width":100,"height":80},"halls":[{"hallID":10,"x":2,"y":2,"width":40,"height":30}],"hallDoors":[],"elements":[]}
        """;

    private static readonly IMapper Mapper = new MapperConfiguration(
        configuration => configuration.AddProfile<VenueMappingProfile>(),
        NullLoggerFactory.Instance).CreateMapper();

    [Fact]
    public async Task SaveSitePlan_IncrementsVersionAndRejectsStaleVersion()
    {
        await using var context = CreateContext();
        context.Venues.Add(new Venue { VenueID = 5, TenantID = 1, Name = "Test venue" });
        context.Halls.Add(new Hall { HallID = 10, VenueID = 5, HallName = "Main hall" });
        await context.SaveChangesAsync();
        using var unitOfWork = new UnitOfWork(context, CreateHttpContextAccessor());
        var service = new VenueService(unitOfWork, Mapper);

        var first = await service.SaveSitePlanAsync(1, 5, new VenueSitePlanSaveDto
        {
            PlanJSON = ValidPlan,
            ExpectedVersion = 0
        });
        var stale = await service.SaveSitePlanAsync(1, 5, new VenueSitePlanSaveDto
        {
            PlanJSON = ValidPlan,
            ExpectedVersion = 0
        });

        Assert.True(first.IsSuccess, first.ErrorMessage);
        Assert.Equal(1, first.Data!.Version);
        Assert.False(stale.IsSuccess);
        Assert.Equal("SITE_PLAN_CONFLICT", stale.ErrorCode);
    }

    [Fact]
    public async Task SaveDrawing_PreservesConcaveHallOutlineAndPartitions()
    {
        var result = await SaveDrawingAsync(BuildDrawingPlan());
        Assert.True(result.IsSuccess, result.ErrorMessage);
        using var document = JsonDocument.Parse(result.Data!.PlanJSON);
        Assert.Equal(4, document.RootElement.GetProperty("schemaVersion").GetInt32());
        Assert.Equal(6, document.RootElement.GetProperty("shapes")[0].GetProperty("points").GetArrayLength());
        Assert.Equal("wall", document.RootElement.GetProperty("shapes")[1].GetProperty("kind").GetString());
    }

    [Theory]
    [InlineData("outside")]
    [InlineData("foreign-hall")]
    [InlineData("crossed-polygon")]
    [InlineData("duplicate-id")]
    [InlineData("unsafe-color")]
    [InlineData("negative-grid")]
    [InlineData("missing-hall-export")]
    [InlineData("rotated-wall-outside")]
    [InlineData("invalid-layer")]
    public async Task SaveDrawing_RejectsInvalidGeometryAndOwnership(string mutation)
    {
        var result = await SaveDrawingAsync(BuildDrawingPlan(mutation));
        Assert.False(result.IsSuccess);
        Assert.Equal("SITE_PLAN_DRAWING_INVALID", result.ErrorCode);
    }

    private static async Task<ExhibitionManagementSystem.Services.Common.ServiceResult<VenueSitePlanDto>> SaveDrawingAsync(string json)
    {
        await using var context = CreateContext();
        context.Venues.Add(new Venue { VenueID = 5, TenantID = 1, Name = "Test venue" });
        context.Halls.Add(new Hall { HallID = 10, VenueID = 5, HallName = "L-shaped hall" });
        await context.SaveChangesAsync();
        using var unitOfWork = new UnitOfWork(context, CreateHttpContextAccessor());
        return await new VenueService(unitOfWork, Mapper).SaveSitePlanAsync(1, 5, new VenueSitePlanSaveDto { PlanJSON = json, ExpectedVersion = 0 });
    }

    private static string BuildDrawingPlan(string? mutation = null)
    {
        var hall = new Dictionary<string, object>
        {
            ["id"] = "hall-10", ["kind"] = "hall", ["layer"] = "halls", ["hallID"] = 10,
            ["x"] = 5, ["y"] = 5, ["width"] = 40, ["height"] = 30, ["rotation"] = 0,
            ["points"] = new[] { new { x = 0, y = 0 }, new { x = 40, y = 0 }, new { x = 40, y = 12 }, new { x = 15, y = 12 }, new { x = 15, y = 30 }, new { x = 0, y = 30 } },
            ["label"] = "L-shaped hall", ["fill"] = "#eff6ff", ["stroke"] = "#2563eb", ["strokeWidth"] = 0.12, ["fontSize"] = 1.6, ["locked"] = false
        };
        var wall = new Dictionary<string, object>(hall)
        {
            ["id"] = "wall-1", ["kind"] = "wall", ["layer"] = "structure", ["x"] = 8, ["y"] = 8,
            ["width"] = 8, ["height"] = 0.01, ["points"] = new[] { new { x = 0, y = 0 }, new { x = 8, y = 0 } }, ["label"] = "Partition"
        };
        wall.Remove("hallID");
        var plan = new Dictionary<string, object>
        {
            ["schemaVersion"] = 4, ["unit"] = "meter", ["bounds"] = new { width = 100, height = 75 },
            ["grid"] = new { size = 1, snap = true, visible = true },
            ["layers"] = new[] { "halls", "structure", "movement", "annotations" }.ToDictionary(l => l, _ => new { visible = true, locked = false }),
            ["shapes"] = new[] { hall, wall }, ["halls"] = new[] { new { hallID = 10, x = 5, y = 5, width = 40, height = 30 } }, ["hallDoors"] = Array.Empty<object>(), ["elements"] = Array.Empty<object>()
        };
        switch (mutation)
        {
            case "outside": hall["x"] = 95; break;
            case "foreign-hall": hall["hallID"] = 99; break;
            case "crossed-polygon": hall["points"] = new[] { new { x = 0, y = 0 }, new { x = 40, y = 30 }, new { x = 0, y = 30 }, new { x = 40, y = 0 } }; break;
            case "duplicate-id": wall["id"] = hall["id"]; break;
            case "unsafe-color": hall["fill"] = "url(https://example.org)"; break;
            case "negative-grid": plan["grid"] = new { size = -1, snap = true, visible = true }; break;
            case "missing-hall-export": plan["halls"] = Array.Empty<object>(); break;
            case "invalid-layer": hall["layer"] = "services"; break;
            case "rotated-wall-outside": wall["x"] = 0; wall["y"] = 0; wall["rotation"] = 90; break;
        }
        return JsonSerializer.Serialize(plan);
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new ApplicationDbContext(options, new BypassTenantContext());
    }

    private static HttpContextAccessor CreateHttpContextAccessor()
    {
        var identity = new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, "admin-1"),
            new Claim("TenantId", "1")
        ], "Test");
        return new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };
    }

    private sealed class BypassTenantContext : ITenantContext
    {
        public int? TenantId => null;
        public bool CanBypassTenantIsolation => true;
    }
}
