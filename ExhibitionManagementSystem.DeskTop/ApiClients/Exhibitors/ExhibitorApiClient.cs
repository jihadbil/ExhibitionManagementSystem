using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using ExhibitionManagementSystem.Models.DTOs.Exhibitor;
using ExhibitionManagementSystem.Models.DTOs.Reservation;
using ExhibitionManagementSystem.Models.DTOs.Common;
using ExhibitionManagementSystem.Services.Common;
using ExhibitionManagementSystem.Services.Interfaces;
using ExhibitionManagementSystem.DeskTop.ApiClients.Base;
using ExhibitionManagementSystem.DeskTop.Services.Session;

namespace ExhibitionManagementSystem.DeskTop.ApiClients.Exhibitors;

public class ExhibitorApiClient : ApiClientBase, IExhibitorService
{
    public ExhibitorApiClient(IHttpClientFactory httpClientFactory, SessionService session)
        : base(httpClientFactory, session)
    {
    }

    public Task<ServiceResult<PagedResultDto<ExhibitorSummaryDto>>> GetByTenantAsync(int tenantId, int page, int pageSize)
    {
        return GetAsync<PagedResultDto<ExhibitorSummaryDto>>($"api/exhibitors?page={page}&pageSize={pageSize}");
    }

    public Task<ServiceResult<IList<ExhibitorSummaryDto>>> SearchAsync(int tenantId, string term)
    {
        return GetAsync<IList<ExhibitorSummaryDto>>($"api/exhibitors/search?term={term}");
    }

    public Task<ServiceResult<ExhibitorDto>> GetByIdAsync(int tenantId, int exhibitorId)
    {
        return GetAsync<ExhibitorDto>($"api/exhibitors/{exhibitorId}");
    }

    public Task<ServiceResult<ExhibitorDto>> GetByUserIdAsync(int tenantId, string userId)
    {
        return GetAsync<ExhibitorDto>($"api/exhibitors/by-user/{userId}");
    }

    public Task<ServiceResult<ExhibitorDto>> CreateAsync(int tenantId, ExhibitorCreateDto dto)
    {
        return PostAsync<ExhibitorDto>("api/exhibitors", dto);
    }

    public Task<ServiceResult<ExhibitorDto>> UpdateAsync(int tenantId, int id, ExhibitorUpdateDto dto)
    {
        return PutAsync<ExhibitorDto>($"api/exhibitors/{id}", dto);
    }

    public Task<ServiceResult> DeleteAsync(int tenantId, int id)
    {
        return DeleteAsync($"api/exhibitors/{id}");
    }

    public Task<ServiceResult<IList<BoothReservationSummaryDto>>> GetReservationsAsync(int tenantId, int exhibitorId)
    {
        return GetAsync<IList<BoothReservationSummaryDto>>($"api/exhibitors/{exhibitorId}/reservations");
    }
}
