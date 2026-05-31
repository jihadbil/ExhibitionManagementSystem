using System.Collections.Generic;
using System.Threading.Tasks;
using ExhibitionManagementSystem.Models.DTOs.Common;
using ExhibitionManagementSystem.Models.DTOs.Visitor;
using ExhibitionManagementSystem.Services.Common;

namespace ExhibitionManagementSystem.Services.Interfaces
{
    public interface IVisitorService
    {
        Task<ServiceResult<PagedResultDto<VisitorDto>>> GetByTenantAsync(int tenantId, int page, int pageSize);
        Task<ServiceResult<IList<VisitorDto>>> SearchAsync(int tenantId, string term);
        Task<ServiceResult<VisitorDto>> GetByIdAsync(int tenantId, int visitorId);
        Task<ServiceResult<VisitorDto>> RegisterAsync(int tenantId, VisitorCreateDto dto);
        Task<ServiceResult<VisitorRatingDto>> SubmitRatingAsync(int tenantId, int visitorId, int exhibitionId, int rating, string? comment);
        Task<ServiceResult<VisitorRatingSummaryDto>> GetRatingSummaryAsync(int tenantId, int exhibitionId);
    }
}
