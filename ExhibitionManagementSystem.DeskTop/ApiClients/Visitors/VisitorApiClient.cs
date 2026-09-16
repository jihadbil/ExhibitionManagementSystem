using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using ExhibitionManagementSystem.Models.DTOs.Visitor;
using ExhibitionManagementSystem.Models.DTOs.Common;
using ExhibitionManagementSystem.Services.Common;
using ExhibitionManagementSystem.Services.Interfaces;
using ExhibitionManagementSystem.DeskTop.ApiClients.Base;
using ExhibitionManagementSystem.DeskTop.Services.Session;

namespace ExhibitionManagementSystem.DeskTop.ApiClients.Visitors;

public class VisitorApiClient : ApiClientBase, IVisitorService
{
    public VisitorApiClient(IHttpClientFactory httpClientFactory, SessionService session)
        : base(httpClientFactory, session)
    {
    }

    public Task<ServiceResult<PagedResultDto<VisitorDto>>> GetByTenantAsync(int tenantId, int page, int pageSize)
    {
        return GetAsync<PagedResultDto<VisitorDto>>($"api/visitors?page={page}&pageSize={pageSize}");
    }

    public Task<ServiceResult<IList<VisitorDto>>> SearchAsync(int tenantId, string term)
    {
        return GetAsync<IList<VisitorDto>>($"api/visitors/search?term={term}");
    }

    public Task<ServiceResult<VisitorDto>> GetByIdAsync(int tenantId, int visitorId)
    {
        return GetAsync<VisitorDto>($"api/visitors/{visitorId}");
    }

    public Task<ServiceResult<VisitorDto>> RegisterAsync(int tenantId, VisitorCreateDto dto)
    {
        return PostAsync<VisitorDto>("api/visitors/register", dto);
    }

    public Task<ServiceResult<VisitorRatingDto>> SubmitRatingAsync(int tenantId, int visitorId, int exhibitionId, int rating, string? comment)
    {
        var request = new SubmitRatingRequest(exhibitionId, rating, comment);
        return PostAsync<VisitorRatingDto>($"api/visitors/{visitorId}/ratings", request);
    }

    public Task<ServiceResult<VisitorRatingSummaryDto>> GetRatingSummaryAsync(int tenantId, int exhibitionId)
    {
        return GetAsync<VisitorRatingSummaryDto>($"api/visitors/ratings/exhibition/{exhibitionId}");
    }
}

public record SubmitRatingRequest(int ExhibitionId, int Rating, string? Comment);
