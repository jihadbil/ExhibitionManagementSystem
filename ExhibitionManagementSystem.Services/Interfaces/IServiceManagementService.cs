using System.Collections.Generic;
using System.Threading.Tasks;
using ExhibitionManagementSystem.Models.DTOs.Service;
using ExhibitionManagementSystem.Services.Common;

namespace ExhibitionManagementSystem.Services.Interfaces
{
    public interface IServiceManagementService
    {
        Task<ServiceResult<IList<ServiceDto>>> GetByTenantAsync(int tenantId);
        Task<ServiceResult<ServiceDto>> GetByIdAsync(int tenantId, int serviceId);
        Task<ServiceResult<ServiceDto>> CreateAsync(int tenantId, ServiceCreateDto dto);
        Task<ServiceResult<ServiceDto>> UpdateAsync(int tenantId, int serviceId, ServiceCreateDto dto);
        Task<ServiceResult> DeactivateAsync(int tenantId, int serviceId);
    }
}
