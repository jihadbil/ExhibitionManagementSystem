using System.Collections.Generic;
using System.Threading.Tasks;
using ExhibitionManagementSystem.Models.DTOs.Venue;
using ExhibitionManagementSystem.Services.Common;

namespace ExhibitionManagementSystem.Services.Interfaces
{
    public interface IVenueService
    {
        Task<ServiceResult<IList<VenueDto>>> GetByTenantAsync(int tenantId);
        Task<ServiceResult<IList<VenueSummaryDto>>> GetSummariesAsync(int tenantId);
        Task<ServiceResult<VenueDto>> GetByIdAsync(int tenantId, int venueId);
        Task<ServiceResult<VenueDto>> CreateAsync(int tenantId, VenueCreateDto dto);
        Task<ServiceResult<VenueDto>> UpdateAsync(int tenantId, int venueId, VenueUpdateDto dto);
        Task<ServiceResult> DeleteAsync(int tenantId, int venueId);
    }
}
