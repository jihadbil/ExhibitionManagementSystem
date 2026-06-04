using ExhibitionManagementSystem.Controllers.Base;
using ExhibitionManagementSystem.Models.DTOs.Currency;
using ExhibitionManagementSystem.Models.DTOs.Financial;
using ExhibitionManagementSystem.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExhibitionManagementSystem.Controllers.Financial;

[Route("api/[controller]")]
public class CurrenciesController : BaseApiController
{
    private readonly ICurrencyService _currencyService;

    public CurrenciesController(ICurrencyService currencyService)
        => _currencyService = currencyService;

    // GET /api/currencies
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IList<CurrencyDto>>> GetAll()
    {
        var result = await _currencyService.GetAllAsync();
        return ToActionResult(result);
    }

    // GET /api/currencies/rates/{fromCurrency}
    [HttpGet("rates/{fromCurrency}")]
    public async Task<ActionResult<IList<ExchangeRateDto>>> GetRates(
        string fromCurrency)
    {
        var result = await _currencyService.GetExchangeRatesAsync(fromCurrency);
        return ToActionResult(result);
    }

    // GET /api/currencies/rate?from=USD&to=LYD
    [HttpGet("rate")]
    public async Task<ActionResult<decimal>> GetCurrentRate(
        [FromQuery] string from,
        [FromQuery] string to)
    {
        var result = await _currencyService.GetCurrentRateAsync(from, to);
        return ToActionResult(result);
    }

    // POST /api/currencies/convert
    // Body: { "amount": 100, "from": "USD", "to": "LYD" }
    [HttpPost("convert")]
    public async Task<ActionResult<decimal>> Convert(
        [FromBody] ConvertCurrencyRequest request)
    {
        var result = await _currencyService.ConvertAmountAsync(
            request.Amount, request.From, request.To);
        return ToActionResult(result);
    }

    // PUT /api/currencies/rates
    [HttpPut("rates")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<ExchangeRateDto>> UpsertRate(
        [FromBody] ExchangeRateDto dto)
    {
        // UserId يُستخرج من JWT — يُسجَّل كـ CreatedByUserId
        var result = await _currencyService.UpsertExchangeRateAsync(UserId, dto);
        return ToActionResult(result);
    }
}

public record ConvertCurrencyRequest(decimal Amount, string From, string To);
