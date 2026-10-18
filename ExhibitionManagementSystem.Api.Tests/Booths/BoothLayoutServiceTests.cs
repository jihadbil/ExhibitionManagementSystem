using System.Security.Claims;
using System.Text.Json;
using AutoMapper;
using ExhibitionManagementSystem.DataAccess;
using ExhibitionManagementSystem.DataAccess.MultiTenancy;
using ExhibitionManagementSystem.DataAccess.Repositories.Implementations;
using ExhibitionManagementSystem.Models;
using ExhibitionManagementSystem.Models.DTOs.Booth;
using ExhibitionManagementSystem.Models.DTOs.Mapping.Profiles;
using ExhibitionManagementSystem.Services.Implementations;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ExhibitionManagementSystem.Api.Tests.Booths;

public sealed class BoothLayoutServiceTests
{
    private static readonly IMapper Mapper = new MapperConfiguration(
        configuration => configuration.AddProfile<BoothMappingProfile>(),
        NullLoggerFactory.Instance).CreateMapper();

    [Fact]
    public async Task SaveLayout_PersistsMeterGeometryAndSynchronizesArea()
    {
        await using var context = await CreateContextAsync();
        using var unitOfWork = new UnitOfWork(context, CreateHttpContextAccessor());
        var service = new BoothService(unitOfWork, Mapper);

        var result = await service.SaveLayoutAsync(1, "admin-1", 10, new BoothLayoutSaveDto
        {
            Booths =
            [
                new BoothLayoutItemDto
                {
                    BoothNumber = "A-1",
                    PosX = 2.5m,
                    PosY = 3.25m,
                    Width = 4.5m,
                    Height = 3.5m,
                    RotationAngle = 0,
                    ShapeType = "Rect",
                    BoothType = "Premium"
                }
            ],
            FloorPlanJSON = "{\"version\":2,\"elements\":[]}"
        });

        Assert.True(result.IsSuccess, result.ErrorMessage);
        var saved = await context.Booths.SingleAsync();
        Assert.Equal(2.5m, saved.PosX);
        Assert.Equal(3.25m, saved.PosY);
        Assert.Equal(4.5m, saved.Width);
        Assert.Equal(3.5m, saved.Height);
        Assert.Equal(15.75m, saved.OriginalAreaSqM);
        Assert.Equal(15.75m, saved.CurrentAreaSqM);
        Assert.Equal(ExhibitionManagementSystem.Models.Enums.BoothType.Premium, saved.BoothType);
        using var plan = JsonDocument.Parse((await context.Halls.FindAsync(10))!.FloorPlanJSON!);
        Assert.Equal(2, plan.RootElement.GetProperty("version").GetInt32());
        Assert.Equal(0, plan.RootElement.GetProperty("elements").GetArrayLength());
        Assert.False(string.IsNullOrWhiteSpace(plan.RootElement.GetProperty("layoutRevision").GetString()));
        Assert.Equal((await context.Halls.FindAsync(10))!.FloorPlanJSON, result.Data!.FloorPlanJSON);
    }

    [Fact]
    public async Task SaveLayout_RejectsOverlappingRotatedBooths()
    {
        await using var context = await CreateContextAsync();
        using var unitOfWork = new UnitOfWork(context, CreateHttpContextAccessor());
        var service = new BoothService(unitOfWork, Mapper);

        var result = await service.SaveLayoutAsync(1, "admin-1", 10, new BoothLayoutSaveDto
        {
            Booths =
            [
                Item("A-1", 5, 5, 6, 4, 35),
                Item("A-2", 8, 6, 5, 4, 330)
            ]
        });

        Assert.False(result.IsSuccess);
        Assert.Equal("BOOTH_COLLISION", result.ErrorCode);
        Assert.Empty(context.Booths);
    }

