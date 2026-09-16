using System.Collections.Generic;
using System.Threading.Tasks;
using ExhibitionManagementSystem.Models.DTOs.Currency;
using ExhibitionManagementSystem.Models.DTOs.Financial;
using ExhibitionManagementSystem.Services.Common;

namespace ExhibitionManagementSystem.Services.Interfaces
{
    public interface ICurrencyService
    {
        Task<ServiceResult<IList<CurrencyDto>>> GetAllAsync();
        Task<ServiceResult<IList<ExchangeRateDto>>> GetExchangeRatesAsync(string fromCurrency);
        Task<ServiceResult<decimal>> GetCurrentRateAsync(string from, string to);
        Task<ServiceResult<decimal>> ConvertAmountAsync(decimal amount, string from, string to);
        Task<ServiceResult<ExchangeRateDto>> UpsertExchangeRateAsync(string userId, ExchangeRateDto dto);
    }
}
