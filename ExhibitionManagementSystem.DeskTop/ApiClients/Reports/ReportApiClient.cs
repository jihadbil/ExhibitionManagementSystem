using System.Net.Http;
using System.Threading.Tasks;
using ExhibitionManagementSystem.Models.DTOs.Financial;
using ExhibitionManagementSystem.Services.Common;
using ExhibitionManagementSystem.Services.Interfaces;
using ExhibitionManagementSystem.DeskTop.ApiClients.Base;
using ExhibitionManagementSystem.DeskTop.Services.Session;

namespace ExhibitionManagementSystem.DeskTop.ApiClients.Reports;

public class ReportApiClient : ApiClientBase, IReportService
{
    public ReportApiClient(IHttpClientFactory httpClientFactory, SessionService session)
        : base(httpClientFactory, session)
    {
    }

    public Task<ServiceResult<FinancialReportDto>> GenerateExhibitionReportAsync(int tenantId, int exhibitionId, string userId)
    {
        return PostAsync<FinancialReportDto>($"api/reports/exhibitions/{exhibitionId}", new { });
    }

    public Task<ServiceResult<FinancialReportDto>> GetReportByIdAsync(int tenantId, int reportId)
    {
        return GetAsync<FinancialReportDto>($"api/reports/{reportId}");
    }
}
