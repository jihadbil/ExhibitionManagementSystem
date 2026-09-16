using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using ExhibitionManagementSystem.Models.DTOs.Currency;
using ExhibitionManagementSystem.Models.DTOs.Financial;
using ExhibitionManagementSystem.Services.Common;
using ExhibitionManagementSystem.Services.Interfaces;
using ExhibitionManagementSystem.DeskTop.ApiClients.Base;
using ExhibitionManagementSystem.DeskTop.Services.Session;

namespace ExhibitionManagementSystem.DeskTop.ApiClients.Currency;

public class CurrencyApiClient : ApiClientBase, ICurrencyService
{
    public CurrencyApiClient(IHttpClientFactory httpClientFactory, SessionService session)
        : base(httpClientFactory, session)
    {
    }

    public Task<ServiceResult<IList<CurrencyDto>>> GetAllAsync()
    {
        return GetAsync<IList<CurrencyDto>>("api/currencies");
    }

    public Task<ServiceResult<IList<ExchangeRateDto>>> GetExchangeRatesAsync(string fromCurrency)
    {
        return GetAsync<IList<ExchangeRateDto>>($"api/currencies/rates/{fromCurrency}");
    }

    public Task<ServiceResult<decimal>> GetCurrentRateAsync(string from, string to)
    {
        return GetAsync<decimal>($"api/currencies/rate?from={from}&to={to}");
    }

    public Task<ServiceResult<decimal>> ConvertAmountAsync(decimal amount, string from, string to)
    {
        var request = new ConvertCurrencyRequest(amount, from, to);
        return PostAsync<decimal>("api/currencies/convert", request);
    }

    public Task<ServiceResult<ExchangeRateDto>> UpsertExchangeRateAsync(string userId, ExchangeRateDto dto)
    {
        return PutAsync<ExchangeRateDto>("api/currencies/rates", dto);
    }
}

public record ConvertCurrencyRequest(decimal Amount, string From, string To);
