using System.Security.Claims;
using AutoMapper;
using ExhibitionManagementSystem.DataAccess;
using ExhibitionManagementSystem.DataAccess.MultiTenancy;
using ExhibitionManagementSystem.DataAccess.Repositories.Implementations;
using ExhibitionManagementSystem.Models;
using ExhibitionManagementSystem.Models.DTOs.Mapping.Profiles;
using ExhibitionManagementSystem.Models.Enums;
using ExhibitionManagementSystem.Services.Implementations;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ExhibitionManagementSystem.Api.Tests.Badges;

public sealed class BadgeServiceTests
{
    private static readonly IMapper Mapper = new MapperConfiguration(
        configuration => configuration.AddProfile<BadgeMappingProfile>(),
        NullLoggerFactory.Instance).CreateMapper();

    [Fact]
    public async Task RenderByQr_UsesTicketTypeTemplateAndAllDisplaySettings()
    {
        await using var context = CreateContext();
        var venue = new Venue { VenueID = 10, TenantID = 1, Name = "قاعة طرابلس" };
        var exhibition = new Exhibition
        {
            ExhibitionID = 20,
            TenantID = 1,
            VenueID = venue.VenueID,
            Venue = venue,
            Name = "معرض التقنية",
            StartDate = new DateTime(2026, 10, 10),
            EndDate = new DateTime(2026, 10, 12)
        };
        var visitor = new Visitor
        {
            VisitorID = 30,
            TenantID = 1,
            FullName = "زائر تجريبي",
            VisitorType = "Regular"
        };
        var ticket = new Ticket
        {
            TicketID = 40,
            TenantID = 1,
            VisitorID = visitor.VisitorID,
            Visitor = visitor,
            ExhibitionID = exhibition.ExhibitionID,
            Exhibition = exhibition,
            TicketType = "VIP",
            QRCode = "VIP-TEST-40"
        };
        var template = new BadgeTemplate
        {
            TemplateID = 50,
            TenantID = 1,
            ExhibitionID = exhibition.ExhibitionID,
            TemplateName = "VIP template",
            TargetParticipantType = ParticipantType.VIP,
            WidthMm = 120,
            HeightMm = 86,
            Orientation = BadgeOrientation.Horizontal,
            HeaderBgColor = "#111111",
            HeaderTextColor = "#eeeeee",
            AccentColor = "#c0a000",
            CategoryBadgeText = "ضيف VIP",
            ShowLogo = false,
            ShowQRCode = true,
            ShowBarcode = false,
            ShowCompanyName = false,
            ShowJobTitle = false,
            ShowHallName = false,
            ShowDates = false,
            CustomFooterText = "تذييل مخصص",
            IsDefault = true
        };

        context.AddRange(venue, exhibition, visitor, ticket, template);
        await context.SaveChangesAsync();

        using var unitOfWork = new UnitOfWork(context, CreateHttpContextAccessor());
        var service = new BadgeService(unitOfWork, Mapper);

        var result = await service.GenerateBadgePayloadByQRCodeAsync(1, ticket.QRCode);

        Assert.True(result.IsSuccess, result.ErrorMessage);
        var payload = Assert.IsType<ExhibitionManagementSystem.Models.DTOs.Badge.BadgePrintPayloadDto>(result.Data);
        Assert.Equal(ParticipantType.VIP, payload.ParticipantType);
        Assert.Equal("ضيف VIP", payload.CategoryBadgeText);
        Assert.Equal("#111111", payload.HeaderBgColor);
        Assert.Equal("#eeeeee", payload.HeaderTextColor);
        Assert.Equal("#c0a000", payload.AccentColor);
        Assert.Equal(BadgeOrientation.Horizontal, payload.Orientation);
        Assert.Equal(120, payload.WidthMm);
        Assert.Equal(86, payload.HeightMm);
        Assert.False(payload.ShowLogo);
        Assert.False(payload.ShowCompanyName);
        Assert.False(payload.ShowJobTitle);
        Assert.False(payload.ShowHallName);
        Assert.False(payload.ShowDates);
        Assert.True(payload.ShowQRCode);
        Assert.True(payload.ShowBarcode);
        Assert.Equal("TKT-00000040", payload.Barcode);
        Assert.Equal("تذييل مخصص", payload.FooterText);
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