    [Fact]
    public async Task SaveLayout_RejectsRotatedBoothOutsideHallBoundary()
    {
        await using var context = await CreateContextAsync();
        using var unitOfWork = new UnitOfWork(context, CreateHttpContextAccessor());
        var service = new BoothService(unitOfWork, Mapper);

        var result = await service.SaveLayoutAsync(1, "admin-1", 10, new BoothLayoutSaveDto
        {
            Booths = [Item("A-1", 0, 0, 6, 4, 45)]
        });

        Assert.False(result.IsSuccess);
        Assert.Equal("BOOTH_OUT_OF_BOUNDS", result.ErrorCode);
        Assert.Empty(context.Booths);
    }

    [Fact]
    public async Task SaveLayout_RejectsBoothOverlappingAisle()
    {
        await using var context = await CreateContextAsync();
        using var unitOfWork = new UnitOfWork(context, CreateHttpContextAccessor());
        var service = new BoothService(unitOfWork, Mapper);

        var result = await service.SaveLayoutAsync(1, "admin-1", 10, new BoothLayoutSaveDto
        {
            Booths = [Item("A-1", 4, 5, 4, 3, 0)],
            FloorPlanJSON = """
                {"version":2,"elements":[{"id":"aisle-1","type":"aisle","x":0,"y":6,"width":20,"height":2,"rotation":0,"label":"الممر الرئيسي"}]}
                """
        });

        Assert.False(result.IsSuccess);
        Assert.Equal("BOOTH_BLOCKS_ACCESS_AREA", result.ErrorCode);
        Assert.Empty(context.Booths);
    }

    [Fact]
    public async Task SaveLayout_PersistsAssignedPriceRule()
    {
        await using var context = await CreateContextAsync();
        context.BoothPriceRules.Add(new BoothPriceRule
        {
            RuleID = 22,
            TenantID = 1,
            RuleName = "قاعدة القاعة",
            PricePerSqM = 175,
            CurrencyCode = "LYD",
            ValidFrom = DateTime.UtcNow.Date.AddDays(-1)
        });
        await context.SaveChangesAsync();
        using var unitOfWork = new UnitOfWork(context, CreateHttpContextAccessor());
        var service = new BoothService(unitOfWork, Mapper);

        var item = Item("A-1", 2, 2, 4, 3, 0);
        item.AssignedPriceRuleID = 22;
        var result = await service.SaveLayoutAsync(1, "admin-1", 10, new BoothLayoutSaveDto
        {
            Booths = [item]
        });

        Assert.True(result.IsSuccess, result.ErrorMessage);
        Assert.Equal(22, (await context.Booths.SingleAsync()).AssignedPriceRuleID);
    }

    [Fact]
    public async Task SaveLayout_RejectsPriceRuleOwnedByAnotherTenant()
    {
        await using var context = await CreateContextAsync();
        context.BoothPriceRules.Add(new BoothPriceRule
        {
            RuleID = 23,
            TenantID = 2,
            RuleName = "قاعدة مستأجر آخر",
            PricePerSqM = 175,
            CurrencyCode = "LYD",
            ValidFrom = DateTime.UtcNow.Date.AddDays(-1)
        });
        await context.SaveChangesAsync();
        using var unitOfWork = new UnitOfWork(context, CreateHttpContextAccessor());
        var service = new BoothService(unitOfWork, Mapper);

        var item = Item("A-1", 2, 2, 4, 3, 0);
        item.AssignedPriceRuleID = 23;
        var result = await service.SaveLayoutAsync(1, "admin-1", 10, new BoothLayoutSaveDto
        {
            Booths = [item]
        });

        Assert.False(result.IsSuccess);
        Assert.Equal("PRICING_RULE_NOT_FOUND", result.ErrorCode);
        Assert.Empty(context.Booths);
    }


