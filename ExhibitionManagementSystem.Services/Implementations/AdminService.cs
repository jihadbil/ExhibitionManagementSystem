using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using ExhibitionManagementSystem.DataAccess.Repositories.Interfaces;
using ExhibitionManagementSystem.Models;
using ExhibitionManagementSystem.Models.Enums;
using ExhibitionManagementSystem.Models.DTOs.Admin;
using ExhibitionManagementSystem.Models.DTOs.Common;
using ExhibitionManagementSystem.Services.Common;
using ExhibitionManagementSystem.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ExhibitionManagementSystem.Services.Implementations
{
    public class AdminService : IAdminService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public AdminService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<ServiceResult<PagedResultDto<AuditLogDto>>> GetAuditLogsAsync(int tenantId, int page, int pageSize)
        {
            var query = _unitOfWork.AuditLogs.AsQueryable()
                .Include(a => a.User)
                .Where(a => a.TenantID == tenantId);

            var totalCount = await query.CountAsync();
            var items = await query
                .OrderByDescending(a => a.ActionAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var dtos = _mapper.Map<IList<AuditLogDto>>(items);
            
            var result = new PagedResultDto<AuditLogDto>
            {
                Items = dtos.ToList(),
                TotalCount = totalCount,
                PageNumber = page,
                PageSize = pageSize
            };

            return ServiceResult<PagedResultDto<AuditLogDto>>.Success(result);
        }

        public async Task<ServiceResult<IList<AuditLogDto>>> GetAuditLogsByEntityAsync(int tenantId, string tableName, string recordId)
        {
            var logs = await _unitOfWork.AuditLogs.AsQueryable()
                .Include(a => a.User)
                .Where(a => a.TenantID == tenantId && 
                            a.TableName == tableName && 
                            a.RecordID == recordId)
                .OrderByDescending(a => a.ActionAt)
                .ToListAsync();

            var dtos = _mapper.Map<IList<AuditLogDto>>(logs);
            return ServiceResult<IList<AuditLogDto>>.Success(dtos);
        }

        public async Task<ServiceResult<IList<TenantSubscriptionDto>>> GetSubscriptionHistoryAsync(int tenantId)
        {
            var subs = await _unitOfWork.TenantSubscriptions.AsQueryable()
                .Include(s => s.Tenant)
                .Where(s => s.TenantID == tenantId)
                .OrderByDescending(s => s.StartDate)
                .ToListAsync();

            var dtos = _mapper.Map<IList<TenantSubscriptionDto>>(subs);
            return ServiceResult<IList<TenantSubscriptionDto>>.Success(dtos);
        }

        public async Task<ServiceResult<TenantSubscriptionDto>> CreateSubscriptionAsync(int tenantId, TenantSubscriptionDto dto)
        {
            var tenant = await _unitOfWork.Tenants.GetByIdAsync(tenantId);
            if (tenant == null)
            {
                return ServiceResult<TenantSubscriptionDto>.Failure("المستأجر غير موجود", "TENANT_NOT_FOUND");
            }

            if (!Enum.TryParse<SubscriptionStatus>(dto.Status, true, out var status))
            {
                status = SubscriptionStatus.Active;
            }

            var sub = new TenantSubscription
            {
                TenantID = tenantId,
                Plan = string.IsNullOrWhiteSpace(dto.PlanName) ? tenant.Plan : dto.PlanName,
                StartDate = dto.StartDate == default ? DateTime.UtcNow.Date : dto.StartDate.Date,
                EndDate = dto.EndDate ?? DateTime.UtcNow.AddMonths(1).Date,
                MonthlyFee = dto.Price,
                CurrencyCode = string.IsNullOrWhiteSpace(dto.CurrencyCode) ? "USD" : dto.CurrencyCode,
                Status = status,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.TenantSubscriptions.AddAsync(sub);
            
            // Sync current plan on the Tenant profile
            tenant.Plan = sub.Plan;
            _unitOfWork.Tenants.Update(tenant);
            
            await _unitOfWork.SaveChangesAsync();

            var fullSub = await _unitOfWork.TenantSubscriptions.AsQueryable()
                .Include(s => s.Tenant)
                .FirstOrDefaultAsync(s => s.SubID == sub.SubID);

            var resultDto = _mapper.Map<TenantSubscriptionDto>(fullSub ?? sub);
            return ServiceResult<TenantSubscriptionDto>.Success(resultDto);
        }
    }
}
