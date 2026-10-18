using System.Security.Claims;
using AutoMapper;
using ExhibitionManagementSystem.DataAccess;
using ExhibitionManagementSystem.DataAccess.MultiTenancy;
using ExhibitionManagementSystem.DataAccess.Repositories.Implementations;
using ExhibitionManagementSystem.Models;
using ExhibitionManagementSystem.Models.DTOs.Mapping.Profiles;
using ExhibitionManagementSystem.Models.DTOs.Pricing;
using ExhibitionManagementSystem.Models.Enums;
using ExhibitionManagementSystem.Services.Implementations;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ExhibitionManagementSystem.Api.Tests.Pricing;

public sealed class PricingSafetyTests
{
    private static readonly IMapper Mapper = new MapperConfiguration(
        configuration => configuration.AddProfile<PricingMappingProfile>(),
        NullLoggerFactory.Instance).CreateMapper();

    [Fact]
    public async Task CalculateBoothPrice_RejectsMissingApplicableRule()
    {
        await using var context = CreateContext();
        using var unitOfWork = new UnitOfWork(context, CreateHttpContextAccessor());
        var service = new PricingService(unitOfWork, Mapper);

        var result = await service.CalculateBoothPriceDetailsAsync(
            1, 10, BoothType.Standard, ExhibitorCategory.Local, 20);

        Assert.False(result.IsSuccess);
        Assert.Equal("PRICING_RULE_NOT_FOUND", result.ErrorCode);
    }

    [Fact]
    public async Task CalculateBoothPrice_ReturnsRuleCurrencyWithAmount()
    {
        await using var context = CreateContext();
        context.BoothPriceRules.Add(new BoothPriceRule
        {
            RuleID = 7,
            TenantID = 1,
            ExhibitionID = 10,
            BoothType = BoothType.Standard,
            ExhibitorCategory = ExhibitorCategory.Local,
            PricePerSqM = 25,
            CurrencyCode = "EUR",
            MinAreaSqM = 10,
            MaxAreaSqM = 30,
            ValidFrom = DateTime.UtcNow.Date.AddDays(-1)
        });
        await context.SaveChangesAsync();
        using var unitOfWork = new UnitOfWork(context, CreateHttpContextAccessor());
        var service = new PricingService(unitOfWork, Mapper);

        var result = await service.CalculateBoothPriceDetailsAsync(
            1, 10, BoothType.Standard, ExhibitorCategory.Local, 20);

        Assert.True(result.IsSuccess, result.ErrorMessage);
        Assert.Equal(500, result.Data!.Amount);
        Assert.Equal("EUR", result.Data.CurrencyCode);
        Assert.Equal(7, result.Data.RuleID);
    }

    [Fact]
    public async Task ExchangeRate_UnknownPairDoesNotAssumeOneToOne()
    {
        await using var context = CreateContext();
        var repository = new ExchangeRateRepository(context);

        var result = await repository.ConvertAsync("AAA", "BBB", 100);

        Assert.Null(result);
    }

    [Fact]
    public async Task CreateBoothPriceRule_RejectsInvalidPriceAndEnumWithoutThrowing()
    {
        await using var context = CreateContext();
        using var unitOfWork = new UnitOfWork(context, CreateHttpContextAccessor());
        var service = new PricingService(unitOfWork, Mapper);

        var invalidPrice = await service.CreateBoothPriceRuleAsync(1, new BoothPriceRuleCreateDto
        {
            BoothType = "Rect",
            ExhibitorCategory = "Local",
            PricePerSqM = 0,
            CurrencyCode = "LYD",
            ValidFrom = DateTime.UtcNow.Date
        });

        Assert.False(invalidPrice.IsSuccess);
        Assert.Equal("INVALID_PRICE", invalidPrice.ErrorCode);

        var invalidType = await service.CreateBoothPriceRuleAsync(1, new BoothPriceRuleCreateDto
        {
            BoothType = "Rect",
            ExhibitorCategory = "Local",
            PricePerSqM = 100,
            CurrencyCode = "LYD",
            ValidFrom = DateTime.UtcNow.Date
        });

        Assert.False(invalidType.IsSuccess);
        Assert.Equal("INVALID_BOOTH_TYPE", invalidType.ErrorCode);
        Assert.Empty(context.BoothPriceRules);
    }

    [Fact]
    public async Task CalculateBoothPrice_UsesNewestRuleWhenSpecificityMatches()
    {
        await using var context = CreateContext();
        context.BoothPriceRules.AddRange(
            new BoothPriceRule
            {
                RuleID = 10, TenantID = 1, ExhibitionID = 5,
                BoothType = BoothType.Standard, ExhibitorCategory = ExhibitorCategory.Local,
                PricePerSqM = 10, CurrencyCode = "LYD",
                ValidFrom = DateTime.UtcNow.Date.AddDays(-10), CreatedAt = DateTime.UtcNow.AddDays(-10)
            },
            new BoothPriceRule
            {
                RuleID = 11, TenantID = 1, ExhibitionID = 5,
                BoothType = BoothType.Standard, ExhibitorCategory = ExhibitorCategory.Local,
                PricePerSqM = 20, CurrencyCode = "LYD",
                ValidFrom = DateTime.UtcNow.Date.AddDays(-1), CreatedAt = DateTime.UtcNow.AddDays(-1)
            });
        await context.SaveChangesAsync();
        using var unitOfWork = new UnitOfWork(context, CreateHttpContextAccessor());
        var service = new PricingService(unitOfWork, Mapper);

        var result = await service.CalculateBoothPriceDetailsAsync(
            1, 5, BoothType.Standard, ExhibitorCategory.Local, 10);

        Assert.True(result.IsSuccess, result.ErrorMessage);
        Assert.Equal(200, result.Data!.Amount);
        Assert.Equal(11, result.Data.RuleID);
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