    [Fact]
    public async Task SaveLayout_UsesConcaveSiteBoundaryWithoutHallRectangleDimensions()
    {
        await using var context = await CreateContextAsync();
        var hall = (await context.Halls.FindAsync(10))!;
        hall.FloorPlanWidth = null; hall.FloorPlanHeight = null;
        (await context.Venues.FindAsync(5))!.SitePlanJSON = ConcaveSite();
        await context.SaveChangesAsync();
        using var unitOfWork = new UnitOfWork(context, CreateHttpContextAccessor());
        var service = new BoothService(unitOfWork, Mapper);
        var bad = await service.SaveLayoutAsync(1, "admin-1", 10, new BoothLayoutSaveDto { Booths = [Item("outside", 12, 12, 3, 3, 0)] });
        Assert.Equal("BOOTH_OUT_OF_BOUNDS", bad.ErrorCode);
        Assert.Empty(context.Booths);
        var good = await service.SaveLayoutAsync(1, "admin-1", 10, new BoothLayoutSaveDto { Booths = [Item("inside", 1, 12, 3, 3, 0)] });
        Assert.True(good.IsSuccess, good.ErrorMessage);
    }

    [Fact]
    public async Task SaveLayout_ConcaveBoothsUseActualAreaAndAllowAnotherBoothInTheirNotch()
    {
        await using var context = await CreateContextAsync();
        using var unitOfWork = new UnitOfWork(context, CreateHttpContextAccessor());
        var service = new BoothService(unitOfWork, Mapper);
        var polygon = Item("L", 1, 1, 6, 6, 0);
        polygon.ShapeType = "Polygon";
        polygon.ShapePolygonJSON = "[{\"x\":0,\"y\":0},{\"x\":6,\"y\":0},{\"x\":6,\"y\":2},{\"x\":2,\"y\":2},{\"x\":2,\"y\":6},{\"x\":0,\"y\":6}]";
        var result = await service.SaveLayoutAsync(1, "admin-1", 10, new BoothLayoutSaveDto { Booths = [polygon, Item("notch", 4, 4, 2, 2, 0)] });
        Assert.True(result.IsSuccess, result.ErrorMessage);
        var saved = await context.Booths.SingleAsync(b => b.BoothNumber == "L");
        Assert.Equal(20m, saved.CurrentAreaSqM);
        Assert.Equal(20m, saved.OriginalAreaSqM);
    }

    [Theory]
    [InlineData("[]")]
    [InlineData("[{\"x\":0,\"y\":0},{\"x\":3,\"y\":3},{\"x\":0,\"y\":3},{\"x\":3,\"y\":0}]")]
    [InlineData("{broken")]
    public async Task SaveLayout_RejectsMalformedPolygonWithoutWriting(string json)
    {
        await using var context = await CreateContextAsync();
        using var unitOfWork = new UnitOfWork(context, CreateHttpContextAccessor());
        var item = Item("bad", 1, 1, 3, 3, 0); item.ShapeType = "Polygon"; item.ShapePolygonJSON = json;
        var result = await new BoothService(unitOfWork, Mapper).SaveLayoutAsync(1, "admin-1", 10, new BoothLayoutSaveDto { Booths = [item] });
        Assert.Equal("INVALID_BOOTH_POLYGON", result.ErrorCode);
        Assert.Empty(context.Booths);
    }

    [Fact]
    public async Task SaveLayout_RejectsAuthoritativeSiteWallEvenIfNoObstaclesWerePosted()
    {
        await using var context = await CreateContextAsync();
        (await context.Venues.FindAsync(5))!.SitePlanJSON = ConcaveSite(true);
        await context.SaveChangesAsync();
        using var unitOfWork = new UnitOfWork(context, CreateHttpContextAccessor());
        var result = await new BoothService(unitOfWork, Mapper).SaveLayoutAsync(1, "admin-1", 10, new BoothLayoutSaveDto { Booths = [Item("wall", 4, 1, 3, 3, 0)], FloorPlanJSON = "{\"version\":2,\"elements\":[]}" });
        Assert.Equal("BOOTH_BLOCKS_ACCESS_AREA", result.ErrorCode);
        Assert.Empty(context.Booths);
    }

