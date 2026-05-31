using System.Collections.Generic;
using System.Threading.Tasks;
using ExhibitionManagementSystem.Models.DTOs.Hall;
using ExhibitionManagementSystem.Services.Common;

namespace ExhibitionManagementSystem.Services.Interfaces
{
    public interface IHallService
    {
        Task<ServiceResult<IList<HallDto>>> GetByVenueAsync(int tenantId, int venueId);
        Task<ServiceResult<HallDto>> GetByIdAsync(int tenantId, int hallId);
        Task<ServiceResult<HallDto>> CreateAsync(int tenantId, HallCreateDto dto);
        Task<ServiceResult<HallDto>> UpdateAsync(int tenantId, int hallId, HallUpdateDto dto);
        Task<ServiceResult> DeleteAsync(int tenantId, int hallId);
    }
}
