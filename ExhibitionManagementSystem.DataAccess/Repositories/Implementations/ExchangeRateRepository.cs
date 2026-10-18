using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ExhibitionManagementSystem.Models;
using ExhibitionManagementSystem.DataAccess.Repositories.Interfaces;

namespace ExhibitionManagementSystem.DataAccess.Repositories.Implementations
{
    public class ExchangeRateRepository : GenericRepository<ExchangeRate>, IExchangeRateRepository
    {
        public ExchangeRateRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<ExchangeRate?> GetRateAsync(string from, string to, DateTime date)
        {
            var targetDate = date.Date;
            return await _dbSet.AsNoTracking()
                .Where(r => r.FromCurrency == from && r.ToCurrency == to && r.RateDate <= targetDate)
                .OrderByDescending(r => r.RateDate)
                .FirstOrDefaultAsync();
        }

        public async Task<ExchangeRate?> GetLatestRateAsync(string from, string to)
        {
            var today = DateTime.UtcNow.Date;
            return await _dbSet.AsNoTracking()
                .Where(r => r.FromCurrency == from && r.ToCurrency == to && r.RateDate <= today)
                .OrderByDescending(r => r.RateDate)
                .FirstOrDefaultAsync();
        }

        public async Task<decimal?> ConvertAsync(string from, string to, decimal amount, DateTime? date = null)
        {
            var result = await ConvertWithDetailsAsync(from, to, amount, date);
            return result?.TargetAmount;
        }

        public async Task<CurrencyConversionResult?> ConvertWithDetailsAsync(
            string from,
            string to,
            decimal amount,
            DateTime? date = null,
            string? bridgeCurrency = null)
        {
            from = from.Trim().ToUpperInvariant();
            to = to.Trim().ToUpperInvariant();
            var effectiveDate = (date ?? DateTime.UtcNow).Date;

            if (string.Equals(from, to, StringComparison.Ordinal))
            {
                return CreateResult(amount, from, to, 1m, effectiveDate, "Same currency");
            }

            var direct = await GetRateAsync(from, to, effectiveDate);
            if (direct != null && direct.Rate > 0)
            {
                return CreateResult(amount, from, to, direct.Rate, direct.RateDate, direct.Source);
            }

            var inverse = await GetRateAsync(to, from, effectiveDate);
            if (inverse != null && inverse.Rate > 0)
            {
                return CreateResult(amount, from, to, 1m / inverse.Rate, inverse.RateDate, $"Inverse: {inverse.Source}");
            }

            var bridge = bridgeCurrency?.Trim().ToUpperInvariant();
            if (!string.IsNullOrWhiteSpace(bridge) && bridge != from && bridge != to)
            {
                var first = await ConvertWithDetailsAsync(from, bridge, 1m, effectiveDate);
                if (first != null)
                {
                    var second = await ConvertWithDetailsAsync(bridge, to, 1m, effectiveDate);
                    if (second != null)
                    {
                        return CreateResult(amount, from, to, first.ExchangeRate * second.ExchangeRate,
                            first.RateDate < second.RateDate ? first.RateDate : second.RateDate,
                            $"Bridge {bridge}: {first.Source}; {second.Source}");
                    }
                }
            }

            return null;
        }

        private static CurrencyConversionResult CreateResult(
            decimal amount,
            string from,
            string to,
            decimal rate,
            DateTime rateDate,
            string source)
        {
            rate = decimal.Round(rate, 6, MidpointRounding.AwayFromZero);
            return new()
            {
                SourceAmount = amount,
                SourceCurrencyCode = from,
                TargetAmount = RoundMoney(amount * rate),
                TargetCurrencyCode = to,
                ExchangeRate = rate,
                RateDate = rateDate.Date,
                Source = source.Length > 100 ? source[..100] : source
            };
        }

        private static decimal RoundMoney(decimal amount) =>
            decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
        }
}
