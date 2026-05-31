using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using ExhibitionManagementSystem.DataAccess.Repositories.Interfaces;
using ExhibitionManagementSystem.Models;
using ExhibitionManagementSystem.Models.DTOs.Admin;
using ExhibitionManagementSystem.Models.DTOs.Common;
using ExhibitionManagementSystem.Models.DTOs.Tenant;
using ExhibitionManagementSystem.Services.Common;
using ExhibitionManagementSystem.Services.Interfaces;

namespace ExhibitionManagementSystem.Services.Implementations
{
    public class TenantService : ITenantService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public TenantService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<ServiceResult<PagedResultDto<TenantDto>>> GetAllAsync(int page, int pageSize)
        {
            var (items, totalCount) = await _unitOfWork.Tenants.GetPagedAsync(page, pageSize);
            
            var dtos = _mapper.Map<List<TenantDto>>(items);
            
            var result = new PagedResultDto<TenantDto>
            {
                Items = dtos,
                TotalCount = totalCount,
                PageNumber = page,
                PageSize = pageSize
            };

            return ServiceResult<PagedResultDto<TenantDto>>.Success(result);
        }

        public async Task<ServiceResult<TenantDto>> GetByIdAsync(int tenantId)
        {
            var tenant = await _unitOfWork.Tenants.GetByIdAsync(tenantId);
            if (tenant == null)
            {
                return ServiceResult<TenantDto>.Failure("المستأجر غير موجود", "TENANT_NOT_FOUND");
            }

            var dto = _mapper.Map<TenantDto>(tenant);
            return ServiceResult<TenantDto>.Success(dto);
        }

        public async Task<ServiceResult<TenantDto>> CreateAsync(TenantCreateDto dto)
        {
            var isUnique = await _unitOfWork.Tenants.IsSubdomainUniqueAsync(dto.Subdomain);
            if (!isUnique)
            {
                return ServiceResult<TenantDto>.Failure("النطاق الفرعي مستخدم بالفعل", "SUBDOMAIN_ALREADY_EXISTS");
            }

            var tenant = _mapper.Map<Tenant>(dto);
            await _unitOfWork.Tenants.AddAsync(tenant);
            await _unitOfWork.SaveChangesAsync();

            // Re-fetch to load currency navigation property for mapping Symbol
            var createdTenant = await _unitOfWork.Tenants.GetByIdAsync(tenant.TenantID);
            var resultDto = _mapper.Map<TenantDto>(createdTenant ?? tenant);

            return ServiceResult<TenantDto>.Success(resultDto);
        }

        public async Task<ServiceResult<TenantDto>> UpdateAsync(int tenantId, TenantUpdateDto dto)
        {
            var tenant = await _unitOfWork.Tenants.GetByIdAsync(tenantId);
            if (tenant == null)
            {
                return ServiceResult<TenantDto>.Failure("المستأجر غير موجود", "TENANT_NOT_FOUND");
            }

            var isUnique = await _unitOfWork.Tenants.IsSubdomainUniqueAsync(dto.Subdomain, tenantId);
            if (!isUnique)
            {
                return ServiceResult<TenantDto>.Failure("النطاق الفرعي مستخدم بالفعل", "SUBDOMAIN_ALREADY_EXISTS");
            }

            _mapper.Map(dto, tenant);
            tenant.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Tenants.Update(tenant);
            await _unitOfWork.SaveChangesAsync();

            var resultDto = _mapper.Map<TenantDto>(tenant);
            return ServiceResult<TenantDto>.Success(resultDto);
        }

        public async Task<ServiceResult> DeleteAsync(int tenantId)
        {
            var tenant = await _unitOfWork.Tenants.GetByIdAsync(tenantId);
            if (tenant == null)
            {
                return ServiceResult.Failure("المستأجر غير موجود", "TENANT_NOT_FOUND");
            }

            // Using "System" as the default deleting user for service layer
            await _unitOfWork.Tenants.SoftDeleteAsync(tenantId, "System");
            await _unitOfWork.SaveChangesAsync();

            return ServiceResult.Success();
        }

        public async Task<ServiceResult<TenantSubscriptionDto>> GetActiveSubscriptionAsync(int tenantId)
        {
            var tenant = await _unitOfWork.Tenants.GetWithActiveSubscriptionAsync(tenantId);
            if (tenant == null)
            {
                return ServiceResult<TenantSubscriptionDto>.Failure("لا يوجد اشتراك نشط لهذا المستأجر", "ACTIVE_SUBSCRIPTION_NOT_FOUND");
            }

            var today = DateTime.UtcNow.Date;
            var activeSub = tenant.TenantSubscriptions
                .FirstOrDefault(s => (s.Status == Models.Enums.SubscriptionStatus.Active || s.Status == Models.Enums.SubscriptionStatus.Trial) &&
                                     s.StartDate.Date <= today &&
                                     s.EndDate.Date >= today);

            if (activeSub == null)
            {
                return ServiceResult<TenantSubscriptionDto>.Failure("لا يوجد اشتراك نشط لهذا المستأجر", "ACTIVE_SUBSCRIPTION_NOT_FOUND");
            }

            var dto = _mapper.Map<TenantSubscriptionDto>(activeSub);
            return ServiceResult<TenantSubscriptionDto>.Success(dto);
        }
    }
}
