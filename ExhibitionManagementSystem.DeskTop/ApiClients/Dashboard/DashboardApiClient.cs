using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using ExhibitionManagementSystem.Models.DTOs.Dashboard;
using ExhibitionManagementSystem.Services.Common;
using ExhibitionManagementSystem.Services.Interfaces;
using ExhibitionManagementSystem.DeskTop.ApiClients.Base;
using ExhibitionManagementSystem.DeskTop.Services.Session;

namespace ExhibitionManagementSystem.DeskTop.ApiClients.Dashboard;

public class DashboardApiClient : ApiClientBase, IDashboardService
{
    public DashboardApiClient(IHttpClientFactory httpClientFactory, SessionService session)
        : base(httpClientFactory, session)
    {
    }

    public Task<ServiceResult<DashboardStatsDto>> GetStatsAsync()
    {
        return GetAsync<DashboardStatsDto>("api/dashboard/stats");
    }

    public Task<ServiceResult<IList<RevenueChartPointDto>>> GetRevenueChartAsync(string period = "6m")
    {
        return GetAsync<IList<RevenueChartPointDto>>($"api/dashboard/revenue-chart?period={period}");
    }

    public Task<ServiceResult<IList<ActivityItemDto>>> GetRecentActivityAsync(int limit = 8)
    {
        return GetAsync<IList<ActivityItemDto>>($"api/dashboard/recent-activity?limit={limit}");
    }
}
