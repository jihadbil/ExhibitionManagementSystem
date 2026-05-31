using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using ExhibitionManagementSystem.DataAccess.Repositories.Interfaces;
using ExhibitionManagementSystem.Models;
using ExhibitionManagementSystem.Models.DTOs.Hall;
using ExhibitionManagementSystem.Services.Common;
using ExhibitionManagementSystem.Services.Interfaces;

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
            var hall = await _unitOfWork.Halls.GetByIdWithIncludesAsync(hallId, h => h.Venue);
            if (hall == null || hall.Venue == null || hall.Venue.TenantID != tenantId)
            {
                return ServiceResult<HallDto>.Failure("القاعة غير موجودة", "HALL_NOT_FOUND");
            }

            _mapper.Map(dto, hall);
            hall.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Halls.Update(hall);
            await _unitOfWork.SaveChangesAsync();

            var resultDto = _mapper.Map<HallDto>(hall);
            return ServiceResult<HallDto>.Success(resultDto);
        }

        public async Task<ServiceResult> DeleteAsync(int tenantId, int hallId)
        {
            var hall = await _unitOfWork.Halls.GetByIdWithIncludesAsync(hallId, h => h.Venue);
            if (hall == null || hall.Venue == null || hall.Venue.TenantID != tenantId)
            {
                return ServiceResult.Failure("القاعة غير موجودة", "HALL_NOT_FOUND");
            }

            await _unitOfWork.Halls.SoftDeleteAsync(hallId, "System");
            await _unitOfWork.SaveChangesAsync();

            return ServiceResult.Success();
        }
    }
}
