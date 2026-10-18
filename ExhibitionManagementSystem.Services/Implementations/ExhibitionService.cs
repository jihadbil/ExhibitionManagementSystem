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
            var query = _unitOfWork.Exhibitions.AsQueryable()
                .AsNoTracking()
                .Include(e => e.Venue)
                .Where(e => e.TenantID == tenantId);

            var totalCount = await query.CountAsync();
            var items = await query
                .OrderBy(e => e.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

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
            await _unitOfWork.BeginSerializableTransactionAsync();
            try
            {
                var result = await CreateCoreAsync(tenantId, dto);
                if (result.IsSuccess) await _unitOfWork.CommitTransactionAsync();
                return result;
            }
            catch (DbUpdateException)
            { return ServiceResult<ExhibitionDto>.Failure("تعارض إنشاء المعرض مع تعديل الموقع", "VENUE_CONFLICT"); }
            catch (Exception ex) when (DatabaseConflict.IsLockConflict(ex))
            { return ServiceResult<ExhibitionDto>.Failure("تعارض إنشاء المعرض مع تعديل الموقع", "VENUE_CONFLICT"); }
            finally { await _unitOfWork.RollbackTransactionAsync(); }
        }

        private async Task<ServiceResult<ExhibitionDto>> CreateCoreAsync(int tenantId, ExhibitionCreateDto dto)
        {
            if (dto.StartDate == default || dto.EndDate == default || dto.EndDate < dto.StartDate || dto.EntryFee < 0 || dto.EntryFee > 9999999999999999.99m || dto.ExpectedVisitors < 0)
                return ServiceResult<ExhibitionDto>.Failure("تواريخ المعرض أو رسوم الدخول أو عدد الزوار غير صالحة", "INVALID_EXHIBITION_INPUT");
            var venue = await _unitOfWork.Venues.GetByIdAsync(dto.VenueID);
            if (venue == null || venue.TenantID != tenantId)
            {
                return ServiceResult<ExhibitionDto>.Failure("مكان الفعالية غير موجود", "VENUE_NOT_FOUND");
            }

            var exhibition = _mapper.Map<Exhibition>(dto);
            exhibition.TenantID = tenantId;
            exhibition.Status = ExhibitionStatus.Planning;

            if (string.IsNullOrWhiteSpace(exhibition.EntryCurrency))
            {
                var tenant = await _unitOfWork.Tenants.GetByIdAsync(tenantId);
                exhibition.EntryCurrency = tenant?.BaseCurrency ?? "USD";
            }

            exhibition.EntryCurrency = exhibition.EntryCurrency.Trim().ToUpperInvariant();
            if (!await _unitOfWork.Currencies.ExistsAsync(c => c.CurrencyCode == exhibition.EntryCurrency && c.IsActive))
                return ServiceResult<ExhibitionDto>.Failure("عملة الدخول غير موجودة أو غير نشطة", "INVALID_CURRENCY");

            await _unitOfWork.Exhibitions.AddAsync(exhibition);
            await _unitOfWork.SaveChangesAsync();

            var resultDto = _mapper.Map<ExhibitionDto>(exhibition);
            return ServiceResult<ExhibitionDto>.Success(resultDto);
        }

        public async Task<ServiceResult<ExhibitionDto>> UpdateAsync(int tenantId, int id, ExhibitionUpdateDto dto)
        {
            await _unitOfWork.BeginSerializableTransactionAsync();
            try
            {
                var result = await UpdateCoreAsync(tenantId, id, dto);
                if (result.IsSuccess) await _unitOfWork.CommitTransactionAsync();
                return result;
            }
            catch (Exception ex) when (DatabaseConflict.IsLockConflict(ex))
            { return ServiceResult<ExhibitionDto>.Failure("تعارض تعديل المعرض مع تعديل آخر", "EXHIBITION_DATES_LOCKED"); }
            finally { await _unitOfWork.RollbackTransactionAsync(); }
        }

        private async Task<ServiceResult<ExhibitionDto>> UpdateCoreAsync(int tenantId, int id, ExhibitionUpdateDto dto)
        {
            if (dto.StartDate == default || dto.EndDate == default || dto.EndDate < dto.StartDate || dto.EntryFee < 0 || dto.EntryFee > 9999999999999999.99m || dto.ExpectedVisitors < 0 || dto.VenueID <= 0)
                return ServiceResult<ExhibitionDto>.Failure("تواريخ المعرض أو رسوم الدخول أو عدد الزوار غير صالحة", "INVALID_EXHIBITION_INPUT");
            var normalizedStatus = dto.Status?.Trim().ToLowerInvariant() switch
            {
                "active" => "Open", "ended" => "Closed", "canceled" => "Cancelled", "draft" => "Planning", _ => dto.Status
            };
            if (!Enum.TryParse<ExhibitionStatus>(normalizedStatus, true, out var requestedStatus) || !Enum.IsDefined(requestedStatus))
                return ServiceResult<ExhibitionDto>.Failure("حالة المعرض غير صالحة", "INVALID_STATUS");
            dto.Status = requestedStatus.ToString();
            if (requestedStatus == ExhibitionStatus.Open && (dto.StartDate.Date > DateTime.UtcNow.Date || dto.EndDate.Date < DateTime.UtcNow.Date))
                return ServiceResult<ExhibitionDto>.Failure("المعرض خارج فترة انعقاده ولا يمكن فتحه", "EXHIBITION_OUTSIDE_DATES");
            if (!string.IsNullOrWhiteSpace(dto.EntryCurrency))
            {
                dto.EntryCurrency = dto.EntryCurrency.Trim().ToUpperInvariant();
                if (!await _unitOfWork.Currencies.ExistsAsync(c => c.CurrencyCode == dto.EntryCurrency && c.IsActive))
                    return ServiceResult<ExhibitionDto>.Failure("عملة الدخول غير موجودة أو غير نشطة", "INVALID_CURRENCY");
            }
            var exhibition = await _unitOfWork.Exhibitions.GetByIdAsync(id);
            if (exhibition == null || exhibition.TenantID != tenantId)
            {
                return ServiceResult<ExhibitionDto>.Failure("المعرض غير موجود", "EXHIBITION_NOT_FOUND");
            }

            if ((dto.StartDate.Date != exhibition.StartDate.Date || dto.EndDate.Date != exhibition.EndDate.Date) &&
                (await _unitOfWork.ExhibitionSchedules.ExistsAsync(s => s.ExhibitionID == id &&
                    (s.StartDateTime.Date < dto.StartDate.Date || s.EndDateTime.Date > dto.EndDate.Date)) ||
                 await _unitOfWork.Tickets.ExistsAsync(t => t.ExhibitionID == id && t.ValidDate.HasValue &&
                    (t.ValidDate.Value.Date < dto.StartDate.Date || t.ValidDate.Value.Date > dto.EndDate.Date))))
                return ServiceResult<ExhibitionDto>.Failure("الفترة الجديدة تستبعد فعاليات أو أيام تذاكر قائمة", "EXHIBITION_DATES_LOCKED");

            if (dto.VenueID.HasValue && dto.VenueID.Value > 0 && dto.VenueID.Value != exhibition.VenueID)
            {
                if (await _unitOfWork.BoothReservations.AsQueryable().IgnoreQueryFilters().AnyAsync(r => r.TenantID == tenantId && r.ExhibitionID == id)
                    || await _unitOfWork.ExhibitionSchedules.ExistsAsync(s => s.ExhibitionID == id)
                    || await _unitOfWork.BoothMerges.ExistsAsync(m => m.ExhibitionID == id)
                    || await _unitOfWork.AdvertisingSpaces.AsQueryable().IgnoreQueryFilters()
                        .AnyAsync(s => s.TenantID == tenantId && s.ExhibitionID == id))
                    return ServiceResult<ExhibitionDto>.Failure("لا يمكن تغيير موقع المعرض لوجود حجوزات أو فعاليات أو دمج أو مساحات إعلانية مرتبطة به", "EXHIBITION_VENUE_LOCKED");
                var venueExists = await _unitOfWork.Venues.ExistsAsync(v => v.VenueID == dto.VenueID.Value && v.TenantID == tenantId);
                if (!venueExists)
                {
                    return ServiceResult<ExhibitionDto>.Failure("مكان الفعالية غير موجود", "VENUE_NOT_FOUND");
                }
                exhibition.VenueID = dto.VenueID.Value;
            }

            _mapper.Map(dto, exhibition);
            exhibition.UpdatedAt = DateTime.UtcNow;

            if (string.IsNullOrWhiteSpace(exhibition.EntryCurrency))
            {
                var tenant = await _unitOfWork.Tenants.GetByIdAsync(tenantId);
                exhibition.EntryCurrency = tenant?.BaseCurrency ?? "USD";
            }

            _unitOfWork.Exhibitions.Update(exhibition);
            await _unitOfWork.SaveChangesAsync();

            var updatedExhibition = await _unitOfWork.Exhibitions.GetByIdAsync(id);
            var resultDto = _mapper.Map<ExhibitionDto>(updatedExhibition ?? exhibition);
            return ServiceResult<ExhibitionDto>.Success(resultDto);
        }

        public async Task<ServiceResult<ExhibitionDto>> ChangeStatusAsync(int tenantId, int id, string status)
        {
            var exhibition = await _unitOfWork.Exhibitions.GetByIdAsync(id);
            if (exhibition == null || exhibition.TenantID != tenantId)
            {
                return ServiceResult<ExhibitionDto>.Failure("المعرض غير موجود", "EXHIBITION_NOT_FOUND");
            }

            if (!Enum.TryParse<ExhibitionStatus>(status, true, out var newStatus) || !Enum.IsDefined(newStatus))
            {
                return ServiceResult<ExhibitionDto>.Failure("حالة المعرض غير صالحة", "INVALID_STATUS");
            }

            if (newStatus == ExhibitionStatus.Open)
            {
                if (exhibition.StartDate.Date > DateTime.UtcNow.Date || exhibition.EndDate.Date < DateTime.UtcNow.Date)
                {
                    return ServiceResult<ExhibitionDto>.Failure("لا يمكن فتح المعرض قبل تاريخ بدئه", "EXHIBITION_NOT_STARTED_YET");
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
            await _unitOfWork.BeginSerializableTransactionAsync();
            try
            {
                var result = await DeleteCoreAsync(tenantId, id);
                if (result.IsSuccess) await _unitOfWork.CommitTransactionAsync();
                return result;
            }
            catch (DbUpdateException)
            { return ServiceResult.Failure("تعارض حذف المعرض مع عملية أخرى", "EXHIBITION_DELETE_CONFLICT"); }
            catch (Exception ex) when (DatabaseConflict.IsLockConflict(ex))
            { return ServiceResult.Failure("تعارض حذف المعرض مع عملية أخرى", "EXHIBITION_DELETE_CONFLICT"); }
            finally { await _unitOfWork.RollbackTransactionAsync(); }
        }

        private async Task<ServiceResult> DeleteCoreAsync(int tenantId, int id)
        {
            var exhibition = await _unitOfWork.Exhibitions.GetByIdAsync(id);
            if (exhibition == null || exhibition.TenantID != tenantId)
            {
                return ServiceResult.Failure("المعرض غير موجود", "EXHIBITION_NOT_FOUND");
            }

            if (await _unitOfWork.BoothReservations.AsQueryable().IgnoreQueryFilters().AnyAsync(r => r.TenantID == tenantId && r.ExhibitionID == id)
                || await _unitOfWork.Tickets.AsQueryable().IgnoreQueryFilters().AnyAsync(t => t.TenantID == tenantId && t.ExhibitionID == id)
                || await _unitOfWork.ExhibitionSchedules.ExistsAsync(s => s.ExhibitionID == id)
                || await _unitOfWork.BoothMerges.ExistsAsync(m => m.ExhibitionID == id)
                || await _unitOfWork.Expenses.ExistsAsync(e => e.TenantID == tenantId && e.ExhibitionID == id)
                || await _unitOfWork.FinancialReports.ExistsAsync(r => r.TenantID == tenantId && r.ExhibitionID == id)
                || await _unitOfWork.SponsorshipContracts.AsQueryable().IgnoreQueryFilters()
                    .AnyAsync(c => c.TenantID == tenantId && c.ExhibitionID == id)
                || await _unitOfWork.SponsorshipPackages.AsQueryable().IgnoreQueryFilters()
                    .AnyAsync(p => p.TenantID == tenantId && p.ExhibitionID == id)
                || await _unitOfWork.AdvertisingSpaces.AsQueryable().IgnoreQueryFilters()
                    .AnyAsync(s => s.TenantID == tenantId && s.ExhibitionID == id)
                || await _unitOfWork.BadgeTemplates.AsQueryable().IgnoreQueryFilters()
                    .AnyAsync(t => t.TenantID == tenantId && t.ExhibitionID == id))
                return ServiceResult.Failure("لا يمكن حذف المعرض لوجود حجوزات أو تذاكر أو بيانات مرتبطة به", "EXHIBITION_HAS_DEPENDENCIES");

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
            => await RunScheduleMutationAsync(() => AddScheduleCoreAsync(tenantId, dto));

        private async Task<ServiceResult<ExhibitionScheduleDto>> AddScheduleCoreAsync(int tenantId, ExhibitionScheduleCreateDto dto)
        {
            dto.EventType = string.IsNullOrWhiteSpace(dto.EventType) ? null : dto.EventType.Trim();
            if (dto.StartDateTime == default || dto.EndDateTime <= dto.StartDateTime || dto.MaxAttendees < 0)
                return ServiceResult<ExhibitionScheduleDto>.Failure("تواريخ الفعالية أو السعة غير صالحة", "INVALID_SCHEDULE_INPUT");
            if (!string.IsNullOrWhiteSpace(dto.EventType) &&
                (!Enum.TryParse<EventType>(dto.EventType, true, out var eventType) || !Enum.IsDefined(eventType)))
                return ServiceResult<ExhibitionScheduleDto>.Failure("نوع الفعالية غير صالح", "INVALID_EVENT_TYPE");
            var exhibition = await _unitOfWork.Exhibitions.GetByIdAsync(dto.ExhibitionID);
            if (exhibition == null || exhibition.TenantID != tenantId)
            {
                return ServiceResult<ExhibitionScheduleDto>.Failure("المعرض غير موجود", "EXHIBITION_NOT_FOUND");
            }

            if (exhibition.Status is ExhibitionStatus.Cancelled or ExhibitionStatus.Closed ||
                dto.StartDateTime.Date < exhibition.StartDate.Date || dto.EndDateTime.Date > exhibition.EndDate.Date)
                return ServiceResult<ExhibitionScheduleDto>.Failure("الفعالية خارج فترة المعرض أو المعرض مغلق", "SCHEDULE_OUTSIDE_EXHIBITION");

            if (dto.HallID.HasValue)
            {
                var hall = await _unitOfWork.Halls.GetByIdWithIncludesAsync(dto.HallID.Value, h => h.Venue);
                if (hall == null || hall.Venue == null || hall.Venue.TenantID != tenantId)
                {
                    return ServiceResult<ExhibitionScheduleDto>.Failure("القاعة غير موجودة", "HALL_NOT_FOUND");
                }
                if (hall.VenueID != exhibition.VenueID)
                    return ServiceResult<ExhibitionScheduleDto>.Failure("القاعة لا تتبع موقع المعرض", "HALL_NOT_IN_EXHIBITION_VENUE");
            }

            var schedule = _mapper.Map<ExhibitionSchedule>(dto);
            await _unitOfWork.ExhibitionSchedules.AddAsync(schedule);
            await _unitOfWork.SaveChangesAsync();

            var createdSchedule = await _unitOfWork.ExhibitionSchedules.GetByIdWithIncludesAsync(schedule.ScheduleID, s => s.Hall);
            var resultDto = _mapper.Map<ExhibitionScheduleDto>(createdSchedule ?? schedule);

            return ServiceResult<ExhibitionScheduleDto>.Success(resultDto);
        }

        public async Task<ServiceResult<ExhibitionScheduleDto>> UpdateScheduleAsync(
            int tenantId,
            int exhibitionId,
            int scheduleId,
            ExhibitionScheduleUpdateDto dto)
            => await RunScheduleMutationAsync(() => UpdateScheduleCoreAsync(tenantId, exhibitionId, scheduleId, dto));

        private async Task<ServiceResult<ExhibitionScheduleDto>> UpdateScheduleCoreAsync(
            int tenantId, int exhibitionId, int scheduleId, ExhibitionScheduleUpdateDto dto)
        {
            dto.EventType = string.IsNullOrWhiteSpace(dto.EventType) ? null : dto.EventType.Trim();
            if (dto.StartDateTime == default || dto.EndDateTime <= dto.StartDateTime || dto.MaxAttendees < 0)
                return ServiceResult<ExhibitionScheduleDto>.Failure("تواريخ الفعالية أو السعة غير صالحة", "INVALID_SCHEDULE_INPUT");
            if (!string.IsNullOrWhiteSpace(dto.EventType) &&
                (!Enum.TryParse<EventType>(dto.EventType, true, out var eventType) || !Enum.IsDefined(eventType)))
                return ServiceResult<ExhibitionScheduleDto>.Failure("نوع الفعالية غير صالح", "INVALID_EVENT_TYPE");

            var schedule = await _unitOfWork.ExhibitionSchedules.AsQueryable()
                .Include(candidate => candidate.Exhibition)
                .FirstOrDefaultAsync(candidate =>
                    candidate.ScheduleID == scheduleId &&
                    candidate.ExhibitionID == exhibitionId);
            if (schedule == null || schedule.Exhibition == null || schedule.Exhibition.TenantID != tenantId)
            {
                return ServiceResult<ExhibitionScheduleDto>.Failure("الفعالية غير موجودة", "SCHEDULE_NOT_FOUND");
            }

            if (schedule.Exhibition.Status is ExhibitionStatus.Cancelled or ExhibitionStatus.Closed ||
                dto.StartDateTime.Date < schedule.Exhibition.StartDate.Date ||
                dto.EndDateTime.Date > schedule.Exhibition.EndDate.Date)
            {
                return ServiceResult<ExhibitionScheduleDto>.Failure(
                    "الفعالية خارج فترة المعرض أو المعرض مغلق",
                    "SCHEDULE_OUTSIDE_EXHIBITION");
            }

            if (dto.HallID.HasValue)
            {
                var hall = await _unitOfWork.Halls.GetByIdWithIncludesAsync(dto.HallID.Value, candidate => candidate.Venue);
                if (hall == null || hall.Venue == null || hall.Venue.TenantID != tenantId)
                    return ServiceResult<ExhibitionScheduleDto>.Failure("القاعة غير موجودة", "HALL_NOT_FOUND");
                if (hall.VenueID != schedule.Exhibition.VenueID)
                    return ServiceResult<ExhibitionScheduleDto>.Failure("القاعة لا تتبع موقع المعرض", "HALL_NOT_IN_EXHIBITION_VENUE");
            }

            if (dto.MaxAttendees.HasValue)
            {
                var registrationsCount = await _unitOfWork.ScheduleRegistrations.GetRegistrationCountAsync(scheduleId);
                if (dto.MaxAttendees.Value < registrationsCount)
                {
                    return ServiceResult<ExhibitionScheduleDto>.Failure(
                        "لا يمكن خفض السعة عن عدد المسجلين الحالي في الفعالية",
                        "SCHEDULE_CAPACITY_CONFLICT");
                }
            }

            schedule.HallID = dto.HallID;
            schedule.EventName = dto.EventName.Trim();
            schedule.EventType = string.IsNullOrWhiteSpace(dto.EventType)
                ? null
                : Enum.Parse<EventType>(dto.EventType, true);
            schedule.StartDateTime = dto.StartDateTime;
            schedule.EndDateTime = dto.EndDateTime;
            schedule.SpeakerName = dto.SpeakerName?.Trim() ?? string.Empty;
            schedule.MaxAttendees = dto.MaxAttendees;
            schedule.Description = dto.Description?.Trim() ?? string.Empty;
            schedule.IsPublic = dto.IsPublic;

            _unitOfWork.ExhibitionSchedules.Update(schedule);
            await _unitOfWork.SaveChangesAsync();

            var updatedSchedule = await _unitOfWork.ExhibitionSchedules.GetByIdWithIncludesAsync(
                schedule.ScheduleID,
                candidate => candidate.Hall);
            return ServiceResult<ExhibitionScheduleDto>.Success(
                _mapper.Map<ExhibitionScheduleDto>(updatedSchedule ?? schedule));
        }

        public async Task<ServiceResult> RemoveScheduleAsync(int tenantId, int scheduleId)
        {
            await _unitOfWork.BeginSerializableTransactionAsync();
            try
            {
                var result = await RemoveScheduleCoreAsync(tenantId, scheduleId);
                if (result.IsSuccess) await _unitOfWork.CommitTransactionAsync();
                return result;
            }
            catch (DbUpdateException)
            { return ServiceResult.Failure("لا يمكن حذف فعالية مرتبطة بتسجيلات", "SCHEDULE_HAS_REGISTRATIONS"); }
            catch (Exception ex) when (DatabaseConflict.IsLockConflict(ex))
            { return ServiceResult.Failure("تعارض حذف الفعالية مع عملية أخرى", "SCHEDULE_HAS_REGISTRATIONS"); }
            finally { await _unitOfWork.RollbackTransactionAsync(); }
        }

        private async Task<ServiceResult> RemoveScheduleCoreAsync(int tenantId, int scheduleId)
        {
            var schedule = await _unitOfWork.ExhibitionSchedules.AsQueryable()
                .Include(s => s.Exhibition)
                .FirstOrDefaultAsync(s => s.ScheduleID == scheduleId);

            if (schedule == null || schedule.Exhibition == null || schedule.Exhibition.TenantID != tenantId)
            {
                return ServiceResult.Failure("الجدول الزمني غير موجود", "SCHEDULE_NOT_FOUND");
            }

            if (await _unitOfWork.ScheduleRegistrations.ExistsAsync(r => r.ScheduleID == scheduleId))
                return ServiceResult.Failure("لا يمكن حذف الفعالية لوجود تسجيلات مرتبطة بها", "SCHEDULE_HAS_REGISTRATIONS");
            _unitOfWork.ExhibitionSchedules.Remove(schedule);
            await _unitOfWork.SaveChangesAsync();

            return ServiceResult.Success();
        }

        private async Task<ServiceResult<ExhibitionScheduleDto>> RunScheduleMutationAsync(Func<Task<ServiceResult<ExhibitionScheduleDto>>> mutation)
        {
            await _unitOfWork.BeginSerializableTransactionAsync();
            try
            {
                var result = await mutation();
                if (result.IsSuccess) await _unitOfWork.CommitTransactionAsync();
                return result;
            }
            catch (DbUpdateException)
            { return ServiceResult<ExhibitionScheduleDto>.Failure("تعارض حفظ الفعالية مع تعديل آخر", "SCHEDULE_CONFLICT"); }
            catch (Exception ex) when (DatabaseConflict.IsLockConflict(ex))
            { return ServiceResult<ExhibitionScheduleDto>.Failure("تعارض حفظ الفعالية مع تعديل آخر", "SCHEDULE_CONFLICT"); }
            finally { await _unitOfWork.RollbackTransactionAsync(); }
        }
    }
}
