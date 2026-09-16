using System.Collections.Generic;
using System.Threading.Tasks;
using ExhibitionManagementSystem.Models.DTOs.Pricing;
using ExhibitionManagementSystem.Models.Enums;
using ExhibitionManagementSystem.Services.Common;

namespace ExhibitionManagementSystem.Services.Interfaces
{
    public interface IPricingService
    {
        Task<ServiceResult<decimal>> CalculateBoothPriceAsync(
            int tenantId, 
            int? exhibitionId, 
            BoothType boothType, 
            ExhibitorCategory category, 
            decimal areaSqM);

        Task<ServiceResult<decimal>> CalculateServicePriceAsync(
            int tenantId, 
            int serviceId, 
            int? exhibitionId, 
            int quantity);

        // Booth Price Rules
        Task<ServiceResult<IList<BoothPriceRuleDto>>> GetBoothPriceRulesAsync(int tenantId, int? exhibitionId);
        Task<ServiceResult<BoothPriceRuleDto>> CreateBoothPriceRuleAsync(int tenantId, BoothPriceRuleCreateDto dto);
        Task<ServiceResult<BoothPriceRuleDto>> UpdateBoothPriceRuleAsync(int tenantId, int ruleId, BoothPriceRuleCreateDto dto);
        Task<ServiceResult> DeleteBoothPriceRuleAsync(int tenantId, int ruleId);

        // Service Price Rules
        Task<ServiceResult<IList<ServicePriceRuleDto>>> GetServicePriceRulesAsync(int tenantId, int? exhibitionId);
        Task<ServiceResult<ServicePriceRuleDto>> CreateServicePriceRuleAsync(int tenantId, ServicePriceRuleCreateDto dto);

        // Packages
        Task<ServiceResult<IList<PricingPackageDto>>> GetPackagesAsync(int tenantId);
        Task<ServiceResult<PricingPackageDto>> CreatePackageAsync(int tenantId, PricingPackageCreateDto dto);
    }
}
