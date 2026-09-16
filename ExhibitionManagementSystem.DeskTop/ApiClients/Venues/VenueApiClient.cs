using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using ExhibitionManagementSystem.Models.DTOs.Venue;
using ExhibitionManagementSystem.Services.Common;
using ExhibitionManagementSystem.Services.Interfaces;
using ExhibitionManagementSystem.DeskTop.ApiClients.Base;
using ExhibitionManagementSystem.DeskTop.Services.Session;

namespace ExhibitionManagementSystem.DeskTop.ApiClients.Venues;

public class VenueApiClient : ApiClientBase, IVenueService
{
    public VenueApiClient(IHttpClientFactory httpClientFactory, SessionService session)
        : base(httpClientFactory, session)
    {
    }

    public Task<ServiceResult<IList<VenueDto>>> GetByTenantAsync(int tenantId)
    {
        return GetAsync<IList<VenueDto>>("api/venues");
    }

    public Task<ServiceResult<IList<VenueSummaryDto>>> GetSummariesAsync(int tenantId)
    {
        return GetAsync<IList<VenueSummaryDto>>("api/venues/summaries");
    }

    public Task<ServiceResult<VenueDto>> GetByIdAsync(int tenantId, int venueId)
    {
        return GetAsync<VenueDto>($"api/venues/{venueId}");
    }

    public Task<ServiceResult<VenueDto>> CreateAsync(int tenantId, VenueCreateDto dto)
    {
        return PostAsync<VenueDto>("api/venues", dto);
    }

    public Task<ServiceResult<VenueDto>> UpdateAsync(int tenantId, int venueId, VenueUpdateDto dto)
    {
        return PutAsync<VenueDto>($"api/venues/{venueId}", dto);
    }

    public Task<ServiceResult> DeleteAsync(int tenantId, int venueId)
    {
        return DeleteAsync($"api/venues/{venueId}");
    }
}
