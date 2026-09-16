using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using ExhibitionManagementSystem.Models.DTOs.Hall;
using ExhibitionManagementSystem.Services.Common;
using ExhibitionManagementSystem.Services.Interfaces;
using ExhibitionManagementSystem.DeskTop.ApiClients.Base;
using ExhibitionManagementSystem.DeskTop.Services.Session;

namespace ExhibitionManagementSystem.DeskTop.ApiClients.Halls;

public class HallApiClient : ApiClientBase, IHallService
{
    public HallApiClient(IHttpClientFactory httpClientFactory, SessionService session)
        : base(httpClientFactory, session)
    {
    }

    public Task<ServiceResult<IList<HallDto>>> GetByVenueAsync(int tenantId, int venueId)
    {
        return GetAsync<IList<HallDto>>($"api/halls/venue/{venueId}");
    }

    public Task<ServiceResult<HallDto>> GetByIdAsync(int tenantId, int hallId)
    {
        return GetAsync<HallDto>($"api/halls/{hallId}");
    }

    public Task<ServiceResult<HallDto>> CreateAsync(int tenantId, HallCreateDto dto)
    {
        return PostAsync<HallDto>("api/halls", dto);
    }

    public Task<ServiceResult<HallDto>> UpdateAsync(int tenantId, int hallId, HallUpdateDto dto)
    {
        return PutAsync<HallDto>($"api/halls/{hallId}", dto);
    }

    public Task<ServiceResult> DeleteAsync(int tenantId, int hallId)
    {
        return DeleteAsync($"api/halls/{hallId}");
    }
}
