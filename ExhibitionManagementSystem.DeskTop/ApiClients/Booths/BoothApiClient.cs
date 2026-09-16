using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using ExhibitionManagementSystem.Models.DTOs.Booth;
using ExhibitionManagementSystem.Services.Common;
using ExhibitionManagementSystem.Services.Interfaces;
using ExhibitionManagementSystem.DeskTop.ApiClients.Base;
using ExhibitionManagementSystem.DeskTop.Services.Session;

namespace ExhibitionManagementSystem.DeskTop.ApiClients.Booths;

public class BoothApiClient : ApiClientBase, IBoothService
{
    public BoothApiClient(IHttpClientFactory httpClientFactory, SessionService session)
        : base(httpClientFactory, session)
    {
    }

    public Task<ServiceResult<IList<BoothDto>>> GetByHallAsync(int tenantId, int hallId)
    {
        return GetAsync<IList<BoothDto>>($"api/booths/hall/{hallId}");
    }

    public Task<ServiceResult<IList<BoothSummaryDto>>> GetAvailableAsync(int tenantId, int hallId, int exhibitionId)
    {
        return GetAsync<IList<BoothSummaryDto>>($"api/booths/available?hallId={hallId}&exhibitionId={exhibitionId}");
    }

    public Task<ServiceResult<BoothDto>> GetByIdAsync(int tenantId, int boothId)
    {
        return GetAsync<BoothDto>($"api/booths/{boothId}");
    }

    public Task<ServiceResult<BoothDto>> CreateAsync(int tenantId, BoothCreateDto dto)
    {
        return PostAsync<BoothDto>("api/booths", dto);
    }

    public Task<ServiceResult<BoothDto>> UpdateAsync(int tenantId, int boothId, BoothUpdateDto dto)
    {
        return PutAsync<BoothDto>($"api/booths/{boothId}", dto);
    }

    public Task<ServiceResult<BoothMergeDto>> MergeBoothsAsync(int tenantId, string userId, BoothMergeCreateDto dto)
    {
        return PostAsync<BoothMergeDto>("api/booths/merge", dto);
    }

    public Task<ServiceResult> UnmergeBoothsAsync(int tenantId, int mergeId)
    {
        return DeleteAsync($"api/booths/merge/{mergeId}");
    }

    public Task<ServiceResult> DeleteAsync(int tenantId, int boothId)
    {
        return DeleteAsync($"api/booths/{boothId}");
    }
}
