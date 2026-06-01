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
            return await _dbSet.AsNoTracking()
                .Where(r => r.FromCurrency == from && r.ToCurrency == to)
                .OrderByDescending(r => r.RateDate)
                .FirstOrDefaultAsync();
        }

        public async Task<decimal?> ConvertAsync(string from, string to, decimal amount, DateTime? date = null)
        {
            if (string.Equals(from, to, StringComparison.OrdinalIgnoreCase))
                return amount;

            var rate = date.HasValue
                ? await GetRateAsync(from, to, date.Value)
                : await GetLatestRateAsync(from, to);

            if (rate == null)
            {
                // Try inverse rate mapping
                var inverseRate = date.HasValue
                    ? await GetRateAsync(to, from, date.Value)
                    : await GetLatestRateAsync(to, from);

                if (inverseRate != null && inverseRate.Rate != 0)
                {
                    return amount / inverseRate.Rate;
                }

                return null;
            }

            return amount * rate.Rate;
        }
    }
}
