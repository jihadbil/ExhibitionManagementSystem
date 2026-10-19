using ExhibitionManagementSystem.DataAccess.Repositories.Interfaces;

namespace ExhibitionManagementSystem.Services.Common;

public static class CurrencyTotals
{
    public static async Task<ServiceResult<decimal>> SumAsync(IUnitOfWork unitOfWork,
        IEnumerable<(decimal Amount, string Currency)> amounts, string target)
    {
        decimal total = 0;
        foreach (var group in amounts.GroupBy(x => x.Currency, StringComparer.OrdinalIgnoreCase))
        {
            var converted = await unitOfWork.ExchangeRates.ConvertAsync(group.Key, target,
                group.Sum(x => x.Amount), DateTime.UtcNow.Date);
            if (!converted.HasValue)
                return ServiceResult<decimal>.Failure("سعر صرف مطلوب للتقرير غير متوفر", "EXCHANGE_RATE_NOT_FOUND");
            total += converted.Value;
        }
        return ServiceResult<decimal>.Success(decimal.Round(total, 2, MidpointRounding.AwayFromZero));
    }
}
