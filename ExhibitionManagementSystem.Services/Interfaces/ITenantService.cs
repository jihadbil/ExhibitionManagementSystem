using System.Threading.Tasks;
using ExhibitionManagementSystem.Models.DTOs.Tenant;
using ExhibitionManagementSystem.Models.DTOs.Admin;
using ExhibitionManagementSystem.Models.DTOs.Common;
using ExhibitionManagementSystem.Services.Common;

namespace ExhibitionManagementSystem.Services.Interfaces
{
    public interface ITenantService
    {
        Task<ServiceResult<PagedResultDto<TenantDto>>> GetAllAsync(int page, int pageSize);
        Task<ServiceResult<TenantDto>> GetByIdAsync(int tenantId);
        Task<ServiceResult<TenantDto>> CreateAsync(TenantCreateDto dto);
        Task<ServiceResult<TenantDto>> UpdateAsync(int tenantId, TenantUpdateDto dto);
        Task<ServiceResult> DeleteAsync(int tenantId);
        Task<ServiceResult<TenantSubscriptionDto>> GetActiveSubscriptionAsync(int tenantId);
    }
}
