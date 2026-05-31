using System.Collections.Generic;
using System.Threading.Tasks;
using ExhibitionManagementSystem.Models.DTOs.Exhibition;
using ExhibitionManagementSystem.Models.DTOs.Common;
using ExhibitionManagementSystem.Services.Common;

namespace ExhibitionManagementSystem.Services.Interfaces
{
    public interface IExhibitionService
    {
        Task<ServiceResult<PagedResultDto<ExhibitionSummaryDto>>> GetByTenantAsync(int tenantId, int page, int pageSize);
        Task<ServiceResult<ExhibitionDto>> GetByIdAsync(int tenantId, int exhibitionId);
        Task<ServiceResult<IList<ExhibitionSummaryDto>>> GetActiveAsync(int tenantId);
        Task<ServiceResult<IList<ExhibitionSummaryDto>>> GetUpcomingAsync(int tenantId, int count);
        Task<ServiceResult<ExhibitionDto>> CreateAsync(int tenantId, ExhibitionCreateDto dto);
        Task<ServiceResult<ExhibitionDto>> UpdateAsync(int tenantId, int id, ExhibitionUpdateDto dto);
        Task<ServiceResult<ExhibitionDto>> ChangeStatusAsync(int tenantId, int id, string status);
        Task<ServiceResult> DeleteAsync(int tenantId, int id);

        // Schedules
        Task<ServiceResult<IList<ExhibitionScheduleDto>>> GetSchedulesAsync(int tenantId, int exhibitionId);
        Task<ServiceResult<ExhibitionScheduleDto>> AddScheduleAsync(int tenantId, ExhibitionScheduleCreateDto dto);
        Task<ServiceResult> RemoveScheduleAsync(int tenantId, int scheduleId);
    }
}
