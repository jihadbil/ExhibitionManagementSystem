using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using ExhibitionManagementSystem.Models.DTOs.Exhibition;
using ExhibitionManagementSystem.Models.DTOs.Common;
using ExhibitionManagementSystem.Services.Common;
using ExhibitionManagementSystem.Services.Interfaces;
using ExhibitionManagementSystem.DeskTop.ApiClients.Base;
using ExhibitionManagementSystem.DeskTop.Services.Session;

namespace ExhibitionManagementSystem.DeskTop.ApiClients.Exhibitions;

public class ExhibitionApiClient : ApiClientBase, IExhibitionService
{
    public ExhibitionApiClient(IHttpClientFactory httpClientFactory, SessionService session)
        : base(httpClientFactory, session)
    {
    }

    public Task<ServiceResult<PagedResultDto<ExhibitionSummaryDto>>> GetByTenantAsync(int tenantId, int page, int pageSize)
    {
        return GetAsync<PagedResultDto<ExhibitionSummaryDto>>($"api/exhibitions?page={page}&pageSize={pageSize}");
    }

    public Task<ServiceResult<ExhibitionDto>> GetByIdAsync(int tenantId, int exhibitionId)
    {
        return GetAsync<ExhibitionDto>($"api/exhibitions/{exhibitionId}");
    }

    public Task<ServiceResult<IList<ExhibitionSummaryDto>>> GetActiveAsync(int tenantId)
    {
        return GetAsync<IList<ExhibitionSummaryDto>>("api/exhibitions/active");
    }

    public Task<ServiceResult<IList<ExhibitionSummaryDto>>> GetUpcomingAsync(int tenantId, int count)
    {
        return GetAsync<IList<ExhibitionSummaryDto>>($"api/exhibitions/upcoming?count={count}");
    }

    public Task<ServiceResult<ExhibitionDto>> CreateAsync(int tenantId, ExhibitionCreateDto dto)
    {
        return PostAsync<ExhibitionDto>("api/exhibitions", dto);
    }

    public Task<ServiceResult<ExhibitionDto>> UpdateAsync(int tenantId, int id, ExhibitionUpdateDto dto)
    {
        return PutAsync<ExhibitionDto>($"api/exhibitions/{id}", dto);
    }

    public Task<ServiceResult<ExhibitionDto>> ChangeStatusAsync(int tenantId, int id, string status)
    {
        return PatchAsync<ExhibitionDto>($"api/exhibitions/{id}/status", status);
    }

    public Task<ServiceResult> DeleteAsync(int tenantId, int id)
    {
        return DeleteAsync($"api/exhibitions/{id}");
    }

    public Task<ServiceResult<IList<ExhibitionScheduleDto>>> GetSchedulesAsync(int tenantId, int exhibitionId)
    {
        return GetAsync<IList<ExhibitionScheduleDto>>($"api/exhibitions/{exhibitionId}/schedules");
    }

    public Task<ServiceResult<ExhibitionScheduleDto>> AddScheduleAsync(int tenantId, ExhibitionScheduleCreateDto dto)
    {
        return PostAsync<ExhibitionScheduleDto>($"api/exhibitions/{dto.ExhibitionID}/schedules", dto);
    }

    public Task<ServiceResult> RemoveScheduleAsync(int tenantId, int scheduleId)
    {
        return DeleteAsync($"api/exhibitions/schedules/{scheduleId}");
    }
}
