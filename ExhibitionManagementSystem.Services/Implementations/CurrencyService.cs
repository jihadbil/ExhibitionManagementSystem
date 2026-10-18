using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using ExhibitionManagementSystem.DataAccess.Repositories.Interfaces;
using ExhibitionManagementSystem.Models;
using ExhibitionManagementSystem.Models.DTOs.Currency;
using ExhibitionManagementSystem.Models.DTOs.Financial;
using ExhibitionManagementSystem.Services.Common;
using ExhibitionManagementSystem.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ExhibitionManagementSystem.Services.Implementations
{
    public class CurrencyService : ICurrencyService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public CurrencyService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<ServiceResult<IList<CurrencyDto>>> GetAllAsync()
        {
            var currencies = await _unitOfWork.Currencies.FindAsync(currency => currency.IsActive);
            var dtos = _mapper.Map<IList<CurrencyDto>>(currencies);
            return ServiceResult<IList<CurrencyDto>>.Success(dtos);
        }

        public async Task<ServiceResult<IList<ExchangeRateDto>>> GetExchangeRatesAsync(string fromCurrency)
        {
            fromCurrency = NormalizeCode(fromCurrency);
            var rates = await _unitOfWork.ExchangeRates.FindAsync(r => r.FromCurrency == fromCurrency);
            var dtos = _mapper.Map<IList<ExchangeRateDto>>(rates);
            return ServiceResult<IList<ExchangeRateDto>>.Success(dtos);
        }

        public async Task<ServiceResult<decimal>> GetCurrentRateAsync(string from, string to)
        {
            from = NormalizeCode(from);
            to = NormalizeCode(to);
            if (string.Equals(from, to, StringComparison.OrdinalIgnoreCase))
            {
                return ServiceResult<decimal>.Success(1.0m);
            }

            var rate = await _unitOfWork.ExchangeRates.GetLatestRateAsync(from, to);
            if (rate != null)
            {
                return ServiceResult<decimal>.Success(rate.Rate);
            }

            var inverseRate = await _unitOfWork.ExchangeRates.GetLatestRateAsync(to, from);
            if (inverseRate != null && inverseRate.Rate != 0)
            {
                return ServiceResult<decimal>.Success(1.0m / inverseRate.Rate);
            }

            return ServiceResult<decimal>.Failure($"سعر الصرف من {from} إلى {to} غير متوفر حالياً", "EXCHANGE_RATE_NOT_FOUND");
        }

        public async Task<ServiceResult<decimal>> ConvertAmountAsync(decimal amount, string from, string to)
        {
            if (amount < 0)
                return ServiceResult<decimal>.Failure("المبلغ المراد تحويله لا يمكن أن يكون سالباً", "INVALID_AMOUNT");
            from = NormalizeCode(from);
            to = NormalizeCode(to);
            var converted = await _unitOfWork.ExchangeRates.ConvertAsync(from, to, amount);
            if (converted == null)
                return ServiceResult<decimal>.Failure($"سعر الصرف من {from} إلى {to} غير متوفر", "EXCHANGE_RATE_NOT_FOUND");
            return ServiceResult<decimal>.Success(converted.Value);
        }

        public async Task<ServiceResult<ExchangeRateDto>> UpsertExchangeRateAsync(string userId, ExchangeRateDto dto)
        {
            dto.FromCurrency = NormalizeCode(dto.FromCurrency);
            dto.ToCurrency = NormalizeCode(dto.ToCurrency);
            if (dto.FromCurrency.Length != 3 || dto.ToCurrency.Length != 3)
                return ServiceResult<ExchangeRateDto>.Failure("رمز العملة غير صالح", "INVALID_CURRENCY");
            if (dto.FromCurrency == dto.ToCurrency)
                return ServiceResult<ExchangeRateDto>.Failure("يجب أن تختلف عملة المصدر عن عملة الهدف", "SAME_CURRENCY_RATE");
            if (dto.Rate <= 0)
                return ServiceResult<ExchangeRateDto>.Failure("سعر الصرف يجب أن يكون أكبر من صفر", "INVALID_EXCHANGE_RATE");

            var targetDate = dto.RateDate == default ? DateTime.UtcNow.Date : dto.RateDate.Date;
            if (targetDate > DateTime.UtcNow.Date)
                return ServiceResult<ExchangeRateDto>.Failure("لا يمكن تفعيل سعر صرف بتاريخ مستقبلي", "FUTURE_EXCHANGE_RATE");

            var currencies = await _unitOfWork.Currencies.FindAsync(c =>
                (c.CurrencyCode == dto.FromCurrency || c.CurrencyCode == dto.ToCurrency) && c.IsActive);
            if (currencies.Select(c => c.CurrencyCode).Distinct().Count() != 2)
                return ServiceResult<ExchangeRateDto>.Failure("إحدى العملتين غير موجودة أو غير نشطة", "CURRENCY_NOT_ACTIVE");
            
            var existing = await _unitOfWork.ExchangeRates.FirstOrDefaultAsync(
                r => r.FromCurrency == dto.FromCurrency && 
                     r.ToCurrency == dto.ToCurrency && 
                     r.RateDate == targetDate);

            if (existing != null)
            {
                existing.Rate = dto.Rate;
                existing.Source = string.IsNullOrWhiteSpace(dto.Source) ? "System Update" : dto.Source;
                _unitOfWork.ExchangeRates.Update(existing);
                await _unitOfWork.SaveChangesAsync();

                var resultDto = _mapper.Map<ExchangeRateDto>(existing);
                return ServiceResult<ExchangeRateDto>.Success(resultDto);
            }
            else
            {
                var exchangeRate = new ExchangeRate
                {
                    FromCurrency = dto.FromCurrency,
                    ToCurrency = dto.ToCurrency,
                    Rate = dto.Rate,
                    RateDate = targetDate,
                    Source = string.IsNullOrWhiteSpace(dto.Source) ? "System Insert" : dto.Source,
                    CreatedByUserId = userId,
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.ExchangeRates.AddAsync(exchangeRate);
                await _unitOfWork.SaveChangesAsync();

                var resultDto = _mapper.Map<ExchangeRateDto>(exchangeRate);
                return ServiceResult<ExchangeRateDto>.Success(resultDto);
            }
        }

        private static string NormalizeCode(string? code) =>
            code?.Trim().ToUpperInvariant() ?? string.Empty;
    }
}
