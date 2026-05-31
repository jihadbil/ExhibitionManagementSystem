using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using ExhibitionManagementSystem.DataAccess.Repositories.Interfaces;
using ExhibitionManagementSystem.Models;
using ExhibitionManagementSystem.Models.Enums;
using ExhibitionManagementSystem.Models.DTOs.Exhibition;
using ExhibitionManagementSystem.Models.DTOs.Common;
using ExhibitionManagementSystem.Services.Common;
using ExhibitionManagementSystem.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ExhibitionManagementSystem.Services.Implementations
{
    public class ExhibitionService : IExhibitionService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public ExhibitionService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<ServiceResult<PagedResultDto<ExhibitionSummaryDto>>> GetByTenantAsync(int tenantId, int page, int pageSize)
        {
            var (items, totalCount) = await _unitOfWork.Exhibitions.GetPagedAsync(
                page, 
                pageSize, 
                e => e.TenantID == tenantId,
                e => e.Name,
                false);

            var dtos = _mapper.Map<List<ExhibitionSummaryDto>>(items);

            var result = new PagedResultDto<ExhibitionSummaryDto>
            {
                Items = dtos,
                TotalCount = totalCount,
                PageNumber = page,
                PageSize = pageSize
            };

            return ServiceResult<PagedResultDto<ExhibitionSummaryDto>>.Success(result);
        }

        public async Task<ServiceResult<ExhibitionDto>> GetByIdAsync(int tenantId, int exhibitionId)
        {
            var exhibition = await _unitOfWork.Exhibitions.GetWithVenueAndSchedulesAsync(exhibitionId);
            if (exhibition == null || exhibition.TenantID != tenantId)
            {
                return ServiceResult<ExhibitionDto>.Failure("المعرض غير موجود", "EXHIBITION_NOT_FOUND");
            }

            var dto = _mapper.Map<ExhibitionDto>(exhibition);
            return ServiceResult<ExhibitionDto>.Success(dto);
        }

        public async Task<ServiceResult<IList<ExhibitionSummaryDto>>> GetActiveAsync(int tenantId)
        {
            var exhibitions = await _unitOfWork.Exhibitions.GetActiveExhibitionsAsync(tenantId);
            var dtos = _mapper.Map<IList<ExhibitionSummaryDto>>(exhibitions);
            return ServiceResult<IList<ExhibitionSummaryDto>>.Success(dtos);
        }

        public async Task<ServiceResult<IList<ExhibitionSummaryDto>>> GetUpcomingAsync(int tenantId, int count)
        {
            var exhibitions = await _unitOfWork.Exhibitions.GetUpcomingExhibitionsAsync(tenantId, count);
            var dtos = _mapper.Map<IList<ExhibitionSummaryDto>>(exhibitions);
            return ServiceResult<IList<ExhibitionSummaryDto>>.Success(dtos);
        }

        public async Task<ServiceResult<ExhibitionDto>> CreateAsync(int tenantId, ExhibitionCreateDto dto)
        {
            var venue = await _unitOfWork.Venues.GetByIdAsync(dto.VenueID);
            if (venue == null || venue.TenantID != tenantId)
            {
                return ServiceResult<ExhibitionDto>.Failure("مكان الفعالية غير موجود", "VENUE_NOT_FOUND");
            }

            var exhibition = _mapper.Map<Exhibition>(dto);
            exhibition.TenantID = tenantId;
            exhibition.Status = ExhibitionStatus.Planning;

            await _unitOfWork.Exhibitions.AddAsync(exhibition);
            await _unitOfWork.SaveChangesAsync();

            var resultDto = _mapper.Map<ExhibitionDto>(exhibition);
            return ServiceResult<ExhibitionDto>.Success(resultDto);
        }

        public async Task<ServiceResult<ExhibitionDto>> UpdateAsync(int tenantId, int id, ExhibitionUpdateDto dto)
        {
            var exhibition = await _unitOfWork.Exhibitions.GetByIdAsync(id);
            if (exhibition == null || exhibition.TenantID != tenantId)
            {
                return ServiceResult<ExhibitionDto>.Failure("المعرض غير موجود", "EXHIBITION_NOT_FOUND");
            }

            _mapper.Map(dto, exhibition);
            exhibition.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Exhibitions.Update(exhibition);
            await _unitOfWork.SaveChangesAsync();

            var resultDto = _mapper.Map<ExhibitionDto>(exhibition);
            return ServiceResult<ExhibitionDto>.Success(resultDto);
        }

        public async Task<ServiceResult<ExhibitionDto>> ChangeStatusAsync(int tenantId, int id, string status)
        {
            var exhibition = await _unitOfWork.Exhibitions.GetByIdAsync(id);
            if (exhibition == null || exhibition.TenantID != tenantId)
            {
                return ServiceResult<ExhibitionDto>.Failure("المعرض غير موجود", "EXHIBITION_NOT_FOUND");
            }

            if (!Enum.TryParse<ExhibitionStatus>(status, true, out var newStatus))
            {
                return ServiceResult<ExhibitionDto>.Failure("حالة المعرض غير صالحة", "INVALID_STATUS");
            }

            if (newStatus == ExhibitionStatus.Open)
            {
                if (exhibition.StartDate.Date < DateTime.UtcNow.Date)
                {
                    return ServiceResult<ExhibitionDto>.Failure("لا يمكن فتح المعرض لأن تاريخ البدء في الماضي", "INVALID_START_DATE");
                }
            }
            else if (newStatus == ExhibitionStatus.Closed)
            {
                var reservations = await _unitOfWork.BoothReservations.FindAsync(r => r.ExhibitionID == id && r.Status == ReservationStatus.Confirmed);
                if (reservations == null || reservations.Count == 0)
                {
                    return ServiceResult<ExhibitionDto>.Failure("لا يمكن إغلاق المعرض لعدم وجود حجوزات مؤكدة", "NO_CONFIRMED_RESERVATIONS");
                }
            }

            exhibition.Status = newStatus;
            exhibition.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Exhibitions.Update(exhibition);
            await _unitOfWork.SaveChangesAsync();

            var resultDto = _mapper.Map<ExhibitionDto>(exhibition);
            return ServiceResult<ExhibitionDto>.Success(resultDto);
        }

        public async Task<ServiceResult> DeleteAsync(int tenantId, int id)
        {
            var exhibition = await _unitOfWork.Exhibitions.GetByIdAsync(id);
            if (exhibition == null || exhibition.TenantID != tenantId)
            {
                return ServiceResult.Failure("المعرض غير موجود", "EXHIBITION_NOT_FOUND");
            }

            await _unitOfWork.Exhibitions.SoftDeleteAsync(id, "System");
            await _unitOfWork.SaveChangesAsync();

            return ServiceResult.Success();
        }

        // Schedules
        public async Task<ServiceResult<IList<ExhibitionScheduleDto>>> GetSchedulesAsync(int tenantId, int exhibitionId)
        {
            var exhibition = await _unitOfWork.Exhibitions.GetByIdAsync(exhibitionId);
            if (exhibition == null || exhibition.TenantID != tenantId)
            {
                return ServiceResult<IList<ExhibitionScheduleDto>>.Failure("المعرض غير موجود", "EXHIBITION_NOT_FOUND");
            }

            var schedules = await _unitOfWork.ExhibitionSchedules.GetByExhibitionAsync(exhibitionId);
            var dtos = _mapper.Map<IList<ExhibitionScheduleDto>>(schedules);
            return ServiceResult<IList<ExhibitionScheduleDto>>.Success(dtos);
        }

        public async Task<ServiceResult<ExhibitionScheduleDto>> AddScheduleAsync(int tenantId, ExhibitionScheduleCreateDto dto)
        {
            var exhibition = await _unitOfWork.Exhibitions.GetByIdAsync(dto.ExhibitionID);
            if (exhibition == null || exhibition.TenantID != tenantId)
            {
                return ServiceResult<ExhibitionScheduleDto>.Failure("المعرض غير موجود", "EXHIBITION_NOT_FOUND");
            }

            if (dto.HallID.HasValue)
            {
                var hall = await _unitOfWork.Halls.GetByIdWithIncludesAsync(dto.HallID.Value, h => h.Venue);
                if (hall == null || hall.Venue == null || hall.Venue.TenantID != tenantId)
                {
                    return ServiceResult<ExhibitionScheduleDto>.Failure("القاعة غير موجودة", "HALL_NOT_FOUND");
                }
            }

            var schedule = _mapper.Map<ExhibitionSchedule>(dto);
            await _unitOfWork.ExhibitionSchedules.AddAsync(schedule);
            await _unitOfWork.SaveChangesAsync();

            var createdSchedule = await _unitOfWork.ExhibitionSchedules.GetByIdWithIncludesAsync(schedule.ScheduleID, s => s.Hall);
            var resultDto = _mapper.Map<ExhibitionScheduleDto>(createdSchedule ?? schedule);

            return ServiceResult<ExhibitionScheduleDto>.Success(resultDto);
        }

        public async Task<ServiceResult> RemoveScheduleAsync(int tenantId, int scheduleId)
        {
            var schedule = await _unitOfWork.ExhibitionSchedules.AsQueryable()
                .Include(s => s.Exhibition)
                .FirstOrDefaultAsync(s => s.ScheduleID == scheduleId);

            if (schedule == null || schedule.Exhibition == null || schedule.Exhibition.TenantID != tenantId)
            {
                return ServiceResult.Failure("الجدول الزمني غير موجود", "SCHEDULE_NOT_FOUND");
            }

            _unitOfWork.ExhibitionSchedules.Remove(schedule);
            await _unitOfWork.SaveChangesAsync();

            return ServiceResult.Success();
        }
    }
}
