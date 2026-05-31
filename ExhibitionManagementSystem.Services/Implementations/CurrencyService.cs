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
            var currencies = await _unitOfWork.Currencies.GetAllAsync();
            var dtos = _mapper.Map<IList<CurrencyDto>>(currencies);
            return ServiceResult<IList<CurrencyDto>>.Success(dtos);
        }

        public async Task<ServiceResult<IList<ExchangeRateDto>>> GetExchangeRatesAsync(string fromCurrency)
        {
            var rates = await _unitOfWork.ExchangeRates.FindAsync(r => r.FromCurrency == fromCurrency);
            var dtos = _mapper.Map<IList<ExchangeRateDto>>(rates);
            return ServiceResult<IList<ExchangeRateDto>>.Success(dtos);
        }

        public async Task<ServiceResult<decimal>> GetCurrentRateAsync(string from, string to)
        {
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
            try
            {
                var converted = await _unitOfWork.ExchangeRates.ConvertAsync(from, to, amount);
                return ServiceResult<decimal>.Success(converted);
            }
            catch (Exception ex)
            {
                return ServiceResult<decimal>.Failure($"فشل تحويل العملة: {ex.Message}", "EXCHANGE_RATE_NOT_FOUND");
            }
        }

        public async Task<ServiceResult<ExchangeRateDto>> UpsertExchangeRateAsync(ExchangeRateDto dto)
        {
            var targetDate = dto.RateDate == default ? DateTime.UtcNow.Date : dto.RateDate.Date;
            
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
                    CreatedByUserId = "System", // Default system
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.ExchangeRates.AddAsync(exchangeRate);
                await _unitOfWork.SaveChangesAsync();

                var resultDto = _mapper.Map<ExchangeRateDto>(exchangeRate);
                return ServiceResult<ExchangeRateDto>.Success(resultDto);
            }
        }
    }
}
