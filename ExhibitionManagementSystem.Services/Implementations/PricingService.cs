using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using ExhibitionManagementSystem.DataAccess.Repositories.Interfaces;
using ExhibitionManagementSystem.Models;
using ExhibitionManagementSystem.Models.Enums;
using ExhibitionManagementSystem.Models.DTOs.Pricing;
using ExhibitionManagementSystem.Services.Common;
using ExhibitionManagementSystem.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ExhibitionManagementSystem.Services.Implementations
{
    public class PricingService : IPricingService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public PricingService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<ServiceResult<decimal>> CalculateBoothPriceAsync(
            int tenantId, 
            int? exhibitionId, 
            BoothType boothType, 
            ExhibitorCategory category, 
            decimal areaSqM)
        {
            var rule = await _unitOfWork.BoothPriceRules.GetApplicableRuleAsync(
                tenantId, 
                exhibitionId, 
                boothType, 
                category, 
                areaSqM, 
                DateTime.UtcNow);

            if (rule == null)
            {
                return ServiceResult<decimal>.Failure("لا توجد قاعدة تسعير مناسبة للكشك المحدد", "PRICING_RULE_NOT_FOUND");
            }

            var price = rule.PricePerSqM * areaSqM;
            return ServiceResult<decimal>.Success(price);
        }

        public async Task<ServiceResult<decimal>> CalculateServicePriceAsync(
            int tenantId, 
            int serviceId, 
            int? exhibitionId, 
            int quantity)
        {
            // ExhibitorCategory is null as we don't have category context in parameters.
            var rule = await _unitOfWork.ServicePriceRules.GetApplicableRuleAsync(
                serviceId, 
                exhibitionId, 
                null, 
                DateTime.UtcNow);

            if (rule == null)
            {
                // Fallback: If no rule is found, try to get the service itself and use its DefaultPrice
                var service = await _unitOfWork.Services.GetByIdAsync(serviceId);
                if (service != null && service.TenantID == tenantId && service.DefaultPrice.HasValue)
                {
                    var fallbackPrice = service.DefaultPrice.Value * quantity;
                    return ServiceResult<decimal>.Success(fallbackPrice);
                }

                return ServiceResult<decimal>.Failure("لا توجد قاعدة تسعير مناسبة للخدمة المحددة", "PRICING_RULE_NOT_FOUND");
            }

            var price = rule.UnitPrice * quantity;
            return ServiceResult<decimal>.Success(price);
        }

        // Booth Price Rules
        public async Task<ServiceResult<IList<BoothPriceRuleDto>>> GetBoothPriceRulesAsync(int tenantId, int? exhibitionId)
        {
            var query = _unitOfWork.BoothPriceRules.AsQueryable()
                .Include(r => r.Exhibition)
                .Where(r => r.TenantID == tenantId);

            if (exhibitionId.HasValue)
            {
                query = query.Where(r => r.ExhibitionID == exhibitionId.Value);
            }

            var rules = await query.ToListAsync();
            var dtos = _mapper.Map<IList<BoothPriceRuleDto>>(rules);
            return ServiceResult<IList<BoothPriceRuleDto>>.Success(dtos);
        }

        public async Task<ServiceResult<BoothPriceRuleDto>> CreateBoothPriceRuleAsync(int tenantId, BoothPriceRuleCreateDto dto)
        {
            if (dto.ExhibitionID.HasValue)
            {
                var exhibition = await _unitOfWork.Exhibitions.GetByIdAsync(dto.ExhibitionID.Value);
                if (exhibition == null || exhibition.TenantID != tenantId)
                {
                    return ServiceResult<BoothPriceRuleDto>.Failure("المعرض المحدد غير موجود", "EXHIBITION_NOT_FOUND");
                }
            }

            var rule = _mapper.Map<BoothPriceRule>(dto);
            rule.TenantID = tenantId;

            await _unitOfWork.BoothPriceRules.AddAsync(rule);
            await _unitOfWork.SaveChangesAsync();

            var createdRule = await _unitOfWork.BoothPriceRules.AsQueryable()
                .Include(r => r.Exhibition)
                .FirstOrDefaultAsync(r => r.RuleID == rule.RuleID);

            var resultDto = _mapper.Map<BoothPriceRuleDto>(createdRule ?? rule);
            return ServiceResult<BoothPriceRuleDto>.Success(resultDto);
        }

        public async Task<ServiceResult<BoothPriceRuleDto>> UpdateBoothPriceRuleAsync(int tenantId, int ruleId, BoothPriceRuleCreateDto dto)
        {
            var rule = await _unitOfWork.BoothPriceRules.GetByIdAsync(ruleId);
            if (rule == null || rule.TenantID != tenantId)
            {
                return ServiceResult<BoothPriceRuleDto>.Failure("قاعدة التسعير غير موجودة", "PRICING_RULE_NOT_FOUND");
            }

            if (dto.ExhibitionID.HasValue)
            {
                var exhibition = await _unitOfWork.Exhibitions.GetByIdAsync(dto.ExhibitionID.Value);
                if (exhibition == null || exhibition.TenantID != tenantId)
                {
                    return ServiceResult<BoothPriceRuleDto>.Failure("المعرض المحدد غير موجود", "EXHIBITION_NOT_FOUND");
                }
            }

            _mapper.Map(dto, rule);
            rule.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.BoothPriceRules.Update(rule);
            await _unitOfWork.SaveChangesAsync();

            var updatedRule = await _unitOfWork.BoothPriceRules.AsQueryable()
                .Include(r => r.Exhibition)
                .FirstOrDefaultAsync(r => r.RuleID == rule.RuleID);

            var resultDto = _mapper.Map<BoothPriceRuleDto>(updatedRule ?? rule);
            return ServiceResult<BoothPriceRuleDto>.Success(resultDto);
        }

        public async Task<ServiceResult> DeleteBoothPriceRuleAsync(int tenantId, int ruleId)
        {
            var rule = await _unitOfWork.BoothPriceRules.GetByIdAsync(ruleId);
            if (rule == null || rule.TenantID != tenantId)
            {
                return ServiceResult.Failure("قاعدة التسعير غير موجودة", "PRICING_RULE_NOT_FOUND");
            }

            await _unitOfWork.BoothPriceRules.SoftDeleteAsync(ruleId, "System");
            await _unitOfWork.SaveChangesAsync();

            return ServiceResult.Success();
        }

        // Service Price Rules
        public async Task<ServiceResult<IList<ServicePriceRuleDto>>> GetServicePriceRulesAsync(int tenantId, int? exhibitionId)
        {
            var query = _unitOfWork.ServicePriceRules.AsQueryable()
                .Include(r => r.Service)
                .Include(r => r.Exhibition)
                .Where(r => r.TenantID == tenantId);

            if (exhibitionId.HasValue)
            {
                query = query.Where(r => r.ExhibitionID == exhibitionId.Value);
            }

            var rules = await query.ToListAsync();
            var dtos = _mapper.Map<IList<ServicePriceRuleDto>>(rules);
            return ServiceResult<IList<ServicePriceRuleDto>>.Success(dtos);
        }

        public async Task<ServiceResult<ServicePriceRuleDto>> CreateServicePriceRuleAsync(int tenantId, ServicePriceRuleCreateDto dto)
        {
            var service = await _unitOfWork.Services.GetByIdAsync(dto.ServiceID);
            if (service == null || service.TenantID != tenantId)
            {
                return ServiceResult<ServicePriceRuleDto>.Failure("الخدمة المحددة غير موجودة", "SERVICE_NOT_FOUND");
            }

            if (dto.ExhibitionID.HasValue)
            {
                var exhibition = await _unitOfWork.Exhibitions.GetByIdAsync(dto.ExhibitionID.Value);
                if (exhibition == null || exhibition.TenantID != tenantId)
                {
                    return ServiceResult<ServicePriceRuleDto>.Failure("المعرض المحدد غير موجود", "EXHIBITION_NOT_FOUND");
                }
            }

            var rule = _mapper.Map<ServicePriceRule>(dto);
            rule.TenantID = tenantId;

            await _unitOfWork.ServicePriceRules.AddAsync(rule);
            await _unitOfWork.SaveChangesAsync();

            var createdRule = await _unitOfWork.ServicePriceRules.AsQueryable()
                .Include(r => r.Service)
                .Include(r => r.Exhibition)
                .FirstOrDefaultAsync(r => r.PriceRuleID == rule.PriceRuleID);

            var resultDto = _mapper.Map<ServicePriceRuleDto>(createdRule ?? rule);
            return ServiceResult<ServicePriceRuleDto>.Success(resultDto);
        }

        // Packages
        public async Task<ServiceResult<IList<PricingPackageDto>>> GetPackagesAsync(int tenantId)
        {
            var packages = await _unitOfWork.PricingPackages.AsQueryable()
                .Include(p => p.Currency)
                .Include(p => p.PackageServices)
                    .ThenInclude(ps => ps.Service)
                .Where(p => p.TenantID == tenantId)
                .ToListAsync();

            var dtos = _mapper.Map<IList<PricingPackageDto>>(packages);
            return ServiceResult<IList<PricingPackageDto>>.Success(dtos);
        }

        public async Task<ServiceResult<PricingPackageDto>> CreatePackageAsync(int tenantId, PricingPackageCreateDto dto)
        {
            var package = _mapper.Map<PricingPackage>(dto);
            package.TenantID = tenantId;
            package.IsActive = true;

            await _unitOfWork.PricingPackages.AddAsync(package);
            await _unitOfWork.SaveChangesAsync();

            if (dto.ServiceIDs != null && dto.ServiceIDs.Any())
            {
                foreach (var serviceId in dto.ServiceIDs)
                {
                    var svc = await _unitOfWork.Services.GetByIdAsync(serviceId);
                    if (svc != null && svc.TenantID == tenantId)
                    {
                        var pkgSvc = new PackageService
                        {
                            PackageID = package.PackageID,
                            ServiceID = serviceId,
                            Quantity = 1,
                            UnitPrice = svc.DefaultPrice ?? 0
                        };
                        package.PackageServices.Add(pkgSvc);
                    }
                }
                await _unitOfWork.SaveChangesAsync();
            }

            var fullPackage = await _unitOfWork.PricingPackages.AsQueryable()
                .Include(p => p.Currency)
                .Include(p => p.PackageServices)
                    .ThenInclude(ps => ps.Service)
                .FirstOrDefaultAsync(p => p.PackageID == package.PackageID);

            var resultDto = _mapper.Map<PricingPackageDto>(fullPackage ?? package);
            return ServiceResult<PricingPackageDto>.Success(resultDto);
        }
    }
}
