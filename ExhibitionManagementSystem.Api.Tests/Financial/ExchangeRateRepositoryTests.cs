using ExhibitionManagementSystem.DataAccess;
using ExhibitionManagementSystem.DataAccess.MultiTenancy;
using ExhibitionManagementSystem.DataAccess.Repositories.Implementations;
using ExhibitionManagementSystem.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ExhibitionManagementSystem.Api.Tests.Financial;

public class ExchangeRateRepositoryTests
{
    [Fact]
    public async Task ConvertWithDetails_UsesMostRecentApplicableDirectRate()
    {
        await using var context = CreateContext();
        var today = DateTime.UtcNow.Date;
        context.ExchangeRates.AddRange(
            Rate(1, "USD", "LYD", 4.70m, today.AddDays(-5)),
            Rate(2, "USD", "LYD", 4.85m, today.AddDays(-1)),
            Rate(3, "USD", "LYD", 99m, today.AddDays(1)));
        await context.SaveChangesAsync();

        var repository = new ExchangeRateRepository(context);
        var result = await repository.ConvertWithDetailsAsync("usd", "lyd", 100m, today);

        Assert.NotNull(result);
        Assert.Equal(485m, result!.TargetAmount);
        Assert.Equal(4.85m, result.ExchangeRate);
        Assert.Equal(today.AddDays(-1), result.RateDate);
    }

    [Fact]
    public async Task ConvertWithDetails_UsesInverseRateWhenDirectRateIsMissing()
    {
        await using var context = CreateContext();
        var today = DateTime.UtcNow.Date;
        context.ExchangeRates.Add(Rate(1, "EUR", "USD", 1.25m, today));
        await context.SaveChangesAsync();

        var repository = new ExchangeRateRepository(context);
        var result = await repository.ConvertWithDetailsAsync("USD", "EUR", 100m, today);

        Assert.NotNull(result);
        Assert.Equal(80m, result!.TargetAmount);
        Assert.Equal(0.8m, result.ExchangeRate);
        Assert.StartsWith("Inverse:", result.Source);
    }

    [Fact]
    public async Task ConvertWithDetails_UsesConfiguredBridgeCurrency()
    {
        await using var context = CreateContext();
        var today = DateTime.UtcNow.Date;
        context.ExchangeRates.AddRange(
            Rate(1, "EUR", "LYD", 5.20m, today.AddDays(-1)),
            Rate(2, "LYD", "USD", 0.20m, today));
        await context.SaveChangesAsync();

        var repository = new ExchangeRateRepository(context);
        var result = await repository.ConvertWithDetailsAsync("EUR", "USD", 100m, today, "LYD");

        Assert.NotNull(result);
        Assert.Equal(104m, result!.TargetAmount);
        Assert.Equal(1.04m, result.ExchangeRate);
        Assert.Contains("Bridge LYD", result.Source);
        Assert.Equal(today.AddDays(-1), result.RateDate);
    }

    [Fact]
    public async Task ConvertWithDetails_ReturnsNullWhenOnlyFutureRateExists()
    {
        await using var context = CreateContext();
        var today = DateTime.UtcNow.Date;
        context.ExchangeRates.Add(Rate(1, "USD", "LYD", 4.85m, today.AddDays(1)));
        await context.SaveChangesAsync();

        var repository = new ExchangeRateRepository(context);
        var result = await repository.ConvertWithDetailsAsync("USD", "LYD", 100m, today);

        Assert.Null(result);
    }

    private static ExchangeRate Rate(int id, string from, string to, decimal rate, DateTime date) => new()
    {
        RateID = id,
        FromCurrency = from,
        ToCurrency = to,
        Rate = rate,
        RateDate = date,
        Source = "Test",
        CreatedByUserId = "test-user"
    };

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new ApplicationDbContext(options, new BypassTenantContext());
    }

    private sealed class BypassTenantContext : ITenantContext
    {
        public int? TenantId => null;
        public bool CanBypassTenantIsolation => true;
    }
}
