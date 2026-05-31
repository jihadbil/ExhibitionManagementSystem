using System.Collections.Generic;
using System.Threading.Tasks;
using ExhibitionManagementSystem.Models.DTOs.Exhibitor;
using ExhibitionManagementSystem.Models.DTOs.Reservation;
using ExhibitionManagementSystem.Models.DTOs.Common;
using ExhibitionManagementSystem.Services.Common;

namespace ExhibitionManagementSystem.Services.Interfaces
{
    public interface IExhibitorService
    {
        Task<ServiceResult<PagedResultDto<ExhibitorSummaryDto>>> GetByTenantAsync(int tenantId, int page, int pageSize);
        Task<ServiceResult<IList<ExhibitorSummaryDto>>> SearchAsync(int tenantId, string term);
        Task<ServiceResult<ExhibitorDto>> GetByIdAsync(int tenantId, int exhibitorId);
        Task<ServiceResult<ExhibitorDto>> GetByUserIdAsync(int tenantId, string userId);
        Task<ServiceResult<ExhibitorDto>> CreateAsync(int tenantId, ExhibitorCreateDto dto);
        Task<ServiceResult<ExhibitorDto>> UpdateAsync(int tenantId, int id, ExhibitorUpdateDto dto);
        Task<ServiceResult> DeleteAsync(int tenantId, int id);
        Task<ServiceResult<IList<BoothReservationSummaryDto>>> GetReservationsAsync(int tenantId, int exhibitorId);
    }
}
