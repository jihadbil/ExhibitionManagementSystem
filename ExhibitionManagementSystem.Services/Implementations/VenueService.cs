using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using ExhibitionManagementSystem.DataAccess.Repositories.Interfaces;
using ExhibitionManagementSystem.Models;
using ExhibitionManagementSystem.Models.DTOs.Venue;
using ExhibitionManagementSystem.Services.Common;
using ExhibitionManagementSystem.Services.Interfaces;

namespace ExhibitionManagementSystem.Services.Implementations
{
    public class VenueService : IVenueService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public VenueService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<ServiceResult<IList<VenueDto>>> GetByTenantAsync(int tenantId)
        {
            var venues = await _unitOfWork.Venues.GetByTenantAsync(tenantId);
            var dtos = _mapper.Map<IList<VenueDto>>(venues);
            return ServiceResult<IList<VenueDto>>.Success(dtos);
        }

        public async Task<ServiceResult<IList<VenueSummaryDto>>> GetSummariesAsync(int tenantId)
        {
            var venues = await _unitOfWork.Venues.GetByTenantAsync(tenantId);
            var dtos = _mapper.Map<IList<VenueSummaryDto>>(venues);
            return ServiceResult<IList<VenueSummaryDto>>.Success(dtos);
        }

        public async Task<ServiceResult<VenueDto>> GetByIdAsync(int tenantId, int venueId)
        {
            // We use includes for Halls to map the HallsCount in mapping profile
            var venue = await _unitOfWork.Venues.GetByIdWithIncludesAsync(venueId, v => v.Halls);
            if (venue == null || venue.TenantID != tenantId)
            {
                return ServiceResult<VenueDto>.Failure("مكان الفعالية غير موجود", "VENUE_NOT_FOUND");
            }

            var dto = _mapper.Map<VenueDto>(venue);
            return ServiceResult<VenueDto>.Success(dto);
        }

        public async Task<ServiceResult<VenueDto>> CreateAsync(int tenantId, VenueCreateDto dto)
        {
            var venue = _mapper.Map<Venue>(dto);
            venue.TenantID = tenantId;

            await _unitOfWork.Venues.AddAsync(venue);
            await _unitOfWork.SaveChangesAsync();

            var resultDto = _mapper.Map<VenueDto>(venue);
            return ServiceResult<VenueDto>.Success(resultDto);
        }

        public async Task<ServiceResult<VenueDto>> UpdateAsync(int tenantId, int venueId, VenueUpdateDto dto)
        {
            var venue = await _unitOfWork.Venues.GetByIdAsync(venueId);
            if (venue == null || venue.TenantID != tenantId)
            {
                return ServiceResult<VenueDto>.Failure("مكان الفعالية غير موجود", "VENUE_NOT_FOUND");
            }

            _mapper.Map(dto, venue);
            venue.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Venues.Update(venue);
            await _unitOfWork.SaveChangesAsync();

            var resultDto = _mapper.Map<VenueDto>(venue);
            return ServiceResult<VenueDto>.Success(resultDto);
        }

        public async Task<ServiceResult> DeleteAsync(int tenantId, int venueId)
        {
            var venue = await _unitOfWork.Venues.GetByIdAsync(venueId);
            if (venue == null || venue.TenantID != tenantId)
            {
                return ServiceResult.Failure("مكان الفعالية غير موجود", "VENUE_NOT_FOUND");
            }

            await _unitOfWork.Venues.SoftDeleteAsync(venueId, "System");
            await _unitOfWork.SaveChangesAsync();

            return ServiceResult.Success();
        }
    }
}
