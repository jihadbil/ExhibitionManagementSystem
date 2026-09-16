using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using ExhibitionManagementSystem.Models.DTOs.Service;
using ExhibitionManagementSystem.Services.Common;
using ExhibitionManagementSystem.Services.Interfaces;
using ExhibitionManagementSystem.DeskTop.ApiClients.Base;
using ExhibitionManagementSystem.DeskTop.Services.Session;

namespace ExhibitionManagementSystem.DeskTop.ApiClients.Services;

public class ServiceApiClient : ApiClientBase, IServiceManagementService
{
    public ServiceApiClient(IHttpClientFactory httpClientFactory, SessionService session)
        : base(httpClientFactory, session)
    {
    }

    public Task<ServiceResult<IList<ServiceDto>>> GetByTenantAsync(int tenantId)
    {
        return GetAsync<IList<ServiceDto>>("api/services");
    }

    public Task<ServiceResult<ServiceDto>> GetByIdAsync(int tenantId, int serviceId)
    {
        return GetAsync<ServiceDto>($"api/services/{serviceId}");
    }

    public Task<ServiceResult<ServiceDto>> CreateAsync(int tenantId, ServiceCreateDto dto)
    {
        return PostAsync<ServiceDto>("api/services", dto);
    }

    public Task<ServiceResult<ServiceDto>> UpdateAsync(int tenantId, int serviceId, ServiceCreateDto dto)
    {
        return PutAsync<ServiceDto>($"api/services/{serviceId}", dto);
    }

    public Task<ServiceResult> DeactivateAsync(int tenantId, int serviceId)
    {
        return PatchVoidAsync($"api/services/{serviceId}/deactivate");
    }
}
