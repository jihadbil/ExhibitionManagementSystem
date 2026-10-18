using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using ExhibitionManagementSystem.DataAccess.Repositories.Interfaces;
using ExhibitionManagementSystem.Models;
using ExhibitionManagementSystem.Models.DTOs.Hall;
using ExhibitionManagementSystem.Services.Common;
using ExhibitionManagementSystem.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ExhibitionManagementSystem.Services.Implementations
{
    public class HallService : IHallService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public HallService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<ServiceResult<IList<HallDto>>> GetByVenueAsync(int tenantId, int venueId)
        {
            var venue = await _unitOfWork.Venues.GetByIdAsync(venueId);
            if (venue == null || venue.TenantID != tenantId)
            {
                return ServiceResult<IList<HallDto>>.Failure("مكان الفعالية غير موجود", "VENUE_NOT_FOUND");
            }

            var halls = await _unitOfWork.Halls.GetByVenueAsync(venueId);
            var dtos = _mapper.Map<IList<HallDto>>(halls);
            return ServiceResult<IList<HallDto>>.Success(dtos);
        }

        public async Task<ServiceResult<HallDto>> GetByIdAsync(int tenantId, int hallId)
        {
            var hall = await _unitOfWork.Halls.GetByIdWithIncludesAsync(hallId, h => h.Venue, h => h.Booths);
            if (hall == null || hall.Venue == null || hall.Venue.TenantID != tenantId)
            {
                return ServiceResult<HallDto>.Failure("القاعة غير موجودة", "HALL_NOT_FOUND");
            }

            var dto = _mapper.Map<HallDto>(hall);
            return ServiceResult<HallDto>.Success(dto);
        }

        public async Task<ServiceResult<HallDto>> CreateAsync(int tenantId, HallCreateDto dto)
        {
            await _unitOfWork.BeginSerializableTransactionAsync();
            try
            {
                var result = await CreateCoreAsync(tenantId, dto);
                if (result.IsSuccess) await _unitOfWork.CommitTransactionAsync();
                return result;
            }
            catch (DbUpdateException)
            { return ServiceResult<HallDto>.Failure("تعارض إنشاء القاعة مع تعديل الموقع", "VENUE_CONFLICT"); }
            catch (Exception ex) when (DatabaseConflict.IsLockConflict(ex))
            { return ServiceResult<HallDto>.Failure("تعارض إنشاء القاعة مع تعديل الموقع", "VENUE_CONFLICT"); }
            finally { await _unitOfWork.RollbackTransactionAsync(); }
        }

        private async Task<ServiceResult<HallDto>> CreateCoreAsync(int tenantId, HallCreateDto dto)
        {
            var validationError = ValidateDimensions(dto.AreaSqM, dto.MaxBooths, dto.FloorPlanWidth, dto.FloorPlanHeight);
            if (validationError != null)
                return ServiceResult<HallDto>.Failure(validationError, "INVALID_HALL_DIMENSIONS");
            var planError = HallDrawingSafety.Validate([], null, 0, dto.FloorPlanWidth, dto.FloorPlanHeight, dto.FloorPlanJSON);
            if (planError != null)
                return ServiceResult<HallDto>.Failure(planError.Value.Message, planError.Value.Code);
            var venue = await _unitOfWork.Venues.GetByIdAsync(dto.VenueID);
            if (venue == null || venue.TenantID != tenantId)
            {
                return ServiceResult<HallDto>.Failure("مكان الفعالية غير موجود", "VENUE_NOT_FOUND");
            }

            var hall = _mapper.Map<Hall>(dto);
            await _unitOfWork.Halls.AddAsync(hall);
            await _unitOfWork.SaveChangesAsync();

            var createdHall = await _unitOfWork.Halls.GetByIdWithIncludesAsync(hall.HallID, h => h.Venue);
            var resultDto = _mapper.Map<HallDto>(createdHall ?? hall);

            return ServiceResult<HallDto>.Success(resultDto);
        }

        public async Task<ServiceResult<HallDto>> UpdateAsync(int tenantId, int hallId, HallUpdateDto dto)
        {
            await _unitOfWork.BeginSerializableTransactionAsync();
            try
            {
                var result = await UpdateCoreAsync(tenantId, hallId, dto);
                if (result.IsSuccess) await _unitOfWork.CommitTransactionAsync();
                return result;
            }
            catch (Exception ex) when (DatabaseConflict.IsLockConflict(ex))
            { return ServiceResult<HallDto>.Failure("تعارض تعديل القاعة مع تعديل آخر", "FLOOR_PLAN_CONFLICT"); }
            finally { await _unitOfWork.RollbackTransactionAsync(); }
        }

        private async Task<ServiceResult<HallDto>> UpdateCoreAsync(int tenantId, int hallId, HallUpdateDto dto)
        {
            var hall = await _unitOfWork.Halls.GetByIdWithIncludesAsync(hallId, h => h.Venue);
            if (hall == null || hall.Venue == null || hall.Venue.TenantID != tenantId)
            {
                return ServiceResult<HallDto>.Failure("القاعة غير موجودة", "HALL_NOT_FOUND");
            }

            var validationError = ValidateDimensions(dto.AreaSqM, dto.MaxBooths, dto.FloorPlanWidth, dto.FloorPlanHeight);
            if (validationError != null)
                return ServiceResult<HallDto>.Failure(validationError, "INVALID_HALL_DIMENSIONS");
            var booths = await _unitOfWork.Booths.AsQueryable().AsNoTracking().Where(b => b.HallID == hallId).ToListAsync();
            if (booths.Count > 0 && dto.FloorPlanJSON != null && dto.FloorPlanJSON != hall.FloorPlanJSON)
                return ServiceResult<HallDto>.Failure("احفظ مخطط القاعة من مصمم الأجنحة للحفاظ على اتساق الأجنحة", "FLOOR_PLAN_CONFLICT");
            var planError = HallDrawingSafety.Validate(booths, hall.Venue.SitePlanJSON, hallId,
                dto.FloorPlanWidth, dto.FloorPlanHeight, dto.FloorPlanJSON ?? hall.FloorPlanJSON);
            if (planError != null)
                return ServiceResult<HallDto>.Failure(planError.Value.Message, planError.Value.Code);

            _mapper.Map(dto, hall);
            hall.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Halls.Update(hall);
            await _unitOfWork.SaveChangesAsync();

            var resultDto = _mapper.Map<HallDto>(hall);
            return ServiceResult<HallDto>.Success(resultDto);
        }

        public async Task<ServiceResult> DeleteAsync(int tenantId, int hallId)
        {
            await _unitOfWork.BeginSerializableTransactionAsync();
            try
            {
                var result = await DeleteCoreAsync(tenantId, hallId);
                if (result.IsSuccess) await _unitOfWork.CommitTransactionAsync();
                return result;
            }
            catch (DbUpdateException)
            { return ServiceResult.Failure("تعارض حذف القاعة مع عملية أخرى", "HALL_DELETE_CONFLICT"); }
            catch (Exception ex) when (DatabaseConflict.IsLockConflict(ex))
            { return ServiceResult.Failure("تعارض حذف القاعة مع عملية أخرى", "HALL_DELETE_CONFLICT"); }
            finally { await _unitOfWork.RollbackTransactionAsync(); }
        }

        private async Task<ServiceResult> DeleteCoreAsync(int tenantId, int hallId)
        {
            var hall = await _unitOfWork.Halls.GetByIdWithIncludesAsync(hallId, h => h.Venue);
            if (hall == null || hall.Venue == null || hall.Venue.TenantID != tenantId)
            {
                return ServiceResult.Failure("القاعة غير موجودة", "HALL_NOT_FOUND");
            }

            var activeBooths = await _unitOfWork.Booths.FindAsync(b => b.HallID == hallId && !b.IsDeleted);
            if (activeBooths.Any())
            {
                return ServiceResult.Failure("لا يمكن حذف القاعة لوجود أكشاك نشطة بها. احذف الأكشاك أولاً.", "HALL_HAS_ACTIVE_BOOTHS");
            }

            if (await _unitOfWork.ExhibitionSchedules.AsQueryable().IgnoreQueryFilters()
                    .AnyAsync(s => s.HallID == hallId && s.Exhibition.TenantID == tenantId)
                || await _unitOfWork.AdvertisingSpaces.AsQueryable().IgnoreQueryFilters()
                    .AnyAsync(s => s.TenantID == tenantId && s.HallID == hallId)
                || await _unitOfWork.Booths.AsQueryable().IgnoreQueryFilters()
                    .AnyAsync(b => b.TenantID == tenantId && b.HallID == hallId))
                return ServiceResult.Failure("لا يمكن حذف القاعة لوجود فعاليات أو مساحات إعلانية أو أجنحة مرتبطة بها", "HALL_HAS_DEPENDENCIES");

            await _unitOfWork.Halls.SoftDeleteAsync(hallId, _unitOfWork.CurrentUserId);
            await _unitOfWork.SaveChangesAsync();

            return ServiceResult.Success();
        }

        private static string? ValidateDimensions(decimal? area, int? maxBooths, decimal? width, decimal? height) =>
            area <= 0 || maxBooths < 0 || width <= 0 || height <= 0 || width > 10000 || height > 10000
                ? "مساحة القاعة وأبعادها يجب أن تكون موجبة وعدد الأجنحة غير سالب" : null;
    }
}
