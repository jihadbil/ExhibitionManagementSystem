using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using ExhibitionManagementSystem.Models.DTOs.Pricing;
using ExhibitionManagementSystem.Models.Enums;
using ExhibitionManagementSystem.Services.Common;
using ExhibitionManagementSystem.Services.Interfaces;
using ExhibitionManagementSystem.DeskTop.ApiClients.Base;
using ExhibitionManagementSystem.DeskTop.Services.Session;

namespace ExhibitionManagementSystem.DeskTop.ApiClients.Pricing;

public class PricingApiClient : ApiClientBase, IPricingService
{
    public PricingApiClient(IHttpClientFactory httpClientFactory, SessionService session)
        : base(httpClientFactory, session)
    {
    }

    public Task<ServiceResult<decimal>> CalculateBoothPriceAsync(
        int tenantId, 
        int? exhibitionId, 
        BoothType boothType, 
        ExhibitorCategory category, 
        decimal areaSqM)
    {
        var request = new CalculateBoothPriceRequest(
            exhibitionId ?? 0, 
            boothType.ToString(), 
            category.ToString(), 
            areaSqM);
        return PostAsync<decimal>("api/pricing/calculate/booth", request);
    }

    public Task<ServiceResult<decimal>> CalculateServicePriceAsync(
        int tenantId, 
        int serviceId, 
        int? exhibitionId, 
        int quantity)
    {
        var request = new CalculateServicePriceRequest(
            serviceId, 
            exhibitionId ?? 0, 
            quantity);
        return PostAsync<decimal>("api/pricing/calculate/service", request);
    }

    public Task<ServiceResult<IList<BoothPriceRuleDto>>> GetBoothPriceRulesAsync(int tenantId, int? exhibitionId)
    {
        var url = exhibitionId.HasValue 
            ? $"api/pricing/booth-rules?exhibitionId={exhibitionId}" 
            : "api/pricing/booth-rules";
        return GetAsync<IList<BoothPriceRuleDto>>(url);
    }

    public Task<ServiceResult<BoothPriceRuleDto>> CreateBoothPriceRuleAsync(int tenantId, BoothPriceRuleCreateDto dto)
    {
        return PostAsync<BoothPriceRuleDto>("api/pricing/booth-rules", dto);
    }

    public Task<ServiceResult<BoothPriceRuleDto>> UpdateBoothPriceRuleAsync(int tenantId, int ruleId, BoothPriceRuleCreateDto dto)
    {
        return PutAsync<BoothPriceRuleDto>($"api/pricing/booth-rules/{ruleId}", dto);
    }

    public Task<ServiceResult> DeleteBoothPriceRuleAsync(int tenantId, int ruleId)
    {
        return DeleteAsync($"api/pricing/booth-rules/{ruleId}");
    }

    public Task<ServiceResult<IList<ServicePriceRuleDto>>> GetServicePriceRulesAsync(int tenantId, int? exhibitionId)
    {
        var url = exhibitionId.HasValue 
            ? $"api/pricing/service-rules?exhibitionId={exhibitionId}" 
            : "api/pricing/service-rules";
        return GetAsync<IList<ServicePriceRuleDto>>(url);
    }

    public Task<ServiceResult<ServicePriceRuleDto>> CreateServicePriceRuleAsync(int tenantId, ServicePriceRuleCreateDto dto)
    {
        return PostAsync<ServicePriceRuleDto>("api/pricing/service-rules", dto);
    }

    public Task<ServiceResult<IList<PricingPackageDto>>> GetPackagesAsync(int tenantId)
    {
        return GetAsync<IList<PricingPackageDto>>("api/pricing/packages");
    }

    public Task<ServiceResult<PricingPackageDto>> CreatePackageAsync(int tenantId, PricingPackageCreateDto dto)
    {
        return PostAsync<PricingPackageDto>("api/pricing/packages", dto);
    }
}

public record CalculateBoothPriceRequest(
    int ExhibitionId,
    string BoothType,
    string ExhibitorCategory,
    decimal? AreaSqM);

public record CalculateServicePriceRequest(
    int ServiceId,
    int ExhibitionId,
    int Quantity);
