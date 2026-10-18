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
            decimal areaSqM,
            int? boothId = null,
            string? productCategory = null)
        {
            var result = await CalculateBoothPriceDetailsAsync(
                tenantId, exhibitionId, boothType, category, areaSqM, boothId, productCategory);
            return result.IsSuccess
                ? ServiceResult<decimal>.Success(result.Data!.Amount)
                : ServiceResult<decimal>.Failure(
                    result.ErrorMessage ?? "فشل حساب سعر الجناح",
                    result.ErrorCode ?? "PRICING_ERROR");
        }

        public async Task<ServiceResult<BoothPriceCalculationDto>> CalculateBoothPriceDetailsAsync(
            int tenantId,
            int? exhibitionId,
            BoothType boothType,
            ExhibitorCategory category,
            decimal areaSqM,
            int? boothId = null,
            string? productCategory = null)
        {
            if (areaSqM <= 0)
            {
                return ServiceResult<BoothPriceCalculationDto>.Failure(
                    "مساحة الجناح يجب أن تكون أكبر من صفر",
                    "INVALID_BOOTH_AREA");
            }

            BoothPriceRule? rule = null;
            var now = DateTime.UtcNow;

            if (boothId.HasValue && boothId.Value > 0)
            {
                var booth = await _unitOfWork.Booths.GetByIdAsync(boothId.Value);
                if (booth == null || booth.TenantID != tenantId)
                {
                    return ServiceResult<BoothPriceCalculationDto>.Failure(
                        "الجناح غير موجود",
                        "BOOTH_NOT_FOUND");
                }

                if (booth.AssignedPriceRuleID.HasValue)
                {
                    rule = await _unitOfWork.BoothPriceRules.GetByIdAsync(booth.AssignedPriceRuleID.Value);
                    var isApplicable = rule != null
                        && rule.TenantID == tenantId
                        && (!rule.ExhibitionID.HasValue || rule.ExhibitionID == exhibitionId)
                        && (!rule.BoothType.HasValue || rule.BoothType == boothType)
                        && (!rule.ExhibitorCategory.HasValue || rule.ExhibitorCategory == category)
                        && (string.IsNullOrEmpty(rule.ProductCategory) ||
                            string.Equals(rule.ProductCategory, productCategory, StringComparison.OrdinalIgnoreCase))
                        && (!rule.MinAreaSqM.HasValue || areaSqM >= rule.MinAreaSqM.Value)
                        && (!rule.MaxAreaSqM.HasValue || areaSqM <= rule.MaxAreaSqM.Value)
                        && rule.ValidFrom.Date <= now.Date
                        && (!rule.ValidTo.HasValue || rule.ValidTo.Value.Date >= now.Date);
                    if (!isApplicable)
                    {
                        return ServiceResult<BoothPriceCalculationDto>.Failure(
                            "قاعدة التسعير المعينة للجناح غير صالحة لهذا الحجز",
                            "PRICING_RULE_NOT_APPLICABLE");
                    }
                }
            }

            if (rule == null)
            {
                rule = await _unitOfWork.BoothPriceRules.GetApplicableRuleAsync(
                    tenantId, 
                    exhibitionId, 
                    boothType, 
                    category, 
                    areaSqM, 
                    now,
                    productCategory);
            }

            if (rule == null)
            {
                return ServiceResult<BoothPriceCalculationDto>.Failure(
                    "لا توجد قاعدة تسعير مناسبة للجناح المحدد",
                    "PRICING_RULE_NOT_FOUND");
            }

            return ServiceResult<BoothPriceCalculationDto>.Success(new BoothPriceCalculationDto
            {
                Amount = rule.PricePerSqM * areaSqM,
                CurrencyCode = rule.CurrencyCode,
                RuleID = rule.RuleID
            });
        }

        public async Task<ServiceResult<decimal>> CalculateServicePriceAsync(
            int tenantId, 
            int serviceId, 
            int? exhibitionId, 
            int quantity)
        {
            var details = await CalculateServicePriceDetailsAsync(
                tenantId, serviceId, exhibitionId, quantity);
            return details.IsSuccess
                ? ServiceResult<decimal>.Success(details.Data!.TotalAmount)
                : ServiceResult<decimal>.Failure(details.ErrorMessage!, details.ErrorCode!);
        }

        public async Task<ServiceResult<ServicePriceCalculationDto>> CalculateServicePriceDetailsAsync(
            int tenantId,
            int serviceId,
            int? exhibitionId,
            int quantity,
            ExhibitorCategory? exhibitorCategory = null)
        {
            if (quantity <= 0)
            {
                return ServiceResult<ServicePriceCalculationDto>.Failure(
                    "كمية الخدمة يجب أن تكون أكبر من صفر", "INVALID_SERVICE_QUANTITY");
            }

            var rule = await _unitOfWork.ServicePriceRules.GetApplicableRuleAsync(
                serviceId,
                exhibitionId,
                exhibitorCategory,
                DateTime.UtcNow);

            if (rule == null)
            {
                // Fallback: If no rule is found, try to get the service itself and use its DefaultPrice
                var service = await _unitOfWork.Services.GetByIdAsync(serviceId);
                if (service != null && service.TenantID == tenantId && service.DefaultPrice.HasValue)
                {
                    var fallbackPrice = service.DefaultPrice.Value * quantity;
                    return ServiceResult<ServicePriceCalculationDto>.Success(new ServicePriceCalculationDto
                    {
                        UnitPrice = service.DefaultPrice.Value,
                        TotalAmount = fallbackPrice,
                        CurrencyCode = service.DefaultCurrencyCode,
                        PriceRuleID = null
                    });
                }

                return ServiceResult<ServicePriceCalculationDto>.Failure("لا توجد قاعدة تسعير مناسبة للخدمة المحددة", "PRICING_RULE_NOT_FOUND");
            }

            var price = rule.UnitPrice * quantity;
            return ServiceResult<ServicePriceCalculationDto>.Success(new ServicePriceCalculationDto
            {
                UnitPrice = rule.UnitPrice,
                TotalAmount = price,
                CurrencyCode = rule.CurrencyCode,
                PriceRuleID = rule.PriceRuleID
            });
        }

        // Booth Price Rules
        public async Task<ServiceResult<IList<BoothPriceRuleDto>>> GetBoothPriceRulesAsync(int tenantId, int? exhibitionId)
        {
            var query = _unitOfWork.BoothPriceRules.AsQueryable()
                .Include(r => r.Exhibition)
                .Where(r => r.TenantID == tenantId);

            if (exhibitionId.HasValue)
            {
                query = query.Where(r => r.ExhibitionID == null || r.ExhibitionID == exhibitionId.Value);
            }

            var rules = await query.ToListAsync();
            var dtos = _mapper.Map<IList<BoothPriceRuleDto>>(rules);
            return ServiceResult<IList<BoothPriceRuleDto>>.Success(dtos);
        }

        public async Task<ServiceResult<BoothPriceRuleDto>> CreateBoothPriceRuleAsync(int tenantId, BoothPriceRuleCreateDto dto)
        {
            var validationError = ValidateBoothPriceRule(dto);
            if (validationError.HasValue)
            {
                return ServiceResult<BoothPriceRuleDto>.Failure(validationError.Value.Message, validationError.Value.Code);
            }

            if (dto.ExhibitionID.HasValue)
            {
                var exhibition = await _unitOfWork.Exhibitions.GetByIdAsync(dto.ExhibitionID.Value);
                if (exhibition == null || exhibition.TenantID != tenantId)
                {
                    return ServiceResult<BoothPriceRuleDto>.Failure("المعرض المحدد غير موجود", "EXHIBITION_NOT_FOUND");
                }
            }

            NormalizeBoothPriceRule(dto);
            if (!await _unitOfWork.Currencies.ExistsAsync(c => c.CurrencyCode == dto.CurrencyCode && c.IsActive))
                return ServiceResult<BoothPriceRuleDto>.Failure("عملة التسعير غير موجودة أو غير نشطة", "INVALID_CURRENCY");
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
            var validationError = ValidateBoothPriceRule(dto);
            if (validationError.HasValue)
            {
                return ServiceResult<BoothPriceRuleDto>.Failure(validationError.Value.Message, validationError.Value.Code);
            }

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

            NormalizeBoothPriceRule(dto);
            if (!await _unitOfWork.Currencies.ExistsAsync(c => c.CurrencyCode == dto.CurrencyCode && c.IsActive))
                return ServiceResult<BoothPriceRuleDto>.Failure("عملة التسعير غير موجودة أو غير نشطة", "INVALID_CURRENCY");
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

            var isAssigned = await _unitOfWork.Booths.ExistsAsync(booth => booth.AssignedPriceRuleID == ruleId);
            if (isAssigned)
            {
                return ServiceResult.Failure(
                    "لا يمكن حذف قاعدة التسعير لأنها معينة لجناح واحد أو أكثر",
                    "PRICING_RULE_IN_USE");
            }

            await _unitOfWork.BoothPriceRules.SoftDeleteAsync(ruleId, "System");
            await _unitOfWork.SaveChangesAsync();

            return ServiceResult.Success();
        }

        private static (string Message, string Code)? ValidateBoothPriceRule(BoothPriceRuleCreateDto dto)
        {
            if (!MoneyInput.IsValid(dto.PricePerSqM))
                return ("سعر المتر يجب أن يكون موجباً وضمن الحجم المدعوم وبمنزلتين عشريتين كحد أقصى", "INVALID_PRICE");
            if (string.IsNullOrWhiteSpace(dto.CurrencyCode) || dto.CurrencyCode.Trim().Length != 3)
                return ("رمز العملة غير صالح", "INVALID_CURRENCY");
            if (!string.IsNullOrWhiteSpace(dto.BoothType) &&
                !EnumInput.TryParse<BoothType>(dto.BoothType, out _))
                return ("نوع الجناح غير صالح", "INVALID_BOOTH_TYPE");
            if (!string.IsNullOrWhiteSpace(dto.ExhibitorCategory) &&
                !EnumInput.TryParse<ExhibitorCategory>(dto.ExhibitorCategory, out _))
                return ("فئة العارض غير صالحة", "INVALID_EXHIBITOR_CATEGORY");
            if (dto.MinAreaSqM < 0 || dto.MaxAreaSqM <= 0)
                return ("حدود المساحة غير صالحة", "INVALID_AREA_RANGE");
            if (dto.MinAreaSqM.HasValue && dto.MaxAreaSqM.HasValue && dto.MinAreaSqM > dto.MaxAreaSqM)
                return ("الحد الأدنى للمساحة يجب ألا يتجاوز الحد الأقصى", "INVALID_AREA_RANGE");
            if (dto.ValidTo.HasValue && dto.ValidTo.Value.Date < dto.ValidFrom.Date)
                return ("تاريخ انتهاء القاعدة يجب ألا يسبق تاريخ بدايتها", "INVALID_VALIDITY_RANGE");
            return null;
        }

        private static void NormalizeBoothPriceRule(BoothPriceRuleCreateDto dto)
        {
            dto.CurrencyCode = dto.CurrencyCode.Trim().ToUpperInvariant();
            dto.BoothType = string.IsNullOrWhiteSpace(dto.BoothType) ? null : dto.BoothType.Trim();
            dto.ExhibitorCategory = string.IsNullOrWhiteSpace(dto.ExhibitorCategory) ? null : dto.ExhibitorCategory.Trim();
            dto.ProductCategory = dto.ProductCategory?.Trim() ?? string.Empty;
            dto.RuleName = string.IsNullOrWhiteSpace(dto.RuleName) ? null : dto.RuleName.Trim();
            dto.Notes = dto.Notes?.Trim() ?? string.Empty;
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
            dto.CurrencyCode = dto.CurrencyCode.Trim().ToUpperInvariant();
            dto.ValidFrom = dto.ValidFrom == default ? DateTime.UtcNow.Date : dto.ValidFrom.Date;
            dto.ValidTo = dto.ValidTo?.Date;
            dto.ExhibitorCategory = string.IsNullOrWhiteSpace(dto.ExhibitorCategory) ? null : dto.ExhibitorCategory.Trim();
            if (!MoneyInput.IsValid(dto.UnitPrice, allowZero: true))
                return ServiceResult<ServicePriceRuleDto>.Failure("سعر الخدمة يجب أن يكون غير سالب وضمن الحجم المدعوم وبمنزلتين عشريتين كحد أقصى", "INVALID_PRICE");
            if (dto.ValidTo < dto.ValidFrom)
                return ServiceResult<ServicePriceRuleDto>.Failure("تاريخ انتهاء القاعدة يسبق بدايتها", "INVALID_VALIDITY_RANGE");
            if (dto.ExhibitorCategory != null &&
                (!Enum.TryParse<ExhibitorCategory>(dto.ExhibitorCategory, true, out var category) || !Enum.IsDefined(category)))
                return ServiceResult<ServicePriceRuleDto>.Failure("فئة العارض غير صالحة", "INVALID_EXHIBITOR_CATEGORY");
            if (!await _unitOfWork.Currencies.ExistsAsync(c => c.CurrencyCode == dto.CurrencyCode && c.IsActive))
                return ServiceResult<ServicePriceRuleDto>.Failure("عملة التسعير غير موجودة أو غير نشطة", "INVALID_CURRENCY");

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
            dto.CurrencyCode = dto.CurrencyCode.Trim().ToUpperInvariant();
            var tenant = await _unitOfWork.Tenants.GetByIdAsync(tenantId);
            if (tenant == null)
                return ServiceResult<PricingPackageDto>.Failure("المستأجر غير موجود", "TENANT_NOT_FOUND");
            if (!MoneyInput.IsValid(dto.TotalPrice, allowZero: true) || dto.ValidFrom == default || dto.ValidTo < dto.ValidFrom ||
                dto.ServiceIDs == null || dto.ServiceIDs.Count != dto.ServiceIDs.Distinct().Count())
                return ServiceResult<PricingPackageDto>.Failure("سعر الباقة أو تواريخها أو قائمة خدماتها غير صالحة", "INVALID_PACKAGE_INPUT");
            if (!await _unitOfWork.Currencies.ExistsAsync(c => c.CurrencyCode == dto.CurrencyCode && c.IsActive))
                return ServiceResult<PricingPackageDto>.Failure("عملة الباقة غير صالحة", "INVALID_CURRENCY");
            var package = _mapper.Map<PricingPackage>(dto);
            package.TenantID = tenantId;
            package.IsActive = true;
            var pricingSnapshots = new List<object>();
            foreach (var serviceId in dto.ServiceIDs)
            {
                var svc = await _unitOfWork.Services.GetByIdAsync(serviceId);
                if (svc == null || svc.TenantID != tenantId || !svc.IsActive || !svc.DefaultPrice.HasValue || svc.DefaultPrice < 0)
                    return ServiceResult<PricingPackageDto>.Failure("إحدى خدمات الباقة غير متاحة أو ليس لها سعر", "SERVICE_NOT_AVAILABLE");
                var conversion = await _unitOfWork.ExchangeRates.ConvertWithDetailsAsync(svc.DefaultCurrencyCode,
                    dto.CurrencyCode, svc.DefaultPrice.Value, DateTime.UtcNow, tenant.BaseCurrency);
                if (conversion == null)
                    return ServiceResult<PricingPackageDto>.Failure("سعر صرف إحدى خدمات الباقة غير متوفر", "EXCHANGE_RATE_NOT_FOUND");
                package.PackageServices.Add(new PackageService { ServiceID = serviceId, Quantity = 1, UnitPrice = conversion.TargetAmount });
                pricingSnapshots.Add(new { ServiceID = serviceId, SourceAmount = svc.DefaultPrice.Value,
                    SourceCurrency = svc.DefaultCurrencyCode, BillingCurrency = dto.CurrencyCode,
                    conversion.ExchangeRate, conversion.RateDate, conversion.Source, conversion.TargetAmount });
            }
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                await _unitOfWork.PricingPackages.AddAsync(package);
                await _unitOfWork.SaveChangesAsync();
                if (pricingSnapshots.Count > 0)
                {
                    await _unitOfWork.AuditLogs.AddAsync(new AuditLog { TenantID = tenantId, UserId = _unitOfWork.CurrentUserId, TableName = "PackageServicePricing",
                        RecordID = package.PackageID.ToString(), Action = "PriceSnapshot", ActionAt = DateTime.UtcNow,
                        NewValues = System.Text.Json.JsonSerializer.Serialize(pricingSnapshots) });
                    await _unitOfWork.SaveChangesAsync();
                }
                await _unitOfWork.CommitTransactionAsync();
            }
            catch (DbUpdateException)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return ServiceResult<PricingPackageDto>.Failure("تعذر حفظ الباقة كاملة؛ لم يتم اعتماد أي جزء منها", "PACKAGE_SAVE_FAILED");
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
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
