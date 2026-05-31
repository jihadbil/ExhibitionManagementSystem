using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using ExhibitionManagementSystem.DataAccess.Repositories.Interfaces;
using ExhibitionManagementSystem.Models;
using ExhibitionManagementSystem.Models.DTOs.Exhibitor;
using ExhibitionManagementSystem.Models.DTOs.Reservation;
using ExhibitionManagementSystem.Models.DTOs.Common;
using ExhibitionManagementSystem.Services.Common;
using ExhibitionManagementSystem.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ExhibitionManagementSystem.Services.Implementations
{
    public class ExhibitorService : IExhibitorService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public ExhibitorService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<ServiceResult<PagedResultDto<ExhibitorSummaryDto>>> GetByTenantAsync(int tenantId, int page, int pageSize)
        {
            var (items, totalCount) = await _unitOfWork.Exhibitors.GetPagedAsync(
                page,
                pageSize,
                e => e.TenantID == tenantId,
                e => e.CompanyName,
                false);

            var dtos = _mapper.Map<List<ExhibitorSummaryDto>>(items);

            var result = new PagedResultDto<ExhibitorSummaryDto>
            {
                Items = dtos,
                TotalCount = totalCount,
                PageNumber = page,
                PageSize = pageSize
            };

            return ServiceResult<PagedResultDto<ExhibitorSummaryDto>>.Success(result);
        }

        public async Task<ServiceResult<IList<ExhibitorSummaryDto>>> SearchAsync(int tenantId, string term)
        {
            var exhibitors = await _unitOfWork.Exhibitors.SearchAsync(tenantId, term);
            var dtos = _mapper.Map<IList<ExhibitorSummaryDto>>(exhibitors);
            return ServiceResult<IList<ExhibitorSummaryDto>>.Success(dtos);
        }

        public async Task<ServiceResult<ExhibitorDto>> GetByIdAsync(int tenantId, int exhibitorId)
        {
            var exhibitor = await _unitOfWork.Exhibitors.GetByIdAsync(exhibitorId);
            if (exhibitor == null || exhibitor.TenantID != tenantId)
            {
                return ServiceResult<ExhibitorDto>.Failure("العارض غير موجود", "EXHIBITOR_NOT_FOUND");
            }

            var dto = _mapper.Map<ExhibitorDto>(exhibitor);
            return ServiceResult<ExhibitorDto>.Success(dto);
        }

        public async Task<ServiceResult<ExhibitorDto>> GetByUserIdAsync(int tenantId, string userId)
        {
            var exhibitor = await _unitOfWork.Exhibitors.GetByUserIdAsync(userId);
            if (exhibitor == null || exhibitor.TenantID != tenantId)
            {
                return ServiceResult<ExhibitorDto>.Failure("العارض غير موجود للمستخدم المحدد", "EXHIBITOR_NOT_FOUND");
            }

            var dto = _mapper.Map<ExhibitorDto>(exhibitor);
            return ServiceResult<ExhibitorDto>.Success(dto);
        }

        public async Task<ServiceResult<ExhibitorDto>> CreateAsync(int tenantId, ExhibitorCreateDto dto)
        {
            var exists = await _unitOfWork.Exhibitors.ExistsAsync(e => e.TenantID == tenantId && e.Email == dto.Email);
            if (exists)
            {
                return ServiceResult<ExhibitorDto>.Failure("البريد الإلكتروني للعارض مستخدم بالفعل", "EMAIL_ALREADY_EXISTS");
            }

            var exhibitor = _mapper.Map<Exhibitor>(dto);
            exhibitor.TenantID = tenantId;

            await _unitOfWork.Exhibitors.AddAsync(exhibitor);
            await _unitOfWork.SaveChangesAsync();

            var resultDto = _mapper.Map<ExhibitorDto>(exhibitor);
            return ServiceResult<ExhibitorDto>.Success(resultDto);
        }

        public async Task<ServiceResult<ExhibitorDto>> UpdateAsync(int tenantId, int id, ExhibitorUpdateDto dto)
        {
            var exhibitor = await _unitOfWork.Exhibitors.GetByIdAsync(id);
            if (exhibitor == null || exhibitor.TenantID != tenantId)
            {
                return ServiceResult<ExhibitorDto>.Failure("العارض غير موجود", "EXHIBITOR_NOT_FOUND");
            }

            var emailExists = await _unitOfWork.Exhibitors.ExistsAsync(e => e.TenantID == tenantId && e.Email == dto.Email && e.ExhibitorID != id);
            if (emailExists)
            {
                return ServiceResult<ExhibitorDto>.Failure("البريد الإلكتروني للعارض مستخدم بالفعل", "EMAIL_ALREADY_EXISTS");
            }

            _mapper.Map(dto, exhibitor);
            exhibitor.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Exhibitors.Update(exhibitor);
            await _unitOfWork.SaveChangesAsync();

            var resultDto = _mapper.Map<ExhibitorDto>(exhibitor);
            return ServiceResult<ExhibitorDto>.Success(resultDto);
        }

        public async Task<ServiceResult> DeleteAsync(int tenantId, int id)
        {
            var exhibitor = await _unitOfWork.Exhibitors.GetByIdAsync(id);
            if (exhibitor == null || exhibitor.TenantID != tenantId)
            {
                return ServiceResult.Failure("العارض غير موجود", "EXHIBITOR_NOT_FOUND");
            }

            await _unitOfWork.Exhibitors.SoftDeleteAsync(id, "System");
            await _unitOfWork.SaveChangesAsync();

            return ServiceResult.Success();
        }

        public async Task<ServiceResult<IList<BoothReservationSummaryDto>>> GetReservationsAsync(int tenantId, int exhibitorId)
        {
            var exhibitor = await _unitOfWork.Exhibitors.GetByIdAsync(exhibitorId);
            if (exhibitor == null || exhibitor.TenantID != tenantId)
            {
                return ServiceResult<IList<BoothReservationSummaryDto>>.Failure("العارض غير موجود", "EXHIBITOR_NOT_FOUND");
            }

            var reservations = await _unitOfWork.BoothReservations.AsQueryable()
                .Include(r => r.Booth)
                .Include(r => r.Exhibition)
                .Include(r => r.Exhibitor)
                .Where(r => r.ExhibitorID == exhibitorId)
                .ToListAsync();

            var dtos = _mapper.Map<IList<BoothReservationSummaryDto>>(reservations);
            return ServiceResult<IList<BoothReservationSummaryDto>>.Success(dtos);
        }
    }
}
