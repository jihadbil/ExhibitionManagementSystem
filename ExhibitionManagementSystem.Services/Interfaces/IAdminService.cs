using System.Collections.Generic;
using System.Threading.Tasks;
using ExhibitionManagementSystem.Models.DTOs.Admin;
using ExhibitionManagementSystem.Models.DTOs.Common;
using ExhibitionManagementSystem.Services.Common;

namespace ExhibitionManagementSystem.Services.Interfaces
{
    public interface IAdminService
    {
        Task<ServiceResult<PagedResultDto<AuditLogDto>>> GetAuditLogsAsync(int tenantId, int page, int pageSize);
        Task<ServiceResult<IList<AuditLogDto>>> GetAuditLogsByEntityAsync(int tenantId, string tableName, string recordId);
        Task<ServiceResult<IList<TenantSubscriptionDto>>> GetSubscriptionHistoryAsync(int tenantId);
        Task<ServiceResult<TenantSubscriptionDto>> CreateSubscriptionAsync(int tenantId, TenantSubscriptionDto dto);
    }
}
