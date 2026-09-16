using System.Net.Http;
using System.Threading.Tasks;
using ExhibitionManagementSystem.Models.DTOs.Tenant;
using ExhibitionManagementSystem.Models.DTOs.Admin;
using ExhibitionManagementSystem.Models.DTOs.Common;
using ExhibitionManagementSystem.Services.Common;
using ExhibitionManagementSystem.Services.Interfaces;
using ExhibitionManagementSystem.DeskTop.ApiClients.Base;
using ExhibitionManagementSystem.DeskTop.Services.Session;

namespace ExhibitionManagementSystem.DeskTop.ApiClients.Tenants
{
    public class TenantsApiClient : ApiClientBase, ITenantService
    {
        public TenantsApiClient(IHttpClientFactory httpClientFactory, SessionService session)
            : base(httpClientFactory, session)
        {
        }

        public Task<ServiceResult<PagedResultDto<TenantDto>>> GetAllAsync(int page, int pageSize)
        {
            return GetAsync<PagedResultDto<TenantDto>>($"api/tenants?page={page}&pageSize={pageSize}");
        }

        public Task<ServiceResult<TenantDto>> GetByIdAsync(int tenantId)
        {
            return GetAsync<TenantDto>($"api/tenants/{tenantId}");
        }

        public Task<ServiceResult<TenantDto>> CreateAsync(TenantCreateDto dto)
        {
            return PostAsync<TenantDto>("api/tenants", dto);
        }

        public Task<ServiceResult<TenantDto>> UpdateAsync(int tenantId, TenantUpdateDto dto)
        {
            return PutAsync<TenantDto>($"api/tenants/{tenantId}", dto);
        }

        public Task<ServiceResult> DeleteAsync(int tenantId)
        {
            return DeleteAsync($"api/tenants/{tenantId}");
        }

        public Task<ServiceResult<TenantSubscriptionDto>> GetActiveSubscriptionAsync(int tenantId)
        {
            return GetAsync<TenantSubscriptionDto>($"api/tenants/{tenantId}/subscription");
        }
    }
}
