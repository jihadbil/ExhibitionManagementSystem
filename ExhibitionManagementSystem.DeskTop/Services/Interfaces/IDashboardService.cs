using System.Collections.Generic;
using System.Threading.Tasks;
using ExhibitionManagementSystem.Models.DTOs.Dashboard;
using ExhibitionManagementSystem.Services.Common;

namespace ExhibitionManagementSystem.Services.Interfaces;

public interface IDashboardService
{
    Task<ServiceResult<DashboardStatsDto>> GetStatsAsync();
    Task<ServiceResult<IList<RevenueChartPointDto>>> GetRevenueChartAsync(string period = "6m");
    Task<ServiceResult<IList<ActivityItemDto>>> GetRecentActivityAsync(int limit = 8);
}
