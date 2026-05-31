using System;
using System.Threading.Tasks;
using ExhibitionManagementSystem.Models;

namespace ExhibitionManagementSystem.DataAccess.Repositories.Interfaces
{
    public interface IExchangeRateRepository : IGenericRepository<ExchangeRate>
    {
        Task<ExchangeRate?> GetRateAsync(string from, string to, DateTime date);
        Task<ExchangeRate?> GetLatestRateAsync(string from, string to);
        Task<decimal> ConvertAsync(string from, string to, decimal amount, DateTime? date = null);
    }
}
