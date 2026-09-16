using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using ExhibitionManagementSystem.Models.DTOs.Admin;
using ExhibitionManagementSystem.Models.DTOs.Common;
using ExhibitionManagementSystem.Services.Common;
using ExhibitionManagementSystem.Services.Interfaces;
using ExhibitionManagementSystem.DeskTop.ApiClients.Base;
using ExhibitionManagementSystem.DeskTop.Services.Session;

namespace ExhibitionManagementSystem.DeskTop.ApiClients.Admin
{
    public class AdminApiClient : ApiClientBase, IAdminService
    {
        public AdminApiClient(IHttpClientFactory httpClientFactory, SessionService session)
            : base(httpClientFactory, session)
        {
        }

        public Task<ServiceResult<PagedResultDto<AuditLogDto>>> GetAuditLogsAsync(int tenantId, int page, int pageSize)
        {
            return GetAsync<PagedResultDto<AuditLogDto>>($"api/admin/audit-logs?page={page}&pageSize={pageSize}");
        }

        public Task<ServiceResult<IList<AuditLogDto>>> GetAuditLogsByEntityAsync(int tenantId, string tableName, string recordId)
        {
            return GetAsync<IList<AuditLogDto>>($"api/admin/audit-logs/{tableName}/{recordId}");
        }

        public Task<ServiceResult<IList<TenantSubscriptionDto>>> GetSubscriptionHistoryAsync(int tenantId)
        {
            return GetAsync<IList<TenantSubscriptionDto>>("api/admin/subscriptions");
        }

        public Task<ServiceResult<TenantSubscriptionDto>> CreateSubscriptionAsync(int tenantId, TenantSubscriptionDto dto)
        {
            return PostAsync<TenantSubscriptionDto>("api/admin/subscriptions", dto);
        }
    }
}