    [Fact]
    public async Task SaveLayout_RejectsStaleSiteAndFloorVersionsAndChangesRevisionOnGeometryOnlySave()
    {
        await using var context = await CreateContextAsync();
        (await context.Venues.FindAsync(5))!.SitePlanVersion = 7;
        await context.SaveChangesAsync();
        using var unitOfWork = new UnitOfWork(context, CreateHttpContextAccessor());
        var service = new BoothService(unitOfWork, Mapper);
        var staleSite = await service.SaveLayoutAsync(1,"admin-1",10,new BoothLayoutSaveDto { ExpectedSitePlanVersion = 6, Booths = [Item("A",1,1,3,3,0)] });
        Assert.Equal("FLOOR_PLAN_CONFLICT", staleSite.ErrorCode);
        var first = await service.SaveLayoutAsync(1,"admin-1",10,new BoothLayoutSaveDto { ExpectedSitePlanVersion = 7, ExpectedFloorPlanJSON = "", FloorPlanJSON = "{\"version\":2,\"elements\":[]}", Booths = [Item("A",1,1,3,3,0)] });
        Assert.True(first.IsSuccess,first.ErrorMessage);
        var item = Item("A",5,1,3,3,0); item.BoothID = first.Data!.Booths[0].BoothID;
        var staleFloor = await service.SaveLayoutAsync(1,"admin-1",10,new BoothLayoutSaveDto { ExpectedFloorPlanJSON = "", Booths = [item] });
        Assert.Equal("FLOOR_PLAN_CONFLICT",staleFloor.ErrorCode);
        var second = await service.SaveLayoutAsync(1,"admin-1",10,new BoothLayoutSaveDto { ExpectedFloorPlanJSON = first.Data.FloorPlanJSON, FloorPlanJSON = first.Data.FloorPlanJSON, Booths = [item] });
        Assert.True(second.IsSuccess,second.ErrorMessage);
        Assert.NotEqual(first.Data.FloorPlanJSON,second.Data!.FloorPlanJSON);
        Assert.Equal(5m,(await context.Booths.SingleAsync()).PosX);
    }

    private static string ConcaveSite(bool wall = false) => JsonSerializer.Serialize(new {
        schemaVersion = 4,
        shapes = new object[] { new { id="hall",kind="hall",hallID=10,x=0,y=0,width=20,height=20,rotation=0,points=new[] {new{x=0,y=0},new{x=20,y=0},new{x=20,y=8},new{x=8,y=8},new{x=8,y=20},new{x=0,y=20}} } }
            .Concat(wall ? new object[] {new {id="wall",kind="wall",x=5,y=0,width=.01,height=8,rotation=0,strokeWidth=.3,points=new[] {new{x=0,y=0},new{x=0,y=8}}}} : Array.Empty<object>())
    });

    private static BoothLayoutItemDto Item(
        string number,
        decimal x,
        decimal y,
        decimal width,
        decimal height,
        decimal rotation) => new()
        {
            BoothNumber = number,
            PosX = x,
            PosY = y,
            Width = width,
            Height = height,
            RotationAngle = rotation,
            ShapeType = rotation == 0 ? "Rect" : "Rotated"
        };

    private static async Task<ApplicationDbContext> CreateContextAsync()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        var context = new ApplicationDbContext(options, new BypassTenantContext());
        context.Venues.Add(new Venue { VenueID = 5, TenantID = 1, Name = "Test venue" });
        context.Halls.Add(new Hall
        {
            HallID = 10,
            VenueID = 5,
            HallName = "Main hall",
            FloorPlanWidth = 40,
            FloorPlanHeight = 30,
            IsActive = true
        });
        await context.SaveChangesAsync();
        return context;
    }

    private static HttpContextAccessor CreateHttpContextAccessor()
    {
        var identity = new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, "admin-1"),
            new Claim("TenantId", "1")
        ], "Test");
        return new HttpContextAccessor { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) } };
    }

    private sealed class BypassTenantContext : ITenantContext
    {
        public int? TenantId => null;
        public bool CanBypassTenantIsolation => true;
    }
}
